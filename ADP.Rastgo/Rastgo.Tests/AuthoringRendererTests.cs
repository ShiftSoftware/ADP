using ShiftSoftware.ADP.Rastgo;
using System.Text.Json;

namespace Rastgo.Tests;

public sealed class AuthoringRendererTests
{
    [Fact]
    public void Dashboard_and_trends_expose_authoring_navigation()
    {
        var now = DateTimeOffset.UtcNow;

        var dashboard = DashboardRenderer.Render([], now);
        var trends = TrendsRenderer.Render([], now, TimeSpan.FromDays(30));

        Assert.Contains("href=\"docs\"", dashboard, StringComparison.Ordinal);
        Assert.Contains("href=\"docs\"", trends, StringComparison.Ordinal);
        Assert.Contains("searchParams.set('code', functionKey)", dashboard, StringComparison.Ordinal);
        Assert.Contains("searchParams.set('code', functionKey)", trends, StringComparison.Ordinal);
    }

    [Fact]
    public void Renderer_encodes_pack_and_catalog_text()
    {
        var check = new CheckDefinition
        {
            Name = "quality.<script>alert(1)</script>",
            Domain = "operations",
            Category = "quality",
            Measures = [new MeasureSpec { Key = "v", Source = "custom" }],
            Assert = new AssertSpec { Type = "threshold", Of = "v", Min = "1" },
        };
        var catalog = new AuthoringCatalog(
            "checks.yaml",
            DateTimeOffset.UtcNow,
            new AuthoringCheckPack([check], []),
            [new SourceCatalogSnapshot("custom", "Custom", [
                new CatalogDataset("<img src=x onerror=alert(1)>", "table", "main", [new CatalogField("<b>id</b>", "text")])
            ], DateTimeOffset.UtcNow)],
            TimeSpan.FromMinutes(15));

        var html = AuthoringRenderer.Render(catalog);

        Assert.DoesNotContain("<script>alert(1)</script>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img src=x onerror=alert(1)>", html, StringComparison.Ordinal);
        Assert.Contains("quality.&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html, StringComparison.Ordinal);
        Assert.Contains("href=\"dashboard\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"trends\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"docs?refresh=true\"", html, StringComparison.Ordinal);
        Assert.Contains("Agent-assisted authoring", html, StringComparison.Ordinal);
        Assert.Contains("Download agent context", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Copy agent package", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Open agent context JSON", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-agent-brief", html, StringComparison.Ordinal);
        Assert.Contains("data-authoring-group", html, StringComparison.Ordinal);
        Assert.Contains("data-agent-context", html, StringComparison.Ordinal);
        Assert.Contains("searchParams.set('code', functionKey)", html, StringComparison.Ordinal);

        var json = AuthoringRenderer.RenderAgentContext(catalog);
        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain('\n', json);
        Assert.DoesNotContain("\"grouped\"", json, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("rastgo-authoring-context/v2", document.RootElement.GetProperty("schema").GetString());
        Assert.False(document.RootElement.TryGetProperty("purpose", out _));
        Assert.False(document.RootElement.TryGetProperty("brief", out _));
        Assert.False(document.RootElement.TryGetProperty("agentContract", out _));
        Assert.False(document.RootElement.TryGetProperty("instructions", out _));
        var agent = document.RootElement.GetProperty("agent");
        Assert.Equal("closed-world", agent.GetProperty("evidenceMode").GetString());
        Assert.Contains("CLARIFY:", agent.GetProperty("responses").GetProperty("clarify").GetString(), StringComparison.Ordinal);
        Assert.Contains("BLOCKED:", agent.GetProperty("responses").GetProperty("blocked").GetString(), StringComparison.Ordinal);
        Assert.Contains(agent.GetProperty("rules").EnumerateArray(),
            rule => rule.GetString()!.Contains("root query", StringComparison.Ordinal));
        Assert.Contains(agent.GetProperty("rules").EnumerateArray(),
            rule => rule.GetString()!.Contains("Copy paths verbatim", StringComparison.Ordinal));
        Assert.Equal(1, document.RootElement.GetProperty("pack").GetProperty("checks").GetArrayLength());
        Assert.Equal("scope", document.RootElement.GetProperty("language").GetProperty("fields").GetProperty("columns")[0].GetString());
        Assert.Equal(1, document.RootElement.GetProperty("sources").GetArrayLength());
    }

    [Fact]
    public void Agent_context_keeps_referenced_files_and_compacts_broad_inventory()
    {
        var files = new[]
        {
            new CatalogDataset("KRG.csv", "csv", "Stock", [], "metadata") { Referenced = true },
            new CatalogDataset("Basra.csv", "csv", "Stock", [], "metadata") { Referenced = true },
            new CatalogDataset("unrelated.csv", "csv", "Archive", [], "metadata"),
        };
        var catalog = new AuthoringCatalog(
            "checks.yaml",
            DateTimeOffset.UtcNow,
            new AuthoringCheckPack([], []),
            [new SourceCatalogSnapshot("fileshare", "File share", files, DateTimeOffset.UtcNow, Truncated: true)
            {
                ExcludedTopLevelFolders = [".internal-sync"],
            }],
            TimeSpan.FromMinutes(15));

        Assert.Contains(".internal-sync", AuthoringRenderer.Render(catalog), StringComparison.Ordinal);
        using var document = JsonDocument.Parse(AuthoringRenderer.RenderAgentContext(catalog));
        var source = document.RootElement.GetProperty("sources")[0];
        var fileCatalog = source.GetProperty("catalog");

        Assert.Equal(3, fileCatalog.GetProperty("discoveredFileCount").GetInt32());
        Assert.Equal(2, fileCatalog.GetProperty("referencedFiles").GetProperty("rows").GetArrayLength());
        Assert.Equal(0, fileCatalog.GetProperty("declaredSchemas").GetProperty("rows").GetArrayLength());
        Assert.Equal(1, fileCatalog.GetProperty("otherFilesByDirectory").GetProperty("rows").GetArrayLength());
        Assert.Equal("Archive", fileCatalog.GetProperty("otherFilesByDirectory").GetProperty("rows")[0][0].GetString());
        Assert.Equal(".internal-sync", fileCatalog.GetProperty("excludedTopLevelFolders")[0].GetString());
    }
}
