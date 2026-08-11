namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Identifies the semantic role of a catalog service item within a service program.
/// This classification is orthogonal to whether an evaluated item is free or paid.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
[Docable]
public enum ServiceItemProgramRole
{
    /// <summary>A normal scheduled-service item that may contribute to the base schedule cap.</summary>
    [System.ComponentModel.Description("Scheduled service (contributes to the base schedule cap when otherwise eligible)")]
    ScheduledService = 0,

    /// <summary>A reward item that keeps normal free-item lifecycle behavior without defining the base schedule cap.</summary>
    [System.ComponentModel.Description("Reward (keeps normal free-item lifecycle behavior but does not define the base schedule cap)")]
    Reward = 1
}
