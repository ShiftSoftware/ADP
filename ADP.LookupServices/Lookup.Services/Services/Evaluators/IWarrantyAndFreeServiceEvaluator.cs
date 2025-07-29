using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;

public interface IWarrantyAndFreeServiceEvaluator
{
    Task<DateTime?> EvaluateWarrantyStartDate(Brands brand, bool ignoreBrokerStock, VehicleLookupDTO data, CompanyDataAggregateCosmosModel companyDataAggregate);
    Task<DateTime?> EvaluateFreeServiceStartDate(Brands brand, bool ignoreBrokerStock, VehicleLookupDTO data, CompanyDataAggregateCosmosModel companyDataAggregate);
}