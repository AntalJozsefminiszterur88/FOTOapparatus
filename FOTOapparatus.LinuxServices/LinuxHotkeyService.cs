using FOTOapparatus.Core.Interfaces;
using SkiaSharp;

namespace FOTOapparatus.LinuxServices;

public sealed class LinuxHotkeyService : IHotkeyService
{
    public async Task SendCtrlNumberAsync(int number, CancellationToken cancellationToken = default)
    {
        var safeNumber = Math.Clamp(number, 0, 9);
        await ProcessRunner.RunAsync(
            "xdotool",
            ["key", $"ctrl+{safeNumber}"],
            cancellationToken: cancellationToken);
    }

    public async Task SimulateMouseClickAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        await ProcessRunner.RunAsync(
            "xdotool",
            ["mousemove", x.ToString(), y.ToString(), "click", "1"],
            cancellationToken: cancellationToken);
    }

    public async Task<string?> GetPixelColorHexAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"fotoapparatus_pixel_{Guid.NewGuid():N}.png");
        try
        {
            var result = await ProcessRunner.RunAsync(
                "gnome-screenshot",
                ["-f", tempPath],
                timeoutMs: 10000,
                cancellationToken: cancellationToken);

            if (!result.Succeeded || !File.Exists(tempPath))
            {
                return null;
            }

            using var bitmap = SKBitmap.Decode(tempPath);
            if (bitmap is null || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
            {
                return null;
            }

            var color = bitmap.GetPixel(x, y);
            return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
        }
        catch
        {
            return null;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
