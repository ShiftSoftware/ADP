window.ExplorerOverview = {
  explanations: {
    collect: ['↓', 'Resolve & measure', 'Read the fact that matters.', 'Rastgo reads host-registered sources. Measures return a scalar value or keyed groups; checks and their measures run sequentially. Credentials and query-only access belong to the host.'],
    snapshot: ['≡', 'Apply the policy', 'Make the expectation explicit.', 'Age, threshold and difference assertions compare measurements with authored limits. Critical breaches Fail; other severities Warn. A source failure remains Error.'],
    serving: ['⇢', 'Emit verdict rows', 'Keep the reason with the verdict.', 'Each result carries the check identity, optional group, metrics, message, run ID and timing. A source error stops assertion evaluation for the entire check.'],
    cosmos: ['↗', 'JSONL history', 'Preserve a history per domain.', 'The JSONL sink partitions results by domain and run. The host must provide unique run IDs: reusing one overwrites its corresponding file. Writing a result is separate from reading a dashboard.'],
    publish: ['↗', 'Dashboard & trends', 'Read the verdict in context.', 'The dashboard combines domain histories, selects the newest results per check name and rolls up the worst verdict. Per-domain staleness remains visible; trends retain measurements over time.']
  },
  routes: [['dms','collect',0],['feeds','collect',1],['apps','collect',2],['logs','collect',3],['serving','cosmos',0],['cosmos','publish',0,false,true]]
};
