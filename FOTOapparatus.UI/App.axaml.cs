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
            var mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;

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
                            Dispatcher.UIThread.Post(() => mainWindow.ShowFromExternalRequest());
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            window.ShowFromExternalRequest();
        }
    }

    private void ShowTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            window.ShowFromExternalRequest();
        }
    }

    private async void ExitTrayMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            await window.ExitApplicationAsync();
        }
    }
}
