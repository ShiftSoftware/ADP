# Hawta Engineering Explorer

A local browser app for understanding Hawta and reviewing engineering decisions.
It includes eight mechanisms and thirty authored scenarios: file gating, typed
row comparison and merge safeguards, bounded fetching, projection gating,
replication, atomic publishing, deferred residency, and Cosmos reconciliation.

Source families, keys, versions and consumer labels are generic examples. The
diagrams do not represent a deployment or measured execution. The overview's
ownership boundary is an example host arrangement, not an automatic discovery.

## Run

Requires Node.js 22 or newer. Uses only Node built-ins; no package install,
framework build or .NET service is needed. From the ADP repository root:

```sh
node ADP.Hawta/Explorer/server.mjs
```

Open [the local Explorer](http://127.0.0.1:4178/). The server binds to loopback.
`HAWTA_EXPLORER_PORT` can override the default port. Paths are resolved relative
to the scripts, so launching from another working directory also works.

On Windows, the helper can run in the current terminal or as a hidden process:

```powershell
.\ADP.Hawta\Explorer\start.ps1
.\ADP.Hawta\Explorer\start.ps1 -Background -Port 4178
```

The background command returns the process ID and log path. Logs go to the OS
temporary directory. Stop that process with `Stop-Process -Id <ProcessId>`, or
press Ctrl+C for a foreground server.

## Explore and verify

Select a stage and choose **Explore inside**. Mechanism and scenario selectors
change the diagram. Click a node or row, or activate it with Enter/Space, to keep
its explanation selected. Inspect Behavior, Evidence, Choices, or Change brief.
Brief edits stay in the page session; preparing or copying a draft sends nothing.
Reloading discards drafts. Theme preference is saved in browser storage when
available. Mobile inspectors close with Escape and return focus to the selection.

All applicable connections animate concurrently: three evenly phased particles
on a 3.4-second loop. Paths describe the selected scenario; they do not indicate
execution order or additional runs. Bypassed paths carry no particles. Pause is
shared by both views. Reduced motion starts paused and can be explicitly resumed.

Run from the repository root:

```sh
node ADP.Hawta/Explorer/verify.mjs
node --test ADP.Hawta/Explorer/portability.test.mjs
```

The checks cover scene/event contracts, evidence references, typed example hashes,
route masks, simultaneous particles, reduced motion, a relocated Explorer,
source freshness and bounded file access. They do not execute or benchmark the
Hawta pipeline. For UI changes also inspect both themes at 1280×720 and 1440×900,
plus narrow mobile layouts, keyboard focus, drawers, brief editing and motion.

## Source evidence

`evidence.json` is an intentional checked-in asset: captured excerpts from public
ADP implementation and tests, with repository-relative paths and capture time.
This keeps the Evidence layer usable immediately and when the original source
files are unavailable. No capture runs automatically at startup.

`evidence-sources.mjs` is the explicit source allowlist. Capture and freshness
checks resolve those paths within this checkout; neither HTTP parameters nor a
captured path can select an arbitrary file. Redirected symlinks/junctions are
rejected. The server serves only its listed browser assets and freshness results.

Hashes cover the complete UTF-8 source file after CRLF is normalized to LF
(`sha256-lf-v1`). A fresh comparison reports **unchanged**, **changed**, or
**unavailable**. Line-ending differences alone do not mark another checkout stale.
A changed file requires reviewing the explanation and excerpt ranges; freshness
does not prove the explanation is correct or the feature is enabled in production.
Opening Evidence reloads the snapshot. Freshness results identify their capture
and source hash; results from a different capture are treated as unverified.

To deliberately refresh the snapshot after reviewing source and line ranges:

```sh
node ADP.Hawta/Explorer/capture-evidence.mjs
```

Review `evidence.json` and rerun the checks. Captured tests are evidence references,
not claims that those tests have run. Static hosting can show the captured snapshot
but cannot perform local source comparisons; the UI reports freshness unverified.

## Structure

- `index.html`, `animation.js`: overview, theme/motion controls and mobile inspector.
- `mechanisms.js`: authored scenarios, stable event identities, state reducer and
  complete scene models. `explorer.js` renders them and the inspection layers.
- `style.css`, `explorer.css`: shared semantic theme and responsive layout.
- `evidence-sources.mjs`, `evidence.mjs`, `capture-evidence.mjs`: public source
  catalog, bounded reads, normalized hashing and explicit capture.
- `server.mjs`, `start.ps1`: local serving. `verify.mjs` and
  `portability.test.mjs`: demonstration and portability checks.

The semantic colors, radii and type stacks map the canonical
[ADP harness theme](../../ADP.WebComponents/adp-web-components/src/templates/harness.src.css)
into plain CSS. Gold is a fill in the light theme; readable text, thin marks and
focus rings use the darker accent. Status colors describe example data. This local
mapping preserves the compact app layout without importing a UI framework.

The versioned event contract distinguishes illustrative presentation offsets from
observation times. No recorded-run adapter, import or telemetry connection exists.
A future adapter must supply actual resource names and observation evidence from
run data and preserve unknown states; it must not infer measurements from the
illustrative particle clock or authored scenarios.
