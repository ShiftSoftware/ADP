// Free service items matched into the menus, by MENU CODE.
//
// The service-items system was filled BY HAND from the exported menu; the menu lookup exists to make
// that manual step unnecessary. Before switching over, every FREE service item must be findable in
// the menu — and the identity both sides share is the menu code (the item's PackageCode was
// transcribed from the generated menu Code). This audit runs the REAL bulk vehicle lookup per batch
// (service items and the WHOLE menu — all variants, the free flag is not authored yet — out of the
// same call), looks each free item up among the menu's generated lines by code, compares the
// secondary properties (mileage, description, price) on matches, and writes two reports:
//
//   reports/free-service-menu-parity-report.md   — the explanation: verdicts, totals, hot spots, caveats
//   reports/free-service-menu-parity-details.csv — one row per free item, with the menu line it found
//
// Menu lines no free item points at are EXPECTED (the menu also prices paid work) and are never
// counted against parity.
//
// Usage:
//   dotnet run --project ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity -- --duckdb <path-or-connection-string>
//     [--vins-file <path>] [--limit <N>] [--batch-size <N=1000>] [--language <code=en>]
//     [--country <id>] [--ignore-broker-stock] [--no-broker-stock] [--out <dir>]
//     [--hash-salt <deployment-identity-salt>] [--hash-min-length <N=5>]
//
// --hash-salt (or ADP_PARITY_HASH_SALT) is required when the store encodes company/branch/region/
// brand ids as identity hash ids — it is the deployment's own secret and never lives in this repo.

using DuckDB.NET.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;
using ShiftSoftware.ShiftEntity.Core;
using System.Globalization;
using System.Text;

// Every number this program parses or prints is invariant — reports must not change shape with the
// machine's regional settings.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// ---- arguments --------------------------------------------------------------------------------

string? duckDb = Environment.GetEnvironmentVariable("ADP_PARITY_DUCKDB");
string? vinsFile = null;
int? limit = null;
var batchSize = 1000;
var language = "en";
long? countryId = null;
var ignoreBrokerStock = false;
var lookupBrokerStock = true;
string? hashSalt = Environment.GetEnvironmentVariable("ADP_PARITY_HASH_SALT");
var hashMinLength = 5;
string? outDir = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--duckdb": duckDb = NextValue(args, ref i); break;
        case "--vins-file": vinsFile = NextValue(args, ref i); break;
        case "--limit": limit = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--batch-size": batchSize = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--language": language = NextValue(args, ref i); break;
        case "--country": countryId = long.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--ignore-broker-stock": ignoreBrokerStock = true; break;
        case "--no-broker-stock": lookupBrokerStock = false; break;
        case "--hash-salt": hashSalt = NextValue(args, ref i); break;
        case "--hash-min-length": hashMinLength = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--out": outDir = NextValue(args, ref i); break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 2;
    }
}

if (string.IsNullOrWhiteSpace(duckDb))
{
    Console.Error.WriteLine("A DuckDB database is required: pass --duckdb <path-or-connection-string> " +
        "or set the ADP_PARITY_DUCKDB environment variable.");
    return 2;
}

if (batchSize < 1)
{
    Console.Error.WriteLine("--batch-size must be at least 1.");
    return 2;
}

if (limit is < 1)
{
    Console.Error.WriteLine("--limit must be at least 1.");
    return 2;
}

outDir ??= Path.Combine(FindRepoRoot(AppContext.BaseDirectory), "ADP.Menus", "samples", "ADP.Menus.Sample.FreeServiceParity", "reports");
Directory.CreateDirectory(outDir);

var csvPath = Path.Combine(outDir, "free-service-menu-parity-details.csv");
var reportPath = Path.Combine(outDir, "free-service-menu-parity-report.md");

// A crashed or interrupted run must never leave the previous explanation sitting beside a
// half-written CSV as though they belonged together.
File.Delete(reportPath);

var vins = vinsFile is null ? null : File.ReadAllLines(vinsFile).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

// --limit bounds the run either way: over a VIN file it takes the first N entries.
if (vins is not null && limit is not null)
    vins = vins.Take(limit.Value).ToList();

// ---- composition ------------------------------------------------------------------------------

