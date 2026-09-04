using ShiftSoftware.ADP.Lookup.Services.DuckDB.Reports;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

namespace ShiftSoftware.ADP.Lookup.Services.Bench;

/// <summary>
/// A client's side of a measurement: its lookup rules, its family roster and the report files it
/// publishes. Profiles are client-owned and never live in this repository. A client repository
/// holds its profile source next to the host that owns the same options, and the bench compiles it
/// in through the <c>BenchProfileDir</c> build property (or the <c>LOOKUP_BENCH_PROFILES</c>
/// environment variable); <c>--profile=&lt;name&gt;</c> chooses among the profiles compiled in.
/// Every option in a profile moves a status, a date or a verdict; a measurement over different
/// options would time a different program, so a profile is a transcription of the host's own
/// <c>AddLookupService</c> block, kept in step with it.
/// </summary>
public interface IBenchProfile
{
    /// <summary>The name given to <c>--profile=</c>; compared case-insensitively.</summary>
    string Name { get; }

    /// <summary>The host's lookup options, transcribed; the storage source must be DuckDB.</summary>
    LookupOptions Options { get; }

    /// <summary>
    /// The families the client's source carries: the modules it runs, declared on purpose rather
    /// than discovered as empty when a table is missing. <see cref="AggregateFamilies.All"/> for a
    /// client that runs every module.
    /// </summary>
    IReadOnlyList<AggregateFamily> Families { get; }

    /// <summary>
    /// The report files the client publishes: <see cref="VehicleReports.All"/>,
    /// <see cref="VehicleReports.WithoutBrokerStock"/> for a client without brokers, or its own list.
    /// </summary>
    IReadOnlyList<VehicleReport> Reports { get; }
}

internal static class BenchProfiles
{
    /// <summary>Every profile compiled into this build, by name.</summary>
    public static IReadOnlyList<IBenchProfile> Discover() =>
        typeof(BenchProfiles).Assembly.GetTypes()
            .Where(type => typeof(IBenchProfile).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false }
                           && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (IBenchProfile)Activator.CreateInstance(type)!)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
