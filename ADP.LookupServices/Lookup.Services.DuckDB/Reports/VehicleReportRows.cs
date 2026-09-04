using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// What a row of each vehicle report is, as one function per report over the lookup DTO. The
/// per-VIN report service (<see cref="DuckDBVehicleReportService"/>) and the bulk engine both build
/// their rows here, so a report produced either way carries the same columns with the same values
/// — the only thing the two paths may differ in is how the DTO was obtained.
/// </summary>
public static class VehicleReportRows
{
    public static string NormalizeVin(string vin)
    {
        return vin?.Trim()?.ToUpperInvariant();
    }

    /// <summary>
    /// The service-items report's rows for one vehicle: one per <c>ServiceItemID</c> (the best of
    /// the duplicates, see <see cref="BuildBestItemsByServiceId"/>), ordered by
    /// <see cref="ServiceItemIdComparer"/>. A vehicle with no lookup, or no items, yields no rows.
    /// </summary>
    public static List<VehicleServiceItemReportModel> ServiceItems(string vin, VehicleLookupDTO lookup)
    {
        var bestItemsByServiceId = BuildBestItemsByServiceId(lookup?.ServiceItems);
        var freeServiceItemStartDate = lookup?.Warranty?.FreeServiceStartDate;
        var rows = new List<VehicleServiceItemReportModel>(bestItemsByServiceId.Count);

        foreach (var item in bestItemsByServiceId.Values.OrderBy(x => x.ServiceItemID, ServiceItemIdComparer))
            rows.Add(ServiceItem(vin, item, freeServiceItemStartDate));

        return rows;
    }

    public static VehicleServiceItemReportModel ServiceItem(string vin, VehicleServiceItemDTO item, DateTime? freeServiceItemStartDate)
    {
        return new VehicleServiceItemReportModel
        {
            VIN = vin ?? string.Empty,
            FreeServiceItemStartDate = freeServiceItemStartDate,
            ServiceItemId = item?.ServiceItemID?.Trim() ?? string.Empty,
            ServiceItemName = item?.Name ?? string.Empty,
            GroupName = item?.Group?.Name ?? string.Empty,
            GroupTabOrder = item?.Group?.TabOrder,
            GroupIsDefault = item?.Group?.IsDefault,
            GroupIsSequential = item?.Group?.IsSequential,
            Status = item?.Status ?? string.Empty,
            StatusEnum = item?.StatusEnum,
            Type = item?.Type ?? string.Empty,
            TypeEnum = item?.TypeEnum,
            Price = item?.Cost,
            MenuCode = item?.PackageCode ?? string.Empty,
            ActivatedAt = item?.ActivatedAt == default ? null : item.ActivatedAt,
            ExpiresAt = item?.ExpiresAt,
            ClaimDate = item?.ClaimDate,
            CampaignId = item?.CampaignID,
            CampaignUniqueReference = item?.CampaignUniqueReference ?? string.Empty,
            ModelCostId = item?.ModelCostID,
            PaidServiceInvoiceLineId = item?.PaidServiceInvoiceLineID ?? string.Empty,
            CompanyName = item?.CompanyName ?? string.Empty,
            InvoiceNumber = item?.InvoiceNumber ?? string.Empty,
            JobNumber = item?.JobNumber ?? string.Empty,
            MaximumMileage = item?.MaximumMileage,
            Claimable = item?.Claimable,
            ClaimingMethod = item?.ClaimingMethodEnum,
            VehicleInspectionId = item?.VehicleInspectionID ?? string.Empty,
            VehicleInspectionTypeId = item?.VehicleInspectionTypeID ?? string.Empty
        };
    }

    /// <summary>The SSC report's rows for one vehicle, one per campaign.</summary>
    public static List<VehicleSscReportModel> Ssc(string vin, VehicleLookupDTO lookup)
    {
        var rows = new List<VehicleSscReportModel>();
        foreach (var ssc in lookup?.SSC ?? Enumerable.Empty<SscDTO>())
            rows.Add(Ssc(vin, ssc));
        return rows;
    }