// The same hash-id setup a DuckDB lookup host uses. When the deployment encodes company/branch/
// region/brand ids as identity hash ids in the store, the storage's Decode(companyHashId, …) calls
// need the SAME salt the host uses — a deployment secret, so it arrives via --hash-salt (or
// ADP_PARITY_HASH_SALT), never from this repo. Without one, ids are read as plain numbers.
var services = new ServiceCollection();
services.AddShiftEntityHashId(h =>
{
    h.RegisterHashId(false);

    if (!string.IsNullOrWhiteSpace(hashSalt))
        h.RegisterIdentityHashId(hashSalt, hashMinLength);
});
await using var serviceProvider = services.BuildServiceProvider();
var hashIdService = serviceProvider.GetRequiredService<IHashIdService>();

// Read-only, always: the audit must never hold a write claim on (or create) the store.
var connectionString = BuildReadOnlyConnectionString(duckDb);
Console.WriteLine($"Database: {connectionString}");

using var connection = new DuckDBConnection(connectionString);

try
{
    connection.Open();
}
catch (DuckDBException exception)
{
    // Read-only refuses to create a missing file, and a writer (a sync, or an open DuckDB UI/CLI)
    // holds an exclusive claim — either way the store is not readable right now, and that is the
    // whole answer: nothing has been compared yet.
    Console.Error.WriteLine($"The DuckDB store could not be opened: {exception.Message.Trim()}");
    Console.Error.WriteLine("Close whatever holds it (a sync run, a DuckDB UI/CLI session), or point --duckdb at a readable copy, then rerun.");
    return 3;
}

using var menuStorage = new DuckDBServiceMenuLookupStorageService(connection);
var menuLookup = new ServiceMenuLookupService(
    menuStorage,
    new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions())));

var lookupOptions = new LookupOptions { LookupBrokerStock = lookupBrokerStock };

var vehicleLookup = new VehicleLookupService(
    new DuckDBVehicleLookupStorageService(connection, hashIdService),
    serviceProvider,
    logCosmosService: null,
    options: lookupOptions,
    serviceMenuLookupService: menuLookup);

var reportService = new DuckDBVehicleReportService(connection, vehicleLookup);
var auditor = new FreeServiceMenuParityAuditor(vehicleLookup, reportService);

// ---- run --------------------------------------------------------------------------------------

var requestOptions = new VehicleLookupRequestOptions
{
    LanguageCode = language,
    IgnoreBrokerStock = ignoreBrokerStock,
    ServiceMenuOptions = new VehicleServiceMenuRequestOptions { CountryID = countryId },
};

Console.WriteLine(vins is not null
    ? $"Auditing {vins.Count} VIN(s) from {vinsFile}…"
    : limit is not null
        ? $"Auditing the first {limit} distinct VIN(s)…"
        : "Auditing every distinct VIN in the store…");

var startedAt = DateTimeOffset.UtcNow;
FreeServiceParityReportModel report;

try
{
    report = await auditor.ExportToCsvAsync(csvPath, vins, limit, batchSize, requestOptions);
}
catch (FormatException exception)
{
    // The classic shape of this failure is HashIdService.Decode meeting an encoded id ('d3D6X')
    // while no identity salt is registered — Decode then falls back to a plain number parse.
    Console.Error.WriteLine($"A stored id could not be decoded: {exception.Message}");
    Console.Error.WriteLine("If the store encodes company/branch/region/brand ids as hash ids, rerun with " +
        "--hash-salt <the deployment's identity salt> (or set ADP_PARITY_HASH_SALT).");
    return 4;
}

var elapsed = DateTimeOffset.UtcNow - startedAt;

File.WriteAllText(reportPath, BuildExplanation(report, startedAt, elapsed, connectionString, language, countryId, lookupBrokerStock, vinsFile, limit));

// ---- console summary --------------------------------------------------------------------------

Console.WriteLine();
Console.WriteLine($"VINs answered: {report.VinCount:N0} of {report.RequestedVinCount:N0} requested ({elapsed.TotalSeconds:N0}s)");
foreach (var (outcome, count) in report.OutcomeCounts.OrderBy(x => x.Key))
    Console.WriteLine($"  {outcome,-22} {count:N0}");
Console.WriteLine();
Console.WriteLine($"Free items: {report.TotalFreeServiceItems:N0} | matched {report.TotalMatched:N0} " +
    $"(+{report.TotalMatchedWithDifferences:N0} with differences) | no code {report.TotalItemsWithoutMenuCode:N0} " +
    $"| code unmatched {report.TotalItemsCodeUnmatched:N0}");
Console.WriteLine();
Console.WriteLine($"Explanation: {reportPath}");
Console.WriteLine($"Details:     {csvPath}");

return 0;

// ---- helpers ----------------------------------------------------------------------------------

