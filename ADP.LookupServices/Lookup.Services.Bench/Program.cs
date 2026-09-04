using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DuckDB.NET.Data;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Bench;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Reports;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ShiftEntity.Core;

// ---------------------------------------------------------------------------------------------
// The D10 measurement (bulk-lookup.md): what the evaluators cost per VIN at a 230 K-vehicle scale,
// and what the streamed data plane delivers, against the current per-VIN path on the same machine
// and the same source. Read-only against the source; writes only its results file — and, with
// --report=, the files the host stage (VehicleReportRun) produces. The source is a read snapshot, a
// Hawta store or a Hawta published set; the rules, the roster and the report set are the client's,
// compiled in as a profile (IBenchProfile, the BenchProfileDir build property) and chosen by --profile=.
// ---------------------------------------------------------------------------------------------

string Arg(string prefix, string fallback) =>
    args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..] ?? fallback;

// The client's rules, roster and report set live in the client's own repository, never here; the
// build compiles them in from BenchProfileDir (or LOOKUP_BENCH_PROFILES) and this picks one.
IBenchProfile ResolveProfile(string name)
{
    var profiles = BenchProfiles.Discover();
    var chosen = name.Length > 0
        ? profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        : profiles.Count == 1 ? profiles[0] : null;
    if (chosen is null)
    {
        Console.Error.WriteLine(profiles.Count == 0
            ? "BENCH  no profile is compiled in. Build with -p:BenchProfileDir=<directory holding the client's IBenchProfile source> (or set LOOKUP_BENCH_PROFILES): profiles are client-owned and never live in this repository."
            : $"BENCH  choose a profile with --profile=<name>; compiled in: {string.Join(", ", profiles.Select(p => p.Name))}.");
        Environment.Exit(2);
    }
    return chosen!;
}

var snapshot = Arg("--snapshot=", "");                                           // a read snapshot file (one of the three sources must be given)
var limit = int.Parse(Arg("--vins=", "0"));                                   // 0 = the whole universe
var degree = int.Parse(Arg("--degree=", Math.Max(1, Environment.ProcessorCount / 2).ToString()));
var sampleSize = int.Parse(Arg("--sample=", "20000"));
var baselineSize = int.Parse(Arg("--baseline=", "5000"));
var resultsPath = Arg("--out=", "bench-results.json");
var parityOnly = args.Contains("--parity-only");                                // skip the timed passes, diagnose parity
var diagnoseVin = Arg("--diagnose=", "");                                        // one VIN: both aggregates and both lookups, diffed
var reportDir = Arg("--report=", "");                                            // produce the profile's report files here (skips the timed passes)
var compareDir = Arg("--compare=", "");                                          // a folder holding production's copies of those files, to diff ours against
var pinnedNow = Arg("--now=", "");                                               // the instant the evaluators take as "now" (default: the system clock)
var compareOnly = args.Contains("--compare-only");                              // the files under --report= already exist; only diff them
var profileName = Arg("--profile=", "").Trim();                                  // which compiled-in profile (the only one, if omitted)
var storePath = Arg("--store=", "");                                             // a Hawta store (the write DB, or a scratch rebuild): the serving tables under data.Serving*
var publishDir = Arg("--publish=", "");                                          // a Hawta publish directory: the newest company-data-read manifest's parquet
var hashSalt = Arg("--hash-salt=", Environment.GetEnvironmentVariable("ADP_LOOKUP_HASH_SALT") ?? "");   // the host's identity hash-id salt; never a default in this source
var reportSet = Arg("--reports=", "");                                           // all | no-broker (default: the profile's report set)
var maxFailures = int.Parse(Arg("--max-failures=", "0"));                        // vehicles the evaluators may refuse before a run fails (the host stage's knob)
var reportMode = reportDir.Length > 0;
var profile = ResolveProfile(profileName);
var results = new Dictionary<string, object?> { ["machineCores"] = Environment.ProcessorCount, ["degree"] = degree, ["profile"] = profile.Name };

// ---- hash ids exactly as the report host registers them; the salt is the host's, never this file's ----
var services = new ServiceCollection();
if (hashSalt.Length > 0)
{
    services.AddShiftEntityHashId(h =>
    {
        h.RegisterHashId(false);
        h.RegisterIdentityHashId(hashSalt, 5);
    });
}
var provider = services.BuildServiceProvider();
var hashIds = hashSalt.Length > 0 ? provider.GetRequiredService<IHashIdService>() : null;

