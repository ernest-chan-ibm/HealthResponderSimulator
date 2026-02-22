using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace HealthTriageAgent.Plugins;

/// <summary>
/// Kernel functions (tools) available to the HealthTriageAgent.
/// Each method decorated with [KernelFunction] is automatically discoverable
/// and invocable by the agent to assist with health triage.
/// </summary>
public class HealthTriagePlugin
{
    /// <summary>
    /// Contacts a virtual physician generalist for medical advice.
    /// </summary>
    [KernelFunction(nameof(ContactVirtualPhysician))]
    [Description("Inquiring medical advice from a physician generalist")]
    public string ContactVirtualPhysician(
        [Description("A summary of the symptoms and context to present to the physician")] string symptoms)
    {
        // TODO: Implement — e.g. call a physician API or AI specialist model
        //await Task.CompletedTask;
        return $"[Placeholder] Virtual physician contacted with symptoms: {symptoms}";
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
