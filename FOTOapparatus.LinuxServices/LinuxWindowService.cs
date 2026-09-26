using System.Text.RegularExpressions;
using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.LinuxServices;

public sealed partial class LinuxWindowService : IWindowService
{
    private readonly IProcessRunner _processRunner;

    [GeneratedRegex(@"^(?<id>0x[0-9a-fA-F]+)\s+\S+\s+(?<class>\S+)\s+\S+\s+(?<title>.+)$")]
    private static partial Regex WmctrlLineRegex();

    [GeneratedRegex(@"0x[0-9a-fA-F]+")]
    private static partial Regex ActiveWindowRegex();

    public LinuxWindowService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux())
        {
            return [];
        }

        try
        {
            var result = await _processRunner.RunAsync(
                "wmctrl",
                ["-lx"],
                cancellationToken: cancellationToken);

            if (!result.Succeeded)
            {
                return [];
            }

            return result.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseWindow)
                .Where(window => window is not null)
                .Cast<WindowInfo>()
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> ActivateWindowAsync(WindowInfo window, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _processRunner.RunAsync(
                "wmctrl",
                ["-ia", window.Id],
                cancellationToken: cancellationToken);

            return result.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetActiveWindowIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _processRunner.RunAsync(
                "xprop",
                ["-root", "_NET_ACTIVE_WINDOW"],
                cancellationToken: cancellationToken);

            if (!result.Succeeded)
            {
                return null;
            }

            return ActiveWindowRegex().Match(result.StandardOutput).Value;
        }
        catch
        {
            return null;
        }
    }

    public WindowInfo? FindBestMatch(IEnumerable<WindowInfo> windows, string title, string? className = null)
    {
        var windowList = windows as IReadOnlyList<WindowInfo> ?? windows.ToList();

        if (!string.IsNullOrWhiteSpace(className))
        {
            var classMatch = windowList.FirstOrDefault(window =>
                string.Equals(window.ClassName, className, StringComparison.OrdinalIgnoreCase));
            if (classMatch is not null)
            {
                return classMatch;
            }
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleMatch = windowList.FirstOrDefault(window =>
                                 string.Equals(window.Title, title, StringComparison.CurrentCultureIgnoreCase))
                             ?? windowList.FirstOrDefault(window =>
                                 window.Title.Contains(title, StringComparison.CurrentCultureIgnoreCase))
                             ?? windowList.FirstOrDefault(window =>
                                 title.Contains(window.Title, StringComparison.CurrentCultureIgnoreCase));
            if (titleMatch is not null)
            {
                return titleMatch;
            }
        }

        // Discord's official client and Vencord/Vesktop use different, stable X11
        // window classes while the visible title changes with the active channel.
        if (IsDiscordClientIdentifier(title) || IsDiscordClientIdentifier(className))
        {
            return windowList.FirstOrDefault(window => IsDiscordClientIdentifier(window.ClassName));
        }

        return null;
    }

    private static bool IsDiscordClientIdentifier(string? value)
        => value?.Contains("discord", StringComparison.OrdinalIgnoreCase) == true
           || value?.Contains("vencord", StringComparison.OrdinalIgnoreCase) == true
           || value?.Contains("vesktop", StringComparison.OrdinalIgnoreCase) == true;

    private static WindowInfo? ParseWindow(string line)
    {
        var match = WmctrlLineRegex().Match(line.Trim());
        if (!match.Success)
        {
            return null;
        }

        var title = match.Groups["title"].Value.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new WindowInfo
        {
            Id = match.Groups["id"].Value,
            ClassName = match.Groups["class"].Value,
            Title = title,
        };
    }
}
