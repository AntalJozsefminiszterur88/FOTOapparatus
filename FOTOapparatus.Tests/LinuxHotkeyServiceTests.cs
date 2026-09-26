using FOTOapparatus.LinuxServices;

namespace FOTOapparatus.Tests;

public sealed class LinuxHotkeyServiceTests
{
    [Fact]
    public async Task SendCtrlNumberAsync_TargetsSelectedWindowAndClearsModifiers()
    {
        var runner = new RecordingProcessRunner();
        var service = new LinuxHotkeyService(runner);

        await service.SendCtrlNumberAsync(6, "0x04800007");

        Assert.Equal("xdotool", runner.FileName);
        Assert.Equal(
            ["key", "--window", "0x04800007", "--clearmodifiers", "ctrl+6"],
            runner.Arguments);
    }

    private sealed class RecordingProcessRunner : IProcessRunner
    {
        public string? FileName { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; } = [];

        public Task<ProcessResult> RunAsync(
            string fileName,
            IEnumerable<string> arguments,
            int timeoutMs = 15000,
            CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            Arguments = arguments.ToList();
            return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
        }
    }
}
