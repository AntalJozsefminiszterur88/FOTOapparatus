using Avalonia;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace FOTOapparatus.UI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", "FOTOapparatusPipe_Linux", PipeDirection.Out);
            client.Connect(100);
            using var writer = new System.IO.StreamWriter(client);
            writer.WriteLine("SHOW");
            writer.Flush();
            Console.WriteLine("Az alkalmazás már fut. Értesítés a meglévő példánynak...");
            return; // Successfully notified existing instance
        }
        catch (Exception)
        {
            // Could not connect, which means no instance is running or it crashed.
            // Continue starting the app.
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
