using System.Text.RegularExpressions;
using FOTOapparatus.Avalonia.Models;

namespace FOTOapparatus.Avalonia.Services;

public sealed class WindowDiscoveryService
{
    private static readonly Regex WmctrlLineRegex = new(
        @"^(?<id>0x[0-9a-fA-F]+)\s+\S+\s+\S+\s+(?<class>\S+)\s+(?<title>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex ActiveWindowRegex = new(
        @"0x[0-9a-fA-F]+",
        RegexOptions.Compiled);

    public async Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux())
        {
            return [];
        }

        try
        {
            var result = await ProcessRunner.RunAsync(
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
            var result = await ProcessRunner.RunAsync(
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
            var result = await ProcessRunner.RunAsync(
                "xprop",
                ["-root", "_NET_ACTIVE_WINDOW"],
                cancellationToken: cancellationToken);

            if (!result.Succeeded)
            {
                return null;
            }

            return ActiveWindowRegex.Match(result.StandardOutput).Value;
        }
        catch
        {
            return null;
        }
    }

    public WindowInfo? FindBestMatch(IEnumerable<WindowInfo> windows, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return windows.FirstOrDefault(window =>
                   string.Equals(window.Title, title, StringComparison.CurrentCultureIgnoreCase))
               ?? windows.FirstOrDefault(window =>
                   window.Title.Contains(title, StringComparison.CurrentCultureIgnoreCase))
               ?? windows.FirstOrDefault(window =>
                   title.Contains(window.Title, StringComparison.CurrentCultureIgnoreCase));
    }

    private static WindowInfo? ParseWindow(string line)
    {
        var match = WmctrlLineRegex.Match(line.Trim());
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
