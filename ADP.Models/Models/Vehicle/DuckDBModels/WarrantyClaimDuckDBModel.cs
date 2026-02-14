using System;

namespace ShiftSoftware.ADP.Models.Vehicle.DuckDBModels;

public class WarrantyClaimDuckDBModel : WarrantyClaimModel
{
    public new Guid id { get; set; }
    public DateTime? ModificationDate { get; set; }
}
