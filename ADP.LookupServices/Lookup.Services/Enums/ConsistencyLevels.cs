using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The data consistency level for lookup queries.
/// </summary>
[Docable]
public enum ConsistencyLevels
{
    /// <summary>Strong consistency — always reads the latest data (higher latency).</summary>
    Strong = 1,
    /// <summary>Eventual consistency — may read slightly stale data (lower latency).</summary>
    Eventual = 2,
}