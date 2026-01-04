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
        var labors = CompanyDataAggregate.LaborLines;
        var parts = CompanyDataAggregate.PartLines;

        var groupedLaborLines = labors?
            .GroupBy(l => new
            {
                l.CompanyID,
                l.BranchID,
                l.InvoiceNumber,
                l.OrderDocumentNumber
            });

        var groupedPartLines = parts?
           .GroupBy(l => new
           {
               l.CompanyID,
               l.BranchID,
               l.InvoiceNumber,
               l.OrderDocumentNumber
           });

        var joinedInvoices = groupedLaborLines?
            .Select(x => x.Key)
            .Concat(groupedPartLines?.Select(x => x.Key))
            .Distinct();

        var serviceHistory = new List<VehicleServiceHistoryDTO>();

        foreach (var invoice in joinedInvoices)
        {
            var laborLines = groupedLaborLines?
                .Where(x => x.Key.CompanyID == invoice.CompanyID)
                .Where(x => x.Key.BranchID == invoice.BranchID)
                .Where(x => x.Key.InvoiceNumber == invoice.InvoiceNumber)
                .Where(x => x.Key.OrderDocumentNumber == invoice.OrderDocumentNumber)
                .FirstOrDefault()?
                .ToList();

            var partLines = groupedPartLines?
                .Where(x => x.Key.CompanyID == invoice.CompanyID)
                .Where(x => x.Key.BranchID == invoice.BranchID)
                .Where(x => x.Key.InvoiceNumber == invoice.InvoiceNumber)
                .Where(x => x.Key.OrderDocumentNumber == invoice.OrderDocumentNumber)
                .FirstOrDefault()?
                .ToList();

            var numberOfPartLinesAccordingToLaborRecords = laborLines?
                .Select(x => x.NumberOfPartLines)?
                .FirstOrDefault();

            var numberOfLaborLinesAccordingToPartRecords = partLines?
                .Select(x => x.NumberOfLaborLines)?
                .FirstOrDefault();

            if (consistencyLevel == ConsistencyLevels.Strong)
            {
                if (numberOfPartLinesAccordingToLaborRecords != partLines?.Count() || numberOfLaborLinesAccordingToPartRecords != laborLines?.Count())
                    continue; // Inconsistent data, skip this entry
            }

            var serviceHistoryEntry = new VehicleServiceHistoryDTO
            {
                InvoiceNumber = invoice.InvoiceNumber,
                JobNumber = invoice.OrderDocumentNumber,
                AccountNumber = laborLines?.Select(x => x.AccountNumber).FirstOrDefault() ?? partLines?.Select(x => x.AccountNumber).FirstOrDefault(),
                CompanyName = null,
                BranchName = null,
                ServiceDate = new[] { laborLines?.Max(x => x.InvoiceDate), partLines?.Max(x => x.InvoiceDate) }.Max(),

                Mileage = null,
                ServiceType = null,

                LaborLines = laborLines?.Select(l => new VehicleLaborDTO
                {
                    RTSCode = l.LaborCode,
                    ServiceCode = l.ServiceCode,
                    Description = l.ServiceDescription,
                    MenuCode = l.MenuCode,
                }),
                PartLines = partLines?.Select(p => new VehiclePartDTO
                {
                    PartNumber = p.PartNumber,
                    PartDescription = null,
                    MenuCode = p.MenuCode,
                    QTY = p.SoldQuantity,
                })
            };

            if (this.Options.CompanyNameResolver is not null)
                serviceHistoryEntry.CompanyName = await this.Options.CompanyNameResolver(new LookupOptionResolverModel<long?>(invoice.CompanyID, languageCode, this.ServiceProvider));

            if (this.Options.CompanyBranchNameResolver is not null)
                serviceHistoryEntry.BranchName = await this.Options.CompanyBranchNameResolver(new LookupOptionResolverModel<long?>(invoice.BranchID, languageCode, this.ServiceProvider));

            serviceHistory.Add(serviceHistoryEntry);
        }

        return serviceHistory;
    }
}