using System;
using System.Collections.Generic;
using System.Data.Common;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// One VIN-keyed family of the aggregate: the table it streams from, the row filter the per-VIN
/// storage has always applied to it, the model it maps to, and where the row lands on the
/// <see cref="CompanyDataAggregateModel"/>. A family is DECLARED by the host: a table that is not
/// there fails when the stream opens, it is never discovered as silence.
///
/// <para><see cref="Table"/> is the family's canonical name — what the per-VIN storage reads and
/// what a read snapshot calls it. <see cref="From"/> is the relation the stream actually scans,
/// which a source binding (<see cref="BulkLookupSource"/>) may point elsewhere: a Hawta serving
/// table under its schema, or the published parquet of one. <see cref="Bound"/> makes that copy;
/// the model, the filter and the attachment never change with the source.</para>
/// </summary>
public sealed class AggregateFamily
{
    public string Table { get; }
    /// <summary>The SQL relation the stream scans: the quoted table by default, or whatever the source bound.</summary>
    public string From { get; }
    public string Where { get; }
    /// <summary>
    /// The order of a vehicle's rows within its VIN: <c>rowid</c> on a base table — the physical
    /// order a per-VIN scan returns — or what the bound relation offers instead.
    /// </summary>
    public string RowOrder { get; }
    public Type ModelType { get; }
    internal Func<DbDataReader, Func<DbDataReader, object>> BuildReader { get; }
    internal Action<CompanyDataAggregateModel, object> Attach { get; }

    private AggregateFamily(
        string table, string from, string where, string rowOrder, Type modelType,
        Func<DbDataReader, Func<DbDataReader, object>> buildReader,
        Action<CompanyDataAggregateModel, object> attach)
    {
        Table = table;
        From = from;
        Where = where;
        RowOrder = rowOrder;
        ModelType = modelType;
        BuildReader = buildReader;
        Attach = attach;
    }

    public static AggregateFamily Of<TModel>(string table, Action<CompanyDataAggregateModel, TModel> attach, string where = null)
        where TModel : new()
    {
        return new AggregateFamily(
            table, $"\"{table}\"", where, "rowid", typeof(TModel),
            reader =>
            {
                var mapper = DuckDBModelMapper<TModel>.For(reader);
                return current => mapper.Read(current);
            },
            (aggregate, row) => attach(aggregate, (TModel)row));
    }

    /// <summary>
    /// The same family read from another relation: <paramref name="from"/> replaces the scanned
    /// table (a qualified name, a table function), <paramref name="additionalWhere"/> is ANDed to
    /// the family's own filter (a source's live-row predicate, say), and <paramref name="rowOrder"/>
    /// replaces <c>rowid</c> where the relation has none.
    /// </summary>
    public AggregateFamily Bound(string from, string additionalWhere = null, string rowOrder = null)
    {
        if (string.IsNullOrWhiteSpace(from))
            throw new ArgumentException("A bound family needs a relation to read.", nameof(from));
        var where = string.IsNullOrWhiteSpace(additionalWhere) ? Where
            : string.IsNullOrWhiteSpace(Where) ? additionalWhere
            : $"({Where}) AND ({additionalWhere})";
        return new AggregateFamily(Table, from, where, string.IsNullOrWhiteSpace(rowOrder) ? RowOrder : rowOrder, ModelType, BuildReader, Attach);
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Where) ? From : $"{From} WHERE {Where}";
}

