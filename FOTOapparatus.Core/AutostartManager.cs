namespace FOTOapparatus.Core;

public static class AutostartManager
{
    private static readonly string AutostartDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autostart");

    private static readonly string DesktopFile = Path.Combine(AutostartDir, "fotoapparatus.desktop");

    public static bool IsEnabled() => File.Exists(DesktopFile);

    public static bool IsSupported => OperatingSystem.IsLinux();

    public static bool SetEnabled(bool enabled)
    {
        try
        {
            var executablePath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty;
            if (enabled)
            {
                Directory.CreateDirectory(AutostartDir);
                var content = $"""
                               [Desktop Entry]
                               Type=Application
                               Name=FOTO-Apparatus
                               Comment=Automatikus képkészítő alkalmazás
                               Exec="{executablePath}" --hidden
                               Terminal=false
                               Categories=Utility;
                               """;
                File.WriteAllText(DesktopFile, content);
            }
            else
            {
                if (File.Exists(DesktopFile))
                {
                    File.Delete(DesktopFile);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
