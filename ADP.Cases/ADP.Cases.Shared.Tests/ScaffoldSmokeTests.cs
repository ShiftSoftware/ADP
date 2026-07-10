using Xunit;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

/// <summary>
/// Slice-1 scaffold smoke test — proves the test project builds and runs against the
/// Shared assembly. Replaced by the engine characterization + workflow tests in Slice 2.
/// </summary>
public class ScaffoldSmokeTests
{
    [Fact]
    public void SharedAssemblyIsReferenced()
    {
        Assert.NotNull(typeof(ShiftSoftware.ADP.Cases.Shared.Marker).Assembly);
    }
}
