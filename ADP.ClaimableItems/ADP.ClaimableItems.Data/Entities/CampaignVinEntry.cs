using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;
using ShiftSoftware.ShiftEntity.Model.Replication;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

[TemporalShiftEntity]
public class CampaignVinEntry :
    ShiftEntity<CampaignVinEntry>,
    IEntityHasCompany<CampaignVinEntry>,
    IEntityHasCompanyBranch<CampaignVinEntry>,
    IEntityHasCountry<CampaignVinEntry>,
    IShiftEntityReplication
{
    public string VIN { get; set; } = default!;

    public long CampaignID { get; set; }
    public virtual Campaign Campaign { get; set; } = default!;

    public DateTime RecordedDate { get; set; }

    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public long? CountryID { get; set; }

    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }
}