// ---- the source: the profile's roster bound to a read snapshot, a Hawta store or a published set ----
if (snapshot.Length == 0 && storePath.Length == 0 && publishDir.Length == 0)
{
    Console.Error.WriteLine("BENCH  give a source: --snapshot=<read snapshot>, --store=<Hawta write DB> or --publish=<Hawta publish directory>.");
    Environment.Exit(2);
}
var families = profile.Families;
var source = publishDir.Length > 0 ? BulkLookupSource.HawtaPublishNewest(publishDir, "company-data-read", families)
    : storePath.Length > 0 ? BulkLookupSource.HawtaStore(storePath, families)
    : BulkLookupSource.ReadSnapshot(snapshot, families, hashIds);
var options = profile.Options;
results["source"] = source.Description;

Console.WriteLine($"BENCH  profile {profile.Name} over {source.Description}");
Console.WriteLine($"BENCH  {Environment.ProcessorCount} cores, degree {degree}, limit {(limit == 0 ? "all" : limit.ToString())}, sample {sampleSize}, baseline {baselineSize}");
if (hashIds is null)
    Console.WriteLine("BENCH  no --hash-salt= (or ADP_LOOKUP_HASH_SALT): identity hash ids are not decoded — nothing to decode on a Hawta source; over a read snapshot the entries keep the ids their writer stamped. The per-VIN baseline needs the salt and is skipped.");
