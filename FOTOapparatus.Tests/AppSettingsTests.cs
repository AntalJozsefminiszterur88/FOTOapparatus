using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void Clone_CreatesIndependentNestedSettings()
    {
        var original = new AppSettings
        {
            SavePath = "/tmp/captures",
            CustomArea = new RectSettings { X = 1, Y = 2, Width = 300, Height = 200 },
            DiscordSettings = new DiscordSettings { WindowClassName = "vesktop.vesktop" },
            Schedules =
            [
                new ScheduleSettings
                {
                    Id = "schedule-1",
                    Time = new TimeSpan(9, 30, 0),
                    Days = [DayOfWeek.Monday],
                    Enabled = false,
                },
            ],
        };

        var clone = original.Clone();
        clone.CustomArea.Width = 10;
        clone.DiscordSettings.WindowClassName = "discord.Discord";
        clone.Schedules[0].Days.Add(DayOfWeek.Tuesday);
        clone.Schedules[0].Enabled = true;

        Assert.Equal(300, original.CustomArea.Width);
        Assert.Equal("vesktop.vesktop", original.DiscordSettings.WindowClassName);
        Assert.Equal([DayOfWeek.Monday], original.Schedules[0].Days);
        Assert.False(original.Schedules[0].Enabled);
        Assert.Equal("schedule-1", clone.Schedules[0].Id);
    }
}
