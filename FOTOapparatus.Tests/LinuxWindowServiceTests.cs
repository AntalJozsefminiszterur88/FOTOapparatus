using FOTOapparatus.Core.Models;
using FOTOapparatus.LinuxServices;

namespace FOTOapparatus.Tests;

public sealed class LinuxWindowServiceTests
{
    [Fact]
    public async Task GetWindowsAsync_ParsesWmClassAndTitleFromCorrectColumns()
    {
        const string wmctrlOutput = """
            0x04800007  0 vesktop.vesktop host-name Discord | #chat | Server
            0x03000054 -1 nemo-desktop.Nemo-desktop host-name Desktop
            """;
        var service = new LinuxWindowService(new StubProcessRunner(wmctrlOutput));

        var windows = await service.GetWindowsAsync();

        var vesktop = Assert.Single(windows, window => window.Id == "0x04800007");
        Assert.Equal("vesktop.vesktop", vesktop.ClassName);
        Assert.Equal("Discord | #chat | Server", vesktop.Title);
    }

    [Fact]
    public void FindBestMatch_PrefersStableWindowClassOverChangingTitle()
    {
        var service = new LinuxWindowService(new StubProcessRunner(string.Empty));
        WindowInfo[] windows =
        [
            new() { Id = "1", ClassName = "discord.Discord", Title = "Discord" },
            new() { Id = "2", ClassName = "vesktop.vesktop", Title = "Discord | #other-channel | New server" },
        ];

        var result = service.FindBestMatch(windows, "an old title", "vesktop.vesktop");

        Assert.NotNull(result);
        Assert.Equal("2", result.Id);
    }

    [Fact]
    public void FindBestMatch_MigratesOldDiscordTitleToVesktopClass()
    {
        var service = new LinuxWindowService(new StubProcessRunner(string.Empty));
        WindowInfo[] windows =
        [
            new() { Id = "2", ClassName = "vesktop.vesktop", Title = "Vesktop" },
        ];

        var result = service.FindBestMatch(windows, "Discord");

        Assert.NotNull(result);
        Assert.Equal("2", result.Id);
    }

    private sealed class StubProcessRunner(string standardOutput) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(
            string fileName,
            IEnumerable<string> arguments,
            int timeoutMs = 15000,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProcessResult(0, standardOutput, string.Empty));
    }
}
