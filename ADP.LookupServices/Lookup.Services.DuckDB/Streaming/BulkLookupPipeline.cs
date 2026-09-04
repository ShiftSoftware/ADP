using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// The bulk engine's evaluation stage: takes aggregates in the order the stream yields them,
/// evaluates each on a pool of workers (one <see cref="VehicleLookupService"/> per worker — the
/// evaluators keep per-instance state), and hands every vehicle's results to one sink in the
/// stream's order. The sink is the report writer; because it sees vehicles in VIN order, the file
/// it writes is ordered the way today's per-VIN report is, and two runs over one snapshot produce
/// the same file.
/// </summary>
public static class BulkLookupPipeline
{
    /// <summary>Evaluates one aggregate under one set of request options.</summary>
    public delegate Task<VehicleLookupDTO> Evaluator(CompanyDataAggregateModel aggregate, VehicleLookupRequestOptions requestOptions);

    public sealed class Options
    {
        /// <summary>Evaluation workers. Half the cores keeps up with the stream on a developer machine.</summary>
        public int Degree { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

        /// <summary>Aggregates per unit of work handed to a worker.</summary>
        public int ChunkSize { get; set; } = 256;

        /// <summary>Chunks the stream may run ahead of the workers, and the workers ahead of the sink; bounds memory.</summary>
        public int MaxInFlightChunks { get; set; } = 64;
    }

    public sealed class Statistics
    {
        public long Vehicles { get; internal set; }
        public long Evaluations { get; internal set; }
        public TimeSpan Elapsed { get; internal set; }
    }

    /// <summary>
    /// Runs the pipeline with one <see cref="VehicleLookupService"/> per worker.
    /// <paramref name="variants"/> are the request options each vehicle is evaluated under, in
    /// order — a report that needs both "with broker stock" and "ignoring broker stock" declares
    /// two and pays for the stream once. The sink receives the VIN and one DTO per variant, on the
    /// calling thread, in stream order.
    /// </summary>
    public static Task<Statistics> RunAsync(
        IEnumerable<CompanyDataAggregateModel> aggregates,
        Func<VehicleLookupService> lookupPerWorker,
        IReadOnlyList<VehicleLookupRequestOptions> variants,
        Func<string, VehicleLookupDTO[], Task> sink,
        Options options = null,
        CancellationToken cancellationToken = default)
    {
        if (lookupPerWorker is null) throw new ArgumentNullException(nameof(lookupPerWorker));

        return RunAsync(aggregates, () =>
        {
            var lookup = lookupPerWorker();
            return (aggregate, requestOptions) => lookup.LookupAsync(aggregate, requestOptions);
        }, variants, sink, options, cancellationToken);
    }

    /// <summary>
    /// The pipeline over any evaluator; <paramref name="evaluatorPerWorker"/> is called once per
    /// worker, so whatever it returns may keep per-worker state without locking.
    /// </summary>
    public static async Task<Statistics> RunAsync(
        IEnumerable<CompanyDataAggregateModel> aggregates,
        Func<Evaluator> evaluatorPerWorker,
        IReadOnlyList<VehicleLookupRequestOptions> variants,
        Func<string, VehicleLookupDTO[], Task> sink,
        Options options = null,
        CancellationToken cancellationToken = default)
    {
        if (aggregates is null) throw new ArgumentNullException(nameof(aggregates));
        if (evaluatorPerWorker is null) throw new ArgumentNullException(nameof(evaluatorPerWorker));
        if (variants is null || variants.Count == 0) throw new ArgumentException("At least one request variant is required.", nameof(variants));
        if (sink is null) throw new ArgumentNullException(nameof(sink));

        options ??= new Options();
        var degree = Math.Max(1, options.Degree);
        var chunkSize = Math.Max(1, options.ChunkSize);
        var maxInFlight = Math.Max(degree, options.MaxInFlightChunks);

        var clock = Stopwatch.StartNew();
        var statistics = new Statistics();
        using var failure = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = failure.Token;

        using var input = new BlockingCollection<(long Sequence, CompanyDataAggregateModel[] Chunk)>(boundedCapacity: maxInFlight);
        using var output = new BlockingCollection<EvaluatedChunk>();

        var producer = Task.Run(() =>
        {
            try
            {
                long sequence = 0;
                var chunk = new List<CompanyDataAggregateModel>(chunkSize);
                foreach (var aggregate in aggregates)
                {
                    token.ThrowIfCancellationRequested();
                    chunk.Add(aggregate);
                    if (chunk.Count == chunkSize)
                    {
                        input.Add((sequence++, chunk.ToArray()), token);
                        chunk.Clear();
                    }
                }
                if (chunk.Count > 0)
                    input.Add((sequence, chunk.ToArray()), token);
            }
            finally
            {
                input.CompleteAdding();
            }
        }, CancellationToken.None);

        var workers = Enumerable.Range(0, degree).Select(_ => Task.Run(async () =>
        {
            var evaluate = evaluatorPerWorker();
            foreach (var (sequence, chunk) in input.GetConsumingEnumerable(token))
            {
                var results = new (string Vin, VehicleLookupDTO[] Lookups)[chunk.Length];
                for (var i = 0; i < chunk.Length; i++)
                {
                    var lookups = new VehicleLookupDTO[variants.Count];
                    for (var v = 0; v < variants.Count; v++)
                        lookups[v] = await evaluate(chunk[i], variants[v]);
                    results[i] = (VehicleReportRows.NormalizeVin(chunk[i].VIN), lookups);
                }
                output.Add(new EvaluatedChunk(sequence, results), token);
            }
        }, CancellationToken.None)).ToArray();

        var evaluation = Task.WhenAll(workers.Append(producer)).ContinueWith(completed =>
        {
            output.CompleteAdding();
            if (completed.IsFaulted)
                failure.Cancel();
            return completed;
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();

        // The sink sees chunks in sequence order: a chunk that finishes early waits for its
        // predecessors. The input bound caps how many can be waiting.
        var pending = new Dictionary<long, EvaluatedChunk>();
        long next = 0;
        try
        {
            foreach (var evaluated in output.GetConsumingEnumerable(CancellationToken.None))
            {
                pending[evaluated.Sequence] = evaluated;
                while (pending.Remove(next, out var ready))
                {
                    foreach (var (vin, lookups) in ready.Results)
                    {
                        await sink(vin, lookups);
                        statistics.Vehicles++;
                        statistics.Evaluations += lookups.Length;
                    }
                    next++;
                }
            }
        }
        catch
        {
            failure.Cancel();
            try { await evaluation; } catch { /* the sink's failure is the one to surface */ }
            throw;
        }

        await evaluation;                                     // surfaces a producer or worker failure
        if (pending.Count > 0)
            throw new InvalidOperationException($"The pipeline finished with {pending.Count} chunk(s) never handed to the sink; sequence {next} is missing.");

        clock.Stop();
        statistics.Elapsed = clock.Elapsed;
        return statistics;
    }

    private sealed class EvaluatedChunk
    {
        public EvaluatedChunk(long sequence, (string Vin, VehicleLookupDTO[] Lookups)[] results)
        {
            Sequence = sequence;
            Results = results;
        }

        public long Sequence { get; }
        public (string Vin, VehicleLookupDTO[] Lookups)[] Results { get; }
    }
}
