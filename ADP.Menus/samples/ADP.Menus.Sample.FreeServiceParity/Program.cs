// Free service items matched into the FREE menus, by MENU CODE and MILEAGE.
//
// The service-items system was filled BY HAND from the exported menu; the menu lookup exists to make
// that manual step unnecessary. Before switching over, every free service item must be findable in the
// free menu: its PackageCode must be a generated free menu line's Code, and that line's service
// interval must be the item's mileage. This console only parses arguments — the audit is one call,
// FreeServiceMenuParityRunner.RunAsync, and the rules are documented on FreeServiceMenuParityAuditor.
//
// Scope: only menu variants flagged free; conditional free items (catalog items with eligibility
// conditions) are excluded; only VINs with at least one pending free item; and, with
// --invoice-date-from, only VINs invoiced on or after that date or with no invoice date.
//
// Output (reports/ by default, or --out):
//   free-service-menu-parity-details.csv — one row per compared free item
//   free-service-menu-parity-summary.csv — run parameters, scope, VIN outcomes and item totals
//
// Usage:
//   dotnet run --project ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity -- [--duckdb <path-or-connection-string>]
//     [--invoice-date-from <yyyy-MM-dd>] [--vins-file <path>] [--limit <N>] [--batch-size <N=1000>]
//     [--language <code=en>] [--country <id>] [--ignore-broker-stock] [--no-broker-stock] [--out <dir>]
//     [--hash-salt <deployment-identity-salt>] [--hash-min-length <N=5>]
//
// The store and the identity hash-id settings come, in rising precedence, from parity.local.json
// beside this file, then ADP_PARITY_DUCKDB / ADP_PARITY_HASH_SALT, then the arguments above — so once
// the local file exists a rerun is just `dotnet run --project … -- --invoice-date-from 2025-01-01`.
// parity.local.json is gitignored: the salt is the deployment's own secret and never lives in this
// repo. Its HashIdSettings section has the host's shape, so it can be pasted from the host configuration.

using DuckDB.NET.Data;
using ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;
using System.Globalization;
using System.Text.Json;

// Every number this program parses or prints is invariant — reports must not change shape with the
// machine's regional settings.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var projectDirectory = Path.Combine(FindRepoRoot(AppContext.BaseDirectory), "ADP.Menus", "samples", "ADP.Menus.Sample.FreeServiceParity");

// ---- settings ---------------------------------------------------------------------------------

var options = new FreeServiceParityRunOptions();

var localSettingsPath = Path.Combine(projectDirectory, "parity.local.json");
if (File.Exists(localSettingsPath))
{
    var local = JsonSerializer.Deserialize<LocalSettings>(
        File.ReadAllText(localSettingsPath),
        new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

    options.DuckDb = local?.DuckDb ?? options.DuckDb;
    options.HashSalt = local?.HashIdSettings?.Salt ?? options.HashSalt;
    options.HashMinLength = local?.HashIdSettings?.MinHashLength ?? options.HashMinLength;
}

options.DuckDb = Environment.GetEnvironmentVariable("ADP_PARITY_DUCKDB") ?? options.DuckDb;
options.HashSalt = Environment.GetEnvironmentVariable("ADP_PARITY_HASH_SALT") ?? options.HashSalt;

string? vinsFile = null;
string? outDir = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--duckdb": options.DuckDb = NextValue(args, ref i); break;
        case "--invoice-date-from": options.InvoiceDateFrom = ParseDate(NextValue(args, ref i)); break;
        case "--vins-file": vinsFile = NextValue(args, ref i); break;
        case "--limit": options.Limit = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--batch-size": options.BatchSize = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--language": options.Language = NextValue(args, ref i); break;
        case "--country": options.CountryId = long.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--ignore-broker-stock": options.IgnoreBrokerStock = true; break;
        case "--no-broker-stock": options.LookupBrokerStock = false; break;
        case "--hash-salt": options.HashSalt = NextValue(args, ref i); break;
        case "--hash-min-length": options.HashMinLength = int.Parse(NextValue(args, ref i), CultureInfo.InvariantCulture); break;
        case "--out": outDir = NextValue(args, ref i); break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 2;
    }
}

