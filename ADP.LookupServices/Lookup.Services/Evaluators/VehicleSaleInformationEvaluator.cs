using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Vehicle;
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

        VehicleEntryModel vehicle = null;

        var serviceActivation = CompanyDataAggregate
            .VehicleServiceActivations
            .FirstOrDefault();

        if (serviceActivation is not null)
        {
            vehicle = vehicles
                .Where(x => x.CompanyID == serviceActivation.CompanyID)
                .FirstOrDefault();
        }
        else
        {
            vehicle = vehicles
                .OrderByDescending(x => x.InvoiceDate == null)
                .ThenByDescending(x => x.InvoiceDate)
                .FirstOrDefault();
        }

        result.InvoiceDate = vehicle?.InvoiceDate;
        result.WarrantyActivationDate = vehicle?.WarrantyActivationDate;
        result.Status = vehicle.OrderStatus;
        result.Location = vehicle.Location;
        result.SaleType = vehicle.SaleType;
        result.AccountNumber = vehicle.AccountNumber;
        //result.RegionID = vsData.RegionID;

        result.InvoiceNumber = vehicle?.InvoiceNumber;
        result.InvoiceTotal = vehicle?.InvoiceTotal ?? 0;
        //result.CompanyID = vsData?.CompanyID;
        //result.BranchID = vsData?.BranchID;

        result.CustomerID = vehicle?.CustomerID;
        result.CustomerAccountNumber = vehicle?.CustomerAccountNumber;

        if (Options.CountryFromBranchIDResolver is not null)
        {
            var countryResult = await Options.CountryFromBranchIDResolver(new(vehicle.BranchID, languageCode, ServiceProvider));

            if (countryResult is not null)
            {
                result.CountryID = countryResult.Value.countryID?.ToString();
                result.CountryName = countryResult.Value.countryName;
            }
        }

        if (Options.CompanyNameResolver is not null)
            result.CompanyName = await Options.CompanyNameResolver(new(vehicle.CompanyID, languageCode, ServiceProvider));

        if (Options.CompanyBranchNameResolver is not null)
            result.BranchName = await Options.CompanyBranchNameResolver(
                new(vehicle.BranchID, languageCode, ServiceProvider));

        string companyLogo = null;

        if (Options.CompanyLogoResolver is not null)
            companyLogo = await Options.CompanyLogoResolver(new(vehicle.CompanyID, languageCode, ServiceProvider));

        if (Options.LookupBrokerStock)
        {
            var brokerStockEntries = await this.LookupCosmosService.GetBrokerStockAsync(
                vehicle.BrandID,
                CompanyDataAggregate.VIN
            );

            if (brokerStockEntries?.Any() ?? false)
            {
                var currentBrokerStock = brokerStockEntries
                    .Where(x => x.IsAtStock)
                    .FirstOrDefault();

                if (currentBrokerStock is not null)
                {
                    result.Broker = new VehicleBrokerSaleInformation
                    {
                        BrokerID = currentBrokerStock.BrokerID,
                        BrokerName = currentBrokerStock.Broker.Name,
                    };
                }
                else
                {
                    if (brokerStockEntries.Where(x => x.Invoices is not null).SelectMany(x => x.Invoices).Count() > 0)
                    {
                        var lastBrokerStock = brokerStockEntries
                            .OrderByDescending(x => x.Invoices.Max(x => x.InvoiceDate))
                            .FirstOrDefault();

                        if (lastBrokerStock is not null)
                        {
                            var lastBrokerInvoice = lastBrokerStock
                                .Invoices?
                                .OrderByDescending(x => x.InvoiceDate)
                                .FirstOrDefault();

                            result.Broker = new VehicleBrokerSaleInformation
                            {
                                BrokerID = lastBrokerStock.BrokerID,
                                BrokerName = lastBrokerStock.Broker.Name,
                                InvoiceDate = lastBrokerInvoice.InvoiceDate.ToUniversalTime().Date,
                                InvoiceNumber = lastBrokerInvoice.ID,
                            };
                        }
                    }
                }
            }
        }

        //if (CompanyDataAggregate.BrokerInvoices?.Any() ?? false)
        //{
        //    var brokerInvoice = CompanyDataAggregate.BrokerInvoices.FirstOrDefault();
        //    var broker = await LookupCosmosService.GetBrokerAsync(brokerInvoice.ID);

        //    result.Broker = new VehicleBrokerSaleInformation
        //    {
        //        BrokerID = brokerInvoice.ID,
        //        BrokerName = broker?.Name,
        //        CustomerID = (brokerInvoice.BrokerCustomerID ?? brokerInvoice.NonOfficialBrokerCustomerID) ?? 0,
        //        InvoiceDate = brokerInvoice.InvoiceDate,
        //        InvoiceNumber = brokerInvoice.InvoiceNumber,
        //    };
        //}
        //else
        //{
        //    var broker = await LookupCosmosService.GetBrokerAsync(vehicle?.CustomerAccountNumber, vehicle?.CompanyID);

        //    // If vehicle sold to broker and the broker is terminated, then make vsdata as start date.
        //    // If vehicle sold to broker before start date and it is not exists in broker intial vehicles,
        //    // then make vsdata as start date.
        //    if (broker is not null)
        //        if (!broker.TerminationDate.HasValue && (broker.AccountStartDate <= vehicle?.InvoiceDate || CompanyDataAggregate.BrokerInitialVehicles?.Count(x => x?.BrokerID == broker.ID) > 0))
        //            result.Broker = new VehicleBrokerSaleInformation
        //            {
        //                BrokerID = broker.ID,
        //                BrokerName = broker.Name
        //            };
        //}

        return result;
    }
}