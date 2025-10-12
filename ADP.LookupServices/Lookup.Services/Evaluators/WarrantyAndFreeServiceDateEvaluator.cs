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
        if (vehicle is null)
            return null;

        DateTime? warrantyStartDate = null;
        DateTime? freeServiceStartDate = null;

        //Normal company Sale
        if (saleInformation?.Broker is null)
        {
            warrantyStartDate = saleInformation?.WarrantyActivationDate;

            warrantyStartDate = CompanyDataAggregate.VehicleServiceActivations.FirstOrDefault()?.WarrantyActivationDate;

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
                    freeServiceStartDate = saleInformation?.WarrantyActivationDate ?? saleInformation?.InvoiceDate;
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

        result.WarrantyStartDate = warrantyStartDate;

        if (vehicle.Brand == Brands.Lexus)
            result.WarrantyEndDate = warrantyStartDate?.AddYears(4);
        else
            result.WarrantyEndDate = warrantyStartDate?.AddYears(3);


        // Extended warranty
        //var extendedWarranty = extendedWarranties?.FirstOrDefault(x => x.ResponseType == "3");

        //if (extendedWarranty is not null)
        //{
        //    result.ExtendedWarrantyStartDate = extendedWarranty?.UpdatedDate is null ? null
        //        : DateOnly.FromDateTime(extendedWarranty.UpdatedDate.Value);
        //    result.ExtendedWarrantyEndDate = result.ExtendedWarrantyStartDate?.AddYears(2);
        //}

        result.FreeServiceStartDate = freeServiceStartDate;

        return result;
    }
}