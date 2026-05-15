using FOTOapparatus.Avalonia.Models;

namespace FOTOapparatus.Avalonia.Services;

public sealed class SchedulerService : IDisposable
{
    private readonly CaptureService _captureService;
    private readonly IdleMonitorService _idleMonitorService;
    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private readonly Dictionary<string, string> _lastTriggered = new(StringComparer.OrdinalIgnoreCase);
    private Func<AppSettings>? _settingsProvider;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public SchedulerService(CaptureService captureService, IdleMonitorService idleMonitorService)
    {
        _captureService = captureService;
        _idleMonitorService = idleMonitorService;
    }

    public void Start(Func<AppSettings> settingsProvider)
    {
        _settingsProvider = settingsProvider;
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Reload()
        => _lastTriggered.Clear();

    public void Stop()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        try
        {
            _loopTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Intentionally ignored during shutdown.
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _captureLock.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (_settingsProvider is null)
            {
                continue;
            }

            try
            {
                await CheckSchedulesAsync(_settingsProvider(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep the scheduler alive even if one run fails.
            }
        }
    }

    private async Task CheckSchedulesAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Schedules.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        var currentDayToken = MapDayOfWeek(now.DayOfWeek);

        foreach (var schedule in settings.Schedules)
        {
            if (!schedule.Days.Contains(currentDayToken, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TimeSpan.TryParse(schedule.Time, out var scheduledTime))
            {
                continue;
            }

            if (scheduledTime.Hours != now.Hour || scheduledTime.Minutes != now.Minute)
            {
                continue;
            }

            var triggerKey = $"{schedule.Time}|{string.Join(",", schedule.Days.OrderBy(day => day))}";
            var minuteKey = now.ToString("yyyyMMddHHmm");
            if (_lastTriggered.TryGetValue(triggerKey, out var lastMinuteKey) && lastMinuteKey == minuteKey)
            {
                continue;
            }

            if (settings.IdleCheckEnabled)
            {
                var idleSeconds = _idleMonitorService.GetIdleSeconds();
                if (idleSeconds.HasValue && idleSeconds.Value < settings.IdleThresholdMinutes * 60)
                {
                    continue;
                }
            }

            _lastTriggered[triggerKey] = minuteKey;

            await _captureLock.WaitAsync(cancellationToken);
            try
            {
                await _captureService.CaptureAsync(
                    settings.Clone(),
                    "Kép",
                    forceStayForeground: false,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _captureLock.Release();
            }
        }
    }

    private static string MapDayOfWeek(DayOfWeek dayOfWeek)
        => dayOfWeek switch
        {
            DayOfWeek.Monday => "H",
            DayOfWeek.Tuesday => "K",
            DayOfWeek.Wednesday => "Sze",
            DayOfWeek.Thursday => "Cs",
            DayOfWeek.Friday => "P",
            DayOfWeek.Saturday => "Szo",
            _ => "V",
        };
}
