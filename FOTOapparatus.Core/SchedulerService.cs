using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Core;

public sealed class SchedulerService
{
    private readonly IScreenshotService _screenshotService;
    private readonly IIdleService _idleService;
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
        _settings = settings;
        _onLog("Beállítások frissítve az időzítőben.");
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _runnerTask = Task.Run(() => RunLoopAsync(_cts.Token));
        _onLog("Időzítő elindítva.");
    }

    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _onLog("Időzítő leállítva.");
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var now = DateTime.Now;
            
            // Check if we need to run any schedule
            var toRun = _settings.Schedules.Where(s => s.Enabled && s.Time.Hours == now.Hour && s.Time.Minutes == now.Minute && (s.Days.Count == 0 || s.Days.Contains(now.DayOfWeek))).ToList();
            
            if (toRun.Any())
            {
                // To avoid multiple runs in the same minute, we sleep at the end of the loop
                _onLog($"Időzített feladat indul: {toRun.Count} db találat.");
                await ExecuteCaptureAsync(token);
                
                // Wait until the minute passes
                while (DateTime.Now.Minute == now.Minute && !token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);
                }
                continue;
            }

            // Sleep 1 second before checking again
            try
            {
                await Task.Delay(1000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExecuteCaptureAsync(CancellationToken token)
    {
        try
        {
            if (_settings.IdleCheckEnabled)
            {
                var idleTime = await _idleService.GetIdleTimeAsync(token);
                var idleThreshold = TimeSpan.FromMinutes(_settings.IdleThresholdMinutes);
                if (idleTime.HasValue && idleTime.Value < idleThreshold)
                {
                    _onLog($"A felhasználó csak {idleTime.Value.TotalMinutes:F1} perce tétlen (határ: {_settings.IdleThresholdMinutes} perc), képkészítés kihagyva.");
                    return;
                }

                if (!idleTime.HasValue)
                {
                    _onLog("Nem sikerült lekérni az inaktivitási időt, a képkészítés a beállított időpontban folytatódik.");
                }
            }

            var path = await _screenshotService.CaptureAsync(_settings, "Kép", forceStayForeground: false, token);
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
        {
            _onLog($"Hiba az automatikus képkészítés során: {ex.Message}");
        }
    }
}
