using Microsoft.SemanticKernel;
using HealthTriageAgent;
using HealthTriageAgent.Agents;

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

// ── Start HTTP incident server (background) ────────────────────────────────────
var cts    = new CancellationTokenSource();
var server = new TriageHttpServer(kernel);
var httpTask = server.StartAsync(cts.Token);

// ── Start interactive console agent (foreground) ────────────────────────────────
var agent = new HealthTriageAgent.Agents.HealthTriageAgent(kernel);
await agent.RunAsync();

// ── Shutdown ─────────────────────────────────────────────────────────────────────
cts.Cancel();
await httpTask;

