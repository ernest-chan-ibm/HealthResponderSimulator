using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using HeartBeatSimulator.Models;

namespace HeartBeatSimulator;

public class HeartBeatSimulatorService
{
    private const int DefaultBpm = 72;

    private readonly EventGridSenderClient? _client;
    private readonly string _personId;
    private CancellationTokenSource _cts = new();
    private Task? _simulationTask;
    private volatile int _bpm = DefaultBpm;
    private int _beatCount = 0;

    public int BeatsPerMinute
    {
        get => _bpm;
        set
        {
            _bpm = value;
            Console.WriteLine($"  -> Heart rate updated to {value} BPM ({BpmStatus(value)})");
        }
    }

    public HeartBeatSimulatorService(EventGridSenderClient? client, string personId)
    {
        _client = client;
        _personId = personId;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _simulationTask = SimulateAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
        try { _simulationTask?.Wait(TimeSpan.FromSeconds(3)); } catch { }
    }

    private async Task SimulateAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var currentBpm = _bpm;
            var interval = TimeSpan.FromSeconds(60.0 / currentBpm);
            var beatNumber = Interlocked.Increment(ref _beatCount);

            var data = new HeartBeatData
            {
                PersonId = _personId,
                BeatNumber = beatNumber,
                Timestamp = DateTimeOffset.UtcNow
            };

            try
            {
                if (_client is not null)
                {
                    var cloudEvent = new CloudEvent(
                        source: $"/heartbeat/{_personId}",
                        type: "HeartBeatSimulator.HeartBeat",
                        jsonSerializableData: data);

                    await _client.SendAsync(cloudEvent, cancellationToken);
                }

                Console.WriteLine(
                    $"[{data.Timestamp:HH:mm:ss.fff}] Beat #{beatNumber:D5}" +
                    (_client is null ? " [dry run]" : string.Empty));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [!] Failed to publish event: {ex.Message}");
                Console.ResetColor();
            }

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static string BpmStatus(int bpm) => bpm switch
    {
        < 60 => "Bradycardia",
        <= 100 => "Normal",
        _ => "Tachycardia"
    };
}
