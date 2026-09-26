using FOTOapparatus.Core;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"fotoapparatus-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsVesktopWindowIdentity()
    {
        var store = CreateStore();
        var settings = new AppSettings
        {
            SavePath = _directory,
            CaptureType = CaptureTypes.Discord,
            DiscordSettings = new DiscordSettings
            {
                WindowTitle = "Discord",
                WindowClassName = "vesktop.vesktop",
                HotkeyNumber = 6,
            },
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(CaptureTypes.Discord, loaded.CaptureType);
        Assert.Equal("vesktop.vesktop", loaded.DiscordSettings.WindowClassName);
        Assert.Equal(6, loaded.DiscordSettings.HotkeyNumber);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task LoadAsync_InvalidJsonReturnsDefaultsAndReportsError()
    {
        Directory.CreateDirectory(_directory);
        var configFile = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(configFile, "{ not valid json");
        Exception? reportedError = null;
        var store = new JsonSettingsStore(configFile, ex => reportedError = ex);

        var loaded = await store.LoadAsync();

        Assert.NotNull(reportedError);
        Assert.False(string.IsNullOrWhiteSpace(loaded.SavePath));
        Assert.Equal("{ not valid json", await File.ReadAllTextAsync(configFile));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private JsonSettingsStore CreateStore()
        => new(Path.Combine(_directory, "settings.json"));
}
