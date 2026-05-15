using System.Collections.ObjectModel;
using FOTOapparatus.Avalonia.Models;

namespace FOTOapparatus.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _savePath = string.Empty;
    private string _captureType = CaptureTypes.Screenshot;
    private string _screenshotMode = ScreenshotModes.Fullscreen;
    private RectSettings _customArea = new();
    private bool _includeTimestamp = true;
    private string _timestampPosition = TimestampPositions.TopLeft;
    private bool _idleCheckEnabled;
    private int _idleThresholdMinutes = 5;
    private bool _autostartEnabled;
    private bool _autostartSupported = true;
    private bool _isDirty;
    private string _statusMessage = "Alkalmazás betöltve.";
    private string _programWindowTitle = string.Empty;
    private string _discordWindowTitle = string.Empty;
    private WindowInfo? _selectedProgramWindow;
    private WindowInfo? _selectedDiscordWindow;
    private DiscordSettings _discordSettings = new();
    private bool _suppressDirtyTracking;

    public MainWindowViewModel()
    {
        Schedules.CollectionChanged += (_, __) => MarkDirty();
    }

    public ObservableCollection<WindowInfo> AvailableWindows { get; } = [];

    public ObservableCollection<ScheduleEntryViewModel> Schedules { get; } = [];

    public string SavePath
    {
        get => _savePath;
        set => SetTrackedProperty(ref _savePath, value);
    }

    public string CaptureType => _captureType;

    public string ScreenshotMode => _screenshotMode;

    public RectSettings CustomArea => _customArea;

    public bool IncludeTimestamp
    {
        get => _includeTimestamp;
        set => SetTrackedProperty(ref _includeTimestamp, value);
    }

    public string TimestampPosition => _timestampPosition;

    public bool IdleCheckEnabled
    {
        get => _idleCheckEnabled;
        set
        {
            if (SetTrackedProperty(ref _idleCheckEnabled, value))
            {
                OnPropertyChanged(nameof(IsIdleThresholdEnabled));
            }
        }
    }

    public int IdleThresholdMinutes
    {
        get => _idleThresholdMinutes;
        set => SetTrackedProperty(ref _idleThresholdMinutes, Math.Clamp(value, 1, 120));
    }

    public bool AutostartEnabled
    {
        get => _autostartEnabled;
        set => SetTrackedProperty(ref _autostartEnabled, value);
    }

    public bool AutostartSupported
    {
        get => _autostartSupported;
        set => SetProperty(ref _autostartSupported, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string WindowTitle => IsDirty ? "FOTO-Apparátus *" : "FOTO-Apparátus";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public DiscordSettings DiscordSettings => _discordSettings;

    public WindowInfo? SelectedProgramWindow
    {
        get => _selectedProgramWindow;
        set
        {
            if (SetTrackedProperty(ref _selectedProgramWindow, value))
            {
                _programWindowTitle = value?.Title ?? string.Empty;
            }
        }
    }

    public WindowInfo? SelectedDiscordWindow
    {
        get => _selectedDiscordWindow;
        set
        {
            if (SetTrackedProperty(ref _selectedDiscordWindow, value))
            {
                _discordWindowTitle = value?.Title ?? string.Empty;
            }
        }
    }

    public bool IsCaptureScreenshot
    {
        get => _captureType == CaptureTypes.Screenshot;
        set
        {
            if (value)
            {
                SetCaptureType(CaptureTypes.Screenshot);
            }
        }
    }

    public bool IsCaptureProgram
    {
        get => _captureType == CaptureTypes.Program;
        set
        {
            if (value)
            {
                SetCaptureType(CaptureTypes.Program);
            }
        }
    }

    public bool IsCaptureDiscord
    {
        get => _captureType == CaptureTypes.Discord;
        set
        {
            if (value)
            {
                SetCaptureType(CaptureTypes.Discord);
            }
        }
    }

    public bool IsFullscreenMode
    {
        get => _screenshotMode == ScreenshotModes.Fullscreen;
        set
        {
            if (value)
            {
                SetScreenshotMode(ScreenshotModes.Fullscreen);
            }
        }
    }

    public bool IsCustomMode
    {
        get => _screenshotMode == ScreenshotModes.Custom;
        set
        {
            if (value)
            {
                SetScreenshotMode(ScreenshotModes.Custom);
            }
        }
    }

    public bool IsTimestampTopLeft
    {
        get => _timestampPosition == TimestampPositions.TopLeft;
        set
        {
            if (value)
            {
                SetTimestampPosition(TimestampPositions.TopLeft);
            }
        }
    }

    public bool IsTimestampTopRight
    {
        get => _timestampPosition == TimestampPositions.TopRight;
        set
        {
            if (value)
            {
                SetTimestampPosition(TimestampPositions.TopRight);
            }
        }
    }

    public bool IsTimestampBottomLeft
    {
        get => _timestampPosition == TimestampPositions.BottomLeft;
        set
        {
            if (value)
            {
                SetTimestampPosition(TimestampPositions.BottomLeft);
            }
        }
    }

    public bool IsTimestampBottomRight
    {
        get => _timestampPosition == TimestampPositions.BottomRight;
        set
        {
            if (value)
            {
                SetTimestampPosition(TimestampPositions.BottomRight);
            }
        }
    }

    public bool IsSizeOptionsVisible => _captureType != CaptureTypes.Program;

    public bool IsProgramWindowVisible => _captureType == CaptureTypes.Program;

    public bool CanOpenDiscordSettings => _captureType == CaptureTypes.Discord;

    public bool IsIdleThresholdEnabled => IdleCheckEnabled;

    public string CustomAreaDisplay
        => CustomArea.IsValid
            ? $"Méret: {CustomArea.Width} x {CustomArea.Height} (X:{CustomArea.X}, Y:{CustomArea.Y})"
            : "Méret: -";

    public void Load(AppSettings settings)
    {
        _suppressDirtyTracking = true;
        try
        {
            SavePath = settings.SavePath;
            SetCaptureType(settings.CaptureType);
            SetScreenshotMode(settings.ScreenshotMode);
            SetCustomArea(settings.CustomArea.Clone());
            IncludeTimestamp = settings.IncludeTimestamp;
            SetTimestampPosition(settings.TimestampPosition);
            IdleCheckEnabled = settings.IdleCheckEnabled;
            IdleThresholdMinutes = settings.IdleThresholdMinutes;
            AutostartEnabled = settings.AutostartPreferred;

            _discordSettings = settings.DiscordSettings?.Clone() ?? new DiscordSettings();
            _programWindowTitle = settings.TargetWindow ?? string.Empty;
            _discordWindowTitle = _discordSettings.WindowTitle ?? string.Empty;

            foreach (var schedule in Schedules.ToList())
            {
                schedule.PropertyChanged -= ScheduleOnPropertyChanged;
            }

            Schedules.Clear();
            foreach (var schedule in settings.Schedules ?? [])
            {
                AddScheduleInternal(ScheduleEntryViewModel.FromModel(schedule), false);
            }

            StatusMessage = "Beállítások betöltve.";
            IsDirty = false;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    public void SetCustomArea(RectSettings rect)
    {
        _customArea = rect.Clone();
        OnPropertyChanged(nameof(CustomArea));
        OnPropertyChanged(nameof(CustomAreaDisplay));
        MarkDirty();
    }

    public void UpdateDiscordSettings(DiscordSettings settings)
    {
        _discordSettings = settings.Clone();
        _discordWindowTitle = _discordSettings.WindowTitle;
        OnPropertyChanged(nameof(DiscordSettings));
        SetAvailableWindows(AvailableWindows);
        MarkDirty();
    }

    public void SetAvailableWindows(IEnumerable<WindowInfo> windows)
    {
        _suppressDirtyTracking = true;
        try
        {
            var ordered = windows
                .Where(window => !string.IsNullOrWhiteSpace(window.Title))
                .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            AvailableWindows.Clear();
            foreach (var window in ordered)
            {
                AvailableWindows.Add(window);
            }

            SelectedProgramWindow = FindBestWindowMatch(_programWindowTitle);
            SelectedDiscordWindow = FindBestWindowMatch(_discordWindowTitle);
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    public void AddSchedule()
        => AddScheduleInternal(ScheduleEntryViewModel.CreateDefault(), true);

    public void RemoveSchedule(ScheduleEntryViewModel schedule)
    {
        schedule.PropertyChanged -= ScheduleOnPropertyChanged;
        Schedules.Remove(schedule);
        MarkDirty();
    }

    public AppSettings BuildSettings()
        => new()
        {
            SavePath = SavePath,
            CaptureType = _captureType,
            ScreenshotMode = _screenshotMode,
            CustomArea = _customArea.Clone(),
            Schedules = Schedules
                .Select(schedule => schedule.ToModel())
                .Where(schedule => !string.IsNullOrWhiteSpace(schedule.Time) && schedule.Days.Count > 0)
                .ToList(),
            TargetWindow = _programWindowTitle,
            IncludeTimestamp = IncludeTimestamp,
            TimestampPosition = _timestampPosition,
            IdleCheckEnabled = IdleCheckEnabled,
            IdleThresholdMinutes = IdleThresholdMinutes,
            AutostartPreferred = AutostartEnabled,
            DiscordSettings = new DiscordSettings
            {
                StayForeground = _discordSettings.StayForeground,
                UseHotkey = _discordSettings.UseHotkey,
                HotkeyNumber = _discordSettings.HotkeyNumber,
                WindowTitle = _discordWindowTitle,
                DelayAfterHotkey = _discordSettings.DelayAfterHotkey,
            },
        };

    public void MarkSaved(string statusMessage)
    {
        StatusMessage = statusMessage;
        IsDirty = false;
    }

    public void MarkDiscarded()
        => IsDirty = false;

    public void UpdateAutostartFromSystem(bool enabled)
    {
        _suppressDirtyTracking = true;
        try
        {
            AutostartEnabled = enabled;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    private void AddScheduleInternal(ScheduleEntryViewModel schedule, bool markDirty)
    {
        schedule.PropertyChanged += ScheduleOnPropertyChanged;
        Schedules.Add(schedule);
        if (markDirty)
        {
            MarkDirty();
        }
    }

    private void ScheduleOnPropertyChanged(object? sender, EventArgs e)
        => MarkDirty();

    private bool SetTrackedProperty<T>(ref T field, T value, string? propertyName = null)
    {
        var changed = SetProperty(ref field, value, propertyName);
        if (changed)
        {
            MarkDirty();
        }

        return changed;
    }

    private void MarkDirty()
    {
        if (_suppressDirtyTracking)
        {
            return;
        }

        IsDirty = true;
    }

    private void SetCaptureType(string captureType)
    {
        if (SetProperty(ref _captureType, captureType, nameof(CaptureType)))
        {
            OnPropertyChanged(nameof(IsCaptureScreenshot));
            OnPropertyChanged(nameof(IsCaptureProgram));
            OnPropertyChanged(nameof(IsCaptureDiscord));
            OnPropertyChanged(nameof(IsSizeOptionsVisible));
            OnPropertyChanged(nameof(IsProgramWindowVisible));
            OnPropertyChanged(nameof(CanOpenDiscordSettings));
            MarkDirty();
        }
    }

    private void SetScreenshotMode(string screenshotMode)
    {
        if (SetProperty(ref _screenshotMode, screenshotMode, nameof(ScreenshotMode)))
        {
            OnPropertyChanged(nameof(IsFullscreenMode));
            OnPropertyChanged(nameof(IsCustomMode));
            MarkDirty();
        }
    }

    private void SetTimestampPosition(string timestampPosition)
    {
        if (SetProperty(ref _timestampPosition, timestampPosition, nameof(TimestampPosition)))
        {
            OnPropertyChanged(nameof(IsTimestampTopLeft));
            OnPropertyChanged(nameof(IsTimestampTopRight));
            OnPropertyChanged(nameof(IsTimestampBottomLeft));
            OnPropertyChanged(nameof(IsTimestampBottomRight));
            MarkDirty();
        }
    }

    private WindowInfo? FindBestWindowMatch(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return AvailableWindows.FirstOrDefault(window =>
                   string.Equals(window.Title, title, StringComparison.CurrentCultureIgnoreCase))
               ?? AvailableWindows.FirstOrDefault(window =>
                   window.Title.Contains(title, StringComparison.CurrentCultureIgnoreCase))
               ?? AvailableWindows.FirstOrDefault(window =>
                   title.Contains(window.Title, StringComparison.CurrentCultureIgnoreCase));
    }
}
