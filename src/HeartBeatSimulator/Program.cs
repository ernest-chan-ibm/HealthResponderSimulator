using Azure;
using Azure.Messaging.EventGrid.Namespaces;
using HeartBeatSimulator;

// ── Configuration ──────────────────────────────────────────────────────────
var namespaceEndpoint = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_ENDPOINT");
var topicName         = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_TOPIC") ?? "heartbeats";
var namespaceKey      = Environment.GetEnvironmentVariable("EVENTGRID_NAMESPACE_KEY");
var personId          = Environment.GetEnvironmentVariable("HEARTBEAT_PERSON_ID")
    ?? Guid.NewGuid().ToString("N")[..8];

bool dryRun = string.IsNullOrWhiteSpace(namespaceEndpoint) || string.IsNullOrWhiteSpace(namespaceKey);

// ── Startup banner ─────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║       HeartBeat Simulator  ♥         ║");
Console.WriteLine("╚══════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine($"  Person ID : {personId}");
if (dryRun)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Mode      : DRY RUN (set EVENTGRID_NAMESPACE_ENDPOINT + EVENTGRID_NAMESPACE_KEY to publish for real)");
    Console.ResetColor();
}
else
{
    Console.WriteLine($"  Endpoint  : {namespaceEndpoint}");
    Console.WriteLine($"  Topic     : {topicName}");
}
Console.WriteLine();
Console.WriteLine("  Commands:");
Console.WriteLine("    set <bpm>   — change heart rate (e.g. set 90)");
Console.WriteLine("    status      — show current BPM and beat count");
Console.WriteLine("    quit        — stop the simulator");
Console.WriteLine();

// ── Build EventGrid Namespaces sender client (null in dry-run mode) ────
EventGridSenderClient? client = dryRun
    ? null
    : new EventGridSenderClient(new Uri(namespaceEndpoint!), topicName, new AzureKeyCredential(namespaceKey!));

// ── Start simulator ────────────────────────────────────────────────────────
var simulator = new HeartBeatSimulatorService(client, personId);
simulator.Start();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"  Simulating at {simulator.BeatsPerMinute} BPM (Normal resting rate). Ready for commands.");
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

    if (input.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
    {
        var arg = input[4..].Trim();
        if (int.TryParse(arg, out var bpm) && bpm is >= 1 and <= 300)
        {
            simulator.BeatsPerMinute = bpm;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  [!] Invalid BPM. Enter a value between 1 and 300.");
            Console.ResetColor();
        }
        continue;
    }

    if (input.Equals("status", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"  Current rate : {simulator.BeatsPerMinute} BPM");
        continue;
    }

    if (!string.IsNullOrEmpty(input))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  [!] Unknown command. Use: set <bpm> | status | quit");
        Console.ResetColor();
    }
}

// ── Shutdown ───────────────────────────────────────────────────────────────
simulator.Stop();
Console.WriteLine();
Console.WriteLine("  Simulator stopped. Goodbye.");

