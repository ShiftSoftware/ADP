using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ShiftSoftware.ADP.Models.Constants.NoSQLConstants.PartitionKeys;

namespace ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;

public class WarrantyAndFreeServiceEvaluator : IWarrantyAndFreeServiceEvaluator
{
    private readonly LookupOptions options;

    public WarrantyAndFreeServiceEvaluator(LookupOptions options)
    {
        this.options = options;
    }

    public async Task<DateTime?> EvaluateFreeServiceStartDate(Brands brand, bool ignoreBrokerStock, VehicleLookupDTO data, CompanyDataAggregateCosmosModel companyDataAggregate)
    {
        DateTime? warrantyStartDate = null;
        DateTime? freeServiceStartDate = null;

        
        {
            //Normal company Sale
            if (data.SaleInformation?.Broker is null)
            {
                warrantyStartDate = data.SaleInformation?.WarrantyActivationDate;

                var serviceActivation = companyDataAggregate.VehicleServiceActivations.FirstOrDefault();

                if (serviceActivation is not null)
                    warrantyStartDate = serviceActivation.WarrantyActivationDate;

                if (warrantyStartDate is null && options.WarrantyStartDateDefaultsToInvoiceDate)
                    warrantyStartDate = data.SaleInformation?.InvoiceDate;

                freeServiceStartDate = warrantyStartDate;
            }
            else
            {
                //Broker Stock
                if (data.SaleInformation.Broker.InvoiceDate is null)
                {
                    if (ignoreBrokerStock)
                    {
                        warrantyStartDate = null;
                        freeServiceStartDate = data.SaleInformation?.WarrantyActivationDate ?? data.SaleInformation?.InvoiceDate;
                    }
                }
                //Normal Broker Sale
                else
                {
                    warrantyStartDate = data.SaleInformation?.Broker?.InvoiceDate;
                    freeServiceStartDate = data.SaleInformation?.Broker?.InvoiceDate;
                }
            }

            // Set Warranty
            data.Warranty = await GetWarrantyAsync(companyDataAggregate, warrantyStartDate, brand);
        }

        return warrantyStartDate;
    }

    public async Task<DateTime?> EvaluateWarrantyStartDate(Brands brand, bool ignoreBrokerStock, VehicleLookupDTO data, CompanyDataAggregateCosmosModel companyDataAggregate)
    {
        DateTime? warrantyStartDate = null;
        DateTime? freeServiceStartDate = null;


        {
            //Normal company Sale
            if (data.SaleInformation?.Broker is null)
            {
                warrantyStartDate = data.SaleInformation?.WarrantyActivationDate;

                var serviceActivation = companyDataAggregate.VehicleServiceActivations.FirstOrDefault();

                if (serviceActivation is not null)
                    warrantyStartDate = serviceActivation.WarrantyActivationDate;

                if (warrantyStartDate is null && options.WarrantyStartDateDefaultsToInvoiceDate)
                    warrantyStartDate = data.SaleInformation?.InvoiceDate;

                freeServiceStartDate = warrantyStartDate;
            }
            else
            {
                //Broker Stock
                if (data.SaleInformation.Broker.InvoiceDate is null)
                {
                    if (ignoreBrokerStock)
                    {
                        warrantyStartDate = null;
                        freeServiceStartDate = data.SaleInformation?.WarrantyActivationDate ?? data.SaleInformation?.InvoiceDate;
                    }
                }
                //Normal Broker Sale
                else
                {
                    warrantyStartDate = data.SaleInformation?.Broker?.InvoiceDate;
                    freeServiceStartDate = data.SaleInformation?.Broker?.InvoiceDate;
                }
            }

            // Set Warranty
            data.Warranty = await GetWarrantyAsync(companyDataAggregate, warrantyStartDate, brand);
        }

        return freeServiceStartDate;
    }

    private async Task<VehicleWarrantyDTO> GetWarrantyAsync(CompanyDataAggregateCosmosModel companyDataAggregate, DateTime? invoiceDate, Brands brand)
    {
        VehicleWarrantyDTO result = new();

        var shiftDate = companyDataAggregate.WarrantyDateShifts?.FirstOrDefault();
        if (shiftDate is not null)
            invoiceDate = shiftDate.NewDate;

        result.WarrantyStartDate = invoiceDate;

        if (brand == Brands.Lexus)
            result.WarrantyEndDate = invoiceDate?.AddYears(4);
        else
            result.WarrantyEndDate = invoiceDate?.AddYears(3);


        // Extended warranty
        //var extendedWarranty = extendedWarranties?.FirstOrDefault(x => x.ResponseType == "3");

        //if (extendedWarranty is not null)
        //{
        //    result.ExtendedWarrantyStartDate = extendedWarranty?.UpdatedDate is null ? null
        //        : DateOnly.FromDateTime(extendedWarranty.UpdatedDate.Value);
        //    result.ExtendedWarrantyEndDate = result.ExtendedWarrantyStartDate?.AddYears(2);
        //}

        return result;
    }
}