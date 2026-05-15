using FOTOapparatus.Core.Interfaces;

namespace FOTOapparatus.LinuxServices;

public sealed class LinuxIdleService : IIdleService
{
    public async Task<TimeSpan?> GetIdleTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ProcessRunner.RunAsync(
                "xprintidle",
                [],
                cancellationToken: cancellationToken);

            if (result.Succeeded && long.TryParse(result.StandardOutput.Trim(), out var idleMs))
            {
                return TimeSpan.FromMilliseconds(idleMs);
            }
        }
        catch
        {
            // Ignored
        }

        return null;
    }
}
