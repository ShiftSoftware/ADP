using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;

public interface ISaleInfoExtractor
{
    public Task<VehicleSaleInformation> ExtractSaleInformationAsync(List<VehicleEntryModel> vehicles, string languageCode, CompanyDataAggregateCosmosModel companyDataAggregate);
}