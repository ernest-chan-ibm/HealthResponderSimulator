using Azure.Messaging.EventGrid.Namespaces;
using HeartBeatMonitor.Models;

namespace HeartBeatMonitor;

public class HeartBeatMonitorService
{
    private readonly EventGridReceiverClient? _receiver;
    private readonly BpmCalculator _calculator = new(windowSize: 10);
    private CancellationTokenSource _cts = new();
    private Task? _beatTask;
    private Task? _displayTask;

    // Dry-run simulation state
    private volatile int _simulatedBpm = 72;
    public int SimulatedBpm
    {
        get => _simulatedBpm;
        set => _simulatedBpm = value;
    }

    public HeartBeatMonitorService(EventGridReceiverClient? receiver)
    {
        _receiver = receiver;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _beatTask = _receiver is null
            ? RunDryRunAsync(_cts.Token)
            : RunReceiverAsync(_cts.Token);
        _displayTask = RunDisplayAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
        try { Task.WaitAll([_beatTask!, _displayTask!], TimeSpan.FromSeconds(3)); } catch { }
    }

    // ── Real Event Grid pull loop ──────────────────────────────────────────
    private async Task RunReceiverAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _receiver!.ReceiveAsync(
                    maxEvents: 100,
                    maxWaitTime: TimeSpan.FromSeconds(10),
                    cancellationToken: cancellationToken);

                var lockTokens = new List<string>();

                foreach (var detail in result.Value.Details)
                {
                    lockTokens.Add(detail.BrokerProperties.LockToken);

                    if (detail.Event?.Data?.ToObjectFromJson<HeartBeatData>() is { } data)
                        _calculator.RecordBeat(data.Timestamp);
                }

                if (lockTokens.Count > 0)
                    await _receiver.AcknowledgeAsync(lockTokens, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [!] Receive error: {ex.Message}");
                Console.ResetColor();

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ContinueWith(_ => { });
            }
        }
    }

    // ── Dry-run simulation loop ────────────────────────────────────────────
    private async Task RunDryRunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromSeconds(60.0 / _simulatedBpm);

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _calculator.RecordBeat(DateTimeOffset.UtcNow);
        }
    }

    // ── 5-second periodic display loop ────────────────────────────────────
    private async Task RunDisplayAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var calculatedBpm = _calculator.GetCurrentBpm();
            var bpmDisplay = calculatedBpm.HasValue
                ? $"{calculatedBpm.Value:F1} BPM  [{_calculator.Status(calculatedBpm.Value)}]"
                : "-- BPM  [calculating...]";

            Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss}] {bpmDisplay}");
        }
    }
}
