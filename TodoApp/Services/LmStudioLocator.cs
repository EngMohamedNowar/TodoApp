using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TodoApp.Services
{
    /// <summary>
    /// Finds and launches the LM Studio desktop app so the local AI server can be
    /// brought up from inside this application instead of by hand.
    /// </summary>
    public static class LmStudioLocator
    {
        /// <summary>Launches LM Studio; returns the path that was started, or null when it is not installed.</summary>
        public static string? Start()
        {
            foreach (var candidate in CandidatePaths())
            {
                if (!File.Exists(candidate)) continue;

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = candidate,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(candidate) ?? string.Empty
                    });
                    return candidate;
                }
                catch
                {
                    // try the next known location
                }
            }

            return null;
        }

        private static IEnumerable<string> CandidatePaths()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var startMenu = Path.Combine("Microsoft", "Windows", "Start Menu", "Programs");

            yield return Path.Combine(appData, startMenu, "LM Studio.lnk");
            yield return Path.Combine(common, startMenu, "LM Studio.lnk");
            yield return Path.Combine(local, "Programs", "LM Studio", "LM Studio.exe");
            yield return Path.Combine(programFiles, "LM Studio", "LM Studio.exe");
            yield return Path.Combine(programFilesX86, "LM Studio", "LM Studio.exe");
        }
    }
}
