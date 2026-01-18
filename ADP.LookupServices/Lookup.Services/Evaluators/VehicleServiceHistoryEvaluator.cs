using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleServiceHistoryEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;

    public VehicleServiceHistoryEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate, LookupOptions options, IServiceProvider serviceProvider)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = serviceProvider;
    }

    public async Task<IEnumerable<VehicleServiceHistoryDTO>> Evaluate(string languageCode, ConsistencyLevels consistencyLevel)
    {
        var labors = CompanyDataAggregate.LaborLines ?? new List<Models.Service.OrderLaborLineModel>();
        var parts = CompanyDataAggregate.PartLines ?? new List<Models.Part.OrderPartLineModel>();

        if (!labors.Any() && !parts.Any())
            return Enumerable.Empty<VehicleServiceHistoryDTO>();

        var invoiceKeyComparer = new InvoiceKeyEqualityComparer();

        var groupedLaborLines = labors
            .GroupBy(l => new InvoiceKey(l.CompanyID, l.BranchID, l.InvoiceNumber, l.OrderDocumentNumber), invoiceKeyComparer)
            .ToDictionary(g => g.Key, g => g.ToList(), invoiceKeyComparer);

        var groupedPartLines = parts
            .GroupBy(p => new InvoiceKey(p.CompanyID, p.BranchID, p.InvoiceNumber, p.OrderDocumentNumber), invoiceKeyComparer)
            .ToDictionary(g => g.Key, g => g.ToList(), invoiceKeyComparer);

        var allInvoiceKeys = groupedLaborLines
            .Keys
            .Union(groupedPartLines.Keys, invoiceKeyComparer)
            .ToList();

        var serviceHistory = new List<VehicleServiceHistoryDTO>();

        foreach (var invoice in allInvoiceKeys)
        {
            groupedLaborLines.TryGetValue(invoice, out var laborLines);
            groupedPartLines.TryGetValue(invoice, out var partLines);

            if (consistencyLevel == ConsistencyLevels.Strong)
            {
                var numberOfPartLinesAccordingToLaborRecords = laborLines?.FirstOrDefault()?.NumberOfPartLines ?? 0;
                var numberOfLaborLinesAccordingToPartRecords = partLines?.FirstOrDefault()?.NumberOfLaborLines ?? 0;
                var actualPartCount = partLines?.Count ?? 0;
                var actualLaborCount = laborLines?.Count ?? 0;

                if (numberOfPartLinesAccordingToLaborRecords != actualPartCount ||
                    numberOfLaborLinesAccordingToPartRecords != actualLaborCount)
                    continue;
            }

            var serviceHistoryEntry = new VehicleServiceHistoryDTO
            {
                InvoiceNumber = invoice.InvoiceNumber,
                ParentInvoiceNumber = laborLines?.FirstOrDefault()?.ParentInvoiceNumber ?? partLines?.FirstOrDefault()?.ParentInvoiceNumber,
                JobNumber = invoice.OrderDocumentNumber,
                AccountNumber = laborLines?.FirstOrDefault()?.AccountNumber ?? partLines?.FirstOrDefault()?.AccountNumber,
                CompanyName = null,
                BranchName = null,
                ServiceDate = GetMaxServiceDate(laborLines, partLines),
                Mileage = null,
                ServiceType = null,
                LaborLines = laborLines?.Select(l => new VehicleLaborDTO
                {
                    RTSCode = l.LaborCode,
                    ServiceCode = l.ServiceCode,
                    Description = l.ServiceDescription,
                    MenuCode = l.MenuCode,
                }) ?? Enumerable.Empty<VehicleLaborDTO>(),
                PartLines = partLines?.Select(p => new VehiclePartDTO
                {
                    PartNumber = p.PartNumber,
                    PartDescription = null,
                    MenuCode = p.MenuCode,
                    QTY = p.SoldQuantity,
                }) ?? Enumerable.Empty<VehiclePartDTO>()
            };

            if (this.Options.CompanyNameResolver is not null)
                serviceHistoryEntry.CompanyName = await this.Options.CompanyNameResolver(new LookupOptionResolverModel<long?>(invoice.CompanyID, languageCode, this.ServiceProvider));

            if (this.Options.CompanyBranchNameResolver is not null)
                serviceHistoryEntry.BranchName = await this.Options.CompanyBranchNameResolver(new LookupOptionResolverModel<long?>(invoice.BranchID, languageCode, this.ServiceProvider));

            serviceHistory.Add(serviceHistoryEntry);
        }

        return serviceHistory;
    }

    private static DateTime? GetMaxServiceDate(
        List<Models.Service.OrderLaborLineModel> laborLines,
        List<Models.Part.OrderPartLineModel> partLines)
    {
        var laborDate = laborLines?.Max(x => x.InvoiceDate);
        var partDate = partLines?.Max(x => x.InvoiceDate);

        if (laborDate.HasValue && partDate.HasValue)
            return laborDate.Value > partDate.Value ? laborDate.Value : partDate.Value;

        return laborDate ?? partDate;
    }

    private class InvoiceKey
    {
        public long? CompanyID { get; }
        public long? BranchID { get; }
        public string InvoiceNumber { get; }
        public string OrderDocumentNumber { get; }

        public InvoiceKey(long? companyID, long? branchID, string invoiceNumber, string orderDocumentNumber)
        {
            CompanyID = companyID;
            BranchID = branchID;
            InvoiceNumber = invoiceNumber;
            OrderDocumentNumber = orderDocumentNumber;
        }
    }

    private class InvoiceKeyEqualityComparer : IEqualityComparer<InvoiceKey>
    {
        public bool Equals(InvoiceKey x, InvoiceKey y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.CompanyID == y.CompanyID &&
                   x.BranchID == y.BranchID &&
                   x.InvoiceNumber == y.InvoiceNumber &&
                   x.OrderDocumentNumber == y.OrderDocumentNumber;
        }

        public int GetHashCode(InvoiceKey obj)
        {
            if (obj is null) return 0;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (obj.CompanyID?.GetHashCode() ?? 0);
                hash = hash * 31 + (obj.BranchID?.GetHashCode() ?? 0);
                hash = hash * 31 + (obj.InvoiceNumber?.GetHashCode() ?? 0);
                hash = hash * 31 + (obj.OrderDocumentNumber?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}