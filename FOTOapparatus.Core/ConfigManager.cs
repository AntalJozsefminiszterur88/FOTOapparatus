using System.Text.Json;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core;

public static class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FOTOapparatus");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(ConfigFile))
        {
            return AppSettingsDefaults.Create();
        }

        try
        {
            await using var stream = File.OpenRead(ConfigFile);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options);
            settings ??= AppSettingsDefaults.Create();

            if (AppSettingsDefaults.NormalizeInPlace(settings))
            {
                await SaveAsync(settings);
            }

            return settings;
        }
        catch
        {
            return AppSettingsDefaults.Create();
        }
    }

    public static async Task SaveAsync(AppSettings settings)
    {
        AppSettingsDefaults.NormalizeInPlace(settings);
        Directory.CreateDirectory(ConfigDir);
        await using var stream = File.Create(ConfigFile);
        await JsonSerializer.SerializeAsync(stream, settings, Options);
    }
}
