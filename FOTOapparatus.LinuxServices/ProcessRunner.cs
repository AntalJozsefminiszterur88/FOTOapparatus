using System.Diagnostics;

namespace FOTOapparatus.LinuxServices;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        int timeoutMs = 15000,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        int timeoutMs = 15000,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Intentionally ignored.
            }

            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }
}

public readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
