using FOTOapparatus.Core.Interfaces;

namespace FOTOapparatus.LinuxServices;

public sealed class LinuxHotkeyService : IHotkeyService
{
    private readonly IProcessRunner _processRunner;

    public LinuxHotkeyService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task SendCtrlNumberAsync(
        int number,
        string? windowId = null,
        CancellationToken cancellationToken = default)
    {
        var safeNumber = Math.Clamp(number, 0, 9);
        var arguments = new List<string> { "key" };
        if (!string.IsNullOrWhiteSpace(windowId))
        {
            arguments.AddRange(["--window", windowId]);
        }

        arguments.Add("--clearmodifiers");
        arguments.Add($"ctrl+{safeNumber}");

        await _processRunner.RunAsync(
            "xdotool",
            arguments,
            cancellationToken: cancellationToken);
    }
}
