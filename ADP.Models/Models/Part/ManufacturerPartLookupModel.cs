using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

[Docable]
public class ManufacturerPartLookupModel :
    ICompanyProps,
    IBranchProps
{
    [DocIgnore]
    public string id { get; set; } = default!;
    public long? BranchID { get; set; }
    public string BranchHashID { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public long? UserID { get; set; }
    public string? UserEmail { get; set; }

    public string PartNumber { get; set; }
    public decimal Quantity { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public string? LogId { get; set; }

    public ManufacturerPartLookupStatus Status { get; set; }

    public IEnumerable<KeyValuePair<string, string>>? ManufacturerResult { get; set; } = [];
}
