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
    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;
    private readonly IVehicleLookupStorageService LookupCosmosService;

    public VehicleSaleInformationEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options, IServiceProvider serviceProvider, IVehicleLookupStorageService lookupCosmosService)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = serviceProvider;
        this.LookupCosmosService = lookupCosmosService;
    }

    public async Task<VehicleSaleInformation> Evaluate(VehicleEntryModel vehicle, VehicleOwnership ownership, VehicleLookupRequestOptions requestOptions)
    {
        VehicleSaleInformation result = new();

        string languageCode = requestOptions.LanguageCode;

        // The entry supplies spec and sale/invoice fields; Company/Country/Region/Branch come from
        // the resolved ownership (activation-authoritative), so the reported sale dealer matches the
        // owner Service Item eligibility was computed for. A null vehicle means the VIN has no
        // entries → no sale information.
        if (vehicle is null)
            return null;

        result.InvoiceDate = vehicle?.InvoiceDate;
        result.WarrantyActivationDate = vehicle?.WarrantyActivationDate;
        result.Status = vehicle.OrderStatus;
        result.Location = vehicle.Location;
        result.SaleType = vehicle.SaleType;
        result.AccountNumber = vehicle.AccountNumber;
        result.RegionID = ownership?.RegionID?.ToString();

        result.InvoiceNumber = vehicle?.InvoiceNumber;
        result.InvoiceTotal = vehicle?.InvoiceTotal ?? 0;
        result.CompanyID = ownership?.CompanyID?.ToString();
        result.BranchID = ownership?.BranchID?.ToString();

        result.CustomerID = vehicle?.CustomerID;
        result.CustomerAccountNumber = vehicle?.CustomerAccountNumber;

        result.CountryID = ownership?.CountryID?.ToString();

        if (Options.CountryNameResolver is not null)
            result.CountryName = await Options.CountryNameResolver(new(ownership?.CountryID, languageCode, ServiceProvider));

        if (Options.CompanyNameResolver is not null)
            result.CompanyName = await Options.CompanyNameResolver(new(ownership?.CompanyID, languageCode, ServiceProvider));

        if (Options.CompanyBranchNameResolver is not null)
            result.BranchName = await Options.CompanyBranchNameResolver(
                new(ownership?.BranchID, languageCode, ServiceProvider));

        if (Options.CityFromBranchIDResolver is not null)
        {
            var cityID = await Options.CityFromBranchIDResolver(
                new(ownership?.BranchID, languageCode, ServiceProvider));

            result.CityID = cityID?.ToString();

            if (cityID.HasValue && Options.CityNameResolver is not null)
                result.CityName = await Options.CityNameResolver(
                    new(cityID, languageCode, ServiceProvider));
        }

        string companyLogo = null;

        if (Options.CompanyLogoResolver is not null)
            companyLogo = await Options.CompanyLogoResolver(new(ownership?.CompanyID, languageCode, ServiceProvider));

        if (Options.LookupBrokerStock)
        {
            var brokerStockEntries = await this.LookupCosmosService.GetBrokerStockAsync(
                vehicle.BrandID,
                CompanyDataAggregate.VIN
            );

            if (brokerStockEntries is not null && brokerStockEntries.Count() > 0)
            {
                var currentBrokerStock = brokerStockEntries
                    .Where(x => x.IsAtStock)
                    .FirstOrDefault();

                //At Broker Stock
                if (currentBrokerStock is not null)
                {
                    result.Broker = new VehicleBrokerSaleInformation
                    {
                        BrokerID = currentBrokerStock.BrokerID,
                        BrokerName = currentBrokerStock.Broker.Name,
                    };
                }
                //Not at Broker Stock
                else
                {
                    var stocksWithInvoices = brokerStockEntries
                        .Where(x => x.Invoices?.Any(i => !i.IsDeleted) == true)
                        .ToList();

                    if (stocksWithInvoices.Count > 0)
                    {
                        var lastBrokerStock = stocksWithInvoices
                            .OrderByDescending(x => x.Invoices.Where(x => !x.IsDeleted).Max(x => x.InvoiceDate))
                            .FirstOrDefault();

                        if (lastBrokerStock is not null)
                        {
                            var lastBrokerInvoice = lastBrokerStock
                                .Invoices?
                                .Where(x => !x.IsDeleted)
                                .OrderByDescending(x => x.InvoiceDate)
                                .FirstOrDefault();
                            if (lastBrokerInvoice is not null)
                            {
                                result.Broker = new VehicleBrokerSaleInformation
                                {
                                    BrokerID = lastBrokerStock.BrokerID,
                                    BrokerName = lastBrokerStock.Broker.Name,
                                    NonOfficialBrokerName = lastBrokerInvoice.NonOfficialBrokerName,
                                    InvoiceDate = lastBrokerInvoice.InvoiceDate.ToUniversalTime().Date,
                                    InvoiceNumber = lastBrokerInvoice.InvoiceNumber,
                                };

                                if (requestOptions.LookupEndCustomer && lastBrokerInvoice.IsCompleted)
                                {
                                    result.EndCustomer = new VehicleSaleEndCustomerInformationDTO
                                    {
                                        Name = lastBrokerInvoice.CustomerName,
                                        Phone = lastBrokerInvoice.CustomerPhone,
                                        IDNumber = lastBrokerInvoice.CustomerIDNumber
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }

        //Load Customer from Customer Database
        if (requestOptions.LookupEndCustomer && result.Broker is null)
        {
            var customer = await this.LookupCosmosService.GetCustomerAsync(vehicle.CustomerID, ownership?.CompanyID);

            if (customer is not null)
            {
                result.EndCustomer = new VehicleSaleEndCustomerInformationDTO
                {
                    ID = vehicle.CustomerID,
                    Name = customer.FullName,
                    Phone = customer.PhoneNumbers?.FirstOrDefault(),
                    IDNumber = customer.IDNumber
                };
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