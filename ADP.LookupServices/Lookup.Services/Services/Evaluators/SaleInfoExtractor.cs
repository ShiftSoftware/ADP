using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;

public class SaleInfoExtractor : ISaleInfoExtractor
{
    private readonly LookupOptions options;
    private readonly IVehicleLoockupCosmosService lookupCosmosService;
    private readonly IServiceProvider services;

    public SaleInfoExtractor(LookupOptions options, IVehicleLoockupCosmosService lookupCosmosService, IServiceProvider services = null)
    {
        this.options = options;
        this.lookupCosmosService = lookupCosmosService;
        this.services = services;
    }

    public async Task<VehicleSaleInformation> ExtractSaleInformationAsync(List<VehicleEntryModel> vehicles, string languageCode, CompanyDataAggregateCosmosModel companyDataAggregate)
    {
        VehicleSaleInformation result = new();

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
        result.RegionID = vsData.RegionID;

        result.InvoiceNumber = vsData?.InvoiceNumber;
        result.InvoiceTotal = vsData?.InvoiceTotal ?? 0;
        result.CompanyID = vsData?.CompanyID;
        result.BranchID = vsData?.BranchID;

        result.CustomerID = vsData?.CustomerID;
        result.CustomerAccountNumber = vsData?.CustomerAccountNumber;

        if (options.CountryFromBranchIDResolver is not null)
        {
            var countryResult = await options.CountryFromBranchIDResolver(new(vsData.BranchID, languageCode, services));

            if (countryResult is not null)
            {
                result.CountryID = countryResult.Value.countryID;
                result.CountryName = countryResult.Value.countryName;
            }
        }

        if (options.CompanyNameResolver is not null)
            result.CompanyName = await options.CompanyNameResolver(new(vsData.CompanyID, languageCode, services));

        if (options.CompanyBranchNameResolver is not null)
            result.BranchName = await options.CompanyBranchNameResolver(
                new(vsData.BranchID, languageCode, services));

        string? companyLogo = null;

        if (options.CompanyLogoResolver is not null)
            companyLogo = await options.CompanyLogoResolver(new(vsData.CompanyID, languageCode, services));

        //if(!string.IsNullOrWhiteSpace(companyLogo))
        //    try
        //    {
        //        result.CompanyLogo = await GetCompanyLogo(JsonSerializer.Deserialize<List<ShiftFileDTO>>(companyLogo));
        //    }
        //    catch (Exception){}

        if (companyDataAggregate.BrokerInvoices?.Any() ?? false)
        {
            var brokerInvoice = companyDataAggregate.BrokerInvoices.FirstOrDefault();
            var broker = await lookupCosmosService.GetBrokerAsync(brokerInvoice.ID);

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
            //var broker = await lookupCosmosService.GetBrokerAsync(vsData?.CustomerAccountNumber, vsData?.CompanyID);

            //// If vehicle sold to broker and the broker is terminated, then make vsdata as start date.
            //// If vehicle sold to broker before start date and it is not exists in broker intial vehicles,
            //// then make vsdata as start date.
            //if (broker is not null)
            //    if (!broker.TerminationDate.HasValue && (broker.AccountStartDate <= vsData?.InvoiceDate || companyDataAggregate.BrokerInitialVehicles?.Count(x => x?.BrokerID == broker.ID) > 0))
            //        result.Broker = new VehicleBrokerSaleInformation
            //        {
            //            BrokerID = broker.ID,
            //            BrokerName = broker.Name
            //        };
        }

        return result;
    }
}