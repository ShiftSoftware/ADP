using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
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

    public async Task<IEnumerable<VehicleServiceHistoryDTO>> Evaluate(string languageCode)
    {
        var invoices = CompanyDataAggregate.Invoices;
        var labors = CompanyDataAggregate.LaborLines;
        var parts = CompanyDataAggregate.PartLines;

        var serviceHistory = new List<VehicleServiceHistoryDTO>();

        if (invoices != null)
        {
            foreach (var x in invoices.OrderByDescending(x => x.InvoiceDate))
            {
                // Remove the branch id from the service type
                var serviceType = x.ServiceDetails;
                var slashIndex = serviceType.IndexOf("/");
                if (slashIndex > 0)
                {
                    var branchId = serviceType.Substring(0, slashIndex);
                    if (int.TryParse(branchId, out _))
                        serviceType = serviceType.Substring(slashIndex + 1);
                }

                var result = new VehicleServiceHistoryDTO
                {
                    ServiceType = serviceType,
                    ServiceDate = x.InvoiceDate,
                    Mileage = x.Mileage,
                    //CompanyID = x.CompanyID,
                    //BranchID = x.BranchID,
                    AccountNumber = x.AccountNumber,
                    InvoiceNumber = x.InvoiceNumber,
                    JobNumber = x.OrderDocumentNumber,
                    LaborLines = labors?.Where(l => l.OrderDocumentNumber == x.OrderDocumentNumber && l.InvoiceNumber == x.InvoiceNumber &&
                        l.CompanyID == x.CompanyID)
                            .Select(l => new VehicleLaborDTO
                            {
                                Description = l.ServiceDescription,
                                MenuCode = l.MenuCode,
                                RTSCode = l.LaborCode,
                                ServiceCode = l.ServiceCode
                            }),
                    PartLines = parts?.Where(p => p.OrderDocumentNumber == x.OrderDocumentNumber && p.InvoiceNumber == x.InvoiceNumber &&
                        p.CompanyID == x.CompanyID)
                            .Select(p => new VehiclePartDTO
                            {
                                MenuCode = p.MenuCode,
                                PartNumber = p.PartNumber,
                                QTY = p.SoldQuantity,
                            })
                };

                if (Options.CompanyNameResolver is not null)
                    result.CompanyName = await Options.CompanyNameResolver(new(x.CompanyID, languageCode, ServiceProvider));

                if (Options.CompanyBranchNameResolver is not null)
                    result.BranchName = await Options.CompanyBranchNameResolver(
                        new(x.BranchID, languageCode, ServiceProvider));

                serviceHistory.Add(result);
            }
        }

        return serviceHistory;
    }
}