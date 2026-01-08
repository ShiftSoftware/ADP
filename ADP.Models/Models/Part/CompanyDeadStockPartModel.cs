namespace ShiftSoftware.ADP.Models.Part;

public class CompanyDeadStockPartModel : IPartitionedItem, ICompanyProps, IBranchProps, IGenuinePartProps
{
    /// <summary>
    /// The ADP ID of the part. This is a unique identifier that ADP generates for each part.
    /// </summary>
    [DocIgnore]
    public string id { get; set; } = default!;

    [DocIgnore]
    public string ItemType => ModelTypes.CompanyDeadStockPart;

    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public long? BranchID { get; set; }
    public string BranchHashID { get; set; }

    /// <summary>
    /// Usually a combination between Company & Branch ID: {companyId-branchId}
    /// </summary>
    [DocIgnore]
    public string Location { get; set; } = default!;

    /// <summary>
    /// Each part has a unique part number that is used to identify it in the catalog and other related documents/systems.
    /// </summary>
    public string PartNumber { get; set; } = default!;

    /// <summary>
    /// Indicates whether the part is a Genuine Part Number
    /// </summary>
    public bool GenuinePartNumber { get; set; }

    /// <summary>
    /// The current AvailableQuantity of the part in the stock.
    /// </summary>
    public decimal AvailableQuantity { get; set; }
}
