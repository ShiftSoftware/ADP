using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class BranchDeadStockDTO
{
    public string CompanyBranchHashID { get; set; }
    public string CompanyBranchName { get; set; }
    public decimal Quantity { get; set; }
}