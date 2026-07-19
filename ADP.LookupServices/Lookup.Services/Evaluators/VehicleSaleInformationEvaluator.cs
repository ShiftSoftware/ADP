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

        await PopulateSupplyChainAsync(result, vehicle, languageCode);

        return result;
    }

    /// <summary>
    /// Surfaces the supply-chain legs (distributor + intermediaries) beside the resolved sale. These are role
    /// facts — which entity imported the vehicle and which moved it onward — so the block is informational; it
    /// never changes the resolved sale, warranty, or eligibility. A company keeps its supply-chain role even
    /// when it also made the end-customer sale (a distributor selling straight to a customer is still the
    /// distributor), so classification here is by company alone. Additive and a no-op when nothing is
    /// configured/present. Handles both source shapes:
    /// <list type="bullet">
    /// <item><b>Multi-entry</b> (per-dealer DMS feeds): the VIN has a separate
    /// <see cref="VehicleEntryModel"/> per leg; classify them by company role against
    /// <see cref="LookupOptions.DistributorCompanyID"/> / <see cref="LookupOptions.IntermediaryCompanyIDs"/>.</item>
    /// <item><b>Single-entry</b>: the one selected entry carries the intermediary leg inline on
    /// <see cref="VehicleEntryModel.Intermediary"/>.</item>
    /// </list>
    /// </summary>
    private async Task PopulateSupplyChainAsync(VehicleSaleInformation result, VehicleEntryModel vehicle, string languageCode)
    {
        var entries = CompanyDataAggregate.VehicleEntries ?? Enumerable.Empty<VehicleEntryModel>();

        // Distributor leg. Two source shapes:
        //  * Multi-entry (per-dealer DMS feeds): a separate entry carries the distributor's invoice + dates;
        //    classify it by company. Latest invoiced wins if the distributor has more than one entry.
        //  * Single-entry: the distributor is implicit — every vehicle ships through it
        //    and there is no distributor entry — so surface it from the configured DistributorCompanyID.
        if (Options.DistributorCompanyID is { } distributorCompanyID)
        {
            // Classified purely by company role: the distributor is the entity that imported the vehicle, which is
            // true regardless of who it went on to sell to. So a distributor entry that is itself the end-customer
            // sale (it sold straight to the customer) is still surfaced here — its invoice is simply the
            // distributor's own outbound sale, which in that case is the sale to the customer.
            var distributorEntry = entries
                .Where(e => e.CompanyID == distributorCompanyID)
                .OrderByDescending(e => e.InvoiceDate)
                .FirstOrDefault();

            if (distributorEntry is not null)
            {
                var (companyName, branchName, cityID, cityName) =
                    await ResolveLegLocationAsync(distributorEntry.CompanyID, distributorEntry.BranchID, languageCode);

                result.Distributor = new VehicleDistributorSaleInformation
                {
                    CompanyID = distributorEntry.CompanyID?.ToString(),
                    CompanyName = companyName,
                    BranchID = distributorEntry.BranchID?.ToString(),
                    BranchName = branchName,
                    InvoiceNumber = distributorEntry.InvoiceNumber,
                    InvoiceDate = distributorEntry.InvoiceDate,
                    CityID = cityID,
                    CityName = cityName,
                };
            }
            else if (entries.Count() == 1)
            {
                // Single-entry source: no distributor entry exists; the configured distributor is the
                // implicit upstream. Its company comes from config; its invoice/date — the distributor's own
                // sale (distributor→dealer on a direct route, distributor→intermediary on a two-leg route) — ride inline on the
                // single entry's Distributor leg when present.
                var (companyName, _, _, _) = await ResolveLegLocationAsync(distributorCompanyID, null, languageCode);

                result.Distributor = new VehicleDistributorSaleInformation
                {
                    CompanyID = distributorCompanyID.ToString(),
                    CompanyName = companyName,
                    InvoiceNumber = vehicle?.Distributor?.InvoiceNumber,
                    InvoiceDate = vehicle?.Distributor?.InvoiceDate,
                };
            }
        }

        // Intermediary legs (0..n): entries whose company is a configured intermediary, earliest-invoiced
        // first (closest to the distributor). The distributor takes precedence, so a company configured as
        // both is surfaced only as the distributor.
        foreach (var intermediaryEntry in entries
            .Where(e => e.CompanyID is { } cid
                && (Options.IntermediaryCompanyIDs?.Contains(cid) ?? false)
                && Options.DistributorCompanyID != cid)
            .OrderBy(e => e.InvoiceDate))
        {
            var (companyName, branchName, cityID, cityName) =
                await ResolveLegLocationAsync(intermediaryEntry.CompanyID, intermediaryEntry.BranchID, languageCode);

            result.Intermediaries.Add(new VehicleIntermediarySaleInformation
            {
                CompanyID = intermediaryEntry.CompanyID?.ToString(),
                CompanyName = companyName,
                BranchID = intermediaryEntry.BranchID?.ToString(),
                BranchName = branchName,
                InvoiceNumber = intermediaryEntry.InvoiceNumber,
                InvoiceDate = intermediaryEntry.InvoiceDate,
                CityID = cityID,
                CityName = cityName,
            });
        }

        // Single-entry embedded intermediary leg — only when the multi-entry classification found
        // none, so the two shapes never double-count. The selected dealer entry carries it inline; its
        // InvoiceNumber/InvoiceDate are the intermediary's own sale to the dealer.
        if (result.Intermediaries.Count == 0 && vehicle?.Intermediary is { } leg)
        {
            var (companyName, branchName, cityID, cityName) =
                await ResolveLegLocationAsync(leg.CompanyID, leg.BranchID, languageCode);

            result.Intermediaries.Add(new VehicleIntermediarySaleInformation
            {
                CompanyID = leg.CompanyID?.ToString(),
                CompanyName = companyName,
                BranchID = leg.BranchID?.ToString(),
                BranchName = branchName,
                InvoiceNumber = leg.InvoiceNumber,
                InvoiceDate = leg.InvoiceDate,
                CityID = cityID,
                CityName = cityName,
            });
        }
    }

    /// <summary>
    /// Resolves a supply-chain leg's display fields (company name, branch name, city id + name) from its raw
    /// company/branch ids using the same host resolvers the dealer sale uses. Each resolver is optional;
    /// unconfigured ones leave the corresponding field null.
    /// </summary>
    private async Task<(string CompanyName, string BranchName, string CityID, string CityName)> ResolveLegLocationAsync(
        long? companyID, long? branchID, string languageCode)
    {
        string companyName = null, branchName = null, cityID = null, cityName = null;

        if (Options.CompanyNameResolver is not null)
            companyName = await Options.CompanyNameResolver(new(companyID, languageCode, ServiceProvider));

        if (Options.CompanyBranchNameResolver is not null)
            branchName = await Options.CompanyBranchNameResolver(new(branchID, languageCode, ServiceProvider));

        if (Options.CityFromBranchIDResolver is not null)
        {
            var cityId = await Options.CityFromBranchIDResolver(new(branchID, languageCode, ServiceProvider));
            cityID = cityId?.ToString();

            if (cityId.HasValue && Options.CityNameResolver is not null)
                cityName = await Options.CityNameResolver(new(cityId, languageCode, ServiceProvider));
        }

        return (companyName, branchName, cityID, cityName);
    }
}