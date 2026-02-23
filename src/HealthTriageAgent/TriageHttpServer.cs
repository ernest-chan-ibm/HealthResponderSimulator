using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;using HealthTriageAgent.Agents;using HealthTriageAgent.Plugins;

namespace HealthTriageAgent;

/// <summary>
/// Lightweight HTTP server that receives incident reports from external processes
/// (e.g. HeartBeatMonitor) and routes them through the triage agent.
/// Listens on http://localhost:5100/triage/incident  (POST)
/// </summary>
public class TriageHttpServer
{
    public const string DefaultUrl = "http://localhost:5100/";
    public const string IncidentPath = "/triage/incident";

    private const string AgentName = "HealthTriageAgent";
    private const string AgentInstructions = Agents.HealthTriageAgent.AgentInstructions;

    private readonly Kernel _kernel;
    private readonly HttpListener _listener = new();

    public TriageHttpServer(Kernel kernel, string url = DefaultUrl)
    {
        _kernel = kernel;
        _listener.Prefixes.Add(url);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  [HTTP] Triage server listening on {DefaultUrl}");
        Console.ResetColor();

        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }

            // Handle each request on a background task so the listener stays responsive
            _ = Task.Run(() => HandleRequestAsync(ctx), cancellationToken);
        }

        _listener.Stop();
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var req  = ctx.Request;
        var resp = ctx.Response;

        if (req.HttpMethod != "POST" ||
            !req.Url!.AbsolutePath.Equals(IncidentPath, StringComparison.OrdinalIgnoreCase))
        {
            resp.StatusCode = 404;
            resp.Close();
            return;
        }

        string body;
        using (var reader = new System.IO.StreamReader(req.InputStream, req.ContentEncoding))
            body = await reader.ReadToEndAsync();

        string report;
        try
        {
            var doc = JsonDocument.Parse(body);
            report = doc.RootElement.GetProperty("report").GetString() ?? body;
        }
        catch
        {
            report = body;
        }

        resp.StatusCode = 202; // Accepted
        resp.Close();

        // Process the incident through the triage agent
        await ProcessIncidentAsync(report);
    }

    private async Task ProcessIncidentAsync(string report)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("══════════════════════════════════════════════════");
        Console.WriteLine("  !! INCOMING INCIDENT FROM HEARTBEAT MONITOR  !!");
        Console.WriteLine("══════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine(report);
        Console.WriteLine();

        try
        {
            var agent = new ChatCompletionAgent
            {
                Name = AgentName,
                Instructions = AgentInstructions,
                Kernel = _kernel,
                Arguments = new KernelArguments(
                    new Microsoft.SemanticKernel.Connectors.AzureOpenAI.AzureOpenAIPromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                    }),
            };

            var thread = new ChatHistoryAgentThread();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Agent: ");
            Console.ResetColor();

            await foreach (var chunk in agent.InvokeStreamingAsync(report, thread))
                Console.Write(chunk.Message.Content);

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [!] Agent error: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("══════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