if (string.IsNullOrWhiteSpace(options.DuckDb))
{
    Console.Error.WriteLine("A DuckDB database is required: set DuckDb in parity.local.json, set the " +
        "ADP_PARITY_DUCKDB environment variable, or pass --duckdb <path-or-connection-string>.");
    return 2;
}

if (options.BatchSize < 1)
{
    Console.Error.WriteLine("--batch-size must be at least 1.");
    return 2;
}

if (options.Limit is < 1)
{
    Console.Error.WriteLine("--limit must be at least 1.");
    return 2;
}

if (vinsFile is not null)
    options.Vins = File.ReadAllLines(vinsFile).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

options.OutputDirectory = outDir ?? Path.Combine(projectDirectory, "reports");

// ---- run --------------------------------------------------------------------------------------

FreeServiceParityRunResult result;

try
{
    result = await FreeServiceMenuParityRunner.RunAsync(options, Console.Out);
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
catch (FormatException exception)
{
    // The classic shape of this failure is HashIdService.Decode meeting an encoded id ('d3D6X')
    // while no identity salt is registered — Decode then falls back to a plain number parse.
    Console.Error.WriteLine($"A stored id could not be decoded: {exception.Message}");
    Console.Error.WriteLine("If the store encodes company/branch/region/brand ids as hash ids, set " +
        "HashIdSettings.Salt in parity.local.json (or ADP_PARITY_HASH_SALT, or --hash-salt) to the deployment's identity salt.");
    return 4;
}

// ---- console summary --------------------------------------------------------------------------

var report = result.Report;

Console.WriteLine();
Console.WriteLine($"VINs compared: {report.VinCount:N0} of {report.RequestedVinCount:N0} requested ({result.Elapsed.TotalSeconds:N0}s)");
Console.WriteLine($"Skipped: {report.SkippedVinCount:N0} — {report.SkippedVinsInvoicedBeforeDate:N0} invoiced before the date, " +
    $"{report.SkippedVinsWithoutFreeItems:N0} with no free items, {report.SkippedVinsWithoutPendingFreeItems:N0} with none pending");
Console.WriteLine();
foreach (var (outcome, count) in report.OutcomeCounts.OrderBy(x => x.Key))
    Console.WriteLine($"  {outcome,-18} {count:N0}");
Console.WriteLine();
Console.WriteLine($"Free items: {report.TotalFreeServiceItems:N0} ({report.TotalPendingFreeServiceItems:N0} pending) | matched {report.TotalMatched:N0} " +
    $"| mileage mismatch {report.TotalMileageMismatch:N0} | code unmatched {report.TotalItemsCodeUnmatched:N0} " +
    $"| no code {report.TotalItemsWithoutMenuCode:N0} | no free menu {report.TotalItemsWithoutFreeMenu:N0} " +
    $"| conditional excluded {report.ExcludedConditionalFreeItems:N0}");
Console.WriteLine();
Console.WriteLine($"Summary: {result.SummaryCsvPath}");
Console.WriteLine($"Details: {result.DetailsCsvPath}");

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

static DateTime ParseDate(string value)
{
    if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        return date;

    Console.Error.WriteLine($"--invoice-date-from expects yyyy-MM-dd, got '{value}'.");
    Environment.Exit(2);
    return default;
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

/// <summary>parity.local.json — the machine's store and the deployment's identity hash-id settings.</summary>
record LocalSettings(string? DuckDb, LocalHashIdSettings? HashIdSettings);

/// <summary>The host's <c>HashIdSettings</c> section; only the parts the audit's decoding needs are read.</summary>
record LocalHashIdSettings(string? Salt, int? MinHashLength);
