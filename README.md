# My Tasks — تطبيق Todo List بـ WPF و SQLite

تطبيق Desktop كامل لإدارة المهام، مبني بـ **WPF + C# (.NET 8)** وبيستخدم **SQLite** كقاعدة بيانات محلية عن طريق **Entity Framework Core**. البيانات بتتخزن فعليًا على الجهاز في ملف `.db` مش في الذاكرة، يعني لو قفلت البرنامج وفتحته تاني هتلاقي كل المهام موجودة.

## المميزات

- إضافة / تعديل / حذف مهام
- تحديد المهمة كمكتملة (checkbox) بحفظ فوري في الداتابيز
- Priority (Low / Medium / High) بلون مميز لكل واحدة
- تصنيفات (Categories) قابلة للفلترة من الـ sidebar
- بحث فوري في العنوان والوصف
- فلاتر: الكل / النشطة / المكتملة
- تاريخ استحقاق (Due Date) مع تمييز المهام المتأخرة باللون الأحمر
- **Dark theme احترافي بالكامل**: خلفية داكنة، بطاقات، أزرار، TextBox/ComboBox/DatePicker/CheckBox كلهم متصممين خصيصًا ليتماشوا مع الثيم (مفيش أي عنصر أبيض غريب وسط الواجهة)
- **Logo مخصص** للتطبيق (أيقونة .ico) ظاهر في الـ exe نفسه، وفي الـ taskbar، وفي شريط عنوان النوافذ
- تاريخ الاستحقاق بيتحدد تلقائيًا على تاريخ النهاردة عند إضافة مهمة جديدة (تقدر تغيره بسهولة)
- **شريط تقدم (Progress Bar)** في الـ sidebar بيوضح نسبة المهام المنجزة % لحظيًا، بيتحدث تلقائي مع أي إضافة/حذف/تحديد إنجاز
- **Pomodoro Timer** (زرار "⏱ Focus Timer" في الـ sidebar): تايمر منفصل بيفتح في نافذة خاصة بيه، يشتغل بالتقنية الكلاسيكية بالظبط:
  - 25 دقيقة تركيز (Focus Session)
  - 5 دقائق راحة قصيرة بعد كل جلسة تركيز
  - 15 دقيقة راحة طويلة تلقائيًا بعد كل 4 جلسات تركيز متتالية
  - صوت تنبيه لما الجلسة تخلص + تبديل تلقائي للمرحلة التالية
  - أزرار Start/Pause, Reset, Skip + عداد لعدد الجلسات المنجزة
  - النافذة بتفضل شغالة في الخلفية وانت بتستخدم قائمة المهام عادي (مش modal)
- **Animations وسلاسة في كل التفاعلات**:
  - كل الأزرار بتعمل "scale-down" خفيف عند الضغط وترجع بـ spring-back ناعم (BackEase) بدل التغيير المفاجئ
  - تأثير hover بيظهر تدريجيًا (fade) بدل ما يتبدل فجأة
  - الـ checkbox بتاع إكمال المهمة بيعمل "pop" (تكبير مرتد) لما تحددها
  - شريط التقدم (progress bar) بيتحرك بسلاسة لما النسبة تتغير بدل ما يقفز فجأة
  - بطاقات المهام بتظهر بحركة fade + slide-up لطيفة
  - النوافذ كلها بتفتح بـ fade-in ناعم
- **إعادة ترتيب المهام بالسحب (Drag & Drop)**: كل بطاقة مهمة ليها مقبض (⠿) على اليسار — اسحبها لفوق أو تحت عشان ترتب المهام زي ما انت عايز، والترتيب بيتحفظ تلقائي في قاعدة البيانات
- **إحصائيات وسجل كامل للـ Pomodoro** (أيقونة 📊 جوه نافذة التايمر):
  - كروت ملخص: ساعات التركيز النهاردة، عدد الأيام اللي استخدمت فيها التطبيق، وعدد أيام الاستمرارية (streak)
  - رسم بياني أسبوعي (Bar Chart) لساعات التركيز يوم بيوم، مع أسهم للتنقل بين الأسابيع
  - تبويب History بيعرض سجل كل جلسة تركيز (تاريخ، وقت البداية والنهاية، المدة)، مع زرار "Clear History" لمسح السجل بالكامل لو حبيت
  - العدادات اليومية (زي "sessions completed today") بتتصفر تلقائيًا كل يوم جديد، لكن السجل التاريخي فاضل محفوظ لحد ما تمسحه بنفسك
- **تعديل مدة التايمر** (أيقونة ⚙ جوه نافذة التايمر): تقدر تغيّر مدة جلسة التركيز، الراحة القصيرة، الراحة الطويلة، وعدد الجلسات قبل الراحة الطويلة — والقيم دي بتتحفظ وتفضل زي ما ظبطتها في المرات الجاية
- **جرس تنبيه** بيشتغل تلقائي في نهاية أي جلسة (سواء تركيز أو راحة) عشان تعرف إن الوقت خلص

## هيكل المشروع (Clean-ish MVVM)

```
TodoApp/
├── Models/          → TodoItem, FocusSession, PomodoroSettingsEntity
├── Data/            → TodoDbContext.cs (اتصال EF Core بـ SQLite + إدارة الـ schema)
├── ViewModels/      → MainViewModel, TodoItemViewModel, PomodoroViewModel, FocusStatsViewModel, RelayCommand, ViewModelBase
├── Views/           → AddEditTodoWindow, PomodoroWindow, PomodoroSettingsWindow, FocusStatsWindow
├── Converters/      → Converters للـ XAML bindings
├── Behaviors/        → SmoothAnimation.cs (تحريك سلس للعرض/الارتفاع)
├── Assets/          → app.ico (لوجو التطبيق)
├── MainWindow.xaml  → الواجهة الرئيسية
└── App.xaml         → الـ Dark theme والـ styles العامة لكل الكنترولز
```

هيكل زي اللي انت متعود عليه في مشاريعك (Repository-free بس MVVM كامل، سهل تضيف عليه Repository/UnitOfWork لو حبيت توسعه).

## متطلبات التشغيل

1. **Windows** (لازم، لأن WPF مش بيشتغل على Linux/Mac)
2. **.NET 8 SDK** — تحمله من https://dotnet.microsoft.com/download/dotnet/8.0
3. **Visual Studio 2022** (أي إصدار حتى Community) أو أي محرر تاني بيدعم .NET

## طريقة التشغيل

### باستخدام Visual Studio
1. افتح ملف `TodoApp.sln`
2. اضغط `F5` أو زرار Start — Visual Studio هيعمل restore للـ NuGet packages أوتوماتيك (`Microsoft.EntityFrameworkCore.Sqlite`)

### باستخدام سطر الأوامر
```bash
cd TodoApp
dotnet restore
dotnet run
```

## فين بتتخزن البيانات؟

قاعدة البيانات بتتعمل أوتوماتيك أول ما تشغل البرنامج في:
```
%AppData%\TodoApp\todo.db
```
مفيش حاجة تعملها يدوي — `Database.EnsureCreated()` بتعمل الجدول لوحدها أول مرة.

## أفكار للتوسيع لاحقًا

- إضافة Repository/UnitOfWork pattern زي مشروع الـ GymManagementSystem بتاعك
- Sub-tasks / checklists جوه كل مهمة
- Notifications/Reminders للمهام القريبة من الاستحقاق
- Export/Import (CSV أو JSON) للنسخ الاحتياطي
- Dark mode
- ربط المشروع بـ migrations بدل EnsureCreated لو هتضيف حقول جديدة مستقبلًا
