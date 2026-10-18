using System;
using System.Linq;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>Matches visits using the same consistent-invoice primitive as eligibility and warranty.</summary>
public static class ServiceConsumptionMatcher
{
    public static ServiceConsumptionEvidenceDTO Match(
        CompanyDataAggregateModel aggregate, ServiceConsumptionRule rule,
        DateTime activatedAt, DateTime? expiresAt, DateTime nowUtc)
    {
        if (rule is null || activatedAt == default || activatedAt > nowUtc)
            return null;

        var packages = rule.PackageCodes?.ToList();
        if (packages is not null && (packages.Count == 0 || packages.Any(string.IsNullOrWhiteSpace)))
            return null;

        return VehicleServiceHistoryEvaluator.GetInvoices(aggregate, ConsistencyLevels.Strong)
            .Where(invoice => packages is null || invoice.LaborLines.Any(line =>
                packages.Contains(line.PackageCode, StringComparer.OrdinalIgnoreCase)))
            .Select(invoice => new
            {
                Invoice = invoice,
                Date = invoice.LaborLines.Select(line => line.InvoiceDate)
                    .Concat(invoice.PartLines.Select(line => line.InvoiceDate)).Max(),
            })
            .Where(x => x.Date >= activatedAt && x.Date <= nowUtc &&
                (expiresAt is null || x.Date <= expiresAt))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Invoice.CompanyID)
            .ThenBy(x => x.Invoice.BranchID)
            .ThenBy(x => x.Invoice.InvoiceNumber, StringComparer.Ordinal)
            .ThenBy(x => x.Invoice.OrderDocumentNumber, StringComparer.Ordinal)
            .Select(x => new ServiceConsumptionEvidenceDTO
            {
                ServiceDate = x.Date.Value,
                InvoiceNumber = x.Invoice.InvoiceNumber,
                JobNumber = x.Invoice.OrderDocumentNumber,
                CompanyID = x.Invoice.CompanyID,
                BranchID = x.Invoice.BranchID,
            }).FirstOrDefault();
    }
}
