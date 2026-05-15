using FOTOapparatus.Avalonia.Models;
using SkiaSharp;

namespace FOTOapparatus.Avalonia.Services;

public sealed class CaptureService
{
    private readonly WindowDiscoveryService _windowDiscoveryService;
    private readonly X11HotkeyService _hotkeyService;

    public CaptureService(WindowDiscoveryService windowDiscoveryService, X11HotkeyService hotkeyService)
    {
        _windowDiscoveryService = windowDiscoveryService;
        _hotkeyService = hotkeyService;
    }

    public async Task<string?> CaptureSelectionPreviewAsync(CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"fotoapparatus_selection_{Guid.NewGuid():N}.png");
        var captured = await CaptureFullScreenAsync(tempPath, cancellationToken);

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
        bool forceStayForeground,
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
                    => await CaptureFullScreenAsync(tempPath, cancellationToken),
            };

            if (!captured || !File.Exists(tempPath))
            {
                return null;
            }

            Directory.CreateDirectory(settings.SavePath);
            var outputPath = BuildOutputPath(settings.SavePath, filenamePrefix);

            if (!NeedsPostProcessing(settings))
            {
                File.Move(tempPath, outputPath, true);
                return outputPath;
            }

            using var sourceBitmap = SKBitmap.Decode(tempPath);
            if (sourceBitmap is null)
            {
                return null;
            }

            using var processedBitmap = PrepareBitmap(sourceBitmap, settings);
            using var image = SKImage.FromBitmap(processedBitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            await using var outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoded.SaveTo(outputStream);
            return outputPath;
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

        var windows = await _windowDiscoveryService.GetWindowsAsync(cancellationToken);
        var targetWindow = _windowDiscoveryService.FindBestMatch(windows, settings.TargetWindow);
        if (targetWindow is null)
        {
            return false;
        }

        var previousWindowId = await _windowDiscoveryService.GetActiveWindowIdAsync(cancellationToken);
        await _windowDiscoveryService.ActivateWindowAsync(targetWindow, cancellationToken);
        await Task.Delay(350, cancellationToken);

        var result = await ProcessRunner.RunAsync(
            "gnome-screenshot",
            ["-w", "-f", tempPath],
            timeoutMs: 20000,
            cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousWindowId)
            && !string.Equals(previousWindowId, targetWindow.Id, StringComparison.OrdinalIgnoreCase))
        {
            await ProcessRunner.RunAsync(
                "wmctrl",
                ["-ia", previousWindowId],
                cancellationToken: cancellationToken);
        }

        return result.Succeeded;
    }

    private async Task<bool> CaptureDiscordAsync(
        AppSettings settings,
        string tempPath,
        bool forceStayForeground,
        CancellationToken cancellationToken)
    {
        var discordSettings = settings.DiscordSettings;
        if (string.IsNullOrWhiteSpace(discordSettings.WindowTitle))
        {
            return false;
        }

        var windows = await _windowDiscoveryService.GetWindowsAsync(cancellationToken);
        var targetWindow = _windowDiscoveryService.FindBestMatch(windows, discordSettings.WindowTitle);
        if (targetWindow is null)
        {
            return false;
        }

        var previousWindowId = await _windowDiscoveryService.GetActiveWindowIdAsync(cancellationToken);
        await _windowDiscoveryService.ActivateWindowAsync(targetWindow, cancellationToken);
        await Task.Delay(350, cancellationToken);

        if (discordSettings.UseHotkey)
        {
            _hotkeyService.SendCtrlNumber(Math.Clamp(discordSettings.HotkeyNumber, 0, 9));
        }

        var delaySeconds = discordSettings.UseHotkey ? discordSettings.DelayAfterHotkey : 0;
        if (delaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }

        var captureSucceeded = await CaptureFullScreenAsync(tempPath, cancellationToken);

        if (!forceStayForeground
            && !discordSettings.StayForeground
            && !string.IsNullOrWhiteSpace(previousWindowId)
            && !string.Equals(previousWindowId, targetWindow.Id, StringComparison.OrdinalIgnoreCase))
        {
            await ProcessRunner.RunAsync(
                "wmctrl",
                ["-ia", previousWindowId],
                cancellationToken: cancellationToken);
        }

        return captureSucceeded;
    }

    private static async Task<bool> CaptureFullScreenAsync(string tempPath, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            "gnome-screenshot",
            ["-f", tempPath],
            timeoutMs: 20000,
            cancellationToken: cancellationToken);

        return result.Succeeded;
    }

    private static SKBitmap PrepareBitmap(SKBitmap sourceBitmap, AppSettings settings)
    {
        var bitmap = CropBitmapIfNeeded(sourceBitmap, settings);
        if (settings.IncludeTimestamp)
        {
            DrawTimestamp(bitmap, settings.TimestampPosition);
        }

        return bitmap;
    }

    private static SKBitmap CropBitmapIfNeeded(SKBitmap sourceBitmap, AppSettings settings)
    {
        if (settings.CaptureType == CaptureTypes.Program
            || settings.ScreenshotMode != ScreenshotModes.Custom
            || !settings.CustomArea.IsValid)
        {
            return sourceBitmap.Copy();
        }

        var x = Math.Clamp(settings.CustomArea.X, 0, Math.Max(0, sourceBitmap.Width - 1));
        var y = Math.Clamp(settings.CustomArea.Y, 0, Math.Max(0, sourceBitmap.Height - 1));
        var width = Math.Clamp(settings.CustomArea.Width, 1, sourceBitmap.Width - x);
        var height = Math.Clamp(settings.CustomArea.Height, 1, sourceBitmap.Height - y);

        var croppedBitmap = new SKBitmap(width, height, sourceBitmap.ColorType, sourceBitmap.AlphaType);
        using var canvas = new SKCanvas(croppedBitmap);
        var sourceRect = new SKRectI(x, y, x + width, y + height);
        var destinationRect = new SKRect(0, 0, width, height);
        canvas.DrawBitmap(sourceBitmap, sourceRect, destinationRect);
        canvas.Flush();
        return croppedBitmap;
    }

    private static void DrawTimestamp(SKBitmap bitmap, string position)
    {
        using var canvas = new SKCanvas(bitmap);
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 180),
            TextSize = Math.Max(18, bitmap.Width / 55f),
            IsAntialias = true,
            Typeface = SKTypeface.Default,
        };
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = shadowPaint.TextSize,
            IsAntialias = true,
            Typeface = SKTypeface.Default,
        };

        var text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var textWidth = textPaint.MeasureText(text);
        var metrics = textPaint.FontMetrics;
        var textHeight = metrics.Descent - metrics.Ascent;
        const int margin = 16;

        float x = margin;
        float y = margin - metrics.Ascent;

        switch (position)
        {
            case TimestampPositions.TopRight:
                x = bitmap.Width - textWidth - margin;
                break;
            case TimestampPositions.BottomLeft:
                y = bitmap.Height - textHeight - margin - metrics.Ascent;
                break;
            case TimestampPositions.BottomRight:
                x = bitmap.Width - textWidth - margin;
                y = bitmap.Height - textHeight - margin - metrics.Ascent;
                break;
        }

        canvas.DrawText(text, x + 2, y + 2, shadowPaint);
        canvas.DrawText(text, x, y, textPaint);
    }

    private static bool NeedsPostProcessing(AppSettings settings)
        => settings.IncludeTimestamp
           || (settings.CaptureType != CaptureTypes.Program
               && settings.ScreenshotMode == ScreenshotModes.Custom
               && settings.CustomArea.IsValid);

    private static string BuildOutputPath(string savePath, string filenamePrefix)
    {
        var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
        return Path.Combine(savePath, $"{filenamePrefix}_{timestamp}.png");
    }
}
