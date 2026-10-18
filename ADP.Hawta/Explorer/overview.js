window.ExplorerOverview = {
  explanations: {
    collect: ['↓', 'Collect & stage', 'Collect from the systems that know.', 'This example brings dealer views, file feeds, app tables and document sources into one ingestion flow. Hawta schedules each source and limits parallel fetching to keep the work bounded.'],
    snapshot: ['≡', 'Reconcile the snapshot', 'Keep the changes. Skip the repeated work.', 'Hawta compares incoming records with its DuckDB snapshot and merges changes in sets. Unchanged file feeds can skip the read, while the snapshot preserves what each source said.'],
    serving: ['⇢', 'Shape for consumers', 'Keep the source. Build a useful view.', 'Serving projections turn source-shaped tables into ADP models. Consumers share those prepared views; the original source tables remain available alongside them.'],
    cosmos: ['↗', 'Cosmos DB', 'Deliver changes to application lookups.', 'Hawta replicates changes from source or serving tables for the data families it owns. App-owned records keep their existing replication path, so every family has one Cosmos writer.'],
    publish: ['↗', 'Versioned Parquet', 'Give reporting a consistent picture.', 'Hawta publishes a versioned set of source and serving tables in its output directory. Reports, analytics and health checks can read that committed set. The host configures storage delivery, independently of Cosmos serving.']
  },
  routes: [['dms','collect',0],['feeds','collect',1],['apps','collect',2],['logs','collect',3],['serving','cosmos',0],['snapshot','cosmos',3,true],['snapshot','publish',1,true],['serving','publish',2]]
};
