using Azure;
using Azure.Messaging.EventGrid.Namespaces;
using HeartBeatMonitor;

// ── Configuration ──────────────────────────────────────────────────────────
var namespaceEndpoint  = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_ENDPOINT");
var topicName          = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_TOPIC") ?? "heartbeats";
var subscriptionName   = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_SUBSCRIPTION") ?? "heartbeat-monitor";
var namespaceKey       = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_KEY");

bool dryRun = string.IsNullOrWhiteSpace(namespaceEndpoint) || string.IsNullOrWhiteSpace(namespaceKey);

// ── Startup banner ─────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║      HeartBeat Monitor  ♥  📈        ║");
Console.WriteLine("╚══════════════════════════════════════╝");
Console.ResetColor();

if (dryRun)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Mode         : DRY RUN — simulating beats locally");
    Console.WriteLine("  (Set EVENTGRID_NAMESPACE_ENDPOINT + EVENTGRID_NAMESPACE_KEY to receive real events)");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("  In dry-run mode you can adjust the simulated incoming rate:");
    Console.WriteLine("  Commands:");
    Console.WriteLine("    set <bpm>   — adjust simulated incoming beat rate");
    Console.WriteLine("    quit        — stop the monitor");
}
else
{
    Console.WriteLine($"  Endpoint     : {namespaceEndpoint}");
    Console.WriteLine($"  Topic        : {topicName}");
    Console.WriteLine($"  Subscription : {subscriptionName}");
    Console.WriteLine();
    Console.WriteLine("  Commands:  quit — stop the monitor");
}
Console.WriteLine();
Console.WriteLine("  BPM is derived from the timing between received heartbeat events.");
Console.WriteLine("  Status: Normal = 60-100 BPM | Bradycardia < 60 | Tachycardia > 100");
Console.WriteLine();

// ── Build Event Grid receiver client (null in dry-run mode) ───────────────
EventGridReceiverClient? receiver = dryRun
    ? null
    : new EventGridReceiverClient(
        new Uri(namespaceEndpoint!),
        topicName,
        subscriptionName,
        new AzureKeyCredential(namespaceKey!));

// ── Start monitor ──────────────────────────────────────────────────────────
var monitor = new HeartBeatMonitorService(receiver);
monitor.Start();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(dryRun
    ? "  Monitoring started (dry run). Waiting for beats..."
    : "  Monitoring started. Pulling events from Event Grid...");
Console.ResetColor();
Console.WriteLine();

// ── Command loop ───────────────────────────────────────────────────────────
while (true)
{
    var input = Console.ReadLine()?.Trim() ?? string.Empty;

    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (dryRun && input.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
    {
        var arg = input[4..].Trim();
        if (int.TryParse(arg, out var bpm) && bpm is >= 1 and <= 300)
        {
            monitor.SimulatedBpm = bpm;
            Console.WriteLine($"  -> Simulated incoming rate set to {bpm} BPM");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  [!] Invalid BPM. Enter a value between 1 and 300.");
            Console.ResetColor();
        }
        continue;
    }

    if (!string.IsNullOrEmpty(input))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(dryRun
            ? "  [!] Unknown command. Use: set <bpm> | quit"
            : "  [!] Unknown command. Use: quit");
        Console.ResetColor();
    }
}

// ── Shutdown ───────────────────────────────────────────────────────────────
monitor.Stop();
Console.WriteLine();
Console.WriteLine("  Monitor stopped. Goodbye.");

