using System.Text.Json;
using FOTOapparatus.Avalonia.Models;

namespace FOTOapparatus.Avalonia.Services;

public sealed class ConfigService
{
    private const string CompanyName = "UMKGL Solutions";
    private const string AppFolderName = "FOTOapp";
    private const string ConfigFileName = "fotoapp_config.json";
    private const string ScreenshotFolderName = "FOTOapp_Screenshots";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
    };

    public string ConfigPath => Path.Combine(GetConfigDirectory(), ConfigFileName);

    public AppSettings CreateDefaultSettings()
        => new()
        {
            SavePath = GetDefaultSavePath(),
            CaptureType = CaptureTypes.Screenshot,
            ScreenshotMode = ScreenshotModes.Fullscreen,
            CustomArea = new RectSettings { X = 0, Y = 0, Width = 100, Height = 100 },
            Schedules = [],
            TargetWindow = string.Empty,
            IncludeTimestamp = true,
            TimestampPosition = TimestampPositions.TopLeft,
            IdleCheckEnabled = false,
            IdleThresholdMinutes = 5,
            DiscordSettings = new DiscordSettings
            {
                StayForeground = false,
                UseHotkey = false,
                HotkeyNumber = 1,
                WindowTitle = string.Empty,
                DelayAfterHotkey = 2.0,
            },
        };

    public AppSettings Load()
    {
        var defaults = CreateDefaultSettings();
        if (!File.Exists(ConfigPath))
        {
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions);
            return MergeWithDefaults(loaded, defaults);
        }
        catch
        {
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(GetConfigDirectory());
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(ConfigPath, json);
    }

    private static AppSettings MergeWithDefaults(AppSettings? loaded, AppSettings defaults)
    {
        if (loaded is null)
        {
            return defaults;
        }

        loaded.SavePath = string.IsNullOrWhiteSpace(loaded.SavePath) ? defaults.SavePath : loaded.SavePath;
        loaded.CaptureType = string.IsNullOrWhiteSpace(loaded.CaptureType) ? defaults.CaptureType : loaded.CaptureType;
        loaded.ScreenshotMode = string.IsNullOrWhiteSpace(loaded.ScreenshotMode) ? defaults.ScreenshotMode : loaded.ScreenshotMode;
        loaded.CustomArea ??= defaults.CustomArea.Clone();
        loaded.Schedules ??= [];
        loaded.TargetWindow ??= string.Empty;
        loaded.TimestampPosition = string.IsNullOrWhiteSpace(loaded.TimestampPosition) ? defaults.TimestampPosition : loaded.TimestampPosition;
        loaded.IdleThresholdMinutes = loaded.IdleThresholdMinutes <= 0 ? defaults.IdleThresholdMinutes : loaded.IdleThresholdMinutes;
        loaded.DiscordSettings ??= defaults.DiscordSettings.Clone();
        loaded.DiscordSettings.WindowTitle ??= string.Empty;
        loaded.DiscordSettings.HotkeyNumber = Math.Clamp(loaded.DiscordSettings.HotkeyNumber, 0, 9);
        loaded.DiscordSettings.DelayAfterHotkey = loaded.DiscordSettings.DelayAfterHotkey <= 0
            ? defaults.DiscordSettings.DelayAfterHotkey
            : loaded.DiscordSettings.DelayAfterHotkey;

        return loaded;
    }

    private static string GetConfigDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
        {
            documents = AppContext.BaseDirectory;
        }

        return Path.Combine(documents, CompanyName, AppFolderName);
    }

    private static string GetDefaultSavePath()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures))
        {
            pictures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures");
        }

        return Path.Combine(pictures, ScreenshotFolderName);
    }
}
