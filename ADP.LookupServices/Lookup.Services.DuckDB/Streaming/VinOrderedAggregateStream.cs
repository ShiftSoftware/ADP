using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// The bulk data plane (bulk-lookup.md D8): one VIN-ordered scan per declared family, merged
/// k-way into one <see cref="CompanyDataAggregateModel"/> per VIN, in VIN order. Nothing is held
/// beyond the current vehicle's rows per family: no VIN lists, no IN-clauses, no reflection per
/// cell, no JSON of the universe. The per-VIN storage assembles the same aggregate from the same
/// tables with the same filters, so an evaluator cannot tell which path fed it.
///
/// <para>By default a VIN with rows but no <c>VehicleEntry</c> is skipped and counted — the reports
/// have always taken their universe from the entries — and every skip is in
/// <see cref="Statistics"/>, because "rows nobody serves" is a number worth watching.</para>
/// </summary>
public sealed class VinOrderedAggregateStream : IEnumerable<CompanyDataAggregateModel>, IDisposable
{
    private readonly string connectionString;
    private readonly IReadOnlyList<AggregateFamily> families;
    private readonly bool requireVehicleEntry;
    private readonly IHashIdService hashIdService;
    private readonly List<VinOrderedFamilyReader> openReaders = new List<VinOrderedFamilyReader>();

    /// <param name="hashIdService">
    /// When given, a vehicle entry's CompanyID / BranchID / RegionID / BrandID are decoded from its
    /// hash ids, exactly as <c>DuckDBVehicleLookupStorageService</c> does when it loads entries —
    /// the hash is the value the writer stamped, the id beside it may be stale or absent.
    /// </param>
    public VinOrderedAggregateStream(string connectionString, IReadOnlyList<AggregateFamily> families, bool requireVehicleEntry = true, IHashIdService hashIdService = null)
    {
        this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        this.families = families ?? throw new ArgumentNullException(nameof(families));
        if (families.Count == 0)
            throw new ArgumentException("Declare at least one family to stream.", nameof(families));
        this.requireVehicleEntry = requireVehicleEntry;
        this.hashIdService = hashIdService;
        Statistics = new StreamStatistics(families);
    }

    public StreamStatistics Statistics { get; }

    public IEnumerator<CompanyDataAggregateModel> GetEnumerator()
    {
        var readers = families.Select(family => new VinOrderedFamilyReader(connectionString, family)).ToArray();
        openReaders.AddRange(readers);
        var attachedThisVin = new long[readers.Length];
        try
        {
            foreach (var reader in readers)
                reader.MoveNext();

            while (true)
            {
                string vin = null;
                foreach (var reader in readers)
                {
                    if (!reader.Exhausted && (vin is null || string.CompareOrdinal(reader.CurrentVin, vin) < 0))
                        vin = reader.CurrentVin;
                }
                if (vin is null)
                    break;

                var aggregate = new CompanyDataAggregateModel { VIN = vin };
                var hasEntry = false;
                Array.Clear(attachedThisVin, 0, attachedThisVin.Length);
                for (var i = 0; i < readers.Length; i++)
                {
                    var reader = readers[i];
                    while (!reader.Exhausted && string.Equals(reader.CurrentVin, vin, StringComparison.Ordinal))
                    {
                        if (reader.Current is VehicleEntryModel entry)
                        {
                            hasEntry = true;
                            DecodeIdentity(entry);
                        }
                        reader.Family.Attach(aggregate, reader.Current);
                        attachedThisVin[i]++;
                        reader.MoveNext();
                    }
                }

                if (requireVehicleEntry && !hasEntry)
                {
                    // Rows of a vehicle nobody serves are read and counted, but not "attached":
                    // RowsAttached is what reached a served aggregate.
                    Statistics.SkippedWithoutEntry++;
                    continue;
                }
                for (var i = 0; i < readers.Length; i++)
                    Statistics.RowsAttached[readers[i].Family] += attachedThisVin[i];
                Statistics.Aggregates++;
                yield return aggregate;
            }

            foreach (var reader in readers)
            {
                Statistics.RowsRead[reader.Family] = reader.RowsRead;
                Statistics.BlankVinRows += reader.BlankVinRows;
                Statistics.NonCanonicalVinRows += reader.NonCanonicalVinRows;
            }
        }
        finally
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
                openReaders.Remove(reader);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void DecodeIdentity(VehicleEntryModel entry)
    {
        if (hashIdService is null)
            return;
        if (entry.CompanyHashID is not null)
            entry.CompanyID = hashIdService.Decode(entry.CompanyHashID, new CompanyHashIdConverter());
        if (entry.BranchHashID is not null)
            entry.BranchID = hashIdService.Decode(entry.BranchHashID, new CompanyBranchHashIdConverter());
        if (entry.RegionHashID is not null)
            entry.RegionID = hashIdService.Decode(entry.RegionHashID, new RegionHashIdConverter());
        if (entry.BrandHashID is not null)
            entry.BrandID = hashIdService.Decode(entry.BrandHashID, new BrandHashIdConverter());
    }

    public void Dispose()
    {
        foreach (var reader in openReaders.ToArray())
            reader.Dispose();
        openReaders.Clear();
    }

    public sealed class StreamStatistics
    {
        internal StreamStatistics(IReadOnlyList<AggregateFamily> families)
        {
            foreach (var family in families)
            {
                RowsRead[family] = 0;
                RowsAttached[family] = 0;
            }
        }

        public long Aggregates { get; internal set; }
        public long SkippedWithoutEntry { get; internal set; }
        public long BlankVinRows { get; internal set; }
        /// <summary>
        /// Rows whose stored VIN is not in canonical form, across every family: rows no lookup path
        /// ever serves (the per-VIN storage and Cosmos both match the stored value exactly). A
        /// data-quality number for the source's owner, never silently "fixed" on one path only.
        /// </summary>
        public long NonCanonicalVinRows { get; internal set; }
        public Dictionary<AggregateFamily, long> RowsRead { get; } = new Dictionary<AggregateFamily, long>();
        public Dictionary<AggregateFamily, long> RowsAttached { get; } = new Dictionary<AggregateFamily, long>();
        public long TotalRows => RowsRead.Values.Sum();
    }
}
