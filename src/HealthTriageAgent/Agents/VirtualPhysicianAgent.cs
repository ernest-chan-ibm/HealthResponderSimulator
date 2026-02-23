using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace HealthTriageAgent.Agents;

/// <summary>
/// A specialist agent that acts as a virtual general practitioner.
/// Given an initial symptom report, it probes for additional context through
/// a focused multi-turn consultation, then converges on a single best
/// recommendation with clear reasoning.
/// </summary>
public class VirtualPhysicianAgent
{
    private const string PhysicianName = "VirtualPhysician";

    private const string PhysicianInstructions = """
        You are an experienced general practitioner conducting a focused clinical consultation.

        Your approach:
        1. GATHER — Review the initial symptom report carefully. Identify what is known and what critical
           information is still missing (duration, severity, onset, relevant history, medications, allergies, etc.).
        2. PROBE — Ask targeted follow-up questions to fill in the gaps. Ask only the most important questions
           first; do not overwhelm the patient. Wait for answers before proceeding.
        3. SYNTHESISE — Once you have enough information (or the patient cannot provide more), form your
           differential diagnosis. Reason through the most likely causes from most to least probable.
        4. RECOMMEND — Commit to a single best course of action. Be decisive. State clearly:
           - What you believe is most likely happening and why.
           - The single recommended next step (e.g., "Go to the emergency room now", "Book a GP appointment
             within 24 hours", "Rest and monitor — return if X or Y occurs").
           - Any immediate self-care steps the patient can take right now while following your recommendation.
           - Red-flag symptoms that should trigger immediate escalation to emergency services.

        Tone and style:
        - Speak directly and clearly, as a skilled doctor would to a patient.
        - Be compassionate but decisive — avoid vague hedging.
        - You may provide a medical assessment and a recommended course of action.
        - Always err on the side of caution for potentially serious conditions.
        - End every consultation with a clear one-sentence summary of your recommendation.
        """;

    private readonly Kernel _kernel;

    public VirtualPhysicianAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <summary>
    /// Runs an interactive consultation starting from the provided symptom report.
    /// Streams agent output to the console and loops until the physician signals
    /// the consultation is complete or the user has no further information to add.
    /// Returns a final summary of the recommendation.
    /// </summary>
    public async Task<string> ConsultAsync(string initialReport)
    {
        var agent = new ChatCompletionAgent
        {
            Name = PhysicianName,
            Instructions = PhysicianInstructions,
            Kernel = _kernel,
        };

        var thread = new ChatHistoryAgentThread();
        var fullResponse = new System.Text.StringBuilder();

        // ── First turn: send the initial report ──────────────────────────
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("  ┌─── Virtual Physician Consultation ───┐");
        Console.ResetColor();

        await StreamTurnAsync(agent, thread, initialReport, fullResponse);

        // ── Follow-up turns: let the physician ask questions ─────────────
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Patient: ");
            Console.ResetColor();

            var userInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(userInput) ||
                userInput.Equals("done", StringComparison.OrdinalIgnoreCase) ||
                userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await StreamTurnAsync(agent, thread, userInput, fullResponse);

            // If the physician has issued a final recommendation, end the loop
            var lastResponse = fullResponse.ToString();
            if (ContainsFinalRecommendation(lastResponse))
                break;
        }

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("  └─── Consultation complete ─────────────┘");
        Console.ResetColor();
        Console.WriteLine();

        return fullResponse.ToString();
    }

    private static async Task StreamTurnAsync(
        ChatCompletionAgent agent,
        ChatHistoryAgentThread thread,
        string message,
        System.Text.StringBuilder collector)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  Physician: ");
        Console.ResetColor();

        var turnText = new System.Text.StringBuilder();

        await foreach (var chunk in agent.InvokeStreamingAsync(message, thread))
        {
            var text = chunk.Message.Content ?? string.Empty;
            Console.Write(text);
            turnText.Append(text);
        }

        Console.WriteLine();
        collector.AppendLine(turnText.ToString());
    }

    /// <summary>
    /// Heuristic: detect when the physician has delivered a final recommendation
    /// so the consultation loop can close automatically.
    /// </summary>
    private static bool ContainsFinalRecommendation(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.Contains("my recommendation") ||
               lower.Contains("i recommend") ||
               lower.Contains("recommended next step") ||
               lower.Contains("in summary") ||
               lower.Contains("to summarise") ||
               lower.Contains("to summarize");
    }
}
