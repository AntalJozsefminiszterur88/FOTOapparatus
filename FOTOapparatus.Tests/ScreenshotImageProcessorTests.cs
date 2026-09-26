using FOTOapparatus.Core.Models;
using FOTOapparatus.LinuxServices;
using SkiaSharp;

namespace FOTOapparatus.Tests;

public sealed class ScreenshotImageProcessorTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"fotoapparatus-image-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ProcessAsync_CropsImageToConfiguredArea()
    {
        Directory.CreateDirectory(_directory);
        var sourcePath = Path.Combine(_directory, "source.png");
        var outputPath = Path.Combine(_directory, "output.png");
        CreateSolidImage(sourcePath, 100, 80);
        var settings = new AppSettings
        {
            CaptureType = CaptureTypes.Screenshot,
            ScreenshotMode = ScreenshotModes.Custom,
            IncludeTimestamp = false,
            CustomArea = new RectSettings { X = 10, Y = 20, Width = 30, Height = 40 },
        };
        var processor = new ScreenshotImageProcessor();

        var result = await processor.ProcessAsync(sourcePath, outputPath, settings);

        Assert.True(result);
        using var output = SKBitmap.Decode(outputPath);
        Assert.NotNull(output);
        Assert.Equal(30, output.Width);
        Assert.Equal(40, output.Height);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static void CreateSolidImage(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.DarkBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
