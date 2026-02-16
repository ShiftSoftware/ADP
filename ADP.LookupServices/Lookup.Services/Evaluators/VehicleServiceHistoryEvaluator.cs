using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleServiceHistoryEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;

    public VehicleServiceHistoryEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options, IServiceProvider serviceProvider)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = serviceProvider;
    }

    public static IEnumerable<VehicleServiceHistoryInvoice> GetInvoices(CompanyDataAggregateModel companyDataAggregate, ConsistencyLevels consistencyLevel)
    {
        var labors = companyDataAggregate.LaborLines ?? new List<OrderLaborLineModel>();
        var parts = companyDataAggregate.PartLines ?? new List<OrderPartLineModel>();

        if (!labors.Any() && !parts.Any())
            return Enumerable.Empty<VehicleServiceHistoryInvoice>();

        var invoiceKeyComparer = new InvoiceKeyEqualityComparer();

        var groupedLaborLines = labors
            .GroupBy(l => new VehicleServiceHistoryInvoice(l.CompanyID, l.BranchID, l.InvoiceNumber, l.OrderDocumentNumber), invoiceKeyComparer)
            .ToDictionary(g => g.Key, g => g.ToList(), invoiceKeyComparer);

        var groupedPartLines = parts
            .GroupBy(p => new VehicleServiceHistoryInvoice(p.CompanyID, p.BranchID, p.InvoiceNumber, p.OrderDocumentNumber), invoiceKeyComparer)
            .ToDictionary(g => g.Key, g => g.ToList(), invoiceKeyComparer);

        var allInvoiceKeys = groupedLaborLines
            .Keys
            .Union(groupedPartLines.Keys, invoiceKeyComparer)
            .ToList();

        var invoices = new List<VehicleServiceHistoryInvoice>();

        foreach (var invoice in allInvoiceKeys)
        {
            if (groupedLaborLines.TryGetValue(invoice, out var laborLines))
                invoice.LaborLines = laborLines;

            if (groupedPartLines.TryGetValue(invoice, out var partLines))
                invoice.PartLines = partLines;

            if (consistencyLevel == ConsistencyLevels.Strong)
            {
                var numberOfPartLinesAccordingToLaborRecords = invoice.LaborLines?.FirstOrDefault()?.NumberOfPartLines ?? 0;
                var numberOfLaborLinesAccordingToPartRecords = invoice.PartLines?.FirstOrDefault()?.NumberOfLaborLines ?? 0;
                var actualPartCount = invoice.PartLines?.Count ?? 0;
                var actualLaborCount = invoice.LaborLines?.Count ?? 0;

                if (numberOfPartLinesAccordingToLaborRecords != actualPartCount ||
                    numberOfLaborLinesAccordingToPartRecords != actualLaborCount)
                    continue;
            }

            invoices.Add(invoice);
        }

        return invoices;
    }

    public async Task<IEnumerable<VehicleServiceHistoryDTO>> Evaluate(string languageCode, ConsistencyLevels consistencyLevel)
    {
        var allInvoices = GetInvoices(this.CompanyDataAggregate, consistencyLevel);

        var serviceHistory = new List<VehicleServiceHistoryDTO>();

        foreach (var invoice in allInvoices)
        {
            var serviceHistoryEntry = new VehicleServiceHistoryDTO
            {
                InvoiceNumber = invoice.InvoiceNumber,
                ParentInvoiceNumber = invoice.LaborLines?.FirstOrDefault()?.ParentInvoiceNumber ?? invoice.PartLines?.FirstOrDefault()?.ParentInvoiceNumber,
                JobNumber = invoice.OrderDocumentNumber,
                AccountNumber = invoice.LaborLines?.FirstOrDefault()?.AccountNumber ?? invoice.PartLines?.FirstOrDefault()?.AccountNumber,
                CompanyName = null,
                BranchName = null,
                ServiceDate = GetMaxServiceDate(invoice.LaborLines, invoice.PartLines),
                Mileage = invoice.LaborLines?.Max(x => x.Odometer),
                ServiceType = invoice.LaborLines.Where(x => x.JobDescription is not null)?.FirstOrDefault()?.JobDescription,
                LaborLines = invoice.LaborLines?.Select(l => new VehicleLaborDTO
                {
                    LaborCode = l.LaborCode,
                    ServiceCode = l.ServiceCode,
                    ServiceDescription = l.ServiceDescription,
                    PackageCode = l.PackageCode,
                }) ?? Enumerable.Empty<VehicleLaborDTO>(),
                PartLines = invoice.PartLines?.Select(p => new VehiclePartDTO
                {
                    PartNumber = p.PartNumber,
                    PartDescription = null,
                    PackageCode = p.PackageCode,
                    QTY = p.SoldQuantity,
                }) ?? Enumerable.Empty<VehiclePartDTO>()
            };

            if (this.Options?.CompanyNameResolver is not null)
                serviceHistoryEntry.CompanyName = await this.Options.CompanyNameResolver(new LookupOptionResolverModel<long?>(invoice.CompanyID, languageCode, this.ServiceProvider));

            if (this.Options?.CompanyBranchNameResolver is not null)
                serviceHistoryEntry.BranchName = await this.Options.CompanyBranchNameResolver(new LookupOptionResolverModel<long?>(invoice.BranchID, languageCode, this.ServiceProvider));

            serviceHistory.Add(serviceHistoryEntry);
        }

        return serviceHistory;
    }

    private static DateTime? GetMaxServiceDate(
        List<OrderLaborLineModel> laborLines,
        List<OrderPartLineModel> partLines
    )
    {
        var laborDate = laborLines?.Max(x => x.InvoiceDate);
        var partDate = partLines?.Max(x => x.InvoiceDate);

        if (laborDate.HasValue && partDate.HasValue)
            return laborDate.Value > partDate.Value ? laborDate.Value : partDate.Value;

        return laborDate ?? partDate;
    }

    public class VehicleServiceHistoryInvoice
    {
        public long? CompanyID { get; }
        public long? BranchID { get; }
        public string InvoiceNumber { get; }
        public string OrderDocumentNumber { get; }

        public List<OrderLaborLineModel> LaborLines { get; set; } = new List<OrderLaborLineModel>();

        public List<OrderPartLineModel> PartLines { get; set; } = new List<OrderPartLineModel>();

        public VehicleServiceHistoryInvoice(long? companyID, long? branchID, string invoiceNumber, string orderDocumentNumber)
        {
            CompanyID = companyID;
            BranchID = branchID;
            InvoiceNumber = invoiceNumber;
            OrderDocumentNumber = orderDocumentNumber;
        }
    }

    private class InvoiceKeyEqualityComparer : IEqualityComparer<VehicleServiceHistoryInvoice>
    {
        public bool Equals(VehicleServiceHistoryInvoice x, VehicleServiceHistoryInvoice y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.CompanyID == y.CompanyID &&
                   x.BranchID == y.BranchID &&
                   x.InvoiceNumber == y.InvoiceNumber &&
                   x.OrderDocumentNumber == y.OrderDocumentNumber;
        }

        public int GetHashCode(VehicleServiceHistoryInvoice obj)
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