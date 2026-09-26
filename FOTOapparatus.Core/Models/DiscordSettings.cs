namespace FOTOapparatus.Core.Models;

public sealed class DiscordSettings
{
    public string WindowTitle { get; set; } = "Discord";
    public string WindowClassName { get; set; } = string.Empty;
    public bool UseHotkey { get; set; } = true;
    public int HotkeyNumber { get; set; } = 1;
    public int DelayAfterHotkey { get; set; } = 1;
    public bool StayForeground { get; set; } = true;

    public DiscordSettings Clone() => new()
    {
        WindowTitle = WindowTitle,
        WindowClassName = WindowClassName,
        UseHotkey = UseHotkey,
        HotkeyNumber = HotkeyNumber,
        DelayAfterHotkey = DelayAfterHotkey,
        StayForeground = StayForeground
    };
}
