using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Reports;

/// <summary>
/// One of the vehicle reports: the file it lands in, the evaluation its rows come from, and how one
/// vehicle's lookup becomes rows. The definitions live in <see cref="VehicleReports"/>.
/// </summary>
public abstract class VehicleReport
{
    private protected VehicleReport(string name, string relativePath, VehicleLookupRequestOptions request)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public string Name { get; }
    /// <summary>Where the file lands under a run's output directory — the report host's published layout, which its consumers read today.</summary>
    public string RelativePath { get; }
    /// <summary>The request the vehicle is evaluated with for this report. Reports with the same request share one evaluation.</summary>
    public VehicleLookupRequestOptions Request { get; }

    internal abstract Writer OpenWriter(string partialPath, string finalPath);

    internal abstract class Writer
    {
        protected Writer(string partialPath, string finalPath)
        {
            PartialPath = partialPath;
            FinalPath = finalPath;
        }

        public string PartialPath { get; }
        public string FinalPath { get; }
        public abstract long RowCount { get; }
        public abstract void Add(string vin, VehicleLookupDTO lookup);
        public abstract Task FlushAsync();
        public abstract Task CompleteAsync();
    }
}

public sealed class VehicleReport<TModel> : VehicleReport
{
    public VehicleReport(string name, string relativePath, VehicleLookupRequestOptions request, Func<string, VehicleLookupDTO, IEnumerable<TModel>> rows)
        : base(name, relativePath, request)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>One vehicle's rows, from its normalized VIN and its lookup — the same row builders the per-VIN report service uses.</summary>
    public Func<string, VehicleLookupDTO, IEnumerable<TModel>> Rows { get; }

    internal override Writer OpenWriter(string partialPath, string finalPath) => new TypedWriter(this, partialPath, finalPath);

    private sealed class TypedWriter : Writer
    {
        private readonly VehicleReport<TModel> report;
        private readonly ParquetReportFile<TModel> file;
        private readonly List<TModel> buffer = new List<TModel>();

        public TypedWriter(VehicleReport<TModel> report, string partialPath, string finalPath)
            : base(partialPath, finalPath)
        {
            this.report = report;
            file = new ParquetReportFile<TModel>(partialPath);
        }

        public override long RowCount => file.RowCount;

        public override void Add(string vin, VehicleLookupDTO lookup) => buffer.AddRange(report.Rows(vin, lookup));

        public override async Task FlushAsync()
        {
            if (buffer.Count == 0)
                return;
            await file.AppendAsync(buffer);
            buffer.Clear();
        }

        public override Task CompleteAsync() => file.CompleteAsync();
    }
}

/// <summary>The vehicle reports as a report host publishes them: the file names, the layout and the rows are the host's.</summary>
public static class VehicleReports
{
    public static readonly VehicleReport ServiceItems = new VehicleReport<VehicleServiceItemReportModel>(
        "vehicle-service-items-report", "ServiceItem/vehicle-service-items-report.parquet",
        new VehicleLookupRequestOptions(), VehicleReportRows.ServiceItems);

    public static readonly VehicleReport ServiceItemsIgnoringBrokerStock = new VehicleReport<VehicleServiceItemReportModel>(
        "vehicle-service-items-report-ignore-broker-stock", "ServiceItem/vehicle-service-items-report-ignore-broker-stock.parquet",
        new VehicleLookupRequestOptions { IgnoreBrokerStock = true }, VehicleReportRows.ServiceItems);

    public static readonly VehicleReport TopLevel = new VehicleReport<VehicleLookupTopLevelReportModel>(
        "vehicle-top-level-report", "Vehicle/vehicle-top-level-report.parquet",
        new VehicleLookupRequestOptions(), (vin, lookup) => new[] { VehicleReportRows.TopLevel(vin, lookup) });

    /// <summary>The three files of a host with broker stock.</summary>
    public static IReadOnlyList<VehicleReport> All { get; } = new[] { ServiceItems, ServiceItemsIgnoringBrokerStock, TopLevel };

    /// <summary>The set for a client without brokers, where the two service-item files would be the same file.</summary>
    public static IReadOnlyList<VehicleReport> WithoutBrokerStock { get; } = new[] { ServiceItems, TopLevel };
}

