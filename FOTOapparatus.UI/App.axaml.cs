using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FOTOapparatus.Core;
using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.LinuxServices;
using Microsoft.Extensions.DependencyInjection;

namespace FOTOapparatus.UI;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private SchedulerService? _schedulerService;
    private SingleInstanceService? _singleInstanceService;

    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Register Core & Linux Services
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IWindowService, LinuxWindowService>();
        services.AddSingleton<IHotkeyService, LinuxHotkeyService>();
        services.AddSingleton<IScreenshotImageProcessor, ScreenshotImageProcessor>();
        services.AddSingleton<IScreenshotService, LinuxScreenshotService>();
        services.AddSingleton<IIdleService, LinuxIdleService>();

        var settingsStore = JsonSettingsStore.CreateDefault(
            ex => Console.Error.WriteLine($"Nem sikerült betölteni a konfigurációt: {ex.Message}"));
        var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
        services.AddSingleton<ISettingsStore>(settingsStore);
        services.AddSingleton(settings);

        services.AddSingleton<SchedulerService>(sp =>
        {
            var screenshot = sp.GetRequiredService<IScreenshotService>();
            var idle = sp.GetRequiredService<IIdleService>();
            var appSettings = sp.GetRequiredService<FOTOapparatus.Core.Models.AppSettings>();
            return new SchedulerService(screenshot, idle, appSettings, msg => Console.WriteLine(msg));
        });

        // Register Window
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();
        _schedulerService = Services.GetRequiredService<SchedulerService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startHidden = Environment.GetCommandLineArgs().Contains("--hidden");
            if (startHidden)
            {
                // Keep background startup lightweight. The complete settings window
                // is constructed only when the user opens it from the tray or pipe.
                _schedulerService.Start();
            }
            else
            {
                _mainWindow = Services.GetRequiredService<MainWindow>();
                desktop.MainWindow = _mainWindow;
            }

            _singleInstanceService = new SingleInstanceService(
                () => Dispatcher.UIThread.Post(ShowMainWindow),
                ex => Console.Error.WriteLine($"Egy példányos kommunikációs hiba: {ex.Message}"));
            _singleInstanceService.Start();

            desktop.Exit += (_, _) =>
            {
                _singleInstanceService.Dispose();
                _schedulerService.StopAsync().GetAwaiter().GetResult();
                (Services as IDisposable)?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private async void ExitTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (_mainWindow is not null)
        {
            await _mainWindow.ExitApplicationAsync();
            return;
        }

        if (_schedulerService is not null)
        {
            await _schedulerService.StopAsync();
        }
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void ShowMainWindow()
    {
        if (Services is null)
        {
            return;
        }

        _mainWindow ??= Services.GetRequiredService<MainWindow>();
        _mainWindow.ShowFromExternalRequest();
    }
}
