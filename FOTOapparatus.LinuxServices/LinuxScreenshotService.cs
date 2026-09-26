using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;
using FOTOapparatus.Core;

namespace FOTOapparatus.LinuxServices;

public sealed class LinuxScreenshotService : IScreenshotService
{
    private readonly IWindowService _windowService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IProcessRunner _processRunner;
    private readonly IScreenshotImageProcessor _imageProcessor;

    public LinuxScreenshotService(
        IWindowService windowService,
        IHotkeyService hotkeyService,
        IProcessRunner processRunner,
        IScreenshotImageProcessor imageProcessor)
    {
        _windowService = windowService;
        _hotkeyService = hotkeyService;
        _processRunner = processRunner;
        _imageProcessor = imageProcessor;
    }

    public async Task<string?> CaptureSelectionPreviewAsync(CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"fotoapparatus_selection_{Guid.NewGuid():N}.png");
        var captured = await CaptureFullScreenInternalAsync(tempPath, cancellationToken);

        if (captured && File.Exists(tempPath))
        {
            return tempPath;
        }

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        return null;
    }

    public async Task<string?> CaptureAsync(
        AppSettings settings,
        string filenamePrefix,
        bool forceStayForeground = false,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"fotoapparatus_{Guid.NewGuid():N}.png");

        try
        {
            var captured = settings.CaptureType switch
            {
                var captureType when captureType == CaptureTypes.Program
                    => await CaptureProgramWindowAsync(settings, tempPath, cancellationToken),
                var captureType when captureType == CaptureTypes.Discord
                    => await CaptureDiscordAsync(settings, tempPath, forceStayForeground, cancellationToken),
                _
                    => await CaptureFullScreenInternalAsync(tempPath, cancellationToken),
            };

            if (!captured || !File.Exists(tempPath))
            {
                return null;
            }

            var savePath = AppSettingsDefaults.NormalizeSavePath(settings.SavePath);
            Directory.CreateDirectory(savePath);
            var outputPath = BuildOutputPath(savePath, filenamePrefix);

            if (!_imageProcessor.NeedsProcessing(settings))
            {
                File.Move(tempPath, outputPath, true);
                return outputPath;
            }

            var processed = await _imageProcessor.ProcessAsync(
                tempPath,
                outputPath,
                settings,
                cancellationToken);
            return processed ? outputPath : null;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private async Task<bool> CaptureProgramWindowAsync(
        AppSettings settings,
        string tempPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TargetWindow))
        {
            return false;
        }

        var windows = await _windowService.GetWindowsAsync(cancellationToken);
        var targetWindow = _windowService.FindBestMatch(windows, settings.TargetWindow);
        if (targetWindow is null)
        {
            return false;
        }

        var previousWindowId = await _windowService.GetActiveWindowIdAsync(cancellationToken);
        if (!await _windowService.ActivateWindowAsync(targetWindow, cancellationToken))
        {
            return false;
        }

        try
        {
            await Task.Delay(350, cancellationToken);
            var result = await _processRunner.RunAsync(
                "gnome-screenshot",
                ["-w", "-f", tempPath],
                timeoutMs: 20000,
                cancellationToken: cancellationToken);
            return result.Succeeded;
        }
        finally
        {
            await RestorePreviousWindowAsync(previousWindowId, targetWindow.Id);
        }
    }

    private async Task<bool> CaptureDiscordAsync(
        AppSettings settings,
        string tempPath,
        bool forceStayForeground,
        CancellationToken cancellationToken)
    {
        var discordSettings = settings.DiscordSettings;
        if (string.IsNullOrWhiteSpace(discordSettings.WindowTitle)
            && string.IsNullOrWhiteSpace(discordSettings.WindowClassName))
        {
            return false;
        }

        var windows = await _windowService.GetWindowsAsync(cancellationToken);
        var targetWindow = _windowService.FindBestMatch(
            windows,
            discordSettings.WindowTitle,
            discordSettings.WindowClassName);
        if (targetWindow is null)
        {
            return false;
        }

        var previousWindowId = await _windowService.GetActiveWindowIdAsync(cancellationToken);
        if (!await _windowService.ActivateWindowAsync(targetWindow, cancellationToken))
        {
            return false;
        }

        try
        {
            // Give Electron/Vesktop enough time to render after activation.
            await Task.Delay(500, cancellationToken);

            if (discordSettings.UseHotkey)
            {
                await _hotkeyService.SendCtrlNumberAsync(
                    Math.Clamp(discordSettings.HotkeyNumber, 0, 9),
                    targetWindow.Id,
                    cancellationToken);
            }

            var delaySeconds = discordSettings.UseHotkey ? discordSettings.DelayAfterHotkey : 0;
            if (delaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }

            return await CaptureFullScreenInternalAsync(tempPath, cancellationToken);
        }
        finally
        {
            if (!forceStayForeground && !discordSettings.StayForeground)
            {
                await RestorePreviousWindowAsync(previousWindowId, targetWindow.Id);
            }
        }
    }

    private async Task RestorePreviousWindowAsync(string? previousWindowId, string targetWindowId)
    {
        if (!string.IsNullOrWhiteSpace(previousWindowId)
            && !string.Equals(previousWindowId, targetWindowId, StringComparison.OrdinalIgnoreCase))
        {
            await _windowService.ActivateWindowAsync(
                new WindowInfo { Id = previousWindowId },
                CancellationToken.None);
        }
    }

    private async Task<bool> CaptureFullScreenInternalAsync(string tempPath, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync(
            "gnome-screenshot",
            ["-f", tempPath],
            timeoutMs: 20000,
            cancellationToken: cancellationToken);

        return result.Succeeded;
    }

    private static string BuildOutputPath(string savePath, string filenamePrefix)
    {
        var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm");
        return Path.Combine(savePath, $"{filenamePrefix}_{timestamp}.png");
    }
}
