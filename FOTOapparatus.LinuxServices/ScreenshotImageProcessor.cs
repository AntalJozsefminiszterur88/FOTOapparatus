using FOTOapparatus.Core.Models;
using SkiaSharp;

namespace FOTOapparatus.LinuxServices;

public interface IScreenshotImageProcessor
{
    bool NeedsProcessing(AppSettings settings);
    Task<bool> ProcessAsync(
        string sourcePath,
        string outputPath,
        AppSettings settings,
        CancellationToken cancellationToken = default);
}

public sealed class ScreenshotImageProcessor : IScreenshotImageProcessor
{
    public bool NeedsProcessing(AppSettings settings)
        => settings.IncludeTimestamp
           || (settings.CaptureType != CaptureTypes.Program
               && settings.ScreenshotMode == ScreenshotModes.Custom
               && settings.CustomArea.IsValid);

    public async Task<bool> ProcessAsync(
        string sourcePath,
        string outputPath,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var sourceBitmap = SKBitmap.Decode(sourcePath);
        if (sourceBitmap is null)
        {
            return false;
        }

        using var subsetBitmap = ExtractSubsetIfNeeded(sourceBitmap, settings);
        var bitmapToSave = subsetBitmap ?? sourceBitmap;
        if (settings.IncludeTimestamp)
        {
            DrawTimestamp(bitmapToSave, settings.TimestampPosition, DateTime.Now);
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var pixels = bitmapToSave.PeekPixels();
        if (pixels is null)
        {
            return false;
        }

        await using var outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        if (!pixels.Encode(outputStream, SKEncodedImageFormat.Png, 100))
        {
            return false;
        }

        await outputStream.FlushAsync(cancellationToken);
        return true;
    }

    private static SKBitmap? ExtractSubsetIfNeeded(SKBitmap sourceBitmap, AppSettings settings)
    {
        if (settings.CaptureType == CaptureTypes.Program
            || settings.ScreenshotMode != ScreenshotModes.Custom
            || !settings.CustomArea.IsValid)
        {
            return null;
        }

        var x = Math.Clamp(settings.CustomArea.X, 0, Math.Max(0, sourceBitmap.Width - 1));
        var y = Math.Clamp(settings.CustomArea.Y, 0, Math.Max(0, sourceBitmap.Height - 1));
        var width = Math.Clamp(settings.CustomArea.Width, 1, sourceBitmap.Width - x);
        var height = Math.Clamp(settings.CustomArea.Height, 1, sourceBitmap.Height - y);

        // ExtractSubset shares the source pixel storage. A 4K crop therefore does
        // not allocate a second bitmap that can be tens of megabytes in size.
        var subsetBitmap = new SKBitmap();
        if (sourceBitmap.ExtractSubset(subsetBitmap, new SKRectI(x, y, x + width, y + height)))
        {
            return subsetBitmap;
        }

        subsetBitmap.Dispose();
        return null;
    }

    private static void DrawTimestamp(SKBitmap bitmap, string position, DateTime timestamp)
    {
        using var canvas = new SKCanvas(bitmap);
        var textSize = Math.Max(18, bitmap.Width / 55f) * 0.5f;
        using var font = new SKFont(SKTypeface.Default, textSize);
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 180),
            IsAntialias = true,
        };
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
        };

        var text = timestamp.ToString("yyyy-MM-dd HH:mm");
        var textWidth = font.MeasureText(text);
        var metrics = font.Metrics;
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

        canvas.DrawText(text, x + 2, y + 2, SKTextAlign.Left, font, shadowPaint);
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, textPaint);
    }
}