    public static VehicleSscReportModel Ssc(string vin, SscDTO ssc)
    {
        // The flat CSV/Parquet SSC report still caps at 3 labors/parts (columns LaborCode1..3 / PartNumber1..3).
        // SscDTO.Labors/Parts are now unbounded, so campaigns with >3 are truncated here. Widening the report
        // schema (or pivoting to one row per part) is a deferred decision — see .shift/repos/adp/ssc-multi-part-labor/.
        var labors = (ssc?.Labors ?? Enumerable.Empty<SSCLaborDTO>()).Take(3).ToList();
        var parts = (ssc?.Parts ?? Enumerable.Empty<SSCPartDTO>()).Take(3).ToList();

        var labor1 = labors.Count > 0 ? labors[0] : null;
        var labor2 = labors.Count > 1 ? labors[1] : null;
        var labor3 = labors.Count > 2 ? labors[2] : null;

        var part1 = parts.Count > 0 ? parts[0] : null;
        var part2 = parts.Count > 1 ? parts[1] : null;
        var part3 = parts.Count > 2 ? parts[2] : null;

        return new VehicleSscReportModel
        {
            VIN = vin ?? string.Empty,
            SSCCode = ssc?.SSCCode ?? string.Empty,
            Description = ssc?.Description ?? string.Empty,
            Repaired = ssc?.Repaired ?? false,
            RepairDate = ssc?.RepairDate,

            LaborCode1 = labor1?.LaborCode ?? string.Empty,
            LaborDescription1 = labor1?.LaborDescription ?? string.Empty,
            LaborAllowedTime1 = labor1?.AllowedTime,

            LaborCode2 = labor2?.LaborCode ?? string.Empty,
            LaborDescription2 = labor2?.LaborDescription ?? string.Empty,
            LaborAllowedTime2 = labor2?.AllowedTime,

            LaborCode3 = labor3?.LaborCode ?? string.Empty,
            LaborDescription3 = labor3?.LaborDescription ?? string.Empty,
            LaborAllowedTime3 = labor3?.AllowedTime,

            PartNumber1 = part1?.PartNumber ?? string.Empty,
            PartDescription1 = part1?.PartDescription ?? string.Empty,
            PartIsAvailable1 = part1?.IsAvailable,

            PartNumber2 = part2?.PartNumber ?? string.Empty,
            PartDescription2 = part2?.PartDescription ?? string.Empty,
            PartIsAvailable2 = part2?.IsAvailable,

            PartNumber3 = part3?.PartNumber ?? string.Empty,
            PartDescription3 = part3?.PartDescription ?? string.Empty,
            PartIsAvailable3 = part3?.IsAvailable,
        };
    }

    /// <summary>The service-history labor report's rows for one vehicle, one per labor line of every visit.</summary>
    public static List<VehicleServiceHistoryLaborReportModel> ServiceHistoryLabors(string vin, VehicleLookupDTO lookup)
    {
        var rows = new List<VehicleServiceHistoryLaborReportModel>();
        foreach (var entry in lookup?.ServiceHistory ?? Enumerable.Empty<VehicleServiceHistoryDTO>())
            foreach (var labor in entry?.LaborLines ?? Enumerable.Empty<VehicleLaborDTO>())
                rows.Add(ServiceHistoryLabor(vin, entry, labor));
        return rows;
    }

    /// <summary>The service-history part report's rows for one vehicle, one per part line of every visit.</summary>
    public static List<VehicleServiceHistoryPartReportModel> ServiceHistoryParts(string vin, VehicleLookupDTO lookup)
    {
        var rows = new List<VehicleServiceHistoryPartReportModel>();
        foreach (var entry in lookup?.ServiceHistory ?? Enumerable.Empty<VehicleServiceHistoryDTO>())
            foreach (var part in entry?.PartLines ?? Enumerable.Empty<VehiclePartDTO>())
                rows.Add(ServiceHistoryPart(vin, entry, part));
        return rows;
    }

    public static VehicleServiceHistoryLaborReportModel ServiceHistoryLabor(string vin, VehicleServiceHistoryDTO serviceHistoryEntry, VehicleLaborDTO labor)
    {
        return new VehicleServiceHistoryLaborReportModel
        {
            VIN = vin ?? string.Empty,
            ServiceType = serviceHistoryEntry?.ServiceType ?? string.Empty,
            ServiceDate = serviceHistoryEntry?.ServiceDate,
            Mileage = serviceHistoryEntry?.Mileage,
            CompanyName = serviceHistoryEntry?.CompanyName ?? string.Empty,
            BranchName = serviceHistoryEntry?.BranchName ?? string.Empty,
            AccountNumber = serviceHistoryEntry?.AccountNumber ?? string.Empty,
            InvoiceNumber = serviceHistoryEntry?.InvoiceNumber ?? string.Empty,
            ParentInvoiceNumber = serviceHistoryEntry?.ParentInvoiceNumber ?? string.Empty,
            JobNumber = serviceHistoryEntry?.JobNumber ?? string.Empty,

            LaborCode = labor?.LaborCode ?? string.Empty,
            LaborPackageCode = labor?.PackageCode ?? string.Empty,
            LaborServiceCode = labor?.ServiceCode ?? string.Empty,
            LaborServiceDescription = labor?.ServiceDescription ?? string.Empty,
        };
    }

