# Builds a Store-ready MSIX package for TodoApp.
#
#   .\New-Package.ps1                 # Release / win-x64 / self-contained
#   .\New-Package.ps1 -FrameworkDependent
#   .\New-Package.ps1 -DevSign        # also signs with a local dev certificate
#
# Output: Packaging\out\TodoApp_<version>_x64.msix

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",

    [switch]$FrameworkDependent,

    [switch]$DevSign
)

$ErrorActionPreference = "Stop"

$here = $PSScriptRoot
$root = Split-Path -Parent $here
$outDir = Join-Path $here "out"
$staging = Join-Path $outDir "staging"

# ---------- Windows SDK tools (makeappx / signtool), fetched from NuGet on first run ----------

$toolsDir = Join-Path $env:LOCALAPPDATA "TodoAppPackaging"

function Find-Tool([string]$name) {
    if (-not (Test-Path $toolsDir)) { return $null }
    return Get-ChildItem $toolsDir -Recurse -Filter $name -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } |
        Select-Object -First 1
}

$makeappx = Find-Tool "makeappx.exe"
if (-not $makeappx) {
    $sdkVer = "10.0.28000.2705"
    $nupkg = Join-Path $env:TEMP "win-sdk-buildtools.zip"
    Write-Host "Downloading Windows SDK build tools ($sdkVer)..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -UseBasicParsing `
        "https://api.nuget.org/v3-flatcontainer/microsoft.windows.sdk.buildtools/$sdkVer/microsoft.windows.sdk.buildtools.$sdkVer.nupkg" `
        -OutFile $nupkg
    $extractDir = Join-Path $toolsDir "extracted"
    if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
    Expand-Archive -Path $nupkg -DestinationPath $extractDir -Force
    $makeappx = Find-Tool "makeappx.exe"
    if (-not $makeappx) { throw "makeappx.exe not found after downloading the SDK tools." }
}
$signtool = Find-Tool "signtool.exe"

# ---------- publish ----------

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
$pubArgs = @(
    "publish", (Join-Path $root "TodoApp\TodoApp.csproj"),
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", $selfContained,
    "-o", $staging
)
Write-Host "Publishing ($Configuration / $Runtime / self-contained=$selfContained)..."
& dotnet $pubArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

if (-not (Test-Path (Join-Path $staging "TodoApp.exe"))) { throw "TodoApp.exe missing from publish output." }

Copy-Item (Join-Path $here "AppxManifest.xml") (Join-Path $staging "AppxManifest.xml") -Force

# ---------- package assets (generated from the brand: violet rounded square + check) ----------

Add-Type -AssemblyName System.Drawing

function New-MasterLogo([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = "AntiAlias"
    $g.PixelOffsetMode = "HighQuality"
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = [float]$size
    $r = [int]($s * 0.19)
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $d = $r * 2

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $c1 = [System.Drawing.Color]::FromArgb(255, 124, 111, 246)
    $c2 = [System.Drawing.Color]::FromArgb(255, 86, 140, 248)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 45)
    $g.FillPath($brush, $path)

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, ($s * 0.105))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $p1 = New-Object System.Drawing.PointF(($s * 0.27), ($s * 0.52))
    $p2 = New-Object System.Drawing.PointF(($s * 0.43), ($s * 0.68))
    $p3 = New-Object System.Drawing.PointF(($s * 0.74), ($s * 0.33))
    $g.DrawLines($pen, [System.Drawing.PointF[]]@($p1, $p2, $p3))

    $g.Dispose()
    return $bmp
}

function Save-Square([System.Drawing.Image]$src, [int]$side, [string]$path) {
    $bmp = New-Object System.Drawing.Bitmap($side, $side)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = "HighQuality"
    $g.PixelOffsetMode = "HighQuality"
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $side, $side)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Save-OnCanvas([System.Drawing.Image]$logo, [int]$w, [int]$h, [int]$logoSide, [string]$path) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = "HighQuality"
    $g.PixelOffsetMode = "HighQuality"
    $g.Clear([System.Drawing.Color]::FromArgb(255, 25, 26, 35))
    $x = [int](($w - $logoSide) / 2)
    $y = [int](($h - $logoSide) / 2)
    $g.DrawImage($logo, $x, $y, $logoSide, $logoSide)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$assetsOut = Join-Path $staging "Assets"
New-Item -ItemType Directory -Force -Path $assetsOut | Out-Null

$master = New-MasterLogo 512
try {
    Save-Square $master 50  (Join-Path $assetsOut "StoreLogo.png")
    Save-Square $master 44  (Join-Path $assetsOut "Square44x44Logo.png")
    Save-Square $master 150 (Join-Path $assetsOut "Square150x150Logo.png")
    Save-Square $master 310 (Join-Path $assetsOut "Square310x310Logo.png")
    Save-OnCanvas $master 310 150 108 (Join-Path $assetsOut "Wide310x150Logo.png")
    Save-OnCanvas $master 620 300 120 (Join-Path $assetsOut "SplashScreen.png")
}
finally {
    $master.Dispose()
}

# ---------- pack ----------

$manifest = Get-Content (Join-Path $staging "AppxManifest.xml") -Raw
if (-not ($manifest -cmatch 'Version="([\d\.]+)"')) { throw "Version not found in AppxManifest.xml" }
$version = $Matches[1]

$rid = $Runtime -replace "-", ""
$pkgPath = Join-Path $outDir ("TodoApp_{0}_{1}.msix" -f $version, $rid)

Write-Host "Packing $pkgPath ..."
& $makeappx.FullName @("pack", "/d", $staging, "/p", $pkgPath, "/o")
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed." }

# ---------- optional local dev signature ----------

if ($DevSign) {
    if (-not $signtool) { throw "signtool.exe not found." }

    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=Eng Mohamed Nowar" `
        -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable `
        -FriendlyName "TodoApp MSIX Dev Cert" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

    $cerPath = Join-Path $outDir "TodoAppDevCert.cer"
    Export-Certificate -FilePath $cerPath -Cert $cert -Force | Out-Null

    & $signtool.FullName @("sign", "/fd", "SHA256", "/sha1", $cert.Thumbprint, "/s", "My", $pkgPath)
    if ($LASTEXITCODE -ne 0) { throw "signtool failed." }

    $trusted = $false
    try {
        Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
        $trusted = $true
    } catch {
        Write-Warning "Could not add the cert to LocalMachine\TrustedPeople (admin required). Run PowerShell as Administrator and re-run with -DevSign, or import '$cerPath' manually."
    }

    Write-Host "Signed dev package (trusted=$trusted)."
}

# ---------- summary ----------

$sizeMb = (Get-Item $pkgPath).Length / 1MB
Write-Host ""
Write-Host ("Package: {0}  ({1:N1} MB)" -f $pkgPath, $sizeMb)
Write-Host ""
Write-Host "Store submission:"
Write-Host "  1. Reserve your app name in Partner Center."
Write-Host "  2. Set Identity/Name and Identity/Publisher in Packaging\AppxManifest.xml to match Partner Center."
Write-Host "  3. Re-run this script and upload the .msix in the submission's Packages step."
Write-Host "Local test install (Developer Mode ON, admin PowerShell):"
Write-Host "  Add-AppxPackage -Path `"$pkgPath`""
