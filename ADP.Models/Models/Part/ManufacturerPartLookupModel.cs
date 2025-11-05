using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model.Flags;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

[Docable]
public class ManufacturerPartLookupModel:
    IEntityHasCompany<ManufacturerPartLookupModel>,
    IEntityHasCompanyBranch<ManufacturerPartLookupModel>,
    IEntityHasCity<ManufacturerPartLookupModel>
{
    [DocIgnore]
    public string id { get; set; } = default!;
    public long? CompanyBranchID { get; set; }
    public long? CompanyID { get; set; }
    public long? CityID { get; set; }
    public long? UserID { get; set; }

    public string PartNumber { get; set; }
    public decimal Quantity { get; set; }
    public ManufacturerOrderType OrderType { get; set; }
    public string? LogId { get; set; }

    public ManufacturerPartLookupBotStatus BotStatus { get; set; }

    public Dictionary<string, string> LookupResult { get; set; }
}
