using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;

[ActionTree("Darlastic", "Darlastic (Centralized Customers) Module Permissions")]
public class DarlasticActionTree
{
    /// <summary>Read = the golden list/search surfaces. Write/Delete are reserved for the
    /// planned steward module (merge / split / overrides) — nothing grants them yet.</summary>
    public readonly static ReadWriteDeleteAction GoldenCustomers = new("Golden Customers");
}
