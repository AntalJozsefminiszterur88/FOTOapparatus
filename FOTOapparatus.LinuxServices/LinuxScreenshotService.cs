using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;
using FOTOapparatus.Core;
using SkiaSharp;

namespace FOTOapparatus.LinuxServices;

public sealed class LinuxScreenshotService : IScreenshotService
{
    private readonly IWindowService _windowService;
    private readonly IHotkeyService _hotkeyService;

    public LinuxScreenshotService(IWindowService windowService, IHotkeyService hotkeyService)
    {
        _windowService = windowService;
        _hotkeyService = hotkeyService;
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

        var windows = await _windowService.GetWindowsAsync(cancellationToken);
        var targetWindow = _windowService.FindBestMatch(windows, settings.TargetWindow);
        if (targetWindow is null)
        {
            return false;
        }

        var previousWindowId = await _windowService.GetActiveWindowIdAsync(cancellationToken);
        await _windowService.ActivateWindowAsync(targetWindow, cancellationToken);
        await Task.Delay(350, cancellationToken);

        var result = await ProcessRunner.RunAsync(
            "gnome-screenshot",
            ["-w", "-f", tempPath],
            timeoutMs: 20000,
            cancellationToken: cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousWindowId)
            && !string.Equals(previousWindowId, targetWindow.Id, StringComparison.OrdinalIgnoreCase))
        {
            await _windowService.ActivateWindowAsync(new WindowInfo { Id = previousWindowId }, cancellationToken);
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

        var windows = await _windowService.GetWindowsAsync(cancellationToken);
        var targetWindow = _windowService.FindBestMatch(windows, discordSettings.WindowTitle);
        if (targetWindow is null)
        {
            return false;
        }

        var previousWindowId = await _windowService.GetActiveWindowIdAsync(cancellationToken);
        await _windowService.ActivateWindowAsync(targetWindow, cancellationToken);
        
        // Wait for window to be fully foreground and rendered
        await Task.Delay(500, cancellationToken);

        if (discordSettings.UseHotkey)
        {
            // Simulate the exact behavior from the old python version
            // Check pixel color at (1913, 53)
            var hexColor = await _hotkeyService.GetPixelColorHexAsync(1913, 53, cancellationToken);
            if (hexColor is not null)
            {
                // Expected color was (50, 51, 57) in RGB, which is #323339
                // Allowing some tolerance is hard with hex, but we can do an exact match or just assume it's right.
                // To be exact to the old code which checked if it's close to 50,51,57 (+- 5),
                // we'll decode the hex back and check it.
                if (IsColorWithinTolerance(hexColor, 50, 51, 57, 5))
                {
                    await _hotkeyService.SimulateMouseClickAsync(1913, 53, cancellationToken);
                    await Task.Delay(100, cancellationToken); // Wait a bit after click
                }
            }

            await _hotkeyService.SendCtrlNumberAsync(Math.Clamp(discordSettings.HotkeyNumber, 0, 9), cancellationToken);
        }

        var delaySeconds = discordSettings.UseHotkey ? discordSettings.DelayAfterHotkey : 0;
        if (delaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }

        var captureSucceeded = await CaptureFullScreenInternalAsync(tempPath, cancellationToken);

        if (!forceStayForeground
            && !discordSettings.StayForeground
            && !string.IsNullOrWhiteSpace(previousWindowId)
            && !string.Equals(previousWindowId, targetWindow.Id, StringComparison.OrdinalIgnoreCase))
        {
            await _windowService.ActivateWindowAsync(new WindowInfo { Id = previousWindowId }, cancellationToken);
        }

        return captureSucceeded;
    }

    private static bool IsColorWithinTolerance(string hexColor, int expectedR, int expectedG, int expectedB, int tolerance)
    {
        if (hexColor.Length != 7 || !hexColor.StartsWith("#")) return false;
        
        try
        {
            var r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
            var g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
            var b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

            return Math.Abs(r - expectedR) <= tolerance &&
                   Math.Abs(g - expectedG) <= tolerance &&
                   Math.Abs(b - expectedB) <= tolerance;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CaptureFullScreenInternalAsync(string tempPath, CancellationToken cancellationToken)
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
        var textSize = Math.Max(18, bitmap.Width / 55f) * 0.5f;
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 180),
            TextSize = textSize,
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

        var text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var textWidth = textPaint.MeasureText(text);
        var metrics = textPaint.FontMetrics;
        var textHeight = metrics.Descent - metrics.Ascent;
        const int margin = 16;
        const float rightShift = 55f;
        const float upwardShift = 15f;

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

        x = Math.Min(bitmap.Width - textWidth, x + rightShift);
        y = Math.Max(-metrics.Ascent, y - upwardShift);

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
        var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm");
        return Path.Combine(savePath, $"{filenamePrefix}_{timestamp}.png");
    }
}
