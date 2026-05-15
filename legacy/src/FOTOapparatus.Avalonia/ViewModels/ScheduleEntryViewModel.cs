using FOTOapparatus.Avalonia.Models;

namespace FOTOapparatus.Avalonia.ViewModels;

public sealed class ScheduleEntryViewModel : ViewModelBase
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

    public static ScheduleEntryViewModel CreateDefault()
    {
        var now = DateTime.Now;
        return new ScheduleEntryViewModel
        {
            TimeText = $"{now.Hour:00}:{now.Minute:00}",
        };
    }

    public static ScheduleEntryViewModel FromModel(ScheduleEntry model)
    {
        var viewModel = new ScheduleEntryViewModel();
        viewModel.TimeText = NormalizeTimeText(model.Time);

        foreach (var day in model.Days)
        {
            switch (day)
            {
                case "H":
                    viewModel.DayMonday = true;
                    break;
                case "K":
                    viewModel.DayTuesday = true;
                    break;
                case "Sze":
                    viewModel.DayWednesday = true;
                    break;
                case "Cs":
                    viewModel.DayThursday = true;
                    break;
                case "P":
                    viewModel.DayFriday = true;
                    break;
                case "Szo":
                    viewModel.DaySaturday = true;
                    break;
                case "V":
                    viewModel.DaySunday = true;
                    break;
            }
        }

        return viewModel;
    }

    public ScheduleEntry ToModel()
        => new()
        {
            Time = NormalizeTimeText(TimeText),
            Days = GetSelectedDays(),
        };

    private static string NormalizeTimeText(string? timeText)
    {
        if (TimeSpan.TryParse(timeText, out var parsedTime))
        {
            return $"{parsedTime.Hours:00}:{parsedTime.Minutes:00}";
        }

        return "00:00";
    }

    private List<string> GetSelectedDays()
    {
        var days = new List<string>();
        if (DayMonday)
        {
            days.Add("H");
        }

        if (DayTuesday)
        {
            days.Add("K");
        }

        if (DayWednesday)
        {
            days.Add("Sze");
        }

        if (DayThursday)
        {
            days.Add("Cs");
        }

        if (DayFriday)
        {
            days.Add("P");
        }

        if (DaySaturday)
        {
            days.Add("Szo");
        }

        if (DaySunday)
        {
            days.Add("V");
        }

        return days;
    }
}
