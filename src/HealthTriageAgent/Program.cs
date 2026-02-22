using Microsoft.SemanticKernel;
using HealthTriageAgent;

// ── Configuration ──────────────────────────────────────────────────────────
var modelId       = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL")       ?? "gpt-4o";
var endpoint      = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")    ?? string.Empty;
var apiKey        = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")     ?? string.Empty;

// ── Build Kernel ─────────────────────────────────────────────────────────────────
var builder = Kernel.CreateBuilder();

if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
    builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
else
    Console.WriteLine("[!] AZURE_OPENAI_ENDPOINT / AZURE_OPENAI_API_KEY not set — running without LLM.");

var kernel = builder.Build();

// ── Start agent ─────────────────────────────────────────────────────────────────
var agent = new HealthTriageAgentService(kernel);
await agent.RunAsync();

