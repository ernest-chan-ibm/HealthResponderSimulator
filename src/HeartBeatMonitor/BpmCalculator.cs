namespace HeartBeatMonitor;

/// <summary>
/// Derives BPM from a sliding window of beat arrival timestamps.
/// No BPM is assumed — it is calculated purely from how fast beats arrive.
/// </summary>
public class BpmCalculator
{
    private readonly int _windowSize;
    private readonly Queue<DateTimeOffset> _timestamps = new();

    public BpmCalculator(int windowSize = 10)
    {
        _windowSize = windowSize;
    }

    private double? _lastBpm;

    /// <summary>
    /// Records a beat and updates the internal BPM calculation.
    /// </summary>
    public void RecordBeat(DateTimeOffset timestamp)
    {
        _timestamps.Enqueue(timestamp);

        while (_timestamps.Count > _windowSize)
            _timestamps.Dequeue();

        if (_timestamps.Count < 2)
        {
            _lastBpm = null;
            return;
        }

        var samples = _timestamps.ToArray();
        var totalMs = (samples[^1] - samples[0]).TotalMilliseconds;
        var avgIntervalMs = totalMs / (samples.Length - 1);
        _lastBpm = 60_000.0 / avgIntervalMs;
    }

    /// <summary>
    /// Returns the most recently calculated BPM, or null if not enough samples yet.
    /// </summary>
    public double? GetCurrentBpm() => _lastBpm;

    public string Status(double bpm) => bpm switch
    {
        < 60   => "Bradycardia",
        <= 100 => "Normal",
        _      => "Tachycardia"
    };
}
