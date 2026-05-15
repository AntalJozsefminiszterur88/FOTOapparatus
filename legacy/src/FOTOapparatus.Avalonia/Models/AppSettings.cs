using System.Text.Json.Serialization;

namespace FOTOapparatus.Avalonia.Models;

public sealed class AppSettings
{
    [JsonPropertyName("save_path")]
    public string SavePath { get; set; } = string.Empty;

    [JsonPropertyName("capture_type")]
    public string CaptureType { get; set; } = CaptureTypes.Screenshot;

    [JsonPropertyName("screenshot_mode")]
    public string ScreenshotMode { get; set; } = ScreenshotModes.Fullscreen;

    [JsonPropertyName("custom_area")]
    public RectSettings CustomArea { get; set; } = new();

    [JsonPropertyName("schedules")]
    public List<ScheduleEntry> Schedules { get; set; } = [];

    [JsonPropertyName("target_window")]
    public string TargetWindow { get; set; } = string.Empty;

    [JsonPropertyName("include_timestamp")]
    public bool IncludeTimestamp { get; set; } = true;

    [JsonPropertyName("timestamp_position")]
    public string TimestampPosition { get; set; } = TimestampPositions.TopLeft;

    [JsonPropertyName("idle_check_enabled")]
    public bool IdleCheckEnabled { get; set; }

    [JsonPropertyName("idle_threshold_minutes")]
    public int IdleThresholdMinutes { get; set; } = 5;

    [JsonPropertyName("autostart_preferred")]
    public bool AutostartPreferred { get; set; }

    [JsonPropertyName("discord_settings")]
    public DiscordSettings DiscordSettings { get; set; } = new();

    public AppSettings Clone()
        => new()
        {
            SavePath = SavePath,
            CaptureType = CaptureType,
            ScreenshotMode = ScreenshotMode,
            CustomArea = CustomArea.Clone(),
            Schedules = Schedules
                .Select(schedule => new ScheduleEntry
                {
                    Time = schedule.Time,
                    Days = [.. schedule.Days],
                })
                .ToList(),
            TargetWindow = TargetWindow,
            IncludeTimestamp = IncludeTimestamp,
            TimestampPosition = TimestampPosition,
            IdleCheckEnabled = IdleCheckEnabled,
            IdleThresholdMinutes = IdleThresholdMinutes,
            AutostartPreferred = AutostartPreferred,
            DiscordSettings = DiscordSettings.Clone(),
        };
}
