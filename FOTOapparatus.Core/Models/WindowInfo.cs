namespace FOTOapparatus.Core.Models;

public sealed class WindowInfo
{
    public string Id { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public override string ToString() => Title;
}