if (pinnedNow.Length > 0)
{
    // Statuses, claimability and warranty flags are read against a clock; pinning it to the instant
    // a production file was produced makes the comparison about the data, not the day.
    var instant = DateTimeOffset.Parse(pinnedNow, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    options.TimeProvider = new FixedTimeProvider(instant);
    Console.WriteLine($"BENCH  evaluators' clock pinned to {instant:O}");
}
var request = new VehicleLookupRequestOptions();

// A vehicle the evaluators refuse (an activation with no resolvable country, say) is counted and
// shown, then left out of the timing and the parity — the report stage has the same bound, so the
// measurement sees what a host would see.
var evaluationFailures = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
async Task<VehicleLookupDTO?> TryLookup(VehicleLookupService lookup, CompanyDataAggregateModel aggregate, VehicleLookupRequestOptions requestOptions)
{
    try
    {
        return await lookup.LookupAsync(aggregate, requestOptions);
    }
    catch (Exception exception) when (evaluationFailures.Count < Math.Max(maxFailures, 1) || evaluationFailures.ContainsKey(aggregate.VIN))
    {
        if (evaluationFailures.TryAdd(aggregate.VIN, $"{exception.GetType().Name}: {exception.Message}") && evaluationFailures.Count <= 5)
            Console.WriteLine($"FAIL   {aggregate.VIN}: {Clip(exception.Message)}");
        return null;
    }
}

// ---- 0. one vehicle, both paths, side by side ----------------------------------------------------
if (diagnoseVin.Length > 0)
{
    var vin = diagnoseVin.Trim().ToUpperInvariant();
    if (hashIds is null)
    {
        Console.WriteLine("DIAG   the per-VIN storage decodes identity hash ids: pass --hash-salt= (or ADP_LOOKUP_HASH_SALT)");
        return;
    }
    var diagnosticReference = source.LoadReference();
    CompanyDataAggregateModel? streamed = null;
    using (var stream = source.OpenStream())
        streamed = stream.FirstOrDefault(a => string.Equals(a.VIN, vin, StringComparison.Ordinal));
    using var diagnosticConnection = OpenTodaysStorageConnection();
    var todayStorage = new DuckDBVehicleLookupStorageService(diagnosticConnection, hashIds);
    var today = (await todayStorage.GetAggregatedCompanyDataForBulkLookupAsync([vin])).FirstOrDefault();
    Console.WriteLine($"DIAG   {vin}: streamed aggregate {(streamed is null ? "MISSING" : "present")}, today's aggregate {(today is null ? "MISSING" : "present")}");
    if (streamed is not null && today is not null)
    {
        Console.WriteLine("       aggregate differences (order-insensitive):");
        foreach (var line in JsonDiff(Comparable(JsonSerializer.Serialize(today)), Comparable(JsonSerializer.Serialize(streamed))))
            Console.WriteLine($"         {line}");
        var todayLookup = new VehicleLookupService(todayStorage, provider, null, options, null);
        var streamedLookup = new VehicleLookupService(diagnosticReference.ForWorker(), provider, null, options, null);
        var todayDto = Comparable(JsonSerializer.Serialize((await todayLookup.LookupAsync([vin])).First()));
        var streamedDto = Comparable(JsonSerializer.Serialize(await streamedLookup.LookupAsync(streamed, request)));
        var crossDto = Comparable(JsonSerializer.Serialize(await todayLookup.LookupAsync(today!, request)));
        Console.WriteLine("       lookup differences, today's path vs streamed (order-insensitive):");
        foreach (var line in JsonDiff(todayDto, streamedDto))
            Console.WriteLine($"         {line}");
        Console.WriteLine("       lookup differences, today's aggregate through today's storage vs the same aggregate through the preloaded storage:");
        foreach (var line in JsonDiff(todayDto, Comparable(JsonSerializer.Serialize(await streamedLookup.LookupAsync(today!, request)))))
            Console.WriteLine($"         {line}");
        Console.WriteLine("       lookup differences, today's storage over today's aggregate via the per-aggregate entry vs the bulk entry:");
        foreach (var line in JsonDiff(todayDto, crossDto))
            Console.WriteLine($"         {line}");
    }
    return;
}

// ---- 1. reference data, once ------------------------------------------------------------------
var reference = source.LoadReference();
var load = reference.Report;
Console.WriteLine($"REF    service items {load.ServiceItems}, models {load.VehicleModels}, colours {load.ExteriorColors}+{load.InteriorColors}, " +
                  $"broker stock {load.BrokerStockRows:N0}, customers {load.Customers:N0} in {load.Elapsed.TotalSeconds:F1} s");
results["reference"] = new { load.ServiceItems, load.VehicleModels, load.ExteriorColors, load.InteriorColors, load.BrokerStockRows, load.Customers, seconds = load.Elapsed.TotalSeconds };

if (!reportMode)
{
// ---- 2. the data plane alone: stream every aggregate, evaluate nothing -------------------------
if (!parityOnly)
{
    var clock = Stopwatch.StartNew();
    using var stream = source.OpenStream();
    long aggregates = 0, entries = 0, labor = 0, parts = 0;
    foreach (var aggregate in Limit(stream, limit))
    {
        aggregates++;
        entries += aggregate.VehicleEntries.Count;
        labor += aggregate.LaborLines.Count;
        parts += aggregate.PartLines.Count;
    }
    clock.Stop();
    var rows = stream.Statistics.TotalRows;
    Console.WriteLine($"STREAM {aggregates:N0} aggregates from {rows:N0} rows in {clock.Elapsed.TotalSeconds:F1} s = " +
                      $"{aggregates / clock.Elapsed.TotalSeconds:N0} VIN/s, {rows / clock.Elapsed.TotalSeconds:N0} rows/s; " +
                      $"skipped without entry {stream.Statistics.SkippedWithoutEntry:N0}, blank VINs {stream.Statistics.BlankVinRows:N0}, " +
                      $"rows with a non-canonical VIN (served by no path) {stream.Statistics.NonCanonicalVinRows:N0}; " +
                      $"peak working set {PeakGb():F1} GB");
    foreach (var family in source.Families)
        Console.WriteLine($"         {family.Table,-36} read {stream.Statistics.RowsRead[family],10:N0}  attached {stream.Statistics.RowsAttached[family],10:N0}");
    Console.WriteLine($"         unreadable cells (left at default, as today's storage does): {DuckDBModelMapperDiagnostics.UnreadableCells:N0}");
    results["stream"] = new { aggregates, rows, seconds = clock.Elapsed.TotalSeconds, vinsPerSecond = aggregates / clock.Elapsed.TotalSeconds, skippedWithoutEntry = stream.Statistics.SkippedWithoutEntry, nonCanonicalVinRows = stream.Statistics.NonCanonicalVinRows, peakGb = PeakGb() };
}

// ---- 3. evaluator CPU per VIN, single-threaded, over materialized aggregates ------------------
List<CompanyDataAggregateModel> sample;
{
    using var stream = source.OpenStream();
    sample = Limit(stream, sampleSize).ToList();
}
var singleLookup = new VehicleLookupService(reference.ForWorker(), provider, null, options, null);
foreach (var aggregate in sample.Take(200))
    await TryLookup(singleLookup, aggregate, request);                        // JIT warm-up, not timed
{
    var clock = Stopwatch.StartNew();
    var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
    long items = 0, ssc = 0, history = 0;
    foreach (var aggregate in sample)
    {
        var dto = await TryLookup(singleLookup, aggregate, request);
        if (dto is null)
            continue;
        items += dto.ServiceItems?.Count() ?? 0;
        ssc += dto.SSC?.Count() ?? 0;
        history += dto.ServiceHistory?.Count() ?? 0;
    }
    clock.Stop();
    var cpu = Process.GetCurrentProcess().TotalProcessorTime - cpuBefore;
    var perVin = clock.Elapsed.TotalMilliseconds * 1000 / sample.Count;
    Console.WriteLine($"EVAL   {sample.Count:N0} VINs single-threaded in {clock.Elapsed.TotalSeconds:F1} s = {perVin:N0} us/VIN wall, " +
                      $"{cpu.TotalMilliseconds * 1000 / sample.Count:N0} us/VIN CPU; {items:N0} service-item rows, {ssc:N0} SSC rows, {history:N0} history entries" +
                      (evaluationFailures.IsEmpty ? "" : $"; {evaluationFailures.Count:N0} vehicle(s) refused by the evaluators (FAIL lines above)"));
    results["evaluateSingle"] = new { vins = sample.Count, seconds = clock.Elapsed.TotalSeconds, microsecondsPerVinWall = perVin, microsecondsPerVinCpu = cpu.TotalMilliseconds * 1000 / sample.Count, serviceItemRows = items, sscRows = ssc };
}

// ---- 4. the whole thing, end to end: stream + parallel evaluation -----------------------------
if (!parityOnly)
{
    var clock = Stopwatch.StartNew();
    using var stream = source.OpenStream();
    using var queue = new BlockingCollection<CompanyDataAggregateModel[]>(boundedCapacity: 64);
    var producer = Task.Run(() =>
    {
        var chunk = new List<CompanyDataAggregateModel>(256);
        foreach (var aggregate in Limit(stream, limit))
        {
            chunk.Add(aggregate);
            if (chunk.Count == 256)
            {
                queue.Add(chunk.ToArray());
                chunk.Clear();
            }
        }
        if (chunk.Count > 0)
            queue.Add(chunk.ToArray());
        queue.CompleteAdding();
    });
    var workers = Enumerable.Range(0, degree).Select(_ => Task.Run(async () =>
    {
        var lookup = new VehicleLookupService(reference.ForWorker(), provider, null, options, null);
        long vins = 0, items = 0, ssc = 0;
        foreach (var chunk in queue.GetConsumingEnumerable())
        {
            foreach (var aggregate in chunk)
            {
                var dto = await TryLookup(lookup, aggregate, request);
                if (dto is null)
                    continue;
                vins++;
                items += dto.ServiceItems?.Count() ?? 0;
                ssc += dto.SSC?.Count() ?? 0;
            }
        }
        return (vins, items, ssc);
    })).ToArray();
    await producer;
    var totals = await Task.WhenAll(workers);
    clock.Stop();
    long vinsDone = totals.Sum(t => t.vins), itemRows = totals.Sum(t => t.items), sscRows = totals.Sum(t => t.ssc);
    Console.WriteLine($"FULL   {vinsDone:N0} VINs streamed and evaluated with {degree} workers in {clock.Elapsed.TotalSeconds:F1} s = " +
                      $"{vinsDone / clock.Elapsed.TotalSeconds:N0} VIN/s; {itemRows:N0} service-item rows, {sscRows:N0} SSC rows; peak working set {PeakGb():F1} GB");
    results["full"] = new { vins = vinsDone, degree, seconds = clock.Elapsed.TotalSeconds, vinsPerSecond = vinsDone / clock.Elapsed.TotalSeconds, serviceItemRows = itemRows, sscRows, peakGb = PeakGb() };
}

if (hashIds is null)
{
    Console.WriteLine("TODAY  skipped with the parity: the per-VIN storage decodes identity hash ids — pass --hash-salt= (or ADP_LOOKUP_HASH_SALT)");
}
else
{
// ---- 5. the current path, for the record: IN-list storage + the same evaluators ---------------
List<string> baselineVins;
using (var connection = OpenTodaysStorageConnection())
{
    using var command = connection.CreateCommand();
    command.CommandText = $"SELECT DISTINCT upper(trim(VIN)) AS VIN FROM VehicleEntry WHERE VIN IS NOT NULL ORDER BY 1 LIMIT {baselineSize}";
    using var reader = command.ExecuteReader();
    baselineVins = new List<string>();
    while (reader.Read())
        baselineVins.Add(reader.GetString(0));
}
Dictionary<string, string> baselineJson;
{
    using var connection = OpenTodaysStorageConnection();
    var storage = new DuckDBVehicleLookupStorageService(connection, hashIds);
    var lookup = new VehicleLookupService(storage, provider, null, options, null);
    baselineVins = baselineVins.Where(vin => !evaluationFailures.ContainsKey(vin)).ToList();   // the same refusal on either path
    await lookup.LookupAsync(baselineVins.Take(50));                            // warm-up
    var clock = Stopwatch.StartNew();
    var dtos = (await lookup.LookupAsync(baselineVins)).ToList();
    clock.Stop();
    var perVin = clock.Elapsed.TotalMilliseconds / baselineVins.Count;
    var universe = results.TryGetValue("stream", out var streamResult) && streamResult is not null
        ? (long)streamResult.GetType().GetProperty("aggregates")!.GetValue(streamResult)!
        : 230_173;
    Console.WriteLine($"TODAY  {baselineVins.Count:N0} VINs through the IN-list storage in {clock.Elapsed.TotalSeconds:F1} s = {perVin * 1000:N0} us/VIN; " +
                      $"extrapolated to {universe:N0} VINs: {perVin * universe / 1000 / 60:F1} min single-threaded");
    results["today"] = new { vins = baselineVins.Count, seconds = clock.Elapsed.TotalSeconds, microsecondsPerVin = perVin * 1000, extrapolatedMinutes = perVin * universe / 1000 / 60 };
    baselineJson = dtos.Where(d => d?.VIN is not null).ToDictionary(d => d.VIN, d => JsonSerializer.Serialize(d));
}

// ---- 6. parity: the streamed path answers exactly what today's path answers --------------------
// Two streamed evaluations of the same vehicles: through a FRESH service (nothing evaluated before)
// and through the instance that already evaluated the sample. If only the fresh one matches today's
// path, an evaluator mutates reference state it shares across vehicles — a finding about the
// evaluators, not about the stream.
{
    var parityVehicles = sample.Where(a => baselineJson.ContainsKey(a.VIN)).Take(baselineSize).ToList();
    var fresh = new VehicleLookupService(reference.ForWorker(), provider, null, options, null);
    // Every evaluation mints a signed token with a millisecond expiry per service item; those two
    // members can never agree between two evaluations and say nothing about the data, so they are
    // dropped from both sides before comparing.
    baselineJson = baselineJson.ToDictionary(pair => pair.Key, pair => Comparable(pair.Value), StringComparer.Ordinal);
    var freshJson = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var aggregate in parityVehicles)
    {
        var dto = await TryLookup(fresh, aggregate, request);
        if (dto is not null)
            freshJson[aggregate.VIN] = Comparable(JsonSerializer.Serialize(dto));
    }
    var reusedJson = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var aggregate in parityVehicles)
    {
        var dto = await TryLookup(singleLookup, aggregate, request);
        if (dto is not null)
            reusedJson[aggregate.VIN] = Comparable(JsonSerializer.Serialize(dto));
    }

    var compared = freshJson.Count;
    var identicalFresh = freshJson.Count(pair => baselineJson[pair.Key] == pair.Value);
    var identicalReused = reusedJson.Count(pair => baselineJson[pair.Key] == pair.Value);
    var freshVsReused = freshJson.Count(pair => reusedJson[pair.Key] == pair.Value);
    Console.WriteLine($"PARITY fresh service: {identicalFresh:N0} of {compared:N0} byte-identical to today's path; " +
                      $"reused service: {identicalReused:N0} of {compared:N0}; fresh vs reused agree on {freshVsReused:N0}");
    foreach (var vin in freshJson.Where(pair => baselineJson[pair.Key] != pair.Value).Select(pair => pair.Key).Take(3))
    {
        Console.WriteLine($"       fresh vs today, {vin}:");
        foreach (var line in JsonDiff(baselineJson[vin], freshJson[vin]))
            Console.WriteLine($"         {line}");
    }
    foreach (var vin in reusedJson.Where(pair => freshJson[pair.Key] != pair.Value).Select(pair => pair.Key).Take(2))
    {
        Console.WriteLine($"       reused vs fresh, {vin}:");
        foreach (var line in JsonDiff(freshJson[vin], reusedJson[vin]))
            Console.WriteLine($"         {line}");
    }
    results["parity"] = new { compared, identicalFresh, identicalReused, freshVsReused, refused = evaluationFailures.Count };
}
}
}

