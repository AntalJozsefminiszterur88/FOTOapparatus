using System.Text.Json.Serialization;

namespace FOTOapparatus.Avalonia.Models;

public sealed class DiscordSettings
{
    [JsonPropertyName("stay_foreground")]
    public bool StayForeground { get; set; }

    [JsonPropertyName("use_hotkey")]
    public bool UseHotkey { get; set; }

    [JsonPropertyName("hotkey_number")]
    public int HotkeyNumber { get; set; } = 1;

    [JsonPropertyName("window_title")]
    public string WindowTitle { get; set; } = string.Empty;

    [JsonPropertyName("delay_after_hotkey")]
    public double DelayAfterHotkey { get; set; } = 2.0;

    public DiscordSettings Clone()
        => new()
        {
            StayForeground = StayForeground,
            UseHotkey = UseHotkey,
            HotkeyNumber = HotkeyNumber,
            WindowTitle = WindowTitle,
            DelayAfterHotkey = DelayAfterHotkey,
        };
}
