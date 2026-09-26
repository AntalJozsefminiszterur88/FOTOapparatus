using System.Text.Json.Serialization;

namespace FOTOapparatus.Core.Models;

public sealed class AppSettings
{
    public string SavePath { get; set; } = string.Empty;
    public string CaptureType { get; set; } = CaptureTypes.Screenshot;
    public string ScreenshotMode { get; set; } = ScreenshotModes.Fullscreen;
    public RectSettings CustomArea { get; set; } = new();
    public bool IncludeTimestamp { get; set; } = true;
    public string TimestampPosition { get; set; } = TimestampPositions.TopLeft;
    public string TargetWindow { get; set; } = string.Empty;
    public DiscordSettings DiscordSettings { get; set; } = new();
    public bool IdleCheckEnabled { get; set; }
    public int IdleThresholdMinutes { get; set; } = 5;
    public bool AutostartEnabled { get; set; }
    public List<ScheduleSettings> Schedules { get; set; } = [];

    public AppSettings Clone()
        => new()
        {
            SavePath = SavePath,
            CaptureType = CaptureType,
            ScreenshotMode = ScreenshotMode,
            CustomArea = CustomArea.Clone(),
            IncludeTimestamp = IncludeTimestamp,
            TimestampPosition = TimestampPosition,
            TargetWindow = TargetWindow,
            DiscordSettings = DiscordSettings.Clone(),
            IdleCheckEnabled = IdleCheckEnabled,
            IdleThresholdMinutes = IdleThresholdMinutes,
            AutostartEnabled = AutostartEnabled,
            Schedules = [.. Schedules.Select(schedule => schedule.Clone())],
        };
}

public sealed class ScheduleSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public TimeSpan Time { get; set; }
    public List<DayOfWeek> Days { get; set; } = [];

    [JsonIgnore]
    public bool Enabled { get; set; } = true;

    public ScheduleSettings Clone()
        => new()
        {
            Id = Id,
            Time = Time,
            Days = [.. Days],
            Enabled = Enabled,
        };
}
