namespace HeartBeatMonitor.Models;

/// <summary>
/// Raw heartbeat event payload received from the Event Grid topic.
/// Does not contain BPM — that is derived from the timing of received beats.
/// </summary>
public class HeartBeatData
{
    public string PersonId { get; set; } = string.Empty;
    public int BeatNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