    public static VehicleServiceHistoryPartReportModel ServiceHistoryPart(string vin, VehicleServiceHistoryDTO serviceHistoryEntry, VehiclePartDTO part)
    {
        return new VehicleServiceHistoryPartReportModel
        {
            VIN = vin ?? string.Empty,
            ServiceType = serviceHistoryEntry?.ServiceType ?? string.Empty,
            ServiceDate = serviceHistoryEntry?.ServiceDate,
            Mileage = serviceHistoryEntry?.Mileage,
            CompanyName = serviceHistoryEntry?.CompanyName ?? string.Empty,
            BranchName = serviceHistoryEntry?.BranchName ?? string.Empty,
            AccountNumber = serviceHistoryEntry?.AccountNumber ?? string.Empty,
            InvoiceNumber = serviceHistoryEntry?.InvoiceNumber ?? string.Empty,
            ParentInvoiceNumber = serviceHistoryEntry?.ParentInvoiceNumber ?? string.Empty,
            JobNumber = serviceHistoryEntry?.JobNumber ?? string.Empty,

            PartNumber = part?.PartNumber ?? string.Empty,
            PartQty = part?.QTY,
            PartPackageCode = part?.PackageCode ?? string.Empty,
            PartDescription = part?.PartDescription ?? string.Empty,
        };
    }