/// <summary>
/// The host stage of the bulk engine (bulk-lookup.md step 4): one pass over a source, every
/// vehicle evaluated once per distinct request, the requested report files written in VIN order
/// and promoted together. A full recompute of a 230 K-vehicle universe takes under a minute on
/// sixteen workers, so this is what a host runs on its cadence; the typed report
/// table merged by key (D8) stays the intraday optimisation for a host that needs one.
///
/// <para>Every file is written under a partial name and promoted only when every file of the run
/// is complete, so a consumer never reads a torn file or this run's top-level file beside the
/// previous run's service items. A failed run leaves the previous files untouched and no partial
/// behind.</para>
/// </summary>
public static class VehicleReportRun
{
    public sealed class Options
    {
        /// <summary>Where the vehicles and the reference data come from.</summary>
        public BulkLookupSource Source { get; set; }
        /// <summary>
        /// The host's lookup service over one worker's reference storage — the host's own options
        /// (rules, clock, signing), the engine's storage. Called once per worker.
        /// </summary>
        public Func<IVehicleLookupStorageService, VehicleLookupService> Lookup { get; set; }
        public string OutputDirectory { get; set; }
        public IReadOnlyList<VehicleReport> Reports { get; set; } = VehicleReports.All;
        public int Degree { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
        /// <summary>Vehicles buffered before the rows are appended to the files.</summary>
        public int FlushEvery { get; set; } = 5_000;
        /// <summary>Stop after this many vehicles (0 = the whole universe). A harness knob, not a host's.</summary>
        public int Limit { get; set; }
        /// <summary>
        /// How many vehicles the evaluators may refuse before the run fails. A refused vehicle (an
        /// evaluator threw — an activation with no resolvable country, say) is left out of every file
        /// and listed in <see cref="Result.Failures"/>; nothing is written for it, nothing is guessed.
        /// The default tolerates none: a host that publishes over known data defects raises this
        /// knowingly, and reads the failures it gets back. A systematic failure — a rule that refuses
        /// every vehicle — still fails the run at the bound instead of publishing an empty file.
        /// </summary>
        public int MaxFailedVehicles { get; set; }
    }

    public sealed class VehicleFailure
    {
        public VehicleFailure(string vin, Exception exception)
        {
            Vin = vin;
            Exception = exception;
        }

        public string Vin { get; }
        public Exception Exception { get; }
    }

    public sealed class ReportFile
    {
        public ReportFile(VehicleReport report, string path, long rows)
        {
            Report = report;
            Path = path;
            Rows = rows;
        }

        public VehicleReport Report { get; }
        public string Path { get; }
        public long Rows { get; }
    }

    public sealed class Result
    {
        public string Source { get; internal set; }
        public long Vehicles { get; internal set; }
        public long Evaluations { get; internal set; }
        public TimeSpan Elapsed { get; internal set; }
        public TimeSpan ReferenceLoad { get; internal set; }
        public long SkippedWithoutEntry { get; internal set; }
        public long BlankVinRows { get; internal set; }
        /// <summary>Rows whose stored VIN is not canonical, served by no path: see <see cref="VinOrderedAggregateStream.StreamStatistics.NonCanonicalVinRows"/>.</summary>
        public long NonCanonicalVinRows { get; internal set; }
        /// <summary>Cells the mapper could not read during this run, left at their default — a data-quality number.</summary>
        public long UnreadableCells { get; internal set; }
        public IReadOnlyList<ReportFile> Files { get; internal set; }
        /// <summary>The vehicles the evaluators refused, within <see cref="Options.MaxFailedVehicles"/>: not in any file, the host's to report.</summary>
        public IReadOnlyList<VehicleFailure> Failures { get; internal set; }
    }

    /// <summary>The refused vehicles, one entry per VIN however many requests it failed, up to the bound.</summary>
    private sealed class FailureLedger
    {
        private readonly int bound;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, VehicleFailure> failures =
            new System.Collections.Concurrent.ConcurrentDictionary<string, VehicleFailure>(StringComparer.Ordinal);

        public FailureLedger(int bound) => this.bound = bound;

        /// <summary>True when the failure is absorbed (the vehicle is dropped); false when it exceeds the bound and must fail the run.</summary>
        public bool Record(string vin, Exception exception)
        {
            var key = vin ?? "";
            if (failures.ContainsKey(key))
                return true;
            if (failures.Count >= bound)
                return false;
            failures.TryAdd(key, new VehicleFailure(key, exception));
            return true;
        }

        public bool Contains(string vin) => failures.ContainsKey(vin ?? "");

