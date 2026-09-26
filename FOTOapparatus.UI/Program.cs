using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace FOTOapparatus.UI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var instanceLock = TryAcquireInstanceLock();
        if (instanceLock is null)
        {
            if (args.Contains("--hidden"))
            {
                Console.WriteLine("Az alkalmazás már fut a háttérben.");
            }
            else if (SingleInstanceService.TryNotifyExistingInstance())
            {
                Console.WriteLine("Az alkalmazás már fut. Értesítés a meglévő példánynak...");
            }
            else
            {
                Console.Error.WriteLine("Az alkalmazás már fut, de pillanatnyilag nem fogad megnyitási kérést.");
            }

            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    private static FileStream? TryAcquireInstanceLock()
    {
        var configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FOTOapparatus");
        Directory.CreateDirectory(configDirectory);

        try
        {
            return new FileStream(
                Path.Combine(configDirectory, "instance.lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                // This is a mostly background application. Software rendering avoids
                // keeping the Mesa/LLVM graphics stack resident for a rarely shown UI.
                RenderingMode = [X11RenderingMode.Software],
            })
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace(Avalonia.Logging.LogEventLevel.Warning);
}
