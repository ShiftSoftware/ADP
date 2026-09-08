(() => {
  'use strict';
  const step = (title, detail, patch) => ({title, detail, patch});
  const scenario = (id, label, assumptions, steps) => ({id, label, assumptions, origin: 'illustrative', events: steps.map((s, i) => ({...s, at: i * 3200, step: i, origin: 'illustrative'}))});
  const graphStep = (title, detail, nodes, flows = [], values = {}) => step(title, detail, {kind: 'graph', nodes, flows, values});
  const fileCase = (id, label, change, result, assumptions, extra = {}) => {
    const skip = change === 'Unchanged';
    const absent = change === 'SourceAbsent';
    const empty = change === 'SourceEmpty';
    const checks = [true, true, change !== 'ConfigurationChanged', true, skip || change === 'ConfigurationChanged' || change === 'TrustExpired', change !== 'TrustExpired'];
    const steps = [step(absent ? 'Locate the source' : 'Ask for metadata, not file contents.', absent ? 'The configured source file cannot be found.' : 'The small probe reads file metadata. The bulk file is still on the share; no row hash has been computed.', {kind: 'file', phase: 0, change, checks, result, ...extra})];
    if (absent) steps.push(step('An absent source is not a deletion.', 'Record SkippedSourceAbsent. Keep snapshot rows and the prior successful stamp.', {phase: 6, outcome: 'guard'}));
    else if (empty) steps.push(step('An empty upload is not a purge.', 'With DeletesEnabled=true and ForceDeletes=false, a zero-byte file returns SkippedSourceEmpty before staging. Header-only input is caught after staging.', {phase: 6, outcome: 'guard'}));
    else {
      steps.push(step('Compare with the last successful stamp.', 'Metadata must be available. Stamp, configuration, path, length, last-write time, and any configured trust interval must all permit skipping.', {phase: 1}));
      if (skip) steps.push(step('Leave the large file where it is.', 'The metadata gate returns Unchanged. Skip transfer, typed staging, row hashing and merge. There is no byte-content hash on this path.', {phase: 6, outcome: 'skip'}));
      else {
        steps.push(step(change + ': take the read path.', 'A non-skip verdict reaches the ordinary reader. Full source reading is required for this scenario.', {phase: 2}));
        steps.push(step('Stage typed rows, then hash them.', 'Column binding and conversion happen first. Row hashes describe typed values; the metadata gate has not hashed the remote file.', {phase: 3}));
        if (id === 'touched') steps.push(step('Content identical; leave the table deferred.', 'For FileChanged on a deferred table only, the staged row-set identity matches the published content identity. Avoid hydration and merge; the source read has already happened.', {phase: 4}));
        else steps.push(step('Continue through the merge.', change === 'ConfigurationChanged' || change === 'TrustExpired' ? 'Configuration and trust-expiry verdicts do not use the deferred content-identity shortcut. Hydrate a deferred table before merging, even if its rows will compare equal.' : 'Different rows require the merge. If this table is deferred, load its committed rows first so comparisons and guardrails see the real baseline.', {phase: 4}));
        steps.push(step('Record the accepted outcome.', result + ' The stamp uses metadata captured before the read, and is written only after success or the explicit content-identical outcome.', {phase: 6, outcome: 'read'}));
      }
    }
    return scenario(id, label, assumptions, steps);
  };
  const fileScenarios = [
    fileCase('unchanged', 'Unchanged file', 'Unchanged', 'SkippedSourceUnchanged', 'Prior successful stamp; same config, path, size and last-write time. ReingestAfter omitted: the stamp has no expiry.'),
    fileCase('touched', 'New timestamp, identical rows', 'FileChanged', 'SkippedContentUnchanged.', 'Same config and size, new last-write time; table is deferred and the row-set content identity is equal.'),
    fileCase('changed', 'Actual row changes', 'FileChanged', 'Successful merge.', 'New file metadata and changed typed rows. Deferred baseline, valid unique keys, and no tripped guardrail.'),
    fileCase('configuration', 'Configuration or version changed', 'ConfigurationChanged', 'Successful merge, even if zero rows differ.', 'Deferred baseline; identical file metadata, changed configuration fingerprint. Manual ingest version and Hawta package version also participate.'),
    fileCase('expired', 'Configured trust interval expired', 'TrustExpired', 'Successful merge after forced reread.', 'Deferred baseline; a trust interval was explicitly configured for this sample. Omitted ReingestAfter would never expire.'),
    fileCase('absent', 'Missing file', 'SourceAbsent', 'SkippedSourceAbsent', 'File no longer exists at its configured path. The absence guard runs before the metadata gate.'),
    fileCase('empty', 'Zero-byte upload', 'SourceEmpty', 'SkippedSourceEmpty', 'The current file is zero bytes, DeletesEnabled=true, ForceDeletes=false, and metadata differs from the successful stamp.')
  ];

  const mergeScenarios = [
    ['mixed', 'New, changed, unchanged & missing', 'Full-universe sample for scope A; DeletesEnabled=true. One of three live rows is missing: below the default absolute floor of 50. Scope B is separate.', 'Commit one insert, one update and one tombstone. A is untouched; E belongs to another scope.'],
    ['same', 'Typed values are unchanged', 'The incoming string "10" becomes integer 10 before hashing. All three scope-A records match the stored typed rows.', 'No business row changes. No new change sequences and no new replication work.'],
    ['source-only', 'Source-only change', 'B changes only Note, a sample source-only column. Replication columns are Part and Qty; all keys remain present.', 'Update the stored source row. Its replication hash is unchanged, so this change does not create Cosmos work.'],
    ['deletes-off', 'Partial input: deletion disabled', 'The sample input is partial, so DeletesEnabled=false. Missing D means nothing about whether D was deleted upstream.', 'Insert C and update B. Retain D; absence in a partial input cannot tombstone it.'],
    ['duplicate', 'Duplicate identity rejected', 'Staging contains A twice. Duplicate keys are rejected before the merge transaction.', 'FailedDuplicateStagingKeys. Nothing is merged and the file success stamp must not advance.'],
    ['invalid', 'Invalid typed value', 'Incoming B has Qty="bad". This is a typed staging failure, before row hashing or SnapshotMerge.', 'The ingest fails visibly. No hashes or accepted mutations are produced for this bad input.'],
    ['wipe', 'Whole-scope wipe refused', 'A local projection, not an empty file, returns zero rows. DeletesEnabled=true and ForceDeletes=false.', 'AbortedMassDelete: every live scope-A row would disappear. A total wipe is refused at any size, even below the absolute floor.']
  ].map(([id, label, assumptions, final]) => scenario(id, label, assumptions, [
    step('Incoming rows beside the stored snapshot.', 'These are invented records for one table. A, B and D belong to scope A. Stored E belongs to scope B.', {kind: 'rows', phase: 0, case: id}),
    step(id === 'invalid' ? 'Typing fails before hashing.' : 'Normalize types before comparison.', id === 'invalid' ? final : 'The sample Part is text, Qty is integer, and Note is nullable text. Hashes below are real MD5 values for these illustrative typed records.', {phase: 1}),
    ...(id === 'invalid' ? [] : [step('Compute row and replication hashes.', 'Length-prefixed values preserve field boundaries. NULL has its own sentinel. The row hash includes Note; the narrower replication hash excludes it.', {phase: 2}),
      step(id === 'duplicate' ? 'Reject duplicate staging keys.' : 'Classify by key, hash and scope.', id === 'duplicate' ? final : 'A matching key and equal hash is unchanged. New keys insert; changed hashes update. Missing rows only become deletion candidates within the current full-universe scope.', {phase: 3}),
      ...(id === 'duplicate' ? [] : [step(id === 'wipe' ? 'The wipe guard aborts the transaction.' : 'Apply policy before accepting changes.', id === 'wipe' ? final : 'Mass-delete and mass-adoption checks run before mutation. The configured ratio and absolute thresholds are both relevant; whole-scope wipes have an additional guard.', {phase: 4}),
        ...(id === 'wipe' ? [] : [step('Accept the set as one transaction.', final, {phase: 5})])])])
  ]));

  const mechanisms = [
    {id:'file-gate', stage:'Collect & stage', title:'Read the metadata. Leave the file.', short:'File-read gate', summary:'A small check can avoid an entire read, staging pass, hash pass and merge.', kind:'file', scenarios:fileScenarios,
      evidence:['gate','fingerprint','file-early','file-content','file-stamp','gate-tests'],
      policies:['Skip requires established metadata and a prior successful stamp, equal fingerprint/path/size/last-write time, plus an unexpired trust interval if configured.','ReingestAfter is optional. Omitted means no expiry; this demo does not claim a production setting.','A changed timestamp with identical rows still requires the read. Only FileChanged on a deferred table can avoid hydration through the separate content-identity guard.','Failed and aborted ingestion does not advance the successful file stamp. The successful stamp uses metadata observed before the read.'],
      tradeoff:'Size and timestamp are not byte identity. A producer that preserves both while changing content can leave the snapshot stale; snapshot-versus-Cosmos parity cannot detect both being stale together.',
      alternative:'A per-source trust interval forces periodic rereads. It trades extra source I/O for a shorter blind interval; decide using the producer contract and measured file-read cost.',
      proposal:'Review timestamp-preserving producers and decide whether specific feeds need a per-source ReingestAfter value.', scope:'SourceChangeGate policy and the consuming host’s per-source configuration. Do not enable a global interval without review.', validation:'Verify unchanged skip; changed timestamp with identical rows; config/manual-version changes; expiry; absent and empty input; failed merge leaves stamp unchanged. Measure metadata and full-read costs separately under named storage conditions.'},
    {id:'row-merge', stage:'Reconcile the snapshot', title:'Compare typed rows. Change only what changed.', short:'Typed rows & safeguards', summary:'See the hash comparison, the mutation set, and the conditions that can stop it.', kind:'rows', scenarios:mergeScenarios,
      evidence:['hash','merge','merge-guards','merge-policy','file-content','merge-tests'],
      policies:['Typed staging precedes MD5 row hashes. The canonical recipe distinguishes NULL from empty text and separates values by their lengths.','The row hash covers source content; the replication hash covers document-affecting columns. Source-only changes need not queue Cosmos work.','Deletion is opt-in and scope-bounded. Defaults are 20% and an absolute floor of 50; both must be exceeded, except that a complete scope wipe is always guarded unless forced.','Duplicate or invalid keys/hashes fail staging. Large cross-scope adoption is guarded separately. Actual changes advance change sequences; unchanged scans do not.'],
      tradeoff:'MD5 is used for efficient change detection, not adversarial integrity. A business-key correction can appear as delete plus insert; the engine cannot prove upstream lineage.',
      alternative:'A stronger hash, different key policy or different deletion thresholds changes cost and semantics. Validate actual failure modes before changing these independently.',
      proposal:'Review the identity, source-only fields, and deletion policy for one concrete source family.', scope:'One source/table definition, its mappings and corresponding merge tests; do not widen a shared override.', validation:'Cover NULL versus empty; ambiguous field boundaries; integer normalization; source-only edits; missing rows with deletion on/off; duplicates; invalid typing; whole-scope wipe; scope isolation and adoption.'},
    {id:'fetch-window', stage:'Collect & stage', title:'Fetch ahead. Merge in a known order.', short:'Bounded fetch window', summary:'Overlap remote waits while one ordered drain owns the snapshot mutations.', kind:'graph',
      nodes:[['a','Server A · source 1','First in registry'],['b','Server A · source 2','Second in registry'],['c','Server B · source 3','Third in registry'],['q','Completed buffers','Waiting for ordered drain'],['m','Serial merge','One store owner']], edges:[['a','q'],['b','q'],['c','q'],['q','m']],
      scenarios:[scenario('order','A later fetch finishes first','Illustrative Degree=2 and MaxPerConcurrencyGroup=1. These are scenario settings, not production values.',[
        graphStep('Admit work across remote groups.','A1 and B3 can fetch together. A2 waits because Server A already has an in-flight fetch.',{a:'working',b:'waiting',c:'working',q:'idle',m:'idle'},[0,2]),
        graphStep('B3 finishes; A1 still runs.','Completion order does not determine merge order. B3 is buffered, not merged early.',{a:'working',b:'waiting',c:'passed',q:'waiting',m:'waiting'},[0],{q:'B3 ready · waiting for A1'}),
        graphStep('A1 reaches its registry position.','Drain A1 and admit A2 when its group is free. The snapshot still has one serial owner.',{a:'passed',b:'working',c:'passed',q:'waiting',m:'working'},[1,3],{m:'Merge A1',q:'B3 still waiting for A2'}),
        graphStep('A2 finishes, then B3 drains.','The accepted order is A1 → A2 → B3, even though B3 finished fetching first.',{a:'passed',b:'passed',c:'passed',q:'passed',m:'passed'},[3],{m:'A1 → A2 → B3',q:'All drained'})]),
        scenario('backlog','Backlog pauses new admission','Illustrative MaxBufferedRows=2 sample rows. Existing in-flight fetches may return more; this is not a total memory cap.',[
          graphStep('Completed work reaches the threshold.','A buffer already holds three sample rows. Ordinary admission pauses for the next source.',{a:'working',b:'waiting',c:'passed',q:'warning',m:'waiting'},[0],{q:'3 ready rows > threshold 2'}),
          graphStep('In-flight work can still arrive.','The dispatcher cannot resize fetches already admitted. The pending drain source also has a liveness escape hatch.',{a:'passed',b:'waiting',c:'passed',q:'warning',m:'working'},[3],{q:'More than 2 can be resident'}),
          graphStep('The drain frees admission capacity.','Once backlog falls and concurrency/group limits permit it, ordinary fetching can resume.',{a:'passed',b:'working',c:'passed',q:'passed',m:'working'},[1,3],{q:'Backlog reduced'})])],
      evidence:['dispatcher','fetch-events'],policies:['Degree limits concurrent fetches; group limits protect each remote box.','A blocked group can be skipped during admission so another group can use capacity.','MaxBufferedRows gates fetched-but-not-drained backlog, not total memory. In-flight fetches and a drain liveness exception can exceed it.','Merges and store bookkeeping remain serial and in registry order.'],tradeoff:'A slow earlier source can delay the drain and publish even while later fetches have finished. More parallelism can increase memory and pressure on a remote system.',alternative:'Tune degree, per-group concurrency and the backlog threshold together using real per-source latency and volume. Degree=1 disables overlapping fetches, while fetch/drain pipelining may remain.',proposal:'Benchmark one proposed fetch-window configuration against a named source roster.',scope:'SnapshotIngestDispatcher options and host configuration; no source mapping or merge-order change.',validation:'Check in-flight counts per group, out-of-order completion with ordered merges, backlog admission, overshoot, cancellations and an unreachable source. Record workload, storage, source sizes, dates and peak process memory.'},
    {id:'projection', stage:'Shape for consumers', title:'Project only when the inputs moved.', short:'Projection change gate', summary:'A change-sequence watermark answers a different question from a file timestamp.',kind:'graph',nodes:[['a','Vehicle rows','Input watermark 41'],['b','Service rows','Input watermark 46'],['g','Input + config gate','Compare successful stamp'],['p','Serving SQL','Join and shape ADP models'],['s','Serving table','Merge and stamp on success']],edges:[['a','g'],['b','g'],['g','p'],['p','s']],
      scenarios:[scenario('unchanged','Inputs unchanged','Illustrative watermark max(41,46)=46; prior successful watermark 46 and identical configuration.',[
        graphStep('Read declared input watermarks.','The sample inputs have not changed since the successful projection.',{a:'passed',b:'passed',g:'working',p:'idle',s:'idle'},[0,1]),
        graphStep('Skip the serving SQL.','Equal input watermark and fingerprint mean no projection, staging or merge is needed.',{a:'passed',b:'passed',g:'passed',p:'skipped',s:'skipped'},[],{g:'46 = 46 · fingerprint equal',p:'Skipped',s:'Existing serving rows retained'})]),
        scenario('changed','A declared input changed','Illustrative accepted service change advances the shared sequence to 47.',[
          graphStep('An accepted input change advances the watermark.','Only actual accepted changes advance the sequence; polling alone does not.',{a:'passed',b:'working',g:'working',p:'idle',s:'idle'},[1],{b:'Input watermark 47'}),
          graphStep('Run the projection and typed staging.','47 differs from the last successful watermark of 46.',{a:'passed',b:'passed',g:'passed',p:'working',s:'waiting'},[2],{g:'47 ≠ 46'}),
          graphStep('Merge, then record the captured watermark.','Stamp only after success, using the watermark read before the projection. A later input change remains eligible for the next run.',{a:'passed',b:'passed',g:'passed',p:'passed',s:'passed'},[3],{s:'Accepted · stamp 47'})]),
        scenario('config','SQL or dependency configuration changed','Input watermark is still 46; SQL/configuration fingerprint differs.',[
          graphStep('Data equality does not imply mapping equality.','The gate compares configuration as well as input watermarks.',{a:'passed',b:'passed',g:'warning',p:'idle',s:'idle'},[0,1],{g:'Fingerprint changed'}),
          graphStep('Run the revised projection.','Changed SQL, shapes, identity recipe or relevant version settings trigger a new projection.',{a:'passed',b:'passed',g:'passed',p:'working',s:'waiting'},[2]),
          graphStep('Stamp only the successful result.','All inputs must be declared; undeclared dependencies cannot reliably trigger this gate.',{a:'passed',b:'passed',g:'passed',p:'passed',s:'passed'},[3])])],
      evidence:['projection','projection-stamp','projection-tests'],policies:['The gate compares the max change-sequence watermark of all declared inputs with a successful projection stamp.','The configuration fingerprint participates independently of data changes.','Stamp the captured pre-projection watermark only on a successful merge.','Projection zero rows is not presumed a torn upload. Merge guards still protect a whole populated serving scope.'],tradeoff:'A missing dependency declaration can hide a needed recomputation. Broad dependencies are safe for freshness but can recompute unrelated output.',alternative:'Refine declared dependency boundaries only with evidence of wasted work and tests proving every output-affecting input triggers the projection.',proposal:'Audit the declared inputs and fingerprint coverage for one serving projection.',scope:'One ProjectionSnapshotIngestor definition and its tests.',validation:'Same inputs/config skip; each dependency change runs; SQL/config changes run; failed SQL and aborted merges do not stamp; a later change is not lost.'},
    {id:'replication', stage:'Cosmos delivery', title:'Acknowledge the version that actually landed.',short:'Replication & retries',summary:'Dirty state stays in the snapshot until the corresponding Cosmos operation succeeds.',kind:'graph',nodes:[['r','Snapshot row','Current version v17'],['d','Dirty predicate','Acknowledged < current'],['c','Cosmos operation','Captured v17'],['a','Success stamp','Record captured version'],['f','Failure ledger','Bounded attempts per version']],edges:[['r','d'],['d','c'],['c','a'],['c','f'],['f','d']],
      scenarios:[scenario('success','Successful delivery','v17 is an illustrative name for a captured _ReplicationModified timestamp, not a runtime change-sequence value.',[
        graphStep('Select a dirty row.','The last acknowledged version is older than the document-affecting version, or absent; attempts must also remain below the limit.',{r:'passed',d:'working',c:'idle',a:'idle',f:'idle'},[0]),
        graphStep('Capture and send v17.','Keep the captured version alongside the planned Cosmos operation.',{r:'passed',d:'passed',c:'working',a:'waiting',f:'idle'},[1]),
        graphStep('Acknowledge v17 after success.','The watermark is the captured version, not the current wall clock. Clear the failure ledger.',{r:'passed',d:'passed',c:'passed',a:'passed',f:'skipped'},[2],{a:'Acknowledged v17',d:'Clean if current is still v17'})]),
        scenario('newer','Row changes while delivery is in flight','Illustrative captured version v17; a document-affecting change creates v18 before acknowledgment.',[
          graphStep('v17 is already in flight.','The request represents the row version that was loaded.',{r:'passed',d:'passed',c:'working',a:'waiting',f:'idle'},[1]),
          graphStep('The source advances to v18.','A newer local version must not be accidentally acknowledged by the old request.',{r:'working',d:'working',c:'working',a:'waiting',f:'idle'},[0,1],{r:'Current version v18'}),
          graphStep('Acknowledge only v17.','Captured v17 is still below current v18. The row stays dirty and v18 is eligible for a later pump.',{r:'warning',d:'warning',c:'passed',a:'passed',f:'idle'},[2,0],{a:'Acknowledged v17 only',d:'Still dirty · v18 pending'})]),
        scenario('failure','Delivery fails','Illustrative failure of the current row version; no live Cosmos request is made by this demo.',[
          graphStep('Attempt the captured version.','A failure has to remain visible as unfinished work.',{r:'passed',d:'passed',c:'working',a:'waiting',f:'idle'},[1]),
          graphStep('Record failure, not success.','Update the matching version’s failure ledger. A stale failure must not penalize a newer row version.',{r:'warning',d:'warning',c:'blocked',a:'skipped',f:'warning'},[3],{a:'No success watermark'}),
          graphStep('Retry on a later cycle while attempts remain.','The batch cursor avoids burning all attempts on one failing row repeatedly in the same cycle. Exhausted attempts require attention; retries are not infinite.',{r:'warning',d:'waiting',c:'waiting',a:'skipped',f:'warning'},[4],{d:'Later cycle · if eligible'})])],
      evidence:['pump','pump-state','dirty-predicate','captured-version'],policies:['Dirty selection compares _LastReplicationDate with _ReplicationModified and checks the attempt limit.','Successful bookkeeping acknowledges the captured version, not a later row version or wall clock.','Failure bookkeeping applies only to the same captured dirty version.','Per-batch cursors and per-row attempt bookkeeping prevent endless repeated retry within one cycle.'],tradeoff:'A successful remote write followed by a crash before local bookkeeping can require safe repeat delivery. Version bookkeeping preserves pending work but is not a claim of exactly-once transport.',alternative:'Review retry limits, observability and idempotent family operations together; avoid hiding exhausted rows by treating an empty eligible queue as complete.',proposal:'Add or refine an operational view of exhausted replication attempts for a chosen family.',scope:'Read-only failure reporting and focused pump tests; no retry-policy change without separate review.',validation:'Successful capture stamp; newer version during I/O; stale failure after new version; restart after remote success; attempt exhaustion; next-cycle retry eligibility.'},
    {id:'publish',stage:'Publish & reporting',title:'Publish the files first. Reveal the set last.',short:'Atomic Parquet publishing',summary:'Reuse unchanged tables and make a complete version visible through its manifest.',kind:'graph',nodes:[['a','Table A','Changed state signature'],['b','Table B','Unchanged signature'],['f','Versioned files','Write new A · reuse B'],['m','Manifest','Names a complete set'],['r','Report reader','Resolves committed set']],edges:[['a','f'],['b','f'],['f','m'],['m','r']],
      scenarios:[scenario('normal','One changed table','Illustrative committed set P1 contains A1+B1; next set P2 changes A only. Names and timings are sample values.',[
        graphStep('Compare per-table signatures.','The signature covers business content and bookkeeping, including replication progress.',{a:'working',b:'passed',f:'idle',m:'idle',r:'passed'},[],{r:'Still reads P1 = A1 + B1'}),
        graphStep('Write A2 and reuse B1.','Changed data gets a new pinned file name. Unchanged Parquet is reused.',{a:'passed',b:'skipped',f:'working',m:'waiting',r:'passed'},[0,1],{f:'A2 written · B1 reused'}),
        graphStep('Commit manifest P2 last.','Only a complete manifest exposes A2+B1 as the new consistent set.',{a:'passed',b:'skipped',f:'passed',m:'passed',r:'passed'},[2,3],{m:'P2 → A2 + B1',r:'Reads P2 = A2 + B1'})]),
        scenario('interrupted','Interrupted before manifest commit','Illustrative interruption after new A2 exists, before P2 is committed.',[
          graphStep('A2 is written under a new version.','P1 still names A1+B1; the new file does not edit that committed set.',{a:'passed',b:'skipped',f:'working',m:'waiting',r:'passed'},[0],{r:'P1 = A1 + B1'}),
          graphStep('No new manifest, no new visible set.','The interruption leaves readers on P1. A2 alone cannot become a partially published snapshot.',{a:'passed',b:'skipped',f:'warning',m:'blocked',r:'passed'},[],{m:'P2 not committed',r:'Still P1 = A1 + B1'})]),
        scenario('bookkeeping','Only replication bookkeeping changed','Business values are identical; an acknowledgment changed recovery state.',[
          graphStep('Business content alone looks identical.','The publish signature also covers bookkeeping. A report-only content hash would miss recovery progress.',{a:'working',b:'passed',f:'idle',m:'idle',r:'passed'},[],{a:'Replication state changed'}),
          graphStep('Export the changed recovery state.','Publish new A even though the visible business columns are equal.',{a:'passed',b:'skipped',f:'working',m:'waiting',r:'passed'},[0],{f:'Updated acknowledgment stored'}),
          graphStep('Commit a recoverable set.','The manifest and referenced tables can seed recovery with completed replication work retained.',{a:'passed',b:'skipped',f:'passed',m:'passed',r:'passed'},[2,3])])],
      evidence:['publisher','publish-tests','rebuild'],policies:['Export changed tables and reuse unchanged Parquet.','Versioned files land before the manifest is committed. Readers resolve a complete committed set.','Signatures cover every bookkeeping column as well as keys and row hashes.','Retention protects files referenced by retained manifests.'],tradeoff:'Bookkeeping-only changes can cause a table export. That is deliberate because a publish is also a recovery seed.',alternative:'A separate recovery artifact could reduce report-only exports but creates another consistency contract. Treat that as a design proposal, not an existing optimization.',proposal:'Measure the share of publish work caused by business changes versus replication bookkeeping.',scope:'A read-only benchmark of SnapshotPublisher and recovery requirements before any format change.',validation:'Interrupted file upload; failure before manifest commit; unchanged table reuse; bookkeeping-only export; retained manifest references; recovery preserves completed acknowledgments.'},
    {id:'residency',stage:'Snapshot & recovery',title:'Keep quiet tables in their published home.',short:'Lazy residency & rebuild',summary:'Eligible tables can remain in Parquet until the first merge truly needs their rows.',kind:'graph',nodes:[['p','Committed Parquet','Rows + recovery state'],['g','Cold-start eligibility','All sources gateable · no owed pump work'],['w','Local write DB','Definition + deferred reference'],['h','Required merge','Hydrate the real baseline'],['r','Resident table','Stays resident this process']],edges:[['p','g'],['g','w'],['p','h'],['h','r']],
      scenarios:[scenario('defer','Eligible table stays deferred','Illustrative clean committed table; all its sources are gate-wired file sources. No pending replication prevents deferral.',[
        graphStep('Inspect the committed baseline.','A cold start uses published table and bookkeeping metadata to decide eligibility.',{p:'passed',g:'working',w:'idle',h:'idle',r:'idle'},[0]),
        graphStep('Keep rows in Parquet.','The write DB records the table definition and the deferred reference, not a copied row set.',{p:'passed',g:'passed',w:'passed',h:'skipped',r:'skipped'},[1],{w:'Deferred · rows still in Parquet'}),
        graphStep('An unchanged file does not need hydration.','A successful metadata gate can leave the table deferred across quiet cycles.',{p:'passed',g:'passed',w:'passed',h:'skipped',r:'skipped'},[],{h:'No merge needed'})]),
        scenario('hydrate','First required merge hydrates','A changed source needs a merge against a previously deferred table.',[
          graphStep('The table begins deferred.','Its real baseline is the referenced committed Parquet, not an empty local table.',{p:'passed',g:'passed',w:'passed',h:'waiting',r:'idle'},[],{w:'Deferred'}),
          graphStep('Hydrate before comparison and safeguards.','The merge loads existing rows before counting deletes or adoptions; otherwise an empty local shell could disable the guards.',{p:'working',g:'passed',w:'passed',h:'working',r:'waiting'},[2]),
          graphStep('Remain resident after the merge.','There is no general eviction loop. Becoming quiet later does not move these rows out of the running process.',{p:'passed',g:'passed',w:'passed',h:'passed',r:'passed'},[3],{r:'Resident until process ends'})])],
      evidence:['residency','rebuild','merge-guards','file-content'],policies:['Deferral is a cold-start eligibility decision, not a periodic eviction policy.','All sources for a table must support an unchanged answer without reading rows, and replication eligibility must permit deferral.','The first required merge hydrates rows before any baseline-dependent safeguards.','A table remains resident after hydration until the process ends.'],tradeoff:'A cold-start memory saving can become a one-time read cost when the table changes. Reopening an existing store and rebuilding from a new committed set have different state implications.',alternative:'General eviction is not implemented. A proposal for it must define source freshness, pending replication, reference lifetime and recovery behavior.',proposal:'Measure first-change hydration cost and eligibility for a named table before considering a residency-policy change.',scope:'One table’s source registration, startup residency and recovery tests; no eviction loop.',validation:'Cold start with/without pending replication; all-source gate eligibility; unchanged file; touched identical file; required merge hydrates before guards; warm reopen; completed bookkeeping preserved.'},
    {id:'recon',stage:'Serving assurance',title:'Separate unfinished delivery from real drift.',short:'Snapshot–Cosmos parity',summary:'This downstream reconciliation is separate from the ingestion merge.',kind:'graph',nodes:[['s','Expected documents','Snapshot + family mapping'],['c','Actual documents','Cosmos enumeration'],['g','Compare coordinates','id + partition key + document hash'],['p','Pending work','Dirty row explains the mismatch'],['d','Divergence','Settled row disagrees']],edges:[['s','g'],['c','g'],['g','p'],['g','d']],
      scenarios:[scenario('pending','A newer row is still pending','Illustrative snapshot expects new content; Cosmos has old content and the source row is still dirty.',[
        graphStep('Compare intended and actual documents.','Reconciliation covers the full expected family, not just dirty rows.',{s:'passed',c:'passed',g:'working',p:'idle',d:'idle'},[0,1]),
        graphStep('Dirty bookkeeping explains this mismatch.','Classify it as pending delivery. Pending is not automatically a content-divergence alarm.',{s:'passed',c:'passed',g:'passed',p:'warning',d:'skipped'},[2],{p:'PendingUpdate'})]),
        scenario('divergent','A settled row disagrees','Illustrative snapshot bookkeeping says the current document was delivered, but the actual Cosmos document differs.',[
          graphStep('Compare the same coordinates.','A clean row’s actual document still has to agree with the intended mapping.',{s:'passed',c:'passed',g:'working',p:'idle',d:'idle'},[0,1]),
          graphStep('Bookkeeping cannot explain the difference.','Classify it as genuine content divergence for investigation.',{s:'passed',c:'passed',g:'warning',p:'skipped',d:'blocked'},[3],{d:'DivergentContent'})]),
        scenario('blind','Snapshot and Cosmos are stale together','A timestamp-preserving source change was missed upstream. Both downstream tiers still contain the same old data.',[
          graphStep('The two downstream copies agree.','This comparison does not re-read the upstream source.',{s:'passed',c:'passed',g:'working',p:'idle',d:'idle'},[0,1]),
          graphStep('Agreement cannot establish source freshness.','There is no mismatch to classify. A source delivery/freshness contract is a separate check.',{s:'warning',c:'warning',g:'passed',p:'skipped',d:'skipped'},[],{g:'InSync here · freshness unknown'})])],
      evidence:['recon','gate','run-summary'],policies:['Expected and actual documents are compared by full document coordinates and canonical document hashes.','Pending classifications represent dirty, in-flight work. Divergent classifications describe settled state that disagrees.','Shared family document spaces must be considered together; orphan and contention categories need context.','Parity is not an upstream freshness proof.'],tradeoff:'A pending mismatch may be expected briefly but unacceptable when old. Data-delivery expectations belong to a health policy, not the raw parity count.',alternative:'Combine parity with separate source-run freshness and failure signals. Thresholds need per-source expectations rather than a single universal rule.',proposal:'Define a freshness policy for a source family alongside its parity classifications.',scope:'Read-only health policy and report wording; preserve the distinction between pending work, divergence and stale sources.',validation:'Dirty old Cosmos row is pending; settled wrong content is divergent; coordinates with different partition keys do not cross-match; both tiers stale is not treated as proof of freshness.'}
  ];
  const references = [
    {title:'Single-writer lease & recovery marker',text:'A blob lease fences the shared publish surface and Cosmos writer role. A durable active marker adds protection after an unclean exit; lease expiry alone is not automatic permission to take over.',evidence:['lease']},
    {title:'Identity & monotonic version stamps',text:'Database keys, logical keys and occurrence ordinals have different lineage guarantees. Modified stamps advance monotonically so clock regression cannot make a real change disappear from dirty selection.',evidence:['identity','merge']},
    {title:'Per-source failure containment',text:'The dispatcher reports an ingest exception through SnapshotIngestOutcome.Failure and continues with other sources. A drain callback exception still propagates. Endpoint outage suppression is a separate host policy.',evidence:['source-failure']}
  ];
  /**
   * Scene event contract v1. `at` is a relative presentation offset in milliseconds,
   * never a measured pipeline duration. `observedAt` is null for authored events.
   * An adapter may later use origin='recorded' and attach a real observation time,
   * but must supply evidence for observed transitions and explicitly retain unknowns.
   * The renderer reduces state patches; it does not infer missing runtime events.
   * No recorded/live adapter is implemented here. Mechanism and scenario identities
   * are stable across playback, evidence inspection and review-draft generation.
   */
  for (const mechanism of mechanisms) for (const scenario of mechanism.scenarios) {
    scenario.events = scenario.events.map(event => ({
      schemaVersion: 1, mechanismId: mechanism.id, scenarioId: scenario.id,
      observedAt: null, evidenceRefs: [...mechanism.evidence], ...event
    }));
  }
  function stateAt(events, elapsed) {
    let state = {}, current = 0;
    for (let i = 0; i < events.length; i++) {
      if (events[i].at > elapsed) break;
      const patch = events[i].patch;
      state = {...state, ...patch, nodes: {...state.nodes, ...patch.nodes}, values: {...state.values, ...patch.values}}; current = i;
    }
    return {state, current, event: events[current], complete: elapsed >= events.at(-1).at + 3200};
  }
  // Exploration is a complete scenario view, independent of event playback.
  // Signals identify applicable routes, never execution windows. All applicable
  // connections flow concurrently; event timestamps do not drive illustration.
  // These constants and the pose match the overview in animation.js.
  const illustrativeMotion = {period:3400, count:3, routeOffset:.17};
  function particleAt(elapsed, index, offset) {
    const fraction=(elapsed/illustrativeMotion.period+index/illustrativeMotion.count+offset)%1;
    return {fraction, opacity:.95*Math.sin(Math.PI*fraction), radius:index===0?3.4:2.4};
  }
  function explorationFor(mechanism, scenario) {
    const state = stateAt(scenario.events, Number.MAX_SAFE_INTEGER).state;
    const view = {schemaVersion:1, mechanismId:mechanism.id, scenarioId:scenario.id,
      origin:scenario.origin, kind:mechanism.kind, state, period:illustrativeMotion.period,
      nodes:[], edges:[], signals:[], defaultSelection:'summary',
      outcome:scenario.events.at(-1).detail};
    const signal = (edge,kind='signal',label='') => view.signals.push({edge,kind,label});
    const flows = (edges,kind='signal') => edges.forEach(edge=>signal(edge,kind));
    if (view.kind === 'file') {
      const skip=state.change==='Unchanged', guard=['SourceAbsent','SourceEmpty'].includes(state.change), touched=scenario.id==='touched';
      const read=!skip&&!guard, identity=read&&state.change==='FileChanged';
      view.defaultSelection='gate';
      view.nodes=[
        {id:'file',title:'Bulk file · remote share',body:state.change==='SourceAbsent'?'Source not found':skip?'File stays here · no bulk transfer':state.change==='SourceEmpty'?'Zero-byte upload':'Original stays here; read a copy',status:guard?'warning':'passed',detail:'The gate asks for file metadata, not a content hash. '+(read?'Only a non-skip verdict authorizes the full source read.':'The source guard or metadata gate preserves the current snapshot without transferring the bulk file.')},
        {id:'gate',title:guard?'Source guard':'Metadata gate',body:guard?'Before the metadata gate':state.change,status:skip?'passed':'warning',detail:guard?scenario.events.at(-1).detail:'Compare the last successful stamp with configuration, path, size, last-write time, and any configured trust interval. No row hashes exist yet. '+scenario.assumptions},
        {id:'skip',title:guard?'Preserve snapshot':'Avoid expensive work',body:skip?'No read, stage, hash or merge':guard?'No purge from missing input':touched?'Read done; hydration + merge avoided':'Metadata bypass not taken',status:skip||guard||touched?'passed':'skipped',detail:skip?scenario.events.at(-1).detail:guard?scenario.events.at(-1).detail:touched?scenario.events[4].detail:'This scenario cannot take the metadata bypass. It must compare the typed contents through the required ingest path.'},
        {id:'read',title:'Read → type → row hash',body:read?'Full read, then typed staging, then MD5':'All three operations avoided',status:read?'passed':'skipped',detail:read?'The full read happens first. Column binding and type conversion then produce staging rows. Only those typed values are hashed; a timestamp change alone does not prove row changes.':'The metadata skip or source guard stops before this entire path. No bulk read, typed staging, or row hashing occurs.'},
        {id:'identity',title:'Deferred content check',body:!read?'Not evaluated':!identity?'Shortcut not eligible':touched?'Equal row-set identity':'Different row-set identity',status:!identity?'skipped':touched?'passed':'warning',detail:identity?scenario.events[4].detail:'This shortcut is limited to FileChanged with a deferred baseline. Configuration changes and trust expiry continue to merge, even when typed rows compare equal.'},
        {id:'merge',title:'Hydrate → merge → stamp',body:read&&!touched?'Compare real baseline; stamp on success':'Hydration and merge avoided',status:read&&!touched?'passed':'skipped',detail:read&&!touched?scenario.events[4].detail+' '+scenario.events.at(-1).detail:touched?'The content-identical outcome records the metadata captured before the read. It does not hydrate or merge the unchanged deferred table.':'The existing snapshot remains intact. An unchanged gate does not need a row comparison; missing or empty input must not be interpreted as deletion.'}
      ];
      view.edges=[['file','gate'],['gate','skip'],['gate','read'],['file','read'],['read','identity'],['identity','skip'],['identity','merge']];
      if (guard) signal(1,'guard','preserve');
      else if(skip) flows([0,1],'metadata');
      else {
        flows([0,2],'metadata');flows([3,4],'rows');
        signal(touched?5:6,touched?'metadata':'rows');
      }
      return view;
    }
    if (view.kind==='rows') { view.defaultSelection='hash'; return view; }
    const explanations={
      'fetch-window':{
        a:'Degree and per-group limits admit remote fetches. Server A source 1 precedes source 2 in the registry.',
        b:'Server A source 2 waits while source 1 occupies its concurrency group. Admission and ordered draining are separate decisions.',
        c:'Server B can use a free fetch slot while Server A is occupied. Finishing first does not grant permission to merge first.',
        q:'Completed results wait for their registry position. MaxBufferedRows gates new admission; in-flight work can overshoot it. The drain has a liveness exception.',
        m:'One serial store owner merges in registry order. In this sample, the accepted order is A1 → A2 → B3.'},
      projection:{a:'The projection reads the watermarks of every declared input. A missing dependency declaration can hide necessary recomputation.',b:'Accepted input changes advance the shared change sequence; polling by itself does not.',g:'Compare the maximum declared-input watermark and configuration fingerprint with the last successful stamp.',p:'Only a changed watermark or fingerprint runs the serving SQL and typed staging. This is separate from the file-metadata gate.',s:'Merge under the usual scope safeguards. Record the pre-projection watermark only after success, leaving later input changes eligible.'},
      replication:{r:'The document-affecting version may advance while an older request is in flight. Source-only changes need not create a new replication version.',d:'A row is eligible when its acknowledgment is older than its replication version and its attempt limit permits another request.',c:'The operation carries the version captured when the work was loaded. A successful write must be safe to repeat if local acknowledgment is lost.',a:'Acknowledge the captured version, never the wall clock or a newer local row. A v17 acknowledgment cannot clear v18 work.',f:'Failure bookkeeping applies only to the same captured version. Per-cycle cursors and attempt limits bound retries; exhausted rows need attention.'},
      publish:{a:'A table signature includes keys, row hashes and recovery bookkeeping. Even acknowledgment-only changes can require export.',b:'An unchanged table reuses its pinned Parquet file. A reference signal here is not another bulk export.',f:'Write new versioned files without modifying the set named by an existing committed manifest.',m:'Commit the manifest only after every referenced file is ready. A partial upload must not expose a partial set.',r:'A reader resolves a committed manifest. Before the new manifest commits, the previous set remains the visible snapshot.'},
      residency:{p:'Committed Parquet contains both business rows and recovery state. It is the real baseline of a deferred table.',g:'Cold-start deferral requires all relevant sources to answer unchanged without reading rows, plus eligibility with respect to pending replication.',w:'A deferred table records its definition and published reference rather than copying every row into the write database.',h:'Hydrate the committed rows before any baseline-dependent comparison, mass-delete or adoption guard.',r:'Once hydrated, a table remains resident for this process. There is no periodic eviction policy.'},
      recon:{s:'Map the complete expected document family from the snapshot, not just currently dirty rows.',c:'Enumerate actual Cosmos documents and compare their full coordinates, including partition key.',g:'Canonical document hashes identify content differences. Agreement between two downstream tiers cannot establish upstream source freshness.',p:'Dirty bookkeeping can explain a mismatch as pending delivery. The age of pending work requires a separate freshness policy.',d:'A settled row that disagrees is divergence. A dirty row waiting for delivery is a different classification.'}
    };
    view.nodes=mechanism.nodes.map(([id,title,body])=>({id,title,body:state.values[id]||body,status:state.nodes[id]||'idle',detail:explanations[mechanism.id][id]}));
    view.edges=mechanism.edges;
    const edit=(id,patch)=>Object.assign(view.nodes.find(n=>n.id===id),patch);
    switch(mechanism.id) {
      case 'fetch-window':
        view.defaultSelection='q';
        edit('q',{body:scenario.id==='order'?'B3 ready first; drains last':'Admission pauses above threshold 2'});
        edit('m',{body:'Serial: A1 → A2 → B3'});
        if(scenario.id==='order') {
          signal(0,'rows','A1');signal(2,'rows','B3');signal(1,'rows','A2');signal(3,'rows');
        } else { signal(0,'rows','A1');signal(3,'rows','drain');signal(1,'rows','A2'); }
        break;
      case 'projection':
        view.defaultSelection='g';signal(0,'metadata','41');signal(1,'metadata',scenario.id==='changed'?'47':'46');
        if(scenario.id!=='unchanged') flows([2,3],'rows');
        break;
      case 'replication':
        view.defaultSelection=scenario.id==='failure'?'f':'a';
        if(scenario.id==='newer') {signal(1,'rows','v17');signal(0,'metadata','v18');signal(2,'metadata','ack v17');}
        else if(scenario.id==='failure') {flows([0,1,3]);signal(4,'metadata','if eligible');}
        else flows([0,1,2]);
        break;
      case 'publish':
        view.defaultSelection='m';signal(0,'rows','A2');
        if(scenario.id==='interrupted') {edit('m',{body:'P1 committed · P2 absent',status:'warning'});signal(3,'reference','P1');}
        else {signal(1,'reference','reuse B1');signal(2,'metadata','P2');signal(3,'reference','P2');}
        break;
      case 'residency': view.defaultSelection=scenario.id==='defer'?'w':'h';flows(scenario.id==='defer'?[0,1]:[2,3],scenario.id==='defer'?'reference':'rows');break;
      case 'recon': view.defaultSelection='g';flows([0,1]);if(scenario.id!=='blind')signal(scenario.id==='pending'?2:3);break;
    }
    return view;
  }
  window.HawtaEngineering = {mechanisms, references, stateAt, explorationFor, illustrativeMotion, particleAt};
})();
