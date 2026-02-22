namespace HeartBeatSimulator.Models;

/// <summary>
/// Raw heartbeat event payload — contains only what a real heart signal would emit:
/// who it came from, which beat it is, and when. BPM is intentionally excluded;
/// observers must derive it from the timing of received events.
/// </summary>
public class HeartBeatData
{
    public string PersonId { get; set; } = string.Empty;
    public int BeatNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
