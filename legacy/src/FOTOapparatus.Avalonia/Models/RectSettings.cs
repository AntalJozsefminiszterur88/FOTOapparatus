using System.Text.Json.Serialization;

namespace FOTOapparatus.Avalonia.Models;

public sealed class RectSettings
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; } = 100;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 100;

    [JsonIgnore]
    public bool IsValid => Width > 0 && Height > 0;

    public RectSettings Clone()
        => new()
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
        };
}
