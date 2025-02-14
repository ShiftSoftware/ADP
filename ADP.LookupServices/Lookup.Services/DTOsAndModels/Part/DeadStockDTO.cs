using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class DeadStockDTO
{
    public string CompanyIntegrationID { get; set; }
    public string CompanyName { get; set; }
    public IEnumerable<BranchDeadStockDTO> BranchDeadStock { get; set; }
}