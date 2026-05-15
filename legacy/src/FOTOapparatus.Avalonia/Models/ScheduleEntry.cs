using System.Text.Json.Serialization;

namespace FOTOapparatus.Avalonia.Models;

public sealed class ScheduleEntry
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = "09:00";

    [JsonPropertyName("days")]
    public List<string> Days { get; set; } = [];
}
