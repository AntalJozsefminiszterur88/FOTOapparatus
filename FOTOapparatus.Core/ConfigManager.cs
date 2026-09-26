using System.Text.Json;
using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _configFile;
    private readonly Action<Exception>? _onLoadError;

    public JsonSettingsStore(string configFile, Action<Exception>? onLoadError = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configFile);
        _configFile = Path.GetFullPath(configFile);
        _onLoadError = onLoadError;
    }

    public static JsonSettingsStore CreateDefault(Action<Exception>? onLoadError = null)
    {
        var configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FOTOapparatus");
        return new JsonSettingsStore(Path.Combine(configDirectory, "settings.json"), onLoadError);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configFile))
        {
            return AppSettingsDefaults.Create();
        }

        try
        {
            await using var stream = File.OpenRead(_configFile);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                Options,
                cancellationToken);
            settings ??= AppSettingsDefaults.Create();

            if (AppSettingsDefaults.NormalizeInPlace(settings))
            {
                await SaveAsync(settings, cancellationToken);
            }

            return settings;
        }
        catch (Exception ex) when (ex is IOException
                                   or UnauthorizedAccessException
                                   or JsonException
                                   or NotSupportedException)
        {
            _onLoadError?.Invoke(ex);
            return AppSettingsDefaults.Create();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        AppSettingsDefaults.NormalizeInPlace(settings);

        var configDirectory = Path.GetDirectoryName(_configFile)
                              ?? throw new InvalidOperationException("A konfigurációs fájlnak nincs szülőmappája.");
        Directory.CreateDirectory(configDirectory);

        var temporaryFile = Path.Combine(
            configDirectory,
            $".{Path.GetFileName(_configFile)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                             temporaryFile,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 16 * 1024,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, settings, Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryFile, _configFile, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }
}
