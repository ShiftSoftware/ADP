# Rastgo Engineering Explorer

A local engineering guide using the same [shared shell](../../ADP.Explorer.Shared/README.md)
as the [Hawta Explorer](../../ADP.Hawta/Explorer/README.md): identical header,
overview/detail markup, semantic themes, controls, panel proportions and mobile drawers. Eight mechanisms contain 26 authored scenarios. They explain the
current implementation; this is not a connected health dashboard.

## Run

Node.js 22 or newer; no package install, framework build or .NET service required.
From the ADP repository root:

```sh
node ADP.Rastgo/Explorer/server.mjs
```

Open [Rastgo Explorer](http://127.0.0.1:4179/).
`RASTGO_EXPLORER_PORT` overrides the default. The server binds to loopback and
serves only an explicit asset allowlist. Script-relative paths work from another
working directory. On Windows, `./ADP.Rastgo/Explorer/start.ps1` also supports
`-Background -Port 4179`; it returns a process ID and temporary log paths.

Select a stage in the overview and choose **Explore inside**, or use a mechanism
URL such as `#comparison/empty`. The toolbar selects mechanisms and scenarios.
The diagram shows the three authored steps, their relevant boundary and outcome;
select a node or result row to inspect it. **Behavior** explains contracts and
limits; **Evidence** shows captured implementation; **Choices** explains
tradeoffs; **Example** shows YAML or host composition. **Other mechanisms** links
to the full catalog. Native controls support keyboard navigation.

The shared shell preserves Hawta's exact desktop geometry. At 1280×720, overview
uses a 928-pixel canvas and 286-pixel inspector; detail uses an 888-pixel canvas
and 326-pixel inspector. Both use an 18-pixel panel gap. Long explanations scroll
inside the inspector. Below the shared breakpoints, normal document scrolling
and fixed inspector drawers keep content reachable. Escape closes a drawer and
restores focus to its selection. The detail scene and inspector are keyboard
focusable. Theme preference is saved locally; overview and detail share motion
state. Reduced motion starts paused and can be explicitly resumed.

## Contents and boundaries

- Hawta facts and Rastgo policy; the published-run freshness floor.
- YAML, scalar/grouped measures, source errors and verdict rows.
- Age, threshold and diff, including future dates, nulls and empty comparisons.
- DuckDB, Cosmos, SQL Server and file-share sources; qualified connections.
- Partitioned JSONL, per-domain staleness, dashboard rollup and trends.
- Strict authoring diagnostics and bounded metadata discovery.

All values are authored examples. No SQL is executed and no credentials, telemetry,
result-store connection, automatic remediation or publication is implemented here.
The three current assertion types are `age`, `threshold` and `diff`; dedicated
`rate`, `backlog`, `funnel` and `anomaly` assertions are not shipped.
A backlog can be measured in SQL and judged with `threshold`.

Important current semantics are shown explicitly: diff treats absent/null numeric
cells as zero and passes an empty key union; critical breaches Fail, while warning
and info breaches Warn; source errors stay Error; repeated sink run IDs overwrite
the corresponding file. Hosts must provide trusted checks and query-only access.
SQL connectors execute the supplied query and do not sandbox arbitrary SQL.

## Source evidence and validation

`evidence.json` contains reviewed excerpts, capture time, repository-relative
paths and complete-file SHA-256 hashes with CRLF normalized to LF. It remains
readable in a static copy. The local server adds source comparisons marked
unchanged, changed or unavailable. Static hosting has no comparison API and
reports freshness unverified.

```sh
node ADP.Rastgo/Explorer/verify.mjs
node ADP.Rastgo/Explorer/capture-evidence.mjs
```

Run capture only after reviewing source changes and the excerpt ranges in
`evidence-sources.mjs`; it never runs automatically. Then review the snapshot and
rerun verification. The verifier checks scenario/evidence references, capture
integrity, current source hashes, HTTP asset delivery and bounded serving, and a
relocated copy with no original source checkout. It does not run the .NET pipeline.

For browser edits, review both themes, desktop and narrow layouts, scenario
changes, step selection, inspection layers, keyboard focus, and motion controls.
Compare with Hawta at the same actual viewport and normal zoom. At 1280×720,
1440×900 and 1024×768, every authored scenario should fit without whole-page or
canvas scrolling; long inspector content may scroll within its panel. Check
320- and 390-pixel layouts for horizontal overflow and reachable content.
Do not claim production or pipeline verification from this documentation preview.

## Resource structure

This is a multi-file app:

- Browser content: `index.html`, `overview.js`, `style.css`, `mechanisms.js`,
  `explorer.js`, `rastgo-icon.svg`, `evidence.json`.
- Shared browser shell: `ADP.Explorer.Shared/theme.css`, `engineering.css`,
  `shell.js`, `overview.js`, `evidence.js`, explicitly served through `/shared/`.
- Local tools: `server.mjs`, `start.ps1`, `evidence.mjs`,
  `evidence-sources.mjs`, `capture-evidence.mjs`, `verify.mjs`.
- `rastgo-icon.svg` comes from existing Rastgo documentation branding.

Keep the shared directory beside the product directories when relocating the
checkout. For a portable single HTML file, run:

```sh
node ADP.Explorer.Shared/export.mjs rastgo /path/to/rastgo.html --single-file
```

Open or share that generated file; it embeds styles, scripts, icons, scenarios
and evidence. Its offline evidence does not claim to check current source files.
The [shared export workflow](../../ADP.Explorer.Shared/README.md) also supports
asset folders. Opening the checkout's source `Explorer/index.html` directly is
not the standalone workflow: use the server for that entry or generate an export.
