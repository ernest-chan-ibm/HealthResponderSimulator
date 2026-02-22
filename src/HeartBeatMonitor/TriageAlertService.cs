using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace HeartBeatMonitor;

/// <summary>
/// Fires a single-shot call to the HealthTriage agent when an irregular
/// heartbeat incident needs to be escalated.
/// </summary>
public static class TriageAlertService
{
    private const string AgentInstructions = """
        You are a calm and composed health triage assistant.

        Your role:
        - You have basic health knowledge, but you are NOT a doctor and do NOT provide medical advice.
        - You must always remain calm, measured, and reassuring in tone, regardless of how urgent or alarming the reported symptoms are.
        - Symptoms may arrive incrementally over multiple messages. Accumulate and consider all reported symptoms together as new ones come in.
        - Use the tools and resources available in your repository to reason about the symptoms and identify the most likely health concerns.
        - Continuously explore new avenues to determine the best next steps, adapting as the picture becomes clearer.
        - Your goal is to guide the user toward the right experts or validated resources — never to diagnose or prescribe yourself.
        - Always tell the user what kind of medical professional or service they should seek, and how urgently, based on the symptoms presented.
        - If symptoms suggest a life-threatening emergency, calmly but clearly direct the user to call emergency services immediately.
        - Acknowledge each new symptom and explain how it changes or confirms your current assessment.
        """;

    /// <summary>
    /// Sends an incident report to the triage agent and streams the response to the console.
    /// Falls back to a dry-run print if Azure OpenAI env vars are not configured.
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

        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var apiKey   = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var model    = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL");

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  [Triage] Azure OpenAI not configured — skipping agent call (dry run).");
            Console.ResetColor();
            Console.WriteLine("══════════════════════════════════════════════════");
            Console.WriteLine();
            return;
        }

        try
        {
            var kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(model, endpoint, apiKey)
                .Build();

            var agent = new ChatCompletionAgent
            {
                Name = "HealthTriageAgent",
                Instructions = AgentInstructions,
                Kernel = kernel,
            };

            var thread = new ChatHistoryAgentThread();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Agent: ");
            Console.ResetColor();

            await foreach (var response in agent.InvokeStreamingAsync(incidentReport, thread))
                Console.Write(response.Message.Content);

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [Triage] Agent error: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("══════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
