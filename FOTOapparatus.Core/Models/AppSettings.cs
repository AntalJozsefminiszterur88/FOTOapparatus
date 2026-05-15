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
    {
        return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(System.Text.Json.JsonSerializer.Serialize(this)) ?? new AppSettings();
    }
}

public sealed class ScheduleSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public TimeSpan Time { get; set; }
    public List<DayOfWeek> Days { get; set; } = new();
    
    [JsonIgnore]
    public bool Enabled { get; set; } = true; // Temporary state, maybe saved if needed
}
