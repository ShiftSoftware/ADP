using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;

[ActionTree("Darlastic", "Darlastic (Centralized Customers) Module Permissions")]
public class DarlasticActionTree
{
    /// <summary>Read = the golden list/search surfaces. Write/Delete are reserved for the
    /// steward split / override slices that follow CC6 — nothing grants them yet.</summary>
    public readonly static ReadWriteDeleteAction GoldenCustomers = new("Golden Customers");

    /// <summary>
    /// The stewardship queue. Read = see the cases; Write = record verdicts that constrain the
    /// engine's clustering on every later resolve. Held apart from <see cref="GoldenCustomers"/>
    /// because seeing the golden list and being trusted to change how identities resolve are
    /// different grants — most operators want the first and should not have the second.
    /// </summary>
    public readonly static ReadWriteDeleteAction StewardQueue = new("Steward Queue");
}
