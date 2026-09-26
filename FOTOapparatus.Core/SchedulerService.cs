using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core;

public sealed class SchedulerService
{
    private static readonly TimeSpan MaximumClockCheckInterval = TimeSpan.FromMinutes(1);
    private readonly IScreenshotService _screenshotService;
    private readonly IIdleService _idleService;
    private readonly object _lifecycleLock = new();
    private readonly object _settingsLock = new();
    private readonly SemaphoreSlim _settingsChanged = new(0, 1);
    private AppSettings _settings;
    private CancellationTokenSource? _cts;
    private Task? _runnerTask;
    private readonly Action<string> _onLog;

    public SchedulerService(IScreenshotService screenshotService, IIdleService idleService, AppSettings initialSettings, Action<string> onLog)
    {
        _screenshotService = screenshotService;
        _idleService = idleService;
        _settings = initialSettings;
        _onLog = onLog;
    }

    public void UpdateSettings(AppSettings settings)
    {
        lock (_settingsLock)
        {
            _settings = settings;
        }

        try
        {
            _settingsChanged.Release();
        }
        catch (SemaphoreFullException)
        {
            // A pending signal already guarantees that the loop will reload settings.
        }

        _onLog("Beállítások frissítve az időzítőben.");
    }

    public void Start()
    {
        lock (_lifecycleLock)
        {
            if (_runnerTask is { IsCompleted: false })
            {
                return;
            }

            _cts?.Dispose();
            var cancellationSource = new CancellationTokenSource();
            _cts = cancellationSource;
            _runnerTask = Task.Run(() => RunLoopAsync(cancellationSource.Token));
        }

        _onLog("Időzítő elindítva.");
    }

    public void Stop()
    {
        _ = StopAsync();
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cancellationSource;
        Task? runnerTask;

        lock (_lifecycleLock)
        {
            cancellationSource = _cts;
            runnerTask = _runnerTask;
            _cts = null;
            _runnerTask = null;
        }

        if (cancellationSource is null)
        {
            return;
        }

        cancellationSource.Cancel();
        try
        {
            if (runnerTask is not null)
            {
                await runnerTask;
            }
        }
        finally
        {
            cancellationSource.Dispose();
        }

        _onLog("Időzítő leállítva.");
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        DateTime? lastExecutedMinute = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var settings = GetSettings();
                var now = DateTime.Now;
                var currentMinute = SchedulePlanner.FloorToMinute(now);
                var toRun = SchedulePlanner.GetDueSchedules(settings, now);

                if (toRun.Count > 0 && lastExecutedMinute != currentMinute)
                {
                    lastExecutedMinute = currentMinute;
                    _onLog($"Időzített feladat indul: {toRun.Count} db találat.");
                    await ExecuteCaptureAsync(settings, token);
                    continue;
                }

                var waitTime = SchedulePlanner.GetWaitTime(settings, now, MaximumClockCheckInterval);
                await _settingsChanged.WaitAsync(waitTime, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private AppSettings GetSettings()
    {
        lock (_settingsLock)
        {
            return _settings;
        }
    }

    private async Task ExecuteCaptureAsync(AppSettings settings, CancellationToken token)
    {
        try
        {
            if (settings.IdleCheckEnabled)
            {
                var idleTime = await _idleService.GetIdleTimeAsync(token);
                var idleThreshold = TimeSpan.FromMinutes(settings.IdleThresholdMinutes);
                if (idleTime.HasValue && idleTime.Value < idleThreshold)
                {
                    _onLog($"A felhasználó csak {idleTime.Value.TotalMinutes:F1} perce tétlen (határ: {settings.IdleThresholdMinutes} perc), képkészítés kihagyva.");
                    return;
                }

                if (!idleTime.HasValue)
                {
                    _onLog("Nem sikerült lekérni az inaktivitási időt, a képkészítés a beállított időpontban folytatódik.");
                }
            }

            var path = await _screenshotService.CaptureAsync(settings, "Kép", forceStayForeground: false, token);
            if (path != null)
            {
                _onLog($"Automatikus kép elkészült: {path}");
            }
            else
            {
                _onLog("Nem sikerült elkészíteni az automatikus képet.");
            }
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException || !token.IsCancellationRequested)
        {
            _onLog($"Hiba az automatikus képkészítés során: {ex.Message}");
        }
    }
}