/// <summary>
/// The families the aggregate can carry, each with the filter <c>DuckDBVehicleLookupStorageService</c>
/// applies when it loads the same family per VIN — so a streamed aggregate holds exactly the rows
/// the per-VIN path would have loaded.
/// </summary>
public static class AggregateFamilies
{
    public static readonly AggregateFamily VehicleEntry =
        AggregateFamily.Of<VehicleEntryModel>("VehicleEntry", (a, m) => a.VehicleEntries.Add(m));
    public static readonly AggregateFamily InitialOfficialVIN =
        AggregateFamily.Of<InitialOfficialVINModel>("InitialOfficialVIN", (a, m) => a.InitialOfficialVINs.Add(m));
    public static readonly AggregateFamily OrderLaborLine =
        AggregateFamily.Of<OrderLaborLineModel>("OrderLaborLine", (a, m) => a.LaborLines.Add(m));
    public static readonly AggregateFamily OrderPartLine =
        AggregateFamily.Of<OrderPartLineModel>("OrderPartLine", (a, m) => a.PartLines.Add(m));
    public static readonly AggregateFamily SSCAffectedVIN =
        AggregateFamily.Of<SSCAffectedVINModel>("SSCAffectedVIN", (a, m) => a.SSCAffectedVINs.Add(m));
    public static readonly AggregateFamily WarrantyClaim =
        AggregateFamily.Of<WarrantyClaimModel>("WarrantyClaim", (a, m) => a.WarrantyClaims.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily ItemClaim =
        AggregateFamily.Of<ItemClaimModel>("ItemClaim", (a, m) => a.ItemClaims.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily PaidServiceInvoice =
        AggregateFamily.Of<PaidServiceInvoiceModel>("PaidServiceInvoice", (a, m) => a.PaidServiceInvoices.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily ExtendedWarranty =
        AggregateFamily.Of<ExtendedWarrantyModel>("ExtendedWarranty", (a, m) => a.ExtendedWarrantyEntries.Add(m), "IsDeleted = false AND IsActive = true");
    public static readonly AggregateFamily PaintThicknessInspection =
        AggregateFamily.Of<PaintThicknessInspectionModel>("PaintThicknessInspection", (a, m) =>
        {
            // The per-VIN storage leaves this member null when a VIN has no inspection; keep that.
            if (a.PaintThicknessInspections is not List<PaintThicknessInspectionModel> list)
                a.PaintThicknessInspections = list = new List<PaintThicknessInspectionModel>();
            list.Add(m);
        });
    public static readonly AggregateFamily VehicleAccessory =
        AggregateFamily.Of<VehicleAccessoryModel>("VehicleAccessory", (a, m) => a.Accessories.Add(m));
    public static readonly AggregateFamily VehicleInspection =
        AggregateFamily.Of<VehicleInspectionModel>("VehicleInspection", (a, m) => a.VehicleInspections.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily CampaignVinEntry =
        AggregateFamily.Of<CampaignVinEntryModel>("CampaignVinEntry", (a, m) => a.CampaignVinEntries.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily VehicleServiceActivation =
        AggregateFamily.Of<VehicleServiceActivation>("VehicleServiceActivation", (a, m) => a.VehicleServiceActivations.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily FreeServiceItemDateShift =
        AggregateFamily.Of<FreeServiceItemDateShiftModel>("VehicleFreeServiceShiftDate", (a, m) => a.FreeServiceItemDateShifts.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily FreeServiceItemExcludedVIN =
        AggregateFamily.Of<FreeServiceItemExcludedVINModel>("VehicleFreeServiceItemExcludedVIN", (a, m) => a.FreeServiceItemExcludedVINs.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily FreeServiceItemValidityOverride =
        AggregateFamily.Of<FreeServiceItemValidityOverrideModel>("VehicleFreeServiceItemValidityOverride", (a, m) => a.FreeServiceItemValidityOverrides.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily WarrantyDateShift =
        AggregateFamily.Of<WarrantyDateShiftModel>("VehicleWarrantyShiftDate", (a, m) => a.WarrantyDateShifts.Add(m), "IsDeleted = false");
    public static readonly AggregateFamily BrokerInitialVehicle =
        AggregateFamily.Of<BrokerInitialVehicleModel>("BrokerInitialVehicle", (a, m) => a.BrokerInitialVehicles.Add(m), "Deleted = false");
    public static readonly AggregateFamily BrokerInvoice =
        AggregateFamily.Of<BrokerInvoiceModel>("BrokerInvoice", (a, m) => a.BrokerInvoices.Add(m), "IsDeleted = false");

    /// <summary>
    /// Every family the aggregate knows: the roster of a host that runs every module. A host declares
    /// its roster as the subset its source carries — the modules it runs — on purpose, rather than
    /// discovering a missing table as an empty family through a swallowed exception.
    /// </summary>
    public static IReadOnlyList<AggregateFamily> All { get; } = new[]
    {
        VehicleEntry, InitialOfficialVIN, OrderLaborLine, OrderPartLine, SSCAffectedVIN, WarrantyClaim, ItemClaim,
        PaidServiceInvoice, ExtendedWarranty, PaintThicknessInspection, VehicleAccessory, VehicleInspection,
        CampaignVinEntry, VehicleServiceActivation, FreeServiceItemDateShift, FreeServiceItemExcludedVIN,
        FreeServiceItemValidityOverride, WarrantyDateShift, BrokerInitialVehicle, BrokerInvoice,
    };
}
