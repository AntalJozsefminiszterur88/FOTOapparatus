using System.IO.Pipes;

namespace FOTOapparatus.UI;

internal sealed class SingleInstanceService : IDisposable
{
    private const string PipeName = "FOTOapparatusPipe_Linux";
    private const string ShowMessage = "SHOW";

    private readonly Action _showRequested;
    private readonly Action<Exception>? _onError;
    private readonly CancellationTokenSource _cancellationSource = new();
    private Task? _serverTask;

    public SingleInstanceService(Action showRequested, Action<Exception>? onError = null)
    {
        _showRequested = showRequested;
        _onError = onError;
    }

    public static bool TryNotifyExistingInstance(int timeoutMilliseconds = 2000)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeoutMilliseconds);
            using var writer = new StreamWriter(client);
            writer.WriteLine(ShowMessage);
            writer.Flush();
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            return false;
        }
    }

    public void Start()
    {
        if (_serverTask is not null)
        {
            return;
        }

        RemoveStaleUnixPipe();
        _serverTask = Task.Run(() => RunServerAsync(_cancellationSource.Token));
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }

    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync(cancellationToken);
                if (string.Equals(message, ShowMessage, StringComparison.Ordinal))
                {
                    _showRequested();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private static void RemoveStaleUnixPipe()
    {
        if (!OperatingSystem.IsWindows())
        {
            var pipePath = Path.Combine(Path.GetTempPath(), $"CoreFxPipe_{PipeName}");
            try
            {
                File.Delete(pipePath);
            }
            catch (FileNotFoundException)
            {
                // Nothing to clean up.
            }
        }
    }
}
