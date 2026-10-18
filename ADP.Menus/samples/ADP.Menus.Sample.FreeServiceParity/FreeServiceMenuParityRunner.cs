using CsvHelper;
using DuckDB.NET.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ShiftEntity.Core;
using System.Globalization;

namespace ShiftSoftware.ADP.Menus.Sample.FreeServiceParity;

/// <summary>
/// One call runs the whole audit: open the store read-only, compose storage → menu lookup → vehicle
/// lookup, compare, and write the two CSVs into <see cref="FreeServiceParityRunOptions.OutputDirectory"/>:
///
/// <list type="bullet">
///   <item><c>free-service-menu-parity-details.csv</c> — one row per compared free item.</item>
///   <item><c>free-service-menu-parity-summary.csv</c> — <c>Section,Metric,Value</c>: run parameters,
///   scope, VIN outcomes and item totals.</item>
/// </list>
///
/// <para>The stack is composed by hand instead of through <c>AddLookupService</c>, because that
/// registration hard-requires a CosmosClient the audit never uses — everything here reads one DuckDB
/// file.</para>
/// </summary>
public static class FreeServiceMenuParityRunner
{
    public const string DetailsFileName = "free-service-menu-parity-details.csv";
    public const string SummaryFileName = "free-service-menu-parity-summary.csv";

