using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.EndpointParity.Harness;
using ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;
using Xunit;

namespace ShiftSoftware.ADP.EndpointParity.Menus;

/// <summary>
/// Step 01 item D: proves the global "no response body is text/html" assertion FIRES on this group.
///
/// <para>
/// The Menus sample maps a fallback file, so a deleted or renamed route answers <b>200 + HTML</b>
/// rather than 404. Without the assertion, an endpoint disappearing in the upgrade would pass
/// silently as an ordinary success. Asserting that the rule EXISTS is not enough - this test
/// asks the harness to fetch a route that does not exist and requires it to report a hard failure.
/// </para>
/// </summary>
public class FallbackAssertionTest
{
    private readonly ITestOutputHelper output;
    public FallbackAssertionTest(ITestOutputHelper output) => this.output = output;

    [Fact]
    public async Task Missing_route_is_a_hard_failure_not_a_200()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new SampleHostFactory<Program>(
            "ConnectionStrings:SQLServer", "ADP_Parity_Menus_fallback",
            new Dictionary<string, string?> { ["ConnectionStrings:Cosmos"] = "" });

        var client = factory.CreateClient();
        var config = factory.Services.GetRequiredService<IConfiguration>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", ParityAuth.MintToken(
            config["Settings:TokenSettings:Issuer"]!,
            config["Settings:TokenSettings:PrivateKey"]!,
            ParityAuth.BuildAccessTree(ParityGrant.FullAccess,
                new[] { "ShiftIdentityActions", "MenuActionTree" }, new Dictionary<string, int[]>())));

        // What the host does on its own: 200 + HTML, which is the hazard.
        var raw = await client.GetAsync("/api/Menu/ThisRouteWasDeletedByTheUpgrade", ct);
        output.WriteLine($"raw host response: {(int)raw.StatusCode} {raw.Content.Headers.ContentType}");

        // What the harness does with it.
        var runner = new ParityRunner(
            client,
            new Normalizer(new NormalizerOptions { RunStart = DateTimeOffset.UtcNow }),
            Path.Combine(Path.GetTempPath(), "parity-fallback-probe"),
            ParityMode.Capture,
            ParityGrant.FullAccess);

        await runner.RunAsync(new[]
        {
            new ParityCase
            {
                Name = "Probe.DELETED_ROUTE",
                Kind = "DETAIL",
                Method = "GET",
                Url = "/api/Menu/ThisRouteWasDeletedByTheUpgrade",
            },
        }, ct);

        output.WriteLine($"harness hard failures: {runner.HardFailures.Count}");
        foreach (var f in runner.HardFailures) output.WriteLine("  " + f);

        Assert.True(raw.StatusCode == System.Net.HttpStatusCode.OK,
            "precondition: the fallback should answer 200, which is exactly the hazard");
        Assert.NotEmpty(runner.HardFailures);
        Assert.Contains(runner.HardFailures, f => f.Contains("text/html"));
    }
}
