using ShiftSoftware.TypeAuth.Core.Actions;

// TypeAuth's Action collides with System.Action under ImplicitUsings; alias rather than
// fully-qualify every occurrence.
using TypeAuthAction = ShiftSoftware.TypeAuth.Core.Actions.Action;

namespace ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;

/// <summary>
/// TypeAuth action overrides for the surfaces the module exposes on both sides of the wire —
/// controllers on the API, the golden list and steward queue in Blazor. Each property, when set,
/// replaces the corresponding <see cref="DarlasticActionTree"/> action as the gate on that surface;
/// null falls back to the module's own action.
/// </summary>
/// <remarks>
/// The point is to make authorization switchable for a host that already describes customers in its
/// own action tree. Requiring it to adopt a second tree first is the reason authorization stays off
/// — which is how every Darlastic host currently runs, so the surfaces are authentication-only.
///
/// Each side configures its own instance (<c>DarlasticApiOptions.Actions</c> and
/// <c>DarlasticWebOptions.Actions</c>), so point both at the same actions: a UI gated more loosely
/// than the API hands the user buttons that 403, and one gated more tightly hides work the user is
/// allowed to do.
///
/// <para>The read/write surfaces are typed as the base <see cref="Action"/>, not
/// <see cref="ReadWriteDeleteAction"/>, so a host can supply whichever action type expresses its
/// policy — a <see cref="ReadAction"/> for a registry it treats as read-only, for instance. The
/// gates ask <c>Can(action, Access.Read|Write)</c>, which is defined for every action type; a level
/// the supplied action does not carry simply reads as denied.</para>
/// </remarks>
public class DarlasticActionOverrides
{
    /// <summary>Gate on the golden-customer read surfaces (list, detail, provenance).
    /// Default <see cref="DarlasticActionTree.GoldenCustomers"/>.</summary>
    public TypeAuthAction? GoldenCustomers { get; set; }

    /// <summary>Gate on the steward queue and the case browser behind it — Read to see cases,
    /// Write to record verdicts. Default <see cref="DarlasticActionTree.StewardQueue"/>.</summary>
    public TypeAuthAction? StewardQueue { get; set; }

    /// <summary>Gate on the golden grid's export button.
    /// Default <see cref="DarlasticActionTree.ExportGoldenCustomers"/>.</summary>
    public BooleanAction? ExportGoldenCustomers { get; set; }

    /// <summary><see cref="GoldenCustomers"/> or the module's own action.</summary>
    public TypeAuthAction ResolvedGoldenCustomers => GoldenCustomers ?? DarlasticActionTree.GoldenCustomers;

    /// <summary><see cref="StewardQueue"/> or the module's own action.</summary>
    public TypeAuthAction ResolvedStewardQueue => StewardQueue ?? DarlasticActionTree.StewardQueue;

    /// <summary><see cref="ExportGoldenCustomers"/> or the module's own action.</summary>
    public BooleanAction ResolvedExportGoldenCustomers =>
        ExportGoldenCustomers ?? DarlasticActionTree.ExportGoldenCustomers;
}
