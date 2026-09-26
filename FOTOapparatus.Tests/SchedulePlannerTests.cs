using FOTOapparatus.Core;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Tests;

public sealed class SchedulePlannerTests
{
    [Fact]
    public void GetDueSchedules_OnlyReturnsEnabledSchedulesForCurrentDayAndMinute()
    {
        var now = new DateTime(2026, 9, 24, 18, 30, 45, DateTimeKind.Local);
        var settings = new AppSettings
        {
            Schedules =
            [
                ScheduleAt(18, 30, now.DayOfWeek),
                ScheduleAt(18, 31, now.DayOfWeek),
                ScheduleAt(18, 30, now.AddDays(1).DayOfWeek),
                new ScheduleSettings { Time = new TimeSpan(18, 30, 0), Enabled = false },
            ],
        };

        var result = SchedulePlanner.GetDueSchedules(settings, now);

        Assert.Single(result);
        Assert.Equal(new TimeSpan(18, 30, 0), result[0].Time);
    }

    [Fact]
    public void GetWaitTime_ReturnsTimeUntilNearestSchedule()
    {
        var now = new DateTime(2026, 9, 24, 18, 29, 40, DateTimeKind.Local);
        var settings = new AppSettings
        {
            Schedules = [ScheduleAt(18, 30, now.DayOfWeek)],
        };

        var result = SchedulePlanner.GetWaitTime(settings, now, TimeSpan.FromMinutes(1));

        Assert.Equal(TimeSpan.FromSeconds(20), result);
    }

    [Fact]
    public void GetWaitTime_CapsLongWaitToClockCheckInterval()
    {
        var now = new DateTime(2026, 9, 24, 18, 0, 0, DateTimeKind.Local);
        var settings = new AppSettings
        {
            Schedules = [ScheduleAt(23, 0, now.DayOfWeek)],
        };

        var result = SchedulePlanner.GetWaitTime(settings, now, TimeSpan.FromMinutes(1));

        Assert.Equal(TimeSpan.FromMinutes(1), result);
    }

    private static ScheduleSettings ScheduleAt(int hour, int minute, DayOfWeek day)
        => new()
        {
            Time = new TimeSpan(hour, minute, 0),
            Days = [day],
        };
}
