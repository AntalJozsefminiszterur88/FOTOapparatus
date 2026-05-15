using System.IO.Pipes;
using System.Text;

namespace FOTOapparatus.Avalonia.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenerTask;

    public SingleInstanceService(string appName)
    {
        _mutexName = $"{appName}_Mutex";
        _pipeName = $"{appName}_Pipe";
        _mutex = new Mutex(true, _mutexName, out var createdNew);
        IsPrimaryInstance = createdNew;
    }

    public bool IsPrimaryInstance { get; }

    public void StartListening(Action showRequestHandler)
    {
        if (!IsPrimaryInstance || _listenerTask is not null)
        {
            return;
        }

        _listenerTask = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(_cts.Token);
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var message = await reader.ReadLineAsync(_cts.Token);
                    if (string.Equals(message, "show", StringComparison.OrdinalIgnoreCase))
                    {
                        showRequestHandler();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(250, _cts.Token);
                }
            }
        });
    }

    public void NotifyPrimaryInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(400);
            using var writer = new StreamWriter(client, Encoding.UTF8)
            {
                AutoFlush = true,
            };
            writer.WriteLine("show");
        }
        catch
        {
            // Intentionally ignored. Secondary instance still exits.
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listenerTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Intentionally ignored during shutdown.
        }

        if (IsPrimaryInstance)
        {
            _mutex.ReleaseMutex();
        }

        _cts.Dispose();
        _mutex.Dispose();
    }
}
