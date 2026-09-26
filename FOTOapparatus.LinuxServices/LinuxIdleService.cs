using FOTOapparatus.Core.Interfaces;

namespace FOTOapparatus.LinuxServices;

public sealed class LinuxIdleService : IIdleService
{
    private readonly IProcessRunner _processRunner;

    public LinuxIdleService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<TimeSpan?> GetIdleTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _processRunner.RunAsync(
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
