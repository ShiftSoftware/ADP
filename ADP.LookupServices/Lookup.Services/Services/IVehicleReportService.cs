using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public interface IVehicleReportService
{
    Task<IEnumerable<string>> GetDistinctVinsAsync(int? count = null);
    Task<IEnumerable<VehicleServiceItemReportModel>> GetVehicleServiceItemsReportAsync(IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<int> ExportVehicleServiceItemsReportToCsvAsync(string fileFullPath, IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<IEnumerable<VehicleSscReportModel>> GetVehicleSscReportAsync(IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<int> ExportVehicleSscReportToCsvAsync(string fileFullPath, IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<(List<VehicleServiceHistoryLaborReportModel> LaborReports, List<VehicleServiceHistoryPartReportModel> PartReports)> GetVehicleServiceHistoryReportsAsync(IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<(int LaborRowCount, int PartRowCount)> ExportVehicleServiceHistoryReportsToCsvAsync(string laborFileFullPath, string partFileFullPath, IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<IEnumerable<VehicleServiceHistoryLaborReportModel>> GetVehicleServiceHistoryLaborReportAsync(IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<int> ExportVehicleServiceHistoryLaborReportToCsvAsync(string fileFullPath, IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<IEnumerable<VehicleServiceHistoryPartReportModel>> GetVehicleServiceHistoryPartReportAsync(IEnumerable<string> vins = null, int? distinctVinCount = null);
    Task<int> ExportVehicleServiceHistoryPartReportToCsvAsync(string fileFullPath, IEnumerable<string> vins = null, int? distinctVinCount = null);
}