    /// <exception cref="DuckDBException">The store could not be opened (missing, or held by a writer).</exception>
    /// <exception cref="FormatException">A stored hash id could not be decoded — usually a missing <see cref="FreeServiceParityRunOptions.HashSalt"/>.</exception>
    public static async Task<FreeServiceParityRunResult> RunAsync(FreeServiceParityRunOptions options, TextWriter? log = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DuckDb);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BatchSize, 1);

        if (options.Limit is < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "Limit must be at least 1.");

        Directory.CreateDirectory(options.OutputDirectory);

        var detailsPath = Path.Combine(options.OutputDirectory, DetailsFileName);
        var summaryPath = Path.Combine(options.OutputDirectory, SummaryFileName);

        // A crashed or interrupted run must never leave the previous summary sitting beside a
        // half-written details file as though they belonged together.
        File.Delete(summaryPath);

        // The same hash-id setup a DuckDB lookup host uses. Without a salt, ids are read as plain numbers.
        var services = new ServiceCollection();
        services.AddShiftEntityHashId(h =>
        {
            h.RegisterHashId(false);

            if (!string.IsNullOrWhiteSpace(options.HashSalt))
                h.RegisterIdentityHashId(options.HashSalt, options.HashMinLength);
        });
        await using var serviceProvider = services.BuildServiceProvider();
        var hashIdService = serviceProvider.GetRequiredService<IHashIdService>();

        // Read-only, always: the audit must never hold a write claim on (or create) the store.
        var connectionString = BuildReadOnlyConnectionString(options.DuckDb);
        log?.WriteLine($"Database: {connectionString}");

        using var connection = new DuckDBConnection(connectionString);
        connection.Open();

        using var menuStorage = new DuckDBServiceMenuLookupStorageService(connection);
        var menuLookup = new ServiceMenuLookupService(
            menuStorage,
            new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions())));

        var vehicleStorage = new DuckDBVehicleLookupStorageService(connection, hashIdService);

        var vehicleLookup = new VehicleLookupService(
            vehicleStorage,
            serviceProvider,
            logCosmosService: null,
            options: new LookupOptions { LookupBrokerStock = options.LookupBrokerStock },
            serviceMenuLookupService: menuLookup);

        var auditor = new FreeServiceMenuParityAuditor(
            vehicleLookup,
            new DuckDBVehicleReportService(connection, vehicleLookup),
            vehicleStorage);

        var requestOptions = new VehicleLookupRequestOptions
        {
            LanguageCode = options.Language,
            IgnoreBrokerStock = options.IgnoreBrokerStock,
            ServiceMenuOptions = new VehicleServiceMenuRequestOptions { CountryID = options.CountryId },
        };

        // --limit bounds the run either way: over a VIN list it takes the first N entries.
        var vins = options.Vins is not null && options.Limit is not null
            ? options.Vins.Take(options.Limit.Value).ToList()
            : options.Vins;

        log?.WriteLine(vins is not null
            ? $"Auditing {vins.Count:N0} listed VIN(s)…"
            : options.Limit is not null
                ? $"Auditing the first {options.Limit:N0} distinct VIN(s)…"
                : "Auditing every distinct VIN in the store…");
        log?.WriteLine(options.InvoiceDateFrom is null
            ? "Invoice date filter: none"
            : $"Invoice date filter: on or after {options.InvoiceDateFrom:yyyy-MM-dd}, or no invoice date");

        var startedAt = DateTimeOffset.UtcNow;

        var report = await auditor.ExportToCsvAsync(
            detailsPath,
            vins,
            options.Limit,
            options.BatchSize,
            requestOptions,
            options.InvoiceDateFrom);

        var elapsed = DateTimeOffset.UtcNow - startedAt;

        await WriteSummaryAsync(summaryPath, BuildSummary(report, options, connectionString, startedAt, elapsed));

        return new FreeServiceParityRunResult
        {
            Report = report,
            DetailsCsvPath = detailsPath,
            SummaryCsvPath = summaryPath,
            ConnectionString = connectionString,
            Elapsed = elapsed,
        };
    }

    public static string BuildReadOnlyConnectionString(string pathOrConnectionString)
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

    private static List<FreeServiceParitySummaryRow> BuildSummary(
        FreeServiceParityReportModel report,
        FreeServiceParityRunOptions options,
        string connectionString,
        DateTimeOffset startedAt,
        TimeSpan elapsed)
    {
        var rows = new List<FreeServiceParitySummaryRow>();

        void Add(string section, string metric, object? value) =>
            rows.Add(new(section, metric, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));

        Add("Run", "GeneratedAtUtc", startedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        Add("Run", "ElapsedSeconds", Math.Round(elapsed.TotalSeconds));
        Add("Run", "Database", connectionString);
        Add("Run", "InvoiceDateFrom", options.InvoiceDateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(none - all VINs)");
        Add("Run", "MenuVariants", "FreeOnly");
        Add("Run", "ConditionalFreeItems", "excluded");
        Add("Run", "ComparedProperties", "MenuCode + Mileage");
        Add("Run", "Language", options.Language);
        Add("Run", "Country", options.CountryId?.ToString(CultureInfo.InvariantCulture) ?? "default");
        Add("Run", "BrokerStockLookup", options.LookupBrokerStock ? "on" : "off");
        Add("Run", "VinSource", options.Vins is not null ? "list" : "store");
        Add("Run", "Limit", options.Limit?.ToString(CultureInfo.InvariantCulture) ?? "(none)");

        // Requested VINs the lookup never answered are neither compared nor skipped — say so, rather
        // than printing a scope that quietly fails to add up.
        var unanswered = report.RequestedVinCount - report.VinCount - report.SkippedVinCount;

        Add("Scope", "RequestedVins", report.RequestedVinCount);
        Add("Scope", "SkippedInvoicedBeforeDate", report.SkippedVinsInvoicedBeforeDate);
        Add("Scope", "SkippedNoFreeItems", report.SkippedVinsWithoutFreeItems);
        Add("Scope", "SkippedNonePending", report.SkippedVinsWithoutPendingFreeItems);
        Add("Scope", "NotAnsweredByLookup", unanswered);
        Add("Scope", "ComparedVins", report.VinCount);

        foreach (var outcome in Enum.GetValues<FreeServiceParityVinOutcome>())
            Add("VinOutcome", outcome.ToString(), report.OutcomeCounts.GetValueOrDefault(outcome));

        Add("Items", "FreeItemsCompared", report.TotalFreeServiceItems);
        Add("Items", "PendingFreeItemsCompared", report.TotalPendingFreeServiceItems);
        Add("Items", nameof(FreeServiceParityMatchResult.Matched), report.TotalMatched);
        Add("Items", nameof(FreeServiceParityMatchResult.MileageMismatch), report.TotalMileageMismatch);
        Add("Items", nameof(FreeServiceParityMatchResult.FreeItemCodeUnmatched), report.TotalItemsCodeUnmatched);
        Add("Items", nameof(FreeServiceParityMatchResult.FreeItemWithoutMenuCode), report.TotalItemsWithoutMenuCode);
        Add("Items", nameof(FreeServiceParityMatchResult.NoFreeMenu), report.TotalItemsWithoutFreeMenu);
        Add("Items", "ExcludedConditionalFreeItems", report.ExcludedConditionalFreeItems);
        Add("Items", "FreeItemsOnVinsWithNonePending", report.SkippedFreeServiceItems);
        Add("Items", "FreeMenuLinesGenerated", report.TotalFreeMenuLines);

        return rows;
    }

    private static async Task WriteSummaryAsync(string path, IEnumerable<FreeServiceParitySummaryRow> rows)
    {
        await using var writer = new StreamWriter(path, false);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(rows);
    }
}