// ---- 7. the reports: the host stage (VehicleReportRun) over this source -------------------------
// One pass over the stream, one evaluation per distinct request, the rows built by the same
// VehicleReportRows the per-VIN report service uses, written in VIN order into the host's layout.
if (reportMode)
{
    var reports = reportSet == "all" ? VehicleReports.All
        : reportSet == "no-broker" ? VehicleReports.WithoutBrokerStock
        : profile.Reports;

    if (!compareOnly)
    {
        var run = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = source,
            Lookup = storage => new VehicleLookupService(storage, provider, null, options, null),
            OutputDirectory = reportDir,
            Reports = reports,
            Degree = degree,
            Limit = limit,
            MaxFailedVehicles = maxFailures,
        });
        Console.WriteLine($"REPORT {run.Vehicles:N0} vehicles, {run.Evaluations:N0} evaluations with {degree} workers in {run.Elapsed.TotalSeconds:F1} s " +
                          $"(reference loaded in {run.ReferenceLoad.TotalSeconds:F1} s); skipped without entry {run.SkippedWithoutEntry:N0}, blank VINs {run.BlankVinRows:N0}, " +
                          $"rows with a non-canonical VIN (served by no path) {run.NonCanonicalVinRows:N0}; " +
                          $"unreadable cells {run.UnreadableCells:N0}; peak working set {PeakGb():F1} GB");
        foreach (var file in run.Files)
            Console.WriteLine($"       {file.Report.Name,-50} {file.Rows,10:N0} rows  {file.Path}");
        if (run.Failures.Count > 0)
        {
            Console.WriteLine($"       {run.Failures.Count:N0} vehicle(s) refused by the evaluators and left out of every file:");
            foreach (var failure in run.Failures.Take(10))
                Console.WriteLine($"         {failure.Vin}: {Clip(failure.Exception.Message)}");
        }
        results["report"] = new
        {
            source = run.Source, vehicles = run.Vehicles, evaluations = run.Evaluations, degree, seconds = run.Elapsed.TotalSeconds,
            skippedWithoutEntry = run.SkippedWithoutEntry, nonCanonicalVinRows = run.NonCanonicalVinRows,
            files = run.Files.Select(file => new { name = file.Report.Name, rows = file.Rows, path = file.Path }).ToList(),
            refused = run.Failures.Select(failure => new { vin = failure.Vin, error = failure.Exception.Message }).ToList(),
            pinnedNow = pinnedNow.Length > 0 ? pinnedNow : null, peakGb = PeakGb(),
        };
    }

    // ---- 8. against production's files: same source, their path vs ours, key by key and column by column
    if (compareDir.Length > 0)
    {
        var compare = new Dictionary<string, object>();
        foreach (var report in reports)
        {
            var ours = Path.Combine(reportDir, report.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            compare[report.Name] = report == VehicleReports.TopLevel
                ? ReportParity.Compare<VehicleLookupTopLevelReportModel>(report.Name, ours, ReportParity.Find(compareDir, "vehicle-top-level-report*.parquet"), ["VIN"])
                : ReportParity.Compare<VehicleServiceItemReportModel>(report.Name, ours, ReportParity.Find(compareDir, Path.GetFileName(report.RelativePath)), ["VIN", "ServiceItemId"]);
        }
        results["compare"] = compare;
    }
}

