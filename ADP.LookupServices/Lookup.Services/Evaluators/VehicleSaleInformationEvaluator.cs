using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleSaleInformationEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;
    private readonly IVehicleLoockupCosmosService LookupCosmosService;

    public VehicleSaleInformationEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate, LookupOptions options, IServiceProvider serviceProvider, IVehicleLoockupCosmosService lookupCosmosService)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = serviceProvider;
        this.LookupCosmosService = lookupCosmosService;
    }

    public async Task<VehicleSaleInformation> Evaluate(string languageCode)
    {
        VehicleSaleInformation result = new();

        var vehicles = CompanyDataAggregate.VehicleEntries;

        if (!(vehicles?.Any() ?? false))
            return null;

        var vsData = vehicles
            .OrderByDescending(x => x.InvoiceDate == null)
            .ThenByDescending(x => x.InvoiceDate)
            .FirstOrDefault();

        result.InvoiceDate = vsData?.InvoiceDate;
        result.WarrantyActivationDate = vsData?.WarrantyActivationDate;
        result.Status = vsData.OrderStatus;
        result.Location = vsData.Location;
        result.SaleType = vsData.SaleType;
        result.AccountNumber = vsData.AccountNumber;
        //result.RegionID = vsData.RegionID;

        result.InvoiceNumber = vsData?.InvoiceNumber;
        result.InvoiceTotal = vsData?.InvoiceTotal ?? 0;
        //result.CompanyID = vsData?.CompanyID;
        //result.BranchID = vsData?.BranchID;

        result.CustomerID = vsData?.CustomerID;
        result.CustomerAccountNumber = vsData?.CustomerAccountNumber;

        if (Options.CountryFromBranchIDResolver is not null)
        {
            var countryResult = await Options.CountryFromBranchIDResolver(new(vsData.BranchID, languageCode, ServiceProvider));

            if (countryResult is not null)
            {
                result.CountryID = countryResult.Value.countryID?.ToString();
                result.CountryName = countryResult.Value.countryName;
            }
        }

        if (Options.CompanyNameResolver is not null)
            result.CompanyName = await Options.CompanyNameResolver(new(vsData.CompanyID, languageCode, ServiceProvider));

        if (Options.CompanyBranchNameResolver is not null)
            result.BranchName = await Options.CompanyBranchNameResolver(
                new(vsData.BranchID, languageCode, ServiceProvider));

        string? companyLogo = null;

        if (Options.CompanyLogoResolver is not null)
            companyLogo = await Options.CompanyLogoResolver(new(vsData.CompanyID, languageCode, ServiceProvider));

        //if(!string.IsNullOrWhiteSpace(companyLogo))
        //    try
        //    {
        //        result.CompanyLogo = await GetCompanyLogo(JsonSerializer.Deserialize<List<ShiftFileDTO>>(companyLogo));
        //    }
        //    catch (Exception){}

        if (CompanyDataAggregate.BrokerInvoices?.Any() ?? false)
        {
            var brokerInvoice = CompanyDataAggregate.BrokerInvoices.FirstOrDefault();
            var broker = await LookupCosmosService.GetBrokerAsync(brokerInvoice.ID);

            result.Broker = new VehicleBrokerSaleInformation
            {
                BrokerID = brokerInvoice.ID,
                BrokerName = broker?.Name,
                CustomerID = (brokerInvoice.BrokerCustomerID ?? brokerInvoice.NonOfficialBrokerCustomerID) ?? 0,
                InvoiceDate = brokerInvoice.InvoiceDate,
                InvoiceNumber = brokerInvoice.InvoiceNumber,
            };
        }
        else
        {
            var broker = await LookupCosmosService.GetBrokerAsync(vsData?.CustomerAccountNumber, vsData?.CompanyID);

            // If vehicle sold to broker and the broker is terminated, then make vsdata as start date.
            // If vehicle sold to broker before start date and it is not exists in broker intial vehicles,
            // then make vsdata as start date.
            if (broker is not null)
                if (!broker.TerminationDate.HasValue && (broker.AccountStartDate <= vsData?.InvoiceDate || CompanyDataAggregate.BrokerInitialVehicles?.Count(x => x?.BrokerID == broker.ID) > 0))
                    result.Broker = new VehicleBrokerSaleInformation
                    {
                        BrokerID = broker.ID,
                        BrokerName = broker.Name
                    };
        }

        return result;
    }
}