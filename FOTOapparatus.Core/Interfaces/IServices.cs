using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core.Interfaces;

public interface IWindowService
{
    Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default);
    Task<bool> ActivateWindowAsync(WindowInfo window, CancellationToken cancellationToken = default);
    Task<string?> GetActiveWindowIdAsync(CancellationToken cancellationToken = default);
    WindowInfo? FindBestMatch(IEnumerable<WindowInfo> windows, string title, string? className = null);
}

public interface IHotkeyService
{
    Task SendCtrlNumberAsync(int number, string? windowId = null, CancellationToken cancellationToken = default);
}

public interface IScreenshotService
{
    Task<string?> CaptureSelectionPreviewAsync(CancellationToken cancellationToken = default);
    Task<string?> CaptureAsync(AppSettings settings, string filenamePrefix, bool forceStayForeground = false, CancellationToken cancellationToken = default);
}

public interface IIdleService
{
    Task<TimeSpan?> GetIdleTimeAsync(CancellationToken cancellationToken = default);
}

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