File.WriteAllText(resultsPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"BENCH  results written to {Path.GetFullPath(resultsPath)}");

// The per-VIN storage reads bare table names. Over a Hawta source it gets TEMP views of the live
// serving rows under those names — the same relations and filters the stream is bound to — so
// "today's path" runs over exactly the rows the bulk engine reads.
DuckDBConnection OpenTodaysStorageConnection()
{
    var connection = new DuckDBConnection(source.ConnectionString);
    connection.Open();
    if (storePath.Length == 0 && publishDir.Length == 0)
        return connection;
    foreach (var family in source.Families)
        Exec(connection, $"CREATE TEMP VIEW \"{family.Table}\" AS SELECT * FROM {family.From} WHERE {family.Where}");
    Exec(connection, $"CREATE TEMP VIEW \"ServiceItem\" AS {source.Reference.ServiceItemsSql}");
    Exec(connection, $"CREATE TEMP VIEW \"VehicleModel\" AS {source.Reference.VehicleModelsSql}");
    Exec(connection, $"CREATE TEMP VIEW \"ExteriorColor\" AS {source.Reference.ExteriorColorsSql}");
    Exec(connection, $"CREATE TEMP VIEW \"InteriorColor\" AS {source.Reference.InteriorColorsSql}");
    return connection;

    static void Exec(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

static IEnumerable<CompanyDataAggregateModel> Limit(IEnumerable<CompanyDataAggregateModel> aggregates, int limit) =>
    limit <= 0 ? aggregates : aggregates.Take(limit);

static double PeakGb() => Process.GetCurrentProcess().PeakWorkingSet64 / 1024.0 / 1024.0 / 1024.0;

static string Clip(string text) => text.Length <= 300 ? text : text[..297] + "...";

static string Comparable(string json)
{
    var node = JsonNode.Parse(json);
    Strip(node);
    return node?.ToJsonString() ?? "null";

    // Arrays are compared as SETS: the per-VIN scan returns a vehicle's rows in whatever order the
    // parallel scan finished them (today's own output order moves between runs), while the stream
    // holds physical order. Content is what the report carries; order inside a vehicle's lists is
    // not a business fact on either path.
    static void Strip(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                obj.Remove("Signature");
                obj.Remove("SignatureExpiry");
                foreach (var child in obj.Select(p => p.Value).ToList())
                    Strip(child);
                break;
            case JsonArray array:
                foreach (var child in array)
                    Strip(child);
                var sorted = array.Select(child => child?.DeepClone()).OrderBy(child => child?.ToJsonString() ?? "", StringComparer.Ordinal).ToList();
                array.Clear();
                foreach (var child in sorted)
                    array.Add(child);
                break;
        }
    }
}

static List<string> JsonDiff(string today, string streamed)
{
    var lines = new List<string>();
    Walk(JsonNode.Parse(today), JsonNode.Parse(streamed), "$", lines);
    return lines;

    static void Walk(JsonNode? x, JsonNode? y, string path, List<string> lines)
    {
        if (lines.Count >= 12)
            return;
        if (x is JsonObject ox && y is JsonObject oy)
        {
            foreach (var key in ox.Select(p => p.Key).Union(oy.Select(p => p.Key)).ToList())
                Walk(ox[key], oy[key], path + "." + key, lines);
            return;
        }
        if (x is JsonArray ax && y is JsonArray ay)
        {
            if (ax.Count != ay.Count)
            {
                lines.Add($"{path}: array length today={ax.Count} streamed={ay.Count}");
                return;
            }
            for (var i = 0; i < ax.Count; i++)
                Walk(ax[i], ay[i], $"{path}[{i}]", lines);
            return;
        }
        var xs = x?.ToJsonString() ?? "null";
        var ys = y?.ToJsonString() ?? "null";
        if (xs != ys)
            lines.Add($"{path}: today={Clip(xs)} streamed={Clip(ys)}");
    }
}

/// <summary>A clock stopped at one instant, for evaluating a snapshot as of the moment production evaluated it.</summary>
sealed class FixedTimeProvider(DateTimeOffset instant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => instant;
}

/// <summary>
/// Diffs a report file we produced against production's copy with DuckDB: rows and distinct VINs on
/// each side, keys only one side has, and for the keys both have, how many rows differ per column,
/// with a few examples. Order is not compared — both files are VIN-ordered, and the reader of a
/// report does not depend on it.
/// </summary>
static class ReportParity
{
    public static string Find(string directory, string pattern)
    {
        var matches = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
        if (matches.Length == 0)
            throw new FileNotFoundException($"No {pattern} under {directory}.");
        return matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    public static object Compare<TModel>(string label, string oursPath, string theirsPath, string[] key)
    {
        var columns = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(name => !key.Contains(name, StringComparer.Ordinal))
            .ToList();

        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        Exec(connection, $"CREATE VIEW ours AS SELECT * FROM read_parquet('{Sql(oursPath)}')");
        Exec(connection, $"CREATE VIEW theirs AS SELECT * FROM read_parquet('{Sql(theirsPath)}')");

        var keyList = string.Join(", ", key.Select(Quote));
        var joinOn = string.Join(" AND ", key.Select(k => $"o.{Quote(k)} = t.{Quote(k)}"));
        var oursRows = Scalar(connection, "SELECT count(*) FROM ours");
        var theirsRows = Scalar(connection, "SELECT count(*) FROM theirs");
        var oursVins = Scalar(connection, "SELECT count(DISTINCT VIN) FROM ours");
        var theirsVins = Scalar(connection, "SELECT count(DISTINCT VIN) FROM theirs");
        var onlyOurs = Scalar(connection, $"SELECT count(*) FROM (SELECT {keyList} FROM ours EXCEPT SELECT {keyList} FROM theirs)");
        var onlyTheirs = Scalar(connection, $"SELECT count(*) FROM (SELECT {keyList} FROM theirs EXCEPT SELECT {keyList} FROM ours)");
        var matched = Scalar(connection, $"SELECT count(*) FROM ours o JOIN theirs t ON {joinOn}");

        var differing = new Dictionary<string, long>(StringComparer.Ordinal);
        var perColumn = string.Join(", ", columns.Select(c => $"sum(CASE WHEN o.{Quote(c)} IS DISTINCT FROM t.{Quote(c)} THEN 1 ELSE 0 END)"));
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT {perColumn} FROM ours o JOIN theirs t ON {joinOn}";
            using var reader = command.ExecuteReader();
            reader.Read();
            for (var i = 0; i < columns.Count; i++)
                differing[columns[i]] = reader.IsDBNull(i) ? 0 : ToLong(reader.GetValue(i));
        }
        var anyDifference = string.Join(" OR ", columns.Select(c => $"o.{Quote(c)} IS DISTINCT FROM t.{Quote(c)}"));
        var rowsDiffering = Scalar(connection, $"SELECT count(*) FROM ours o JOIN theirs t ON {joinOn} WHERE {anyDifference}");

        Console.WriteLine($"PARITY {label}: ours {oursRows:N0} rows / {oursVins:N0} VINs, production {theirsRows:N0} rows / {theirsVins:N0} VINs; " +
                          $"keys in both {matched:N0}, only ours {onlyOurs:N0}, only production {onlyTheirs:N0}; " +
                          $"rows identical {matched - rowsDiffering:N0}, differing {rowsDiffering:N0}");
        foreach (var (column, count) in differing.Where(pair => pair.Value > 0).OrderByDescending(pair => pair.Value))
        {
            Console.WriteLine($"         {column,-32} {count,10:N0} differ");
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {string.Join(", ", key.Select(k => "o." + Quote(k)))}, o.{Quote(column)}, t.{Quote(column)} FROM ours o JOIN theirs t ON {joinOn} WHERE o.{Quote(column)} IS DISTINCT FROM t.{Quote(column)} LIMIT 3";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var keys = string.Join(" ", key.Select((_, i) => reader.GetValue(i)?.ToString()));
                Console.WriteLine($"             {keys}: ours={Show(reader.GetValue(key.Length))} production={Show(reader.GetValue(key.Length + 1))}");
            }
        }
        foreach (var (side, sql) in new[] { ("only ours", $"SELECT {keyList} FROM ours EXCEPT SELECT {keyList} FROM theirs"), ("only production", $"SELECT {keyList} FROM theirs EXCEPT SELECT {keyList} FROM ours") })
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql + " LIMIT 3";
            using var reader = command.ExecuteReader();
            var examples = new List<string>();
            while (reader.Read())
                examples.Add(string.Join(" ", key.Select((_, i) => reader.GetValue(i)?.ToString())));
            if (examples.Count > 0)
                Console.WriteLine($"         {side}: {string.Join(", ", examples)}");
        }

        return new
        {
            ours = oursPath, production = theirsPath, oursRows, theirsRows, oursVins, theirsVins, matched, onlyOurs, onlyTheirs,
            rowsIdentical = matched - rowsDiffering, rowsDiffering, differingColumns = differing.Where(pair => pair.Value > 0).ToDictionary(pair => pair.Key, pair => pair.Value),
        };
    }

    static string Show(object? value) => value is null or DBNull ? "NULL" : Clip(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
    static string Clip(string text) => text.Length <= 60 ? text : text[..57] + "...";
    static string Quote(string identifier) => "\"" + identifier + "\"";
    static string Sql(string path) => path.Replace('\\', '/').Replace("'", "''");

    static long Scalar(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return ToLong(command.ExecuteScalar());
    }

    static long ToLong(object? value) => value switch
    {
        null or DBNull => 0,
        BigInteger big => (long)big,                          // DuckDB's HUGEINT, what sum() over integers returns
        _ => Convert.ToInt64(value, CultureInfo.InvariantCulture),
    };

    static void Exec(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
