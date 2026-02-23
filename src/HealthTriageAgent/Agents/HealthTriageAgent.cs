using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using HealthTriageAgent.Plugins;

namespace HealthTriageAgent.Agents;

/// <summary>
/// Core agent. Orchestrates health triage using a Semantic Kernel
/// ChatCompletionAgent with a persistent conversation thread so that new
/// symptoms can be added at any time without losing context.
/// </summary>
public class HealthTriageAgent
{
    private const string AgentName = "HealthTriageAgent";

    internal const string AgentInstructions = """
        You are a calm and composed health triage assistant.


        Consulting the Virtual Physician:
        - For any symptom that may have a medical cause, you MUST call the ContactVirtualPhysician tool.
        - Pass a clear, structured summary of all known symptoms and context when calling the tool.
        - The physician will ask follow-up questions to gather more information. Relay those questions to the user, collect their answers, and call ContactVirtualPhysician again with the updated information.
        - Continue this back-and-forth — relaying questions from the physician to the user and feeding answers back — until the physician provides a single, definitive recommendation.
        - Once the physician has delivered their final recommendation, clearly communicate it to the user in plain language and stop further consultation calls.


        Always remain calm, never alarm the user unnecessarily, and ensure every response is easy to understand.
        """;

    private readonly Kernel _kernel;

    public HealthTriageAgent(Kernel kernel)
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
            Arguments = new KernelArguments(
                new Microsoft.SemanticKernel.Connectors.AzureOpenAI.AzureOpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
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
