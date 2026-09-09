using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FOTOapparatus.Core;
using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.LinuxServices;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.IO.Pipes;
using System.Reflection;

namespace FOTOapparatus.UI;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Register Core & Linux Services
        services.AddSingleton<IWindowService, LinuxWindowService>();
        services.AddSingleton<IHotkeyService, LinuxHotkeyService>();
        services.AddSingleton<IScreenshotService, LinuxScreenshotService>();
        services.AddSingleton<IIdleService, LinuxIdleService>();

        // We need to load initial settings, wait on a task synchronously without deadlocking
        var settings = Task.Run(() => ConfigManager.LoadAsync()).GetAwaiter().GetResult();
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

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = Services.GetRequiredService<MainWindow>();

            if (_mainWindow.StartHidden)
            {
                // Do not assign a hidden-start window to MainWindow here. The desktop
                // lifetime would map it first and Hide() could only run afterwards,
                // leaving a transparent X11 frame behind on Cinnamon.
                Dispatcher.UIThread.Post(async () =>
                    await _mainWindow.InitializeForStartupAsync());
            }
            else
            {
                desktop.MainWindow = _mainWindow;
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream("FOTOapparatusPipe_Linux", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                        await server.WaitForConnectionAsync();
                        using var reader = new StreamReader(server);
                        var message = await reader.ReadLineAsync();
                        if (message == "SHOW")
                        {
                            Dispatcher.UIThread.Post(() => _mainWindow.ShowFromExternalRequest());
                        }
                    }
                    catch
                    {
                        await Task.Delay(1000); // Prevent tight loop on permanent failure
                    }
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        _mainWindow?.ShowFromExternalRequest();
    }

    private void ShowTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        _mainWindow?.ShowFromExternalRequest();
    }

    private async void ExitTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (_mainWindow is not null)
        {
            await _mainWindow.ExitApplicationAsync();
        }
    }
}
