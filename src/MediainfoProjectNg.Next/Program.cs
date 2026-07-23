using System.Globalization;
using Avalonia;

namespace MediainfoProjectNg.Next;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // OG MainWindow sets xml:lang="zh-CN" — pin UI culture for parity.
        var zh = CultureInfo.GetCultureInfo("zh-CN");
        CultureInfo.DefaultThreadCurrentCulture = zh;
        CultureInfo.DefaultThreadCurrentUICulture = zh;
        CultureInfo.CurrentCulture = zh;
        CultureInfo.CurrentUICulture = zh;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
