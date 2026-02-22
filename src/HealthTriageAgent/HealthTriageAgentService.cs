using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using HealthTriageAgent.Plugins;

namespace HealthTriageAgent;

/// <summary>
/// Core agent service. Orchestrates health triage using a Semantic Kernel
/// ChatCompletionAgent with a persistent conversation thread so that new
/// symptoms can be added at any time without losing context.
/// </summary>
public class HealthTriageAgentService
{
    private const string AgentName = "HealthTriageAgent";

    private const string AgentInstructions = """
        You are a calm and composed health triage assistant.

        Your role:
        - You have basic health knowledge, but you are NOT a doctor and do NOT provide medical advice.
        - You must always remain calm, measured, and reassuring in tone, regardless of how urgent or alarming the reported symptoms are.
        - Symptoms provided by the user will be scarce and may come in over time. You should not wait for all symptoms to be reported before taking action if you determine that the situation is urgent.
        - Users will not continue to provide new symptoms, you need to be assertive and decide what actions to take based on the information and tools at your disposal.
        - If the symptoms provided by the user is insignificant and does not warrant any action, you should reassure the user that they are likely fine. 
        - Your goal is reduce the risk of a major medical emergency based on the limited information you have by using the tools available to you, which include contacting a virtual physician generalist for advice and calling emergency services to dispatch a unit to the user's location.
        - If you receive new symptoms from the users, you should update your assessment and actions accordingly. Do not wait for all symptoms to be reported before taking action if you determine that the situation is urgent.
        - Before you perform any action, you should explain your reasoning to the user in a clear and reassuring manner, so they understand why you are taking that action. Get a confirmation from the user before any action is taken. 
        """;

    private readonly Kernel _kernel;

    public HealthTriageAgentService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║       Health Triage Agent  🏥         ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("  Describe your symptoms. New symptoms can be added at any time.");
        Console.WriteLine("  Type 'quit' to exit.");
        Console.WriteLine();

        // Register plugins
        _kernel.Plugins.AddFromType<HealthTriagePlugin>("HealthTriage");

        // Build agent
        var agent = new ChatCompletionAgent
        {
            Name = AgentName,
            Instructions = AgentInstructions,
            Kernel = _kernel,
        };

        // Persistent thread — holds the full symptom history across turns
        var thread = new ChatHistoryAgentThread();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("You: ");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;
            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Agent: ");
            Console.ResetColor();

            await foreach (var response in agent.InvokeStreamingAsync(input, thread))
            {
                Console.Write(response.Message.Content);
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        Console.WriteLine("  Session ended. Stay safe.");
    }
}