        public IReadOnlyList<VehicleFailure> ToList() => failures.Values.OrderBy(f => f.Vin, StringComparer.Ordinal).ToList();
    }

    public static async Task<Result> RunAsync(Options options, CancellationToken cancellationToken = default)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (options.Source is null) throw new ArgumentException("A source is required.", nameof(options));
        if (options.Lookup is null) throw new ArgumentException("A lookup factory is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.OutputDirectory)) throw new ArgumentException("An output directory is required.", nameof(options));
        if (options.Reports is null || options.Reports.Count == 0) throw new ArgumentException("Request at least one report.", nameof(options));
        if (options.FlushEvery <= 0) throw new ArgumentException("FlushEvery must be positive.", nameof(options));

        var reports = options.Reports;
        var outputDirectory = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        // Reports that ask for the same request share one evaluation: the variants are the
        // distinct requests, by content, in the order the reports name them.
        var variants = new List<VehicleLookupRequestOptions>();
        var variantKeys = new List<string>();
        var variantOfReport = new int[reports.Count];
        for (var i = 0; i < reports.Count; i++)
        {
            var key = JsonSerializer.Serialize(reports[i].Request);
            var index = variantKeys.IndexOf(key);
            if (index < 0)
            {
                index = variants.Count;
                variants.Add(reports[i].Request);
                variantKeys.Add(key);
            }
            variantOfReport[i] = index;
        }

        var clock = Stopwatch.StartNew();
        var unreadableBefore = DuckDBModelMapperDiagnostics.UnreadableCells;
        var reference = options.Source.LoadReference();
        var referenceLoad = clock.Elapsed;

        var writers = reports.Select(report =>
        {
            var finalPath = Path.Combine(outputDirectory, report.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            return report.OpenWriter(finalPath + ".partial", finalPath);
        }).ToArray();

        var failures = new FailureLedger(options.MaxFailedVehicles);
        var promoted = false;
        try
        {
            using var stream = options.Source.OpenStream();
            var buffered = 0;
            var statistics = await BulkLookupPipeline.RunAsync(
                options.Limit > 0 ? stream.Take(options.Limit) : stream,
                () =>
                {
                    var lookup = options.Lookup(reference.ForWorker());
                    return async (aggregate, request) =>
                    {
                        try
                        {
                            return await lookup.LookupAsync(aggregate, request);
                        }
                        catch (Exception exception) when (failures.Record(VehicleReportRows.NormalizeVin(aggregate.VIN), exception))
                        {
                            return null;                      // refused within the bound: dropped from every file, listed in the result
                        }
                    };
                },
                variants,
                async (vin, lookups) =>
                {
                    if (failures.Contains(vin))
                        return;
                    for (var i = 0; i < writers.Length; i++)
                        writers[i].Add(vin, lookups[variantOfReport[i]]);
                    if (++buffered >= options.FlushEvery)
                    {
                        foreach (var writer in writers)
                            await writer.FlushAsync();
                        buffered = 0;
                    }
                },
                new BulkLookupPipeline.Options { Degree = options.Degree },
                cancellationToken);

            foreach (var writer in writers)
            {
                await writer.FlushAsync();
                await writer.CompleteAsync();
            }

            // Every file is complete before any is promoted.
            foreach (var writer in writers)
                File.Move(writer.PartialPath, writer.FinalPath, overwrite: true);
            promoted = true;
            clock.Stop();

            return new Result
            {
                Source = options.Source.Description,
                Vehicles = statistics.Vehicles,
                Evaluations = statistics.Evaluations,
                Elapsed = clock.Elapsed,
                ReferenceLoad = referenceLoad,
                SkippedWithoutEntry = stream.Statistics.SkippedWithoutEntry,
                BlankVinRows = stream.Statistics.BlankVinRows,
                NonCanonicalVinRows = stream.Statistics.NonCanonicalVinRows,
                UnreadableCells = DuckDBModelMapperDiagnostics.UnreadableCells - unreadableBefore,
                Files = writers.Select(writer => new ReportFile(reports[Array.IndexOf(writers, writer)], writer.FinalPath, writer.RowCount)).ToList(),
                Failures = failures.ToList(),
            };
        }
        finally
        {
            if (!promoted)
            {
                foreach (var writer in writers)
                {
                    try { File.Delete(writer.PartialPath); } catch { /* best effort: a partial must not outlive a failed run */ }
                }
            }
        }
    }
}
