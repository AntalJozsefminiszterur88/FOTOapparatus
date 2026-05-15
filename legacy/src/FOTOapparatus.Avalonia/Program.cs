using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FOTOapparatus.Avalonia.Services;

namespace FOTOapparatus.Avalonia;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        using var singleInstanceService = new SingleInstanceService("FOTOapparatusAvalonia");
        if (!singleInstanceService.IsPrimaryInstance)
        {
            singleInstanceService.NotifyPrimaryInstance();
            return 0;
        }

        App.LaunchArguments = args;
        App.SingleInstanceService = singleInstanceService;

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
