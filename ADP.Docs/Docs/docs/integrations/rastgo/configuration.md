# Configuration

Everything Rastgo runs is declared in two places: the **YAML check packs** (what to measure and assert) and the **host's connection configuration** (where the sources live). This page is the field-by-field reference for both.

## Check file

A check file is a YAML list of checks. Keys are camelCase; unknown keys are ignored (so a pack can carry comments/metadata the engine doesn't read).

```yaml
- name: freshness.loaded.invoice_date
  domain: sync-agent
  category: freshness
  severity: critical
  description: Newest loaded InvoiceDate per dealer.
  breakdown: dealer
  measures:
    - key: loaded
      source: duckdb
      valueKind: timestamp
      sql: |
        SELECT CompanyID AS k,
               MAX(InvoiceDate) AT TIME ZONE 'Asia/Baghdad' AT TIME ZONE 'UTC' AS v
        FROM VehicleEntry
        WHERE InvoiceDate <= now()        -- future-date guard
        GROUP BY CompanyID
  assert:
    type: age
    of: loaded
    warn: 26h
    max: 48h
```

### Check fields

| Field | Required | Default | Notes |
|---|---|---|---|
| `name` | ✅ | — | Stable id; also the result-row `checkName`. Convention: `category.family.detail`. |
| `domain` | ✅ | `""` | Owning pack; becomes the sink partition (`domain=<…>`). |
| `category` | ✅ | `""` | `freshness` / `reconciliation` / `quality` / `flow` — dashboard's top axis. |
| `severity` | — | `warning` | `critical` ⇒ breach is `Fail`; `warning`/`info` ⇒ breach is `Warn`. |
| `description` | — | `null` | One-liner; dashboard tooltip. |
| `breakdown` | — | `null` | Set ⇒ **grouped** (assert per group, one row per group). |
| `measures` | ✅ | `[]` | One or more named measures. |
| `assert` | ✅ | `threshold` | The rule applied to the measures. |

### Measure fields (`MeasureSpec`)

| Field | Applies to | Notes |
|---|---|---|
| `key` | all | Name referenced by the assert (`of` / `left` / `right`). Default `value`. |
| `source` | all | `duckdb` / `cosmos` / `fileshare`. |
| `sql` | duckdb, cosmos | Select `v` (scalar) or `k, v` (grouped). |
| `path` | fileshare | Relative to the configured base; supports wildcards and a `**/` prefix. |
| `database` | cosmos | Cosmos database id. |
| `container` | cosmos | Cosmos container id. |
| `valueKind` | all | `number` (default) / `timestamp` (UTC) / `count` (fileshare). |

### Assert fields (`AssertSpec`)

| Field | Used by | Meaning |
|---|---|---|
| `type` | all | `age` / `threshold` / `diff`. |
| `of` | age, threshold | Which measure key to evaluate (defaults to the first). |
| `left` / `right` | diff | The two measure keys to compare. |
| `max` | age, threshold | age: a duration (`26h`); threshold: numeric ceiling. |
| `min` | threshold | Numeric floor. |
| `warn` | age | Duration warn level, below `max`. |
| `tolerance` | diff | Absolute tolerance on `left - right`. |
| `tolerancePct` | diff | Relative tolerance, as a percent of the right side. |

Durations accept `s`, `m`, `h`, `d` (e.g. `30s`, `90m`, `26h`, `2d`).

## Host connection configuration

The host (the `HealthRunner` console today; a timer Function later) supplies connections via standard `IConfiguration`. The console reads:

| Key | Purpose |
|---|---|
| `ConnectionStrings:DuckDBRead` (or `:DuckDB`) | DuckDB read snapshot for `duckdb` measures. |
| `ConnectionStrings:CosmosDB` | Cosmos account for `cosmos` measures (omit ⇒ cosmos checks report a source error, run still completes). |
| `Rastgo:FileShareBase` (or `--fileshare`) | Base directory for `fileshare` measures. |

When composing through dependency injection, the same values flow through `RastgoOptions` and the source registration extensions:

```csharp
services
    .AddRastgoCore(o =>
    {
        o.FileShareBase = config["Rastgo:FileShareBase"]!;   // base for fileshare measures
        o.ResultsRoot   = config["Rastgo:ResultsRoot"]!;     // where the sink writes
    })
    .AddRastgoDuckDb(config.GetConnectionString("DuckDBRead")!)
    .AddRastgoCosmos(config.GetConnectionString("CosmosDB"));  // null-safe
```

## Result store layout

Results are written by `JsonlResultSink` as one file per (domain, date, run); the dashboard unions them at read time. See [Architecture → The sink](architecture.md#the-sink-partitioned-append-only) for the rationale and the full result-row schema.

```
{ResultsRoot}/results/domain=<domain>/date=<yyyy-MM-dd>/<runId>.jsonl
{ResultsRoot}/dashboard.html
```

!!! note "JSONL today, Parquet later"
    The sink is JSONL now and Parquet-ready later: the partition paths don't change, so swapping the file format is a sink-internal change, not a layout migration.

---

Next: [Getting Started](getting-started.md) — install, compose, author a pack, and run it.
