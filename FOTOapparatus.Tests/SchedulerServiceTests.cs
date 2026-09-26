using FOTOapparatus.Core;
using FOTOapparatus.Core.Interfaces;
using FOTOapparatus.Core.Models;

namespace FOTOapparatus.Tests;

public sealed class SchedulerServiceTests
{
    [Fact]
    public async Task Start_WhenAlreadyRunning_IsIdempotent()
    {
        var logMessages = new List<string>();
        var service = new SchedulerService(
            new NoOpScreenshotService(),
            new NoOpIdleService(),
            new AppSettings(),
            logMessages.Add);

        service.Start();
        service.Start();
        await service.StopAsync();

        Assert.Equal(1, logMessages.Count(message => message == "Időzítő elindítva."));
        Assert.Equal(1, logMessages.Count(message => message == "Időzítő leállítva."));
    }

    private sealed class NoOpScreenshotService : IScreenshotService
    {
        public Task<string?> CaptureSelectionPreviewAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> CaptureAsync(
            AppSettings settings,
            string filenamePrefix,
            bool forceStayForeground = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class NoOpIdleService : IIdleService
    {
        public Task<TimeSpan?> GetIdleTimeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<TimeSpan?>(null);
    }
}
