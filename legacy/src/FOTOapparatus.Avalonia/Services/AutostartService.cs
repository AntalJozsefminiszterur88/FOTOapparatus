using System.Reflection;

namespace FOTOapparatus.Avalonia.Services;

public sealed class AutostartService
{
    private const string DesktopFileName = "fotoapparatus.desktop";

    public bool IsSupported => OperatingSystem.IsLinux();

    public bool IsEnabled()
    {
        if (!IsSupported)
        {
            return false;
        }

        return File.Exists(GetDesktopFilePath());
    }

    public bool SetEnabled(bool enabled)
    {
        if (!IsSupported)
        {
            return false;
        }

        var path = GetDesktopFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!enabled)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }

        File.WriteAllText(path, BuildDesktopFileContent());
        return true;
    }

    private static string GetDesktopFilePath()
    {
        var configDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config");
        }

        return Path.Combine(configDirectory, "autostart", DesktopFileName);
    }

    private static string BuildDesktopFileContent()
    {
        var workingDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var execCommand = BuildExecCommand();

        return $"""
                [Desktop Entry]
                Type=Application
                Version=1.0
                Name=FOTO-Apparátus
                Comment=Ütemezett képernyőkép-készítő Linuxra
                Exec={execCommand}
                Path={workingDirectory}
                Terminal=false
                StartupNotify=false
                X-GNOME-Autostart-enabled=true
                """;
    }

    private static string BuildExecCommand()
    {
        var processPath = Environment.ProcessPath ?? string.Empty;
        var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location ?? string.Empty;

        if (Path.GetFileName(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(entryAssemblyPath))
        {
            return $"dotnet {Quote(entryAssemblyPath)} --start-hidden";
        }

        return $"{Quote(processPath)} --start-hidden";
    }

    private static string Quote(string value)
        => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