static string NextValue(string[] args, ref int i)
{
    if (i + 1 >= args.Length)
    {
        Console.Error.WriteLine($"{args[i]} expects a value.");
        Environment.Exit(2);
    }

    return args[++i];
}

static string BuildReadOnlyConnectionString(string pathOrConnectionString)
{
    // A path that exists on disk is a path, full stop — even one whose name contains '='. Only
    // otherwise does '=' mean "this is already a connection string".
    var connectionString = !File.Exists(pathOrConnectionString) && pathOrConnectionString.Contains('=')
        ? pathOrConnectionString
        : $"DataSource={pathOrConnectionString}";

    return connectionString.Contains("ACCESS_MODE", StringComparison.OrdinalIgnoreCase)
        ? connectionString
        : connectionString.TrimEnd(';') + ";ACCESS_MODE=READ_ONLY";
}

static string FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
            File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            return dir.FullName;
        dir = dir.Parent!;
    }
    throw new InvalidOperationException("Could not find the repo root (no .git directory found).");
}

static string BuildExplanation(
    FreeServiceParityReportModel report,
    DateTimeOffset startedAt,
    TimeSpan elapsed,
    string database,
    string language,
    long? countryId,
    bool lookupBrokerStock,
    string? vinsFile,
    int? limit)
{
    var text = new StringBuilder();
    var answered = Math.Max(report.VinCount, 1);
    var totalItems = Math.Max(report.TotalFreeServiceItems, 1);

    string VinPercent(int count) => ((double)count / answered).ToString("P1", CultureInfo.InvariantCulture);
    string ItemPercent(int count) => ((double)count / totalItems).ToString("P1", CultureInfo.InvariantCulture);

    text.AppendLine("# Free Service Items Matched into the Menus");
    text.AppendLine();
    text.AppendLine($"Generated {startedAt:yyyy-MM-dd HH:mm} UTC in {elapsed.TotalSeconds:N0}s. " +
        "Regenerate any time with `dotnet run --project ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity -- --duckdb <store>`; " +
        "the detail rows are in [free-service-menu-parity-details.csv](free-service-menu-parity-details.csv).");
    text.AppendLine();

    text.AppendLine("## The question");
    text.AppendLine();
    text.AppendLine("The service-items system is filled **by hand** from the exported menu; the menu definitions are");
    text.AppendLine("meant to **generate** those services automatically, and the menu lookup exists to remove the manual");
    text.AppendLine("step. Before switching over, every FREE service item must be findable in the menu. The identity");
    text.AppendLine("both sides share is the **menu code**: the item's `PackageCode` was transcribed from the very");
    text.AppendLine("`Code` the menu generator produces — so this audit looks each free item up among ALL of its");
    text.AppendLine("model's generated menu lines (every variant; the free-of-charge flag is not authored yet and is");
    text.AppendLine("not consulted). The audit is **one-way**: menu lines no free item points at are expected — the");
    text.AppendLine("menu also prices paid work — and are never counted against parity.");
    text.AppendLine();

    text.AppendLine("## How it was measured");
    text.AppendLine();
    text.AppendLine("- One **bulk vehicle lookup** per batch — the real `VehicleLookupService` pipeline over the DuckDB");
    text.AppendLine("  store, the whole service menu attached (`FreeFilter = All`); both sides come out of the same");
    text.AppendLine("  `VehicleLookupDTO`.");
    text.AppendLine("- Free service items are deduplicated exactly as the service-items report does (best row per");
    text.AppendLine("  `ServiceItemID`; items with no id still count); **all statuses count** — an expired or claimed");
    text.AppendLine("  free item is still an entitlement the menu should generate.");
    text.AppendLine("- **Match**: the item's menu code equals a generated line's `Code` (trimmed, case-insensitive).");
    text.AppendLine("  Lines are not consumed — a catalog line can answer any number of entitlements.");
    text.AppendLine("- **Then compare** (reported, never match-breaking): mileage (`MaximumMileage` vs the line's");
    text.AppendLine("  interval KM), description (item name vs line description), and price (item cost vs line total,");
    text.AppendLine("  only when the item carries a cost).");
    text.AppendLine("- An item with **no menu code at all** is its own category — it cannot be looked up, and is exactly");
    text.AppendLine("  the manual-entry gap this migration is meant to close.");
    text.AppendLine();
    text.AppendLine($"Run parameters: database `{database}`, language `{language}`, " +
        $"country `{(countryId?.ToString(CultureInfo.InvariantCulture) ?? "default")}`, " +
        $"broker-stock lookup `{(lookupBrokerStock ? "on" : "off")}`, " +
        (vinsFile is not null ? $"VINs from `{Path.GetFileName(vinsFile)}`." :
         limit is not null ? $"first {limit:N0} distinct VINs." : "every distinct VIN in the store."));
    text.AppendLine();

    text.AppendLine("## Verdict");
    text.AppendLine();
    text.AppendLine($"**{report.VinCount:N0}** VINs answered, of {report.RequestedVinCount:N0} requested. Of their " +
        $"**{report.TotalFreeServiceItems:N0}** free service items: **{report.TotalMatched:N0}** ({ItemPercent(report.TotalMatched)}) " +
        $"matched a menu line with every property agreeing, **{report.TotalMatchedWithDifferences:N0}** ({ItemPercent(report.TotalMatchedWithDifferences)}) " +
        $"matched with property differences, **{report.TotalItemsWithoutMenuCode:N0}** ({ItemPercent(report.TotalItemsWithoutMenuCode)}) " +
        $"carry no menu code, and **{report.TotalItemsCodeUnmatched:N0}** ({ItemPercent(report.TotalItemsCodeUnmatched)}) " +
        "carry a code the menu did not generate.");
    text.AppendLine();

    text.AppendLine("| Outcome | VINs | Share | Meaning |");
    text.AppendLine("|---|---:|---:|---|");

    foreach (var (outcome, count) in report.OutcomeCounts.OrderBy(x => x.Key))
    {
        var meaning = outcome switch
        {
            FreeServiceParityVinOutcome.Match => "every free item found its menu line by code; all properties agree",
            FreeServiceParityVinOutcome.MatchWithDifferences => "every free item found its menu line by code — with property differences to review in the CSV",
            FreeServiceParityVinOutcome.Mismatch => "at least one free item has no code, or a code the menu did not generate — see the CSV",
            FreeServiceParityVinOutcome.NoFreeItems => "the VIN carries no free service items — nothing to look up",
            FreeServiceParityVinOutcome.MenuNotFound => "the VIN has free items but no menu is authored under its derived basic model code",
            FreeServiceParityVinOutcome.MenuUnavailable => "the VIN has free items but the menu store could not be consulted",
            FreeServiceParityVinOutcome.MenuNotRegistered => "no menu lookup registered (should not appear here)",
            FreeServiceParityVinOutcome.NoBasicModelCode => "the VIN has free items but no Katashiki to derive a model code from",
            _ => "",
        };
        text.AppendLine($"| {outcome} | {count:N0} | {VinPercent(count)} | {meaning} |");
    }

    text.AppendLine();
    text.AppendLine("| Totals | |");
    text.AppendLine("|---|---:|");
    text.AppendLine($"| Free service items | {report.TotalFreeServiceItems:N0} |");
    text.AppendLine($"| Matched, all properties agree | {report.TotalMatched:N0} |");
    text.AppendLine($"| Matched, with property differences | {report.TotalMatchedWithDifferences:N0} |");
    text.AppendLine($"| Items with NO menu code | {report.TotalItemsWithoutMenuCode:N0} |");
    text.AppendLine($"| Items whose code the menu did not generate | {report.TotalItemsCodeUnmatched:N0} |");
    text.AppendLine($"| Menu lines generated (context — most serve paid work) | {report.TotalMenuLines:N0} |");
    text.AppendLine();

    var unmatchedModels = report.VinSummaries
        .Where(x => x.Outcome == FreeServiceParityVinOutcome.Mismatch)
        .GroupBy(x => x.BasicModelCode)
        .Select(g => new
        {
            Model = string.IsNullOrWhiteSpace(g.Key) ? "(none)" : g.Key,
            Vins = g.Count(),
            WithoutCode = g.Sum(x => x.ItemsWithoutMenuCodeCount),
            CodeUnmatched = g.Sum(x => x.ItemsCodeUnmatchedCount),
        })
        .OrderByDescending(x => x.Vins)
        .Take(20)
        .ToList();

    if (unmatchedModels.Count > 0)
    {
        text.AppendLine("## Where the unmatched items are");
        text.AppendLine();
        text.AppendLine("Mismatching VINs grouped by their derived basic model code (top 20). \"Code unmatched\" on a");
        text.AppendLine("model-shaped cluster points at menu authoring or code transcription drift; \"no code\" points at");
        text.AppendLine("service-item data entry.");
        text.AppendLine();
        text.AppendLine("| Basic model code | Mismatching VINs | Items w/o menu code | Item codes unmatched |");
        text.AppendLine("|---|---:|---:|---:|");
        foreach (var row in unmatchedModels)
            text.AppendLine($"| {row.Model} | {row.Vins:N0} | {row.WithoutCode:N0} | {row.CodeUnmatched:N0} |");
        text.AppendLine();
    }

    var differenceModels = report.VinSummaries
        .Where(x => x.MatchedWithDifferencesCount > 0)
        .GroupBy(x => x.BasicModelCode)
        .Select(g => new
        {
            Model = string.IsNullOrWhiteSpace(g.Key) ? "(none)" : g.Key,
            Vins = g.Count(),
            Pairs = g.Sum(x => x.MatchedWithDifferencesCount),
        })
        .OrderByDescending(x => x.Pairs)
        .Take(20)
        .ToList();

    if (differenceModels.Count > 0)
    {
        text.AppendLine("## Matches whose properties differ");
        text.AppendLine();
        text.AppendLine("The identity holds — the item's code found its menu line — but mileage, description or price");
        text.AppendLine("disagrees. Filter the CSV to `MatchedWithDifferences` and read the `Differences` column.");
        text.AppendLine();
        text.AppendLine("| Basic model code | VINs affected | Differing matches |");
        text.AppendLine("|---|---:|---:|");
        foreach (var row in differenceModels)
            text.AppendLine($"| {row.Model} | {row.Vins:N0} | {row.Pairs:N0} |");
        text.AppendLine();
    }

    var notFoundModels = report.VinSummaries
        .Where(x => x.Outcome == FreeServiceParityVinOutcome.MenuNotFound)
        .GroupBy(x => x.BasicModelCode)
        .Select(g => new
        {
            Model = string.IsNullOrWhiteSpace(g.Key) ? "(none)" : g.Key,
            Vins = g.Count(),
            FreeItems = g.Sum(x => x.FreeServiceItemCount),
        })
        .OrderByDescending(x => x.Vins)
        .Take(20)
        .ToList();

    if (notFoundModels.Count > 0)
    {
        text.AppendLine("## Free items whose model has no menu at all");
        text.AppendLine();
        text.AppendLine("These VINs carry free service items, but no menu is authored under their derived basic model");
        text.AppendLine("code — nothing the menu side could ever generate for them (top 20 models by VIN count).");
        text.AppendLine();
        text.AppendLine("| Basic model code | VINs | Free items on them |");
        text.AppendLine("|---|---:|---:|");
        foreach (var row in notFoundModels)
            text.AppendLine($"| {row.Model} | {row.Vins:N0} | {row.FreeItems:N0} |");
        text.AppendLine();
    }

    text.AppendLine("## Reading the numbers honestly");
    text.AppendLine();
    text.AppendLine("- **Menu codes are language-dependent.** This run generated codes under " + $"`{language}`; a");
    text.AppendLine("  `PackageCode` transcribed from another language's export will not match — rerun with that");
    text.AppendLine("  `--language` before reading such misses as real.");
    text.AppendLine("- **Menu lines without a free item are not misses.** The menu prices the whole service programme,");
    text.AppendLine("  paid work included; only the free service items' side is being audited.");
    text.AppendLine("- **The audit runs with library-default `LookupOptions`** — no host resolvers, no warranty-period");
    text.AppendLine("  or distributor configuration. Item *statuses* (expired, activation-required) can differ from a");
    text.AppendLine("  production host; the *set* of items generally does not.");
    text.AppendLine("- **The store is as fresh as its last sync.** Both sides read the same DuckDB file, so the");
    text.AppendLine("  comparison is internally consistent even when the file lags the source systems.");
    text.AppendLine();

    text.AppendLine("## The detail file");
    text.AppendLine();
    text.AppendLine("`free-service-menu-parity-details.csv` — one row per free service item: `MatchResult` ∈ `Matched` /");
    text.AppendLine("`MatchedWithDifferences` / `FreeItemWithoutMenuCode` / `FreeItemCodeUnmatched`; the `Differences`");
    text.AppendLine("column spells out property disagreements. The columns are laid out for reading: each compared pair");
    text.AppendLine("sits side by side, the item's value immediately left of the menu's —");
    text.AppendLine("`ItemMenuCode | MenuLineCode`, `ItemMaximumMileage | MenuIntervalKm`,");
    text.AppendLine("`ServiceItemName | MenuDescription`, `ItemCost | MenuTotalPrice` — followed by the item-only");
    text.AppendLine("context (`ServiceItemId`…`ItemClaimDate`) and the menu-only context (`MenuVariantId`…, including");
    text.AppendLine("`MenuVariantIsFree` for when the flag starts being authored).");

    return text.ToString();
}
