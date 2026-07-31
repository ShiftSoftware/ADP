using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Shared.Tests;

public class DarlasticActionOverridesTests
{
    [Fact]
    public void Unset_FallsBackToModuleTree()
    {
        var overrides = new DarlasticActionOverrides();

        Assert.Same(DarlasticActionTree.GoldenCustomers, overrides.ResolvedGoldenCustomers);
        Assert.Same(DarlasticActionTree.StewardQueue, overrides.ResolvedStewardQueue);
        Assert.Same(DarlasticActionTree.ExportGoldenCustomers, overrides.ResolvedExportGoldenCustomers);
    }

    [Fact]
    public void Set_ReplacesModuleAction()
    {
        var hostAction = new ReadWriteDeleteAction("Host Golden Customers");
        var overrides = new DarlasticActionOverrides { GoldenCustomers = hostAction };

        Assert.Same(hostAction, overrides.ResolvedGoldenCustomers);
    }

    [Fact]
    public void Set_LeavesTheOtherSurfacesAlone()
    {
        var overrides = new DarlasticActionOverrides
        {
            GoldenCustomers = new ReadWriteDeleteAction("Host Golden Customers"),
        };

        Assert.Same(DarlasticActionTree.StewardQueue, overrides.ResolvedStewardQueue);
        Assert.Same(DarlasticActionTree.ExportGoldenCustomers, overrides.ResolvedExportGoldenCustomers);
    }

    /// <summary>
    /// The read/write overrides are typed as the base action precisely so a host that treats the
    /// registry as read-only can gate it on a <see cref="ReadAction"/> — the module's own default is
    /// a <see cref="ReadWriteDeleteAction"/>, and requiring that shape back would force such a host
    /// to widen its own tree to satisfy the module.
    /// </summary>
    [Fact]
    public void Set_AcceptsAnActionTypeNarrowerThanTheModuleDefault()
    {
        var readOnlyHostAction = new ReadAction("Host Golden Customers (read-only)");
        var overrides = new DarlasticActionOverrides { GoldenCustomers = readOnlyHostAction };

        Assert.Same(readOnlyHostAction, overrides.ResolvedGoldenCustomers);
    }

    [Fact]
    public void Export_IsABooleanAction()
    {
        // CanAccess resolves booleans through Access.Maximum; a Read/Write-shaped action here would
        // silently never satisfy the export gate.
        Assert.Equal(ActionType.Boolean, DarlasticActionTree.ExportGoldenCustomers.Type);
    }
}
