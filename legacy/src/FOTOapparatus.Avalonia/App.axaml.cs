using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FOTOapparatus.Avalonia.Services;
using FOTOapparatus.Avalonia.Windows;

namespace FOTOapparatus.Avalonia;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    internal static string[] LaunchArguments { get; set; } = [];

    internal static SingleInstanceService? SingleInstanceService { get; set; }

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configService = new ConfigService();
            var settings = configService.Load();
            var windowDiscoveryService = new WindowDiscoveryService();
            var hotkeyService = new X11HotkeyService();
            var idleMonitorService = new IdleMonitorService();
            var captureService = new CaptureService(windowDiscoveryService, hotkeyService);
            var schedulerService = new SchedulerService(captureService, idleMonitorService);
            var autostartService = new AutostartService();
            var startHidden = LaunchArguments.Contains("--start-hidden", StringComparer.OrdinalIgnoreCase);

            _mainWindow = new MainWindow(
                configService,
                captureService,
                windowDiscoveryService,
                autostartService,
                schedulerService,
                settings,
                startHidden);

            desktop.MainWindow = _mainWindow;
            desktop.Exit += (_, _) => _mainWindow.Dispose();

            SingleInstanceService?.StartListening(() =>
                Dispatcher.UIThread.Post(() => _mainWindow.ShowFromExternalRequest()));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
        => _mainWindow?.ShowFromExternalRequest();

    private void ShowTrayMenuItem_OnClick(object? sender, EventArgs e)
        => _mainWindow?.ShowFromExternalRequest();

    private async void ExitTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (_mainWindow is not null)
        {
            await _mainWindow.ExitApplicationAsync();
        }
    }
}
