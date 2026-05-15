using FOTOapparatus.Core.Models;

namespace FOTOapparatus.UI.ViewModels;

public sealed class ScheduleSettingsViewModel : ViewModelBase
{
    private string _timeText = "09:00";
    private bool _dayMonday;
    private bool _dayTuesday;
    private bool _dayWednesday;
    private bool _dayThursday;
    private bool _dayFriday;
    private bool _daySaturday;
    private bool _daySunday;

    public string TimeText
    {
        get => _timeText;
        set => SetProperty(ref _timeText, value);
    }

    public bool DayMonday
    {
        get => _dayMonday;
        set => SetProperty(ref _dayMonday, value);
    }

    public bool DayTuesday
    {
        get => _dayTuesday;
        set => SetProperty(ref _dayTuesday, value);
    }

    public bool DayWednesday
    {
        get => _dayWednesday;
        set => SetProperty(ref _dayWednesday, value);
    }

    public bool DayThursday
    {
        get => _dayThursday;
        set => SetProperty(ref _dayThursday, value);
    }

    public bool DayFriday
    {
        get => _dayFriday;
        set => SetProperty(ref _dayFriday, value);
    }

    public bool DaySaturday
    {
        get => _daySaturday;
        set => SetProperty(ref _daySaturday, value);
    }

    public bool DaySunday
    {
        get => _daySunday;
        set => SetProperty(ref _daySunday, value);
    }

    public static ScheduleSettingsViewModel CreateDefault()
    {
        var now = DateTime.Now;
        return new ScheduleSettingsViewModel
        {
            TimeText = $"{now.Hour:00}:{now.Minute:00}",
        };
    }

    public static ScheduleSettingsViewModel FromModel(ScheduleSettings model)
    {
        var viewModel = new ScheduleSettingsViewModel();
        viewModel.TimeText = $"{model.Time.Hours:00}:{model.Time.Minutes:00}";

        foreach (var day in model.Days)
        {
            switch (day)
            {
                case DayOfWeek.Monday:
                    viewModel.DayMonday = true;
                    break;
                case DayOfWeek.Tuesday:
                    viewModel.DayTuesday = true;
                    break;
                case DayOfWeek.Wednesday:
                    viewModel.DayWednesday = true;
                    break;
                case DayOfWeek.Thursday:
                    viewModel.DayThursday = true;
                    break;
                case DayOfWeek.Friday:
                    viewModel.DayFriday = true;
                    break;
                case DayOfWeek.Saturday:
                    viewModel.DaySaturday = true;
                    break;
                case DayOfWeek.Sunday:
                    viewModel.DaySunday = true;
                    break;
            }
        }

        return viewModel;
    }

    public ScheduleSettings ToModel()
    {
        TimeSpan.TryParse(TimeText, out var time);
        return new ScheduleSettings
        {
            Time = time,
            Days = GetSelectedDays(),
        };
    }

    private static string NormalizeTimeText(string? timeText)
    {
        if (TimeSpan.TryParse(timeText, out var parsedTime))
        {
            return $"{parsedTime.Hours:00}:{parsedTime.Minutes:00}";
        }

        return "00:00";
    }

    private List<DayOfWeek> GetSelectedDays()
    {
        var days = new List<DayOfWeek>();
        if (DayMonday)
        {
            days.Add(DayOfWeek.Monday);
        }

        if (DayTuesday)
        {
            days.Add(DayOfWeek.Tuesday);
        }

        if (DayWednesday)
        {
            days.Add(DayOfWeek.Wednesday);
        }

        if (DayThursday)
        {
            days.Add(DayOfWeek.Thursday);
        }

        if (DayFriday)
        {
            days.Add(DayOfWeek.Friday);
        }

        if (DaySaturday)
        {
            days.Add(DayOfWeek.Saturday);
        }

        if (DaySunday)
        {
            days.Add(DayOfWeek.Sunday);
        }

        return days;
    }
}
