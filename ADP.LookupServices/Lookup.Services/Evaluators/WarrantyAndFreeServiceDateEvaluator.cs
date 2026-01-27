using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class WarrantyAndFreeServiceDateEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    private readonly LookupOptions Options;

    public WarrantyAndFreeServiceDateEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate, LookupOptions options)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
    }

    public VehicleWarrantyDTO Evaluate(VehicleEntryModel vehicle, VehicleSaleInformation saleInformation, bool ignoreBrokerStock)
    {
        DateTime? warrantyStartDate = null;
        DateTime? freeServiceStartDate = null;

        //Normal company Sale
        if (saleInformation?.Broker is null)
        {
            warrantyStartDate = CompanyDataAggregate.VehicleServiceActivations.FirstOrDefault()?.WarrantyActivationDate;

            if (warrantyStartDate is null)
                warrantyStartDate = saleInformation?.WarrantyActivationDate;

            if (warrantyStartDate is null && Options.WarrantyStartDateDefaultsToInvoiceDate)
                warrantyStartDate = saleInformation?.InvoiceDate;

            freeServiceStartDate = warrantyStartDate;
        }
        else
        {
            //Broker Stock
            if (saleInformation.Broker.InvoiceDate is null)
            {
                if (ignoreBrokerStock)
                {
                    warrantyStartDate = null;

                    freeServiceStartDate = CompanyDataAggregate.VehicleServiceActivations.FirstOrDefault()?.WarrantyActivationDate;

                    if (freeServiceStartDate is null)
                        freeServiceStartDate = saleInformation?.WarrantyActivationDate;

                    if (freeServiceStartDate is null && Options.WarrantyStartDateDefaultsToInvoiceDate)
                        freeServiceStartDate = saleInformation?.InvoiceDate;
                }
            }
            //Normal Broker Sale
            else
            {
                warrantyStartDate = saleInformation?.Broker?.InvoiceDate;
                freeServiceStartDate = saleInformation?.Broker?.InvoiceDate;
            }
        }

        VehicleWarrantyDTO result = new();

        var shiftDate = CompanyDataAggregate.WarrantyDateShifts?.FirstOrDefault();

        if (shiftDate is not null)
            warrantyStartDate = shiftDate.NewDate;

        if (warrantyStartDate is not null)
        {
            result.WarrantyStartDate = warrantyStartDate;

            if (Options.BrandStandardWarrantyPeriodsInYears.TryGetValue(vehicle.BrandID ?? 0, out var brandStandardWarrantyPeriodsInYears))
            {
                result.WarrantyEndDate = warrantyStartDate?.AddYears(brandStandardWarrantyPeriodsInYears);
            }
            else
            {
                result.WarrantyEndDate = warrantyStartDate?.AddYears(3);
            }
        }

        //Extended Warranty
        var lastExtendedWarrantyEntry = CompanyDataAggregate
            .ExtendedWarrantyEntries?
            .OrderByDescending(x => x.EndDate)?
            .FirstOrDefault();

        if (lastExtendedWarrantyEntry is not null)
        {
            result.ExtendedWarrantyStartDate = lastExtendedWarrantyEntry.StartDate;
            result.ExtendedWarrantyEndDate = lastExtendedWarrantyEntry.EndDate;
        }

        var freeServiceShiftDate = CompanyDataAggregate.FreeServiceItemDateShifts?.FirstOrDefault();

        if (freeServiceShiftDate is not null)
            freeServiceStartDate = freeServiceShiftDate.NewDate;

        result.FreeServiceStartDate = freeServiceStartDate;

        return result;
    }
}