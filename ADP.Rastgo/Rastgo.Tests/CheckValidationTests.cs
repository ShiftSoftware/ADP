using ShiftSoftware.ADP.Rastgo;

namespace Rastgo.Tests;

public sealed class CheckValidationTests
{
    [Fact]
    public void Authoring_load_reports_unknown_and_semantically_invalid_fields()
    {
        const string yaml = """
        - name: duplicate
          domain: warehouse
          category: quality
          severty: critical
          measures:
            - { key: count, source: missing, sql: "SELECT 1 AS v" }
            - { key: count, source: missing, sql: "SELECT 2 AS v" }
          assert: { type: threshold, of: typo }
        - name: duplicate
          domain: warehouse
          category: freshness
          measures:
            - { key: newest, source: duckdb, sql: "SELECT now() AS v" }
          assert: { type: age, of: newest, max: soon }
        """;

        var pack = YamlCheckLoader.LoadForAuthoring(yaml, ["duckdb"]);

        Assert.False(pack.IsValid);
        Assert.Contains(pack.Diagnostics, d => d.Code == "unknown_property" && d.Path.EndsWith(".severty"));
        Assert.Contains(pack.Diagnostics, d => d.Code == "duplicate_check_name");
        Assert.Contains(pack.Diagnostics, d => d.Code == "duplicate_measure_key");
        Assert.Contains(pack.Diagnostics, d => d.Code == "unknown_source");
        Assert.Contains(pack.Diagnostics, d => d.Code == "unknown_measure_reference");
        Assert.Contains(pack.Diagnostics, d => d.Code == "invalid_duration");
    }

    [Fact]
    public void Valid_pack_keeps_runtime_defaults_and_has_no_diagnostics()
    {
        const string yaml = """
        - name: freshness.orders
          domain: operations
          category: freshness
          measures:
            - key: newest
              source: duckdb
              valueKind: timestamp
              sql: SELECT MAX(created_at) AS v FROM orders
          assert:
            type: age
            of: newest
            warn: 90m
            max: 2h
        """;

        var pack = YamlCheckLoader.LoadForAuthoring(yaml, ["duckdb"]);

        Assert.True(pack.IsValid);
        Assert.Empty(pack.Diagnostics);
        Assert.Equal("warning", pack.Checks[0].Severity);
        Assert.Equal("value", new MeasureSpec().Key);
    }
}