    /// <summary>The top-level report's one row for a vehicle.</summary>
    public static VehicleLookupTopLevelReportModel TopLevel(string vin, VehicleLookupDTO lookup)
    {
        var identifiers = lookup?.Identifiers;
        var sale = lookup?.SaleInformation;
        var broker = sale?.Broker;
        var endCustomer = sale?.EndCustomer;
        var warranty = lookup?.Warranty;
        var variantInfo = lookup?.VehicleVariantInfo;
        var vehicleSpecification = lookup?.VehicleSpecification;

        return new VehicleLookupTopLevelReportModel
        {
            VIN = vin ?? string.Empty,
            IsAuthorized = lookup?.IsAuthorized ?? false,
            NextServiceDate = lookup?.NextServiceDate,
            SSCLogId = lookup?.SSCLogId,
            BasicModelCode = lookup?.BasicModelCode ?? string.Empty,

            IdentifiersVin = identifiers?.VIN ?? string.Empty,
            IdentifiersVariant = identifiers?.Variant ?? string.Empty,
            IdentifiersKatashiki = identifiers?.Katashiki ?? string.Empty,
            IdentifiersColor = identifiers?.Color ?? string.Empty,
            IdentifiersTrim = identifiers?.Trim ?? string.Empty,
            IdentifiersBrandId = identifiers?.BrandID ?? string.Empty,

            SaleCountryId = sale?.CountryID ?? string.Empty,
            SaleCountryName = sale?.CountryName ?? string.Empty,
            SaleCompanyId = sale?.CompanyID ?? string.Empty,
            SaleCompanyName = sale?.CompanyName ?? string.Empty,
            SaleBranchId = sale?.BranchID ?? string.Empty,
            SaleBranchName = sale?.BranchName ?? string.Empty,
            SaleCustomerAccountNumber = sale?.CustomerAccountNumber ?? string.Empty,
            SaleCustomerId = sale?.CustomerID ?? string.Empty,
            SaleInvoiceDate = sale?.InvoiceDate,
            SaleWarrantyActivationDate = sale?.WarrantyActivationDate,
            SaleInvoiceNumber = sale?.InvoiceNumber ?? string.Empty,
            SaleRegionId = sale?.RegionID ?? string.Empty,

            SaleBrokerId = broker?.BrokerID,
            SaleBrokerName = broker?.BrokerName ?? string.Empty,
            SaleBrokerInvoiceNumber = broker?.InvoiceNumber,
            SaleBrokerInvoiceDate = broker?.InvoiceDate,

            SaleEndCustomerId = endCustomer?.ID ?? string.Empty,
            SaleEndCustomerName = endCustomer?.Name ?? string.Empty,
            SaleEndCustomerPhone = endCustomer?.Phone ?? string.Empty,
            SaleEndCustomerIdNumber = endCustomer?.IDNumber ?? string.Empty,

            WarrantyHasActiveWarranty = warranty?.HasActiveWarranty ?? false,
            WarrantyStartDate = warranty?.WarrantyStartDate,
            WarrantyEndDate = warranty?.WarrantyEndDate,
            WarrantyActivationIsRequired = warranty?.ActivationIsRequired ?? false,
            WarrantyHasExtendedWarranty = warranty?.HasExtendedWarranty ?? false,
            WarrantyExtendedStartDate = warranty?.ExtendedWarrantyStartDate,
            WarrantyExtendedEndDate = warranty?.ExtendedWarrantyEndDate,
            WarrantyFreeServiceStartDate = warranty?.FreeServiceStartDate,

            VariantInfoModelCode = variantInfo?.ModelCode ?? string.Empty,
            VariantInfoSfx = variantInfo?.SFX ?? string.Empty,
            VariantInfoModelYear = variantInfo?.ModelYear,

            VehicleSpecModelCode = vehicleSpecification?.ModelCode ?? string.Empty,
            VehicleSpecModelYear = vehicleSpecification?.ModelYear,
            VehicleSpecProductionDate = vehicleSpecification?.ProductionDate,
            VehicleSpecModelDescription = vehicleSpecification?.ModelDescription ?? string.Empty,
            VehicleSpecVariantDescription = vehicleSpecification?.VariantDescription ?? string.Empty,
            VehicleSpecClass = vehicleSpecification?.Class ?? string.Empty,
            VehicleSpecBodyType = vehicleSpecification?.BodyType ?? string.Empty,
            VehicleSpecEngine = vehicleSpecification?.Engine ?? string.Empty,
            VehicleSpecCylinders = vehicleSpecification?.Cylinders ?? string.Empty,
            VehicleSpecLightHeavyType = vehicleSpecification?.LightHeavyType ?? string.Empty,
            VehicleSpecDoors = vehicleSpecification?.Doors ?? string.Empty,
            VehicleSpecFuel = vehicleSpecification?.Fuel ?? string.Empty,
            VehicleSpecTransmission = vehicleSpecification?.Transmission ?? string.Empty,
            VehicleSpecSide = vehicleSpecification?.Side ?? string.Empty,
            VehicleSpecEngineType = vehicleSpecification?.EngineType ?? string.Empty,
            VehicleSpecTankCap = vehicleSpecification?.TankCap ?? string.Empty,
            VehicleSpecStyle = vehicleSpecification?.Style ?? string.Empty,
            VehicleSpecFuelLiter = vehicleSpecification?.FuelLiter,
            VehicleSpecExteriorColor = vehicleSpecification?.ExteriorColor ?? string.Empty,
            VehicleSpecInteriorColor = vehicleSpecification?.InteriorColor ?? string.Empty,
        };
    }

    /// <summary>
    /// The service-items report's deduplication, public so audits and host tooling reuse the exact
    /// semantics instead of re-deciding them: one row per <c>ServiceItemID</c>, keeping the row with
    /// the latest claim, then activation, then expiry. Items with no id are dropped here — a caller
    /// that must not lose them collects them separately.
    /// </summary>
    public static Dictionary<string, VehicleServiceItemDTO> BuildBestItemsByServiceId(IEnumerable<VehicleServiceItemDTO> items)
    {
        return (items ?? Enumerable.Empty<VehicleServiceItemDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.ServiceItemID))
            .GroupBy(x => x.ServiceItemID.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(x => x.ClaimDate ?? DateTimeOffset.MinValue)
                .ThenByDescending(x => x.ActivatedAt)
                .ThenByDescending(x => x.ExpiresAt ?? DateTime.MinValue)
                .First(), StringComparer.Ordinal);
    }

    /// <summary>
    /// The report's service-item ordering: numeric when both ids parse, ordinal otherwise. Public for
    /// the same reason as <see cref="BuildBestItemsByServiceId"/>.
    /// </summary>
    public static readonly IComparer<string> ServiceItemIdComparer = Comparer<string>.Create((left, right) =>
    {
        if (long.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out var leftValue)
            && long.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rightValue))
        {
            return leftValue.CompareTo(rightValue);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    });
}
