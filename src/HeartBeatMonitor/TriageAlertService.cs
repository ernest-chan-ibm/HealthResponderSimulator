using System.Net.Http.Json;

namespace HeartBeatMonitor;

/// <summary>
/// Forwards incident reports to the HealthTriageAgent process via HTTP.
/// The agent process must be running and listening on the configured endpoint.
/// Set TRIAGE_AGENT_URL to override the default http://localhost:5100/triage/incident
/// </summary>
public static class TriageAlertService
{
    private static readonly string AgentEndpoint =
        Environment.GetEnvironmentVariable("TRIAGE_AGENT_URL")
        ?? "http://localhost:5100/triage/incident";

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>
    /// Prints the alert locally and forwards the incident report to the
    /// HealthTriageAgent process over HTTP.
    /// </summary>
    public static async Task FireIncidentAsync(string incidentReport)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("══════════════════════════════════════════════════");
        Console.WriteLine("  !! HEALTH TRIAGE ALERT — IRREGULAR HEARTBEAT  !!");
        Console.WriteLine("══════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine(incidentReport);
        Console.WriteLine();

        try
        {
            var payload  = new { report = incidentReport };
            var response = await _http.PostAsJsonAsync(AgentEndpoint, payload);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [Triage] Incident forwarded to HealthTriageAgent (HTTP {(int)response.StatusCode}).");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  [Triage] Could not reach HealthTriageAgent: {ex.Message}");
            Console.WriteLine($"  [Triage] Ensure HealthTriageAgent is running on {AgentEndpoint}");
            Console.ResetColor();
        }

        Console.WriteLine("══════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
