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

    /// <summary>
    /// Whether the golden grid offers its export button. A grid export is the tenant's whole
    /// customer base — name, phone, email, national ID — in one file, which is a different decision
    /// from reading the grid a page at a time, so it is a grant of its own rather than something
    /// every reader of <see cref="GoldenCustomers"/> inherits.
    ///
    /// <para>Presentation, not protection: a caller holding Read on <see cref="GoldenCustomers"/>
    /// can page the OData feed directly whatever this says. What it removes is the one-click bulk
    /// convenience, which is what a host asking to "hide export on golden" is asking for.</para>
    ///
    /// <para>Booleans resolve through <c>Access.Maximum</c> — a Read grant does not turn one on.</para>
    /// </summary>
    public readonly static BooleanAction ExportGoldenCustomers = new("Export Golden Customers");
}
