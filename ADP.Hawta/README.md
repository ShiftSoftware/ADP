# ADP.Hawta

Hawta ingests source data into a DuckDB snapshot, reconciles typed rows, builds
serving projections, replicates changes to Cosmos DB, and publishes versioned
Parquet snapshots. Hosts configure source mappings, schedules and ownership.
The agent loop can also copy its own run history (source runs, publish attempts,
loop cycles, per-table pump drains and source read times) to Parquet on a cadence, into a location
of its own, so the history can be read outside the process, and it prunes the
copied rows of earlier days out of its write database once a day; see
`SnapshotRunLog`.

- [Engineering Explorer](Explorer/README.md): an interactive overview, eight
  mechanisms and thirty illustrative scenarios with public source evidence and
  editable change briefs. Run locally without building the .NET library.
- `Hawta/`: the library implementation.
- `Hawta.Tests/`: library tests. From the repository root, run
  `dotnet test ADP.Hawta/Hawta.Tests`.
