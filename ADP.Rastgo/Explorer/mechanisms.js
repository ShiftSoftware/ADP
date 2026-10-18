/* Authored explanations and examples. These do not execute SQL or the .NET engine. */
(() => {
  const scenario = (id, label, status, title, detail, values, rows = []) => ({id, label, status, title, detail, values, rows});
  const step = (title, detail) => ({title, detail});
  const mechanisms = [
    {
      id: 'relationship', title: 'Hawta + Rastgo', summary: 'One records the pipeline. The other interprets its evidence. They meet at queryable facts, not a shared health policy.',
      steps: [
        step('Hawta records facts', 'Hawta ingests, merges, projects and delivers data. It records source-run times, status and counts. Transaction guards such as mass-delete refusal remain in Hawta because they must act during the operation.'),
        step('The host exposes a read surface', 'Published Parquet and its manifest let readers inspect a delivered set. Published sourceRuns become meta.sync_runs in a view database. These records describe the last committed publish, not continuous live telemetry.'),
        step('Rastgo applies a host policy', 'A YAML check measures a timestamp, count or difference and applies the host’s expectation. Rastgo does not repair or re-ingest data. Hosts own the check pack, credentials, schedule and response.')
      ],
      choices: ['Keep health thresholds in the check pack; the same age can be normal for a catalog and unacceptable for a stock feed.', 'Rastgo has no ADP package dependency. A host can use it without Hawta, or compose both explicitly.', 'Read published facts and original sources as separate signals. Agreement alone does not establish freshness.'],
      limits: 'A sourceRuns age is bounded by the last publish. If nothing changes, no new manifest may be written. A missing source-run record is different from a recent successful run. This Explorer is not connected to a host.',
      evidence: ['hawta-facts', 'published-runs', 'age'],
      exampleTitle: 'A check over published run facts', exampleCopy: 'Illustrative source key and policy. A host must expose meta.sync_runs and choose its own delivery expectation.',
      example: '- name: freshness.ingest.orders\n  domain: operations\n  category: freshness\n  severity: critical\n  measures:\n    - key: last_run\n      source: duckdb\n      valueKind: timestamp\n      sql: >-\n        SELECT max("StartedAt") AS v\n        FROM meta.sync_runs\n        WHERE "SourceKey" = \'orders\'\n  assert: { type: age, max: 24h, warn: 6h }',
      scenarios: [
        scenario('recent', 'A recently published run', 'Pass', 'A recent fact meets the chosen SLA.', 'The example’s published run is two hours old. It is below both the six-hour warning and 24-hour maximum.', ['Run completed', 'Published run: 2h old', '2h ≤ 6h → Pass']),
        scenario('quiet', 'A quiet published snapshot', 'Warn', 'The publish clock is not the ingest clock.', 'At eight hours this policy warns. That alone cannot tell whether ingestion stopped or the estate stayed unchanged; inspect publish freshness alongside the run fact.', ['Ingestion may still be ticking', 'Last committed run: 8h old', '8h > 6h → Warn']),
        scenario('missing', 'No published run record', 'Error', 'Absence is not a healthy timestamp.', 'MAX over no matching run returns NULL. The age evaluator reports Error, rather than assuming a fresh run or converting absence into zero.', ['No matching published fact', 'MAX(StartedAt) = NULL', 'Cannot evaluate age → Error'])
      ]
    },
    {
      id: 'checks', title: 'From YAML to results', summary: 'A check is a stable identity, one or more measures and one assertion. The runner evaluates checks and their measures sequentially.',
      steps: [
        step('Load the host’s check pack', 'YamlCheckLoader.Load uses camelCase and ignores unknown properties. Loading is not the same as strict validation. LoadForAuthoring adds structural and semantic diagnostics.'),
        step('Resolve and measure', 'SourceRegistry resolves the exact registered name, case-insensitively. A qualified source has no fallback to the bare provider. SQL-like sources return v for scalar checks, or k and v when breakdown is set.'),
        step('Emit a verdict row', 'CheckRunner emits one CheckResult per assertion outcome, carrying identity, group, metrics, message, run ID, time and duration. A measure error becomes an Error outcome before breach severity is considered.')
      ],
      choices: ['Use a stable check name: the dashboard and trend history group results by that name.', 'Keep credentials in the host and grant query-only access. SQL text is supplied by trusted pack authors; these connectors are not a SQL sandbox.', 'Use one scalar check per independently connected tenant when failures should stay isolated.'],
      limits: 'A declared breakdown does not invent missing groups. Design measures that produce all groups you need to observe. Skipped exists in the result contract, but these three assertions do not automatically emit it.',
      evidence: ['model', 'loader', 'runner', 'registry'],
      exampleTitle: 'Scalar and grouped contracts', exampleCopy: 'Illustrative query shapes. Source names must match host registration.',
      example: 'scalar:  SELECT count(*) AS v FROM data."Orders"\n\ngrouped: SELECT "DealerCode" AS k, count(*) AS v\n         FROM data."Orders"\n         GROUP BY "DealerCode"\n\nYAML:    breakdown: dealer\n         source: sql:region-a',
      scenarios: [
        scenario('groups', 'Two measured groups', 'Warn', 'A result for each measured group.', 'The same check can Pass for one group and Warn for another. The displayed check status rolls up to the worst result.', ['One check, breakdown: dealer', 'North: 10 · South: 0', 'min: 1 · severity: warning'], [['North', '10 rows', 'Pass'], ['South', '0 rows', 'Warn']]),
        scenario('unknown', 'Unknown qualified source', 'Error', 'A mistyped name cannot bind elsewhere.', 'A measure naming sql:region-typo fails resolution. It cannot silently use the sql source and return a convincing result from the wrong database.', ['Valid pack shape', 'sql:region-typo not registered', 'Unknown source → Error']),
        scenario('unreachable', 'One measure cannot run', 'Error', 'An unmeasured check has no breach verdict.', 'If either measure reports an error, the entire check becomes one Error row. severity: critical does not turn that source failure into Fail.', ['Two measures for one check', 'Second source unavailable', 'One Error row, no group verdicts'])
      ]
    },
    {
      id: 'freshness', title: 'Age & freshness', summary: 'An age check asks how long ago a timestamp occurred. Freshness needs a clock anchored to the event you actually care about.',
      steps: [
        step('Choose the timestamp', 'File delivery, newest business activity and published ingest runs are different clocks. Select one deliberately; a freshly copied file can still contain old business data.'),
        step('Measure age in UTC', 'The evaluator subtracts the measured timestamp from nowUtc. Normalize naive local timestamps in the query. A future timestamp always produces Warn, even for critical severity.'),
        step('Compare strict boundaries', 'Age greater than max breaches; otherwise age greater than warn warns. Equality at a boundary does not breach that boundary. A critical max breach is Fail; other severities breach as Warn.')
      ],
      choices: ['Use per-feed or per-group expectations so a fresh group cannot hide a stale one.', 'Null timestamps and zero returned rows are Error; they are not fresh data.', 'Durations support seconds, minutes, hours and days, including decimal values.'],
      limits: 'The sample clock is fixed and authored. It does not read your files or measure elapsed time in this page. A Pass certifies the selected timestamp against this policy only.',
      evidence: ['age', 'files', 'duckdb'],
      exampleTitle: 'Freshness per delivered file', exampleCopy: 'Wildcard breakdown measures each matching file. A scalar wildcard instead selects the newest match.',
      example: '- name: freshness.delivered_files\n  domain: operations\n  category: freshness\n  severity: critical\n  breakdown: file\n  measures:\n    - key: delivered\n      source: fileshare\n      path: Parts/*.csv\n      valueKind: timestamp\n  assert: { type: age, of: delivered, warn: 2d, max: 7d }',
      scenarios: [
        scenario('fresh', 'One day old', 'Pass', 'The delivery is within expectation.', 'One day is below the two-day warning. This says nothing about whether the rows inside the file are current.', ['File modification time', 'Age = 1d', '1d ≤ 2d → Pass']),
        scenario('aging', 'Three days old', 'Warn', 'Delivery is late enough to investigate.', 'Three days exceeds warn: 2d but remains below max: 7d. The warning is independent of breach severity.', ['File modification time', 'Age = 3d', '3d > 2d → Warn']),
        scenario('stale', 'Eight days old', 'Fail', 'The critical delivery SLA was breached.', 'Eight days exceeds the seven-day maximum. The same breach at warning or info severity would be Warn.', ['File modification time', 'Age = 8d', '8d > 7d → Fail']),
        scenario('future', 'Timestamp is in the future', 'Warn', 'A future date needs explanation.', 'A timestamp two hours ahead yields a negative age. The evaluator flags it as Warn instead of treating it as exceptionally fresh.', ['Future-dated value', 'Age = −2h', 'Future-date guard → Warn'])
      ]
    },
    {
      id: 'threshold', title: 'Thresholds & quality', summary: 'A numeric floor or ceiling can express volume, quality and backlog policies. Domain logic belongs in the measurement query.',
      steps: [
        step('Measure a meaningful number', 'Examples include live-row counts, duplicate keys and rows stuck beyond a business deadline. A backlog uses an aging predicate in SQL, then a numeric threshold.'),
        step('Apply min and max', 'Values below min or above max breach; equality passes. A null number or no rows produces Error. A number without a bound is not a useful health policy.'),
        step('Interpret the severity', 'critical maps a breach to Fail. warning and info both map a breach to Warn. There is no separate observe-only breach status in this evaluator.')
      ],
      choices: ['Pair count reconciliation with a volume floor to catch two empty datasets agreeing.', 'For signed age-like numbers, set min: 0 to catch future values; a max alone permits negatives.', 'An informational count with min: 0 records a metric but does not detect an abnormal increase.'],
      limits: 'The row count must select the correct business domain. Deleted rows, mapping exclusions and duplicate keys can make a raw count misleading. This example uses synthetic values.',
      evidence: ['threshold', 'language', 'runner'],
      exampleTitle: 'A numeric quality budget', exampleCopy: 'Illustrative query over a host-provided dataset. The host defines what “refused” means.',
      example: '- name: quality.refused_rows\n  domain: operations\n  category: quality\n  severity: critical\n  measures:\n    - key: refused\n      source: duckdb\n      sql: SELECT count(*) AS v FROM data."RefusedRows"\n  assert: { type: threshold, of: refused, max: 0 }',
      scenarios: [
        scenario('zero', 'No refused rows', 'Pass', 'The measured value is on the boundary.', 'Zero equals max: 0. The evaluator only breaches when the value is greater than the maximum.', ['COUNT = 0', 'max: 0', '0 ≤ 0 → Pass']),
        scenario('refused', 'Three refused rows', 'Fail', 'The quality budget was exceeded.', 'Three rows exceed the zero-row budget, and this illustrative policy is critical.', ['COUNT = 3', 'max: 0', '3 > 0 → Fail']),
        scenario('null', 'No numeric value', 'Error', 'Missing is not zero.', 'A NULL metric cannot be evaluated by threshold. The source or query needs investigation before any quality conclusion.', ['Numeric cell = NULL', 'No comparable value', 'Error'])
      ]
    },
    {
      id: 'comparison', title: 'Difference & parity', summary: 'Compare two measures by group with an absolute tolerance or a percentage of the right side. Count equality is a deliberately narrow claim.',
      steps: [
        step('Measure both sides', 'A diff check names left and right measurement keys. The source query must align domains and filters; equal counts from different row populations are not meaningful.'),
        step('Join the union of keys', 'The evaluator uses every group seen on either side. Missing or null numeric cells become zero. This is useful for counts and hazardous for nullable watermarks.'),
        step('Apply either tolerance', 'Pass when |left − right| is within the absolute tolerance OR a positive percentage tolerance of |right|. Defaults are zero. An empty key union returns Pass: “No keys to compare.”')
      ],
      choices: ['Keep a volume/null check alongside parity so missing data cannot pass unnoticed.', 'Published snapshots can change between measures; the runner does not pin every query to one publish ID.', 'Independent field-fidelity checks are needed to verify transformations. Reusing the same mapper on both sides can reproduce the same defect.'],
      limits: 'Both replicas may be equally stale. Matching counts do not establish row identity, mapped-field fidelity or upstream delivery. Add source freshness and domain-specific comparisons.',
      evidence: ['diff', 'duckdb'],
      exampleTitle: 'Grouped count comparison', exampleCopy: 'The right-hand measurement defines the denominator for tolerancePct. Either tolerance can pass.',
      example: 'assert:\n  type: diff\n  left: snapshot\n  right: replica\n  tolerance: 0\n  tolerancePct: 0.5\n\n# Example: left = 1004, right = 1000\n# |Δ| = 4; 0.5% × 1000 = 5 → Pass',
      scenarios: [
        scenario('equal', 'Matching counts', 'Pass', 'The measured counts agree.', 'Both groups match exactly. The example does not establish freshness or compare any document field.', ['Snapshot: 100 / 200', 'Replica: 100 / 200', 'Δ = 0 in both groups'], [['North', '100 ↔ 100', 'Pass'], ['South', '200 ↔ 200', 'Pass']]),
        scenario('tolerance', 'Within percentage tolerance', 'Pass', 'One passing tolerance is enough.', 'An absolute difference of four exceeds tolerance: 0, but is within 0.5% of the right side (five).', ['Left = 1004', 'Right = 1000', '|Δ| 4 ≤ 5 → Pass']),
        scenario('missing', 'One group absent on the right', 'Fail', 'A missing count becomes zero.', 'The right side has no South group, so it is compared as zero. At critical severity and zero tolerance, 200 versus zero fails.', ['South: 200', 'South: absent → 0', '|Δ| = 200 → Fail'], [['South', '200 ↔ 0', 'Fail']]),
        scenario('empty', 'Neither side has keys', 'Pass', 'An empty comparison can pass.', 'The evaluator returns “No keys to compare.” This is implemented behavior, not proof that the system contains the expected data. A companion volume check must establish presence.', ['Left: no cells', 'Right: no cells', 'Empty union → Pass'])
      ]
    },
    {
      id: 'sources', title: 'Sources & isolation', summary: 'Four built-in source kinds connect the same measure contract to different stores. Hosts compose the connections where credentials already live.',
      steps: [
        step('Register the host’s sources', 'DuckDB, Cosmos, SQL Server and file-share connectors implement ICheckSource. Multiple SQL connections use qualified names such as sql:region-a. Duplicate registry names are rejected.'),
        step('Read the selected footprint', 'DuckDB resolves a connection per measure. SQL executes the authored query with a configured timeout. Cosmos needs database and container and currently returns numeric cells only; compute an age-like number in the query and use threshold rather than a timestamp age assert. File-share measures use paths, modification times or counts.'),
        step('Separate transport failure from data failure', 'An unavailable source reports Error. Fail means an evaluated critical assertion breached. Host exit codes and operational alerting are separate integration choices.')
      ],
      choices: ['Federate runners and results; do not collect every system’s credentials into the dashboard.', 'Use query-only credentials and read-only DuckDB connections. Authored SQL is trusted code, not automatically constrained to SELECT.', 'A scalar file wildcard takes the newest matching mtime; use breakdown to see each file. Conflict-copy exclusion is host-configured for freshness, while count includes all matches.'],
      limits: 'Rastgo is a library, not a deployed scheduler. A connector registration does not prove that credentials, paths, network access or a particular host integration are available.',
      evidence: ['registry', 'sql', 'duckdb', 'cosmos', 'files', 'runner'],
      exampleTitle: 'Host composition', exampleCopy: 'API sketch using ShiftSoftware.ADP.Rastgo.Extensions. Host-specific configuration supplies every value.',
      example: 'services.AddRastgoCore(options =>\n{\n    options.FileShareBase = sourceDirectory;\n    options.ResultsRoot = resultsDirectory;\n});\nservices.AddRastgoDuckDb(\n    "Data Source=read.duckdb;ACCESS_MODE=READ_ONLY");\nservices.AddRastgoSql("region-a", readOnlyConnection);\nservices.AddRastgoCosmos(cosmosClient);',
      scenarios: [
        scenario('isolated', 'Separate checks for separate connections', 'Context', 'One unreachable tenant need not hide another.', 'Each scalar check names its own qualified SQL source. A failure in one check does not turn another check’s measured result into Error.', ['sql:region-a · sql:region-b', 'A succeeds · B unavailable', 'Independent result rows'], [['Region A', 'Measured value: 12', 'Pass'], ['Region B', 'Connection unavailable', 'Error']]),
        scenario('publish', 'Publish changes during a run', 'Context', 'Each measure reads one resolved snapshot.', 'A host factory can resolve publish A for one measure and publish B for the next. Each set is coherent, but the complete health run is not guaranteed to share one publish.', ['First measure → publish A', 'New publish becomes available', 'Next measure → publish B']),
        scenario('file', 'A missing expected file', 'Error', 'The expected artifact cannot be measured.', 'A missing plain file path produces a source error. A file-count query has different semantics: zero wildcard matches can be a numeric zero for a threshold.', ['Expected delivery path', 'File not found', 'Source Error'])
      ]
    },
    {
      id: 'history', title: 'History & dashboards', summary: 'Result rows form a shared contract between independently scheduled runners and the rendered dashboard. History is stored as partitioned JSONL today.',
      steps: [
        step('Write a distinct run', 'JsonlResultSink writes results/domain=<domain>/date=<UTC date>/<runId>.jsonl. Hosts must generate unique run IDs: writing the same domain/date/run again overwrites that file.'),
        step('Build each check’s latest view', 'CheckModel groups by CheckName, chooses the latest StartedAtUtc, and rolls up that run’s rows. Fail and Error share the highest rank; then Warn, Skipped, Pass. Use distinct names across federated packs.'),
        step('Read history with context', 'The dashboard’s stale marker compares a check’s RunId with its domain’s newest run. It identifies leftovers such as renamed or removed checks, not elapsed-age SLA failure. ReadSince bounds the trends window by UTC partitions and row time.')
      ],
      choices: ['Keep names stable to preserve trend continuity; display labels can change through DashboardOptions.', 'Each domain can run on its own schedule. Comparing all domains to one global run would mark valid results stale.', 'Rastgo supplies renderers. The host decides when to render, where to serve pages and how to authorize readers.'],
      limits: 'JSONL is the implemented sink; Parquet is not the current result format. A result contract can carry Skipped, but the core runner does not infer it. This Explorer has no connection to a result store.',
      evidence: ['sink', 'dashboard', 'runner'],
      exampleTitle: 'The persistence contract', exampleCopy: 'Shape only. Run IDs are unique per execution and should not be reused.',
      example: 'results/\n  domain=operations/\n    date=2026-10-06/\n      <unique-run-id>.jsonl\n\nCheckResult:\n  runId, checkName, domain, category, severity\n  description, order, breakdownKey\n  status, message, metrics\n  startedAtUtc, durationMs',
      scenarios: [
        scenario('federated', 'Domains run at different times', 'Context', 'Independent schedules remain valid.', 'Domain A’s newest run does not make Domain B stale. The view compares each check with the newest run inside its own domain.', ['A: run a2 · B: run b1', 'Results share one sink', 'Latest is evaluated per domain']),
        scenario('leftover', 'A check disappears from the pack', 'Context', 'History remains, with a stale marker.', 'The removed check still has an older result. When another check in that domain runs again, its old RunId marks it as a leftover in the dashboard.', ['Check existed in run a1', 'Domain now has run a2', 'Old check: stale']),
        scenario('rollup', 'Mixed group verdicts', 'Error', 'The worst group controls the check status.', 'A Pass and an Error roll up to Error. Fail and Error tie in rank; neither has a stronger rank than the other.', ['One check, two groups', 'Pass + Error', 'Rollup → Error'], [['North', 'Within range', 'Pass'], ['South', 'No timestamp', 'Error']])
      ]
    },
    {
      id: 'authoring', title: 'Authoring & discovery', summary: 'The authoring surface combines a strict check-language reference with bounded metadata discovery from the sources registered by the host.',
      steps: [
        step('Inspect the pack strictly', 'LoadForAuthoring retains the tolerant runtime parser, then reports unknown properties and semantic errors. The language currently supports age, threshold and diff; no dedicated rate, backlog, funnel or anomaly assertion is implemented.'),
        step('Discover metadata only', 'ICheckSourceCatalog is optional. Catalogs expose dataset and field metadata, not record values or connection details. The host can supply safe schema hints where discovery cannot infer structure.'),
        step('Cache and render guidance', 'AuthoringCatalogService discovers sources concurrently and isolates discovery failures. Defaults cache successful results for 15 minutes and failures for one minute; forceRefresh bypasses both.')
      ],
      choices: ['Use authoring diagnostics before execution: the runtime loader can silently ignore a misspelled property.', 'Discovery is bounded by configurable dataset, field, depth and file limits. A truncated catalog is not a complete inventory.', 'AuthoringRenderer generates documentation; saving packs, running checks and exposing a web endpoint remain host responsibilities.'],
      limits: 'Catalog discovery is metadata inspection, not a SQL execution playground or production write-back editor. Unsupported future assertions must be expressed with existing measurements and bounds, or implemented separately.',
      evidence: ['loader', 'language', 'catalog'],
      exampleTitle: 'Strict validation before a run', exampleCopy: 'API sketch. Authoring diagnostics do not change the tolerant behavior of Load.',
      example: 'var pack = YamlCheckLoader.LoadForAuthoring(\n    yaml, registry.Names);\n\nif (!pack.IsValid)\n{\n    // Present diagnostics; do not execute this pack.\n}\n\n// Common diagnostic:\n// assert.tolerence → unknown property\n// Suggested property: tolerance',
      scenarios: [
        scenario('valid', 'A supported check definition', 'Context', 'The pack can be reviewed against its sources.', 'A valid definition names an available source and an implemented assertion. Validation is still not proof that a query will execute or return the intended data.', ['Known fields and sources', 'Bounded metadata catalog', 'No blocking authoring diagnostics']),
        scenario('typo', 'Misspelled assertion property', 'Context', 'Authoring reports what runtime loading ignores.', 'An unknown tolerence property gets a diagnostic and spelling suggestion. The runtime loader alone would ignore it and leave the default tolerance.', ['assert.tolerence', 'Unknown property diagnostic', 'Correct to tolerance']),
        scenario('partial', 'One catalog source is unavailable', 'Context', 'Partial metadata stays visibly partial.', 'One discovery failure does not erase the other source catalogs. Its error is cached for a shorter period and can be refreshed explicitly.', ['Multiple registered sources', 'One discovery fails', 'Partial catalog + source error'])
      ]
    }
  ];
  globalThis.RastgoExplorer = {mechanisms};
})();
