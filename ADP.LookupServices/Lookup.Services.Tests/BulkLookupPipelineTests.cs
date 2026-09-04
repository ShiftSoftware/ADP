using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using Xunit;

namespace ShiftSoftware.ADP.Lookup.Services.Tests;

/// <summary>
/// The evaluation stage's contract: whatever order the workers finish in, the sink sees every
/// vehicle exactly once, in the stream's order, with one DTO per request variant; a failing
/// evaluator fails the run instead of leaving a hole in the report.
/// </summary>
public sealed class BulkLookupPipelineTests
{
    private static IEnumerable<CompanyDataAggregateModel> Vehicles(int count) =>
        Enumerable.Range(0, count).Select(i => new CompanyDataAggregateModel { VIN = $"VIN{i:D14}" });

    [Fact]
    public async Task TheSinkSeesEveryVehicleOnce_InStreamOrder_WhateverOrderTheWorkersFinishIn()
    {
        var seen = new List<string>();
        var variants = new[] { new VehicleLookupRequestOptions(), new VehicleLookupRequestOptions { IgnoreBrokerStock = true } };

        var statistics = await BulkLookupPipeline.RunAsync(
            Vehicles(3_000),
            () =>
            {
                var random = new Random();
                return async (aggregate, requestOptions) =>
                {
                    if (random.Next(4) == 0)
                        await Task.Yield();                                    // finish out of order on purpose
                    Thread.SpinWait(random.Next(5_000));
                    return new VehicleLookupDTO { VIN = aggregate.VIN + (requestOptions.IgnoreBrokerStock ? "/ignoring" : "/with") };
                };
            },
            variants,
            (vin, lookups) =>
            {
                Assert.Equal(2, lookups.Length);
                Assert.Equal(vin + "/with", lookups[0].VIN);
                Assert.Equal(vin + "/ignoring", lookups[1].VIN);
                seen.Add(vin);
                return Task.CompletedTask;
            },
            new BulkLookupPipeline.Options { Degree = 8, ChunkSize = 7, MaxInFlightChunks = 4 });

        Assert.Equal(Vehicles(3_000).Select(v => v.VIN), seen);
        Assert.Equal(3_000, statistics.Vehicles);
        Assert.Equal(6_000, statistics.Evaluations);
    }

    [Fact]
    public async Task VinsReachTheSinkNormalized()
    {
        var seen = new List<string>();
        await BulkLookupPipeline.RunAsync(
            [new CompanyDataAggregateModel { VIN = " jtdbr32e0x0000001 " }],
            () => (aggregate, _) => Task.FromResult(new VehicleLookupDTO { VIN = aggregate.VIN }),
            [new VehicleLookupRequestOptions()],
            (vin, _) => { seen.Add(vin); return Task.CompletedTask; });

        Assert.Equal(["JTDBR32E0X0000001"], seen);
    }

    [Fact]
    public async Task AFailingEvaluatorFailsTheRun()
    {
        var delivered = 0;
        var run = BulkLookupPipeline.RunAsync(
            Vehicles(2_000),
            () => (aggregate, _) => aggregate.VIN.EndsWith("0000000000777", StringComparison.Ordinal)
                ? throw new InvalidOperationException("boom")
                : Task.FromResult(new VehicleLookupDTO { VIN = aggregate.VIN }),
            [new VehicleLookupRequestOptions()],
            (_, _) => { delivered++; return Task.CompletedTask; },
            new BulkLookupPipeline.Options { Degree = 4, ChunkSize = 50 });

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => run);
        Assert.Equal("boom", failure.Message);
        Assert.True(delivered < 2_000);
    }

    [Fact]
    public async Task AFailingSinkFailsTheRun_AndStopsTheWorkers()
    {
        var evaluated = 0;
        var run = BulkLookupPipeline.RunAsync(
            Vehicles(100_000),
            () => (aggregate, _) => { Interlocked.Increment(ref evaluated); return Task.FromResult(new VehicleLookupDTO { VIN = aggregate.VIN }); },
            [new VehicleLookupRequestOptions()],
            (vin, _) => vin.EndsWith("0000000000010", StringComparison.Ordinal) ? throw new IOException("disk full") : Task.CompletedTask,
            new BulkLookupPipeline.Options { Degree = 4, ChunkSize = 10, MaxInFlightChunks = 8 });

        await Assert.ThrowsAsync<IOException>(() => run);
        Assert.True(evaluated < 100_000, $"the workers ran on after the sink failed: {evaluated} evaluated");
    }

    [Fact]
    public async Task AnEmptyStreamCompletesWithNothingDelivered()
    {
        var statistics = await BulkLookupPipeline.RunAsync(
            [],
            () => (aggregate, _) => Task.FromResult(new VehicleLookupDTO { VIN = aggregate.VIN }),
            [new VehicleLookupRequestOptions()],
            (_, _) => throw new InvalidOperationException("nothing should reach the sink"));

        Assert.Equal(0, statistics.Vehicles);
    }
}
