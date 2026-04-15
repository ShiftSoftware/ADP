using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

public interface IMenuReportExporter
{
    ValueTask<byte[]> ExportMenusToExcel(MenuExportContext context);
    ValueTask<byte[]> ExportRTSCodesToExcel(RTSCodeExportContext context);
    ValueTask<byte[]> ExportMenuDetailReportToExcel(MenuDetailReportExportContext context);
}

public class MenuExportContext
{
    public required IEnumerable<MenuLineDTO> Lines { get; set; }
    public required Dictionary<long?, BrandMapping> BrandMappings { get; set; }
    public required Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> LabourRateMappings { get; set; }
    public long CountryId { get; set; }
    public decimal TransferRate { get; set; }
}

public class RTSCodeExportContext
{
    public required IEnumerable<RTSCodeExportModel> Items { get; set; }
    public required Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> LabourRateMappings { get; set; }
    public required Dictionary<long?, BrandMapping> BrandMappings { get; set; }
    public long CountryId { get; set; }
    public decimal TransferRate { get; set; }
}

public class MenuDetailReportExportContext
{
    public required IEnumerable<MenuLineDTO> NewVersionLines { get; set; }
    public required IEnumerable<MenuLineDTO> OldVersionLines { get; set; }
    public required Dictionary<long?, BrandMapping> BrandMappings { get; set; }
    public required Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> LabourRateMappings { get; set; }
    public long CountryId { get; set; }
    public decimal TransferRate { get; set; }
    public DateTimeOffset? OldVersionDateTime { get; set; }
    public DateTimeOffset? NewVersionDateTime { get; set; }
}
