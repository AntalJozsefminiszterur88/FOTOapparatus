using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core;

public static class AppSettingsDefaults
{
    private const string DefaultSaveFolderName = "FOTOapp_Screenshots";

    public static AppSettings Create()
        => new()
        {
            SavePath = GetDefaultSavePath(),
        };

    public static bool NormalizeInPlace(AppSettings settings)
    {
        var changed = false;

        var normalizedSavePath = NormalizeSavePath(settings.SavePath);
        if (!string.Equals(settings.SavePath, normalizedSavePath, StringComparison.Ordinal))
        {
            settings.SavePath = normalizedSavePath;
            changed = true;
        }

        if (settings.CustomArea is null)
        {
            settings.CustomArea = new RectSettings();
            changed = true;
        }

        if (settings.DiscordSettings is null)
        {
            settings.DiscordSettings = new DiscordSettings();
            changed = true;
        }

        if (settings.Schedules is null)
        {
            settings.Schedules = [];
            changed = true;
        }

        var idleThresholdMinutes = Math.Clamp(settings.IdleThresholdMinutes, 1, 120);
        if (settings.IdleThresholdMinutes != idleThresholdMinutes)
        {
            settings.IdleThresholdMinutes = idleThresholdMinutes;
            changed = true;
        }

        return changed;
    }

    public static string NormalizeSavePath(string? savePath)
    {
        var trimmedPath = savePath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedPath) || LooksLikeWindowsPath(trimmedPath))
        {
            return GetDefaultSavePath();
        }

        return trimmedPath;
    }

    public static string GetDefaultSavePath()
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (!string.IsNullOrWhiteSpace(picturesPath))
        {
            return Path.Combine(picturesPath, DefaultSaveFolderName);
        }

        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(homePath))
        {
            return Path.Combine(homePath, "Pictures", DefaultSaveFolderName);
        }

        return DefaultSaveFolderName;
    }

    private static bool LooksLikeWindowsPath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true;
        }

        return path.Length >= 3
               && char.IsLetter(path[0])
               && path[1] == ':'
               && (path[2] == '/' || path[2] == '\\');
    }
}
