namespace FOTOapparatus.Avalonia.Models;

public sealed class WindowInfo
{
    public string Id { get; init; } = string.Empty;

    public string ClassName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string DisplayText
        => string.IsNullOrWhiteSpace(ClassName) ? Title : $"{Title} [{ClassName}]";

    public override string ToString() => DisplayText;
}
