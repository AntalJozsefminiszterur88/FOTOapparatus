namespace FOTOapparatus.Core.Models;

public sealed class RectSettings
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool IsValid => Width > 0 && Height > 0;

    public RectSettings Clone() => new()
    {
        X = X,
        Y = Y,
        Width = Width,
        Height = Height
    };
}
