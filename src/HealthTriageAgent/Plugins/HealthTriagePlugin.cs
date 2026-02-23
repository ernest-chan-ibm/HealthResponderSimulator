using System.ComponentModel;
using Microsoft.SemanticKernel;
using HealthTriageAgent.Agents;

namespace HealthTriageAgent.Plugins;

/// <summary>
/// Kernel functions (tools) available to the HealthTriageAgent.
/// Each method decorated with [KernelFunction] is automatically discoverable
/// and invocable by the agent to assist with health triage.
/// </summary>
public class HealthTriagePlugin
{
    /// <summary>
    /// Contacts a virtual physician generalist for a diagnostic consultation.
    /// The Kernel parameter is injected automatically by Semantic Kernel and is
    /// not visible to the AI as a callable argument.
    /// </summary>
    [KernelFunction(nameof(ContactVirtualPhysician))]
    [Description("Inquiring medical advice from a physician generalist")]
    public async Task<string> ContactVirtualPhysician(
        Kernel kernel,
        [Description("A summary of the symptoms and context to present to the physician")] string symptoms)
    {
        var physician = new VirtualPhysicianAgent(kernel);
        return await physician.ConsultAsync(symptoms);
    }


    [KernelFunction(nameof(CallEmergencyServices))]
    [Description("Call emergency services to dispatch a unit to the user's location")]
    public string CallEmergencyServices(
        [Description("The type of unit, e.g., Ambulance, FireTruck")] string unitType,
        [Description("The street address or coordinates")] string location)
    {
        // Your logic to update the simulation state goes here
        return $"Confirmed: {unitType} is en route to {location}.";
    }
}
