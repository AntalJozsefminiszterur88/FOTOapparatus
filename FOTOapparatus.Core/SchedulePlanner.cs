using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core;

public static class SchedulePlanner
{
    public static DateTime FloorToMinute(DateTime value)
        => new(
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            0,
            value.Kind);

    public static IReadOnlyList<ScheduleSettings> GetDueSchedules(AppSettings settings, DateTime now)
        => settings.Schedules
            .Where(schedule => IsDue(schedule, now))
            .ToList();

    public static TimeSpan GetWaitTime(AppSettings settings, DateTime now, TimeSpan maximumWait)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumWait, TimeSpan.Zero);

        DateTime? nextOccurrence = null;

        foreach (var schedule in settings.Schedules.Where(schedule => schedule.Enabled))
        {
            for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
            {
                var date = now.Date.AddDays(dayOffset);
                if (schedule.Days.Count > 0 && !schedule.Days.Contains(date.DayOfWeek))
                {
                    continue;
                }

                var candidate = date.AddHours(schedule.Time.Hours).AddMinutes(schedule.Time.Minutes);
                if (candidate <= now)
                {
                    continue;
                }

                if (nextOccurrence is null || candidate < nextOccurrence.Value)
                {
                    nextOccurrence = candidate;
                }

                break;
            }
        }

        if (nextOccurrence is null)
        {
            return maximumWait;
        }

        var waitTime = nextOccurrence.Value - now;
        return waitTime < maximumWait ? waitTime : maximumWait;
    }

    private static bool IsDue(ScheduleSettings schedule, DateTime now)
        => schedule.Enabled
           && schedule.Time.Hours == now.Hour
           && schedule.Time.Minutes == now.Minute
           && (schedule.Days.Count == 0 || schedule.Days.Contains(now.DayOfWeek));
}
