namespace FOTOapparatus.Core.Models;

public sealed class DiscordSettings
{
    public string WindowTitle { get; set; } = "Discord";
    public bool UseHotkey { get; set; } = true;
    public int HotkeyNumber { get; set; } = 1;
    public int DelayAfterHotkey { get; set; } = 1;
    public bool StayForeground { get; set; } = true;

    public DiscordSettings Clone() => new()
    {
        WindowTitle = WindowTitle,
        UseHotkey = UseHotkey,
        HotkeyNumber = HotkeyNumber,
        DelayAfterHotkey = DelayAfterHotkey,
        StayForeground = StayForeground
    };
}
