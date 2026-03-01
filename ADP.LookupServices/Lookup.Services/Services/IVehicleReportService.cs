using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public interface IVehicleReportService
{
    Task<IEnumerable<string>> GetDistinctVinsAsync(int? count = null);
    Task<IEnumerable<VehicleServiceItemReportModel>> GetVehicleServiceItemsReportAsync(IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<int> ExportVehicleServiceItemsReportToCsvAsync(string fileFullPath, IEnumerable<string> vins = null, int? distinctVinCount = null);
}
