using System;
using System.Collections.Generic;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ToyotaLoyaltyProgramRedeemableItemCosmosModel
{
    public string id { get; set; } = default!;

    public long ToyotaLoyaltyProgramRedeemableItemId { get; set; }

    public Dictionary<string, string> Name { get; set; }

    public decimal? TLP { get; set; }

    public Dictionary<string, string> Photo { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModificationDate { get; set; }

    public string Notes { get; set; }

    public bool? Deleted { get; set; }

    public bool? ArchivedVersion { get; set; }

    public long? CreatedByUserId { get; set; }

    public long? ModifiedByUserId { get; set; }

    public DateTime? PublishDate { get; set; }

    public DateTime? ExpireDate { get; set; }

    public int? ItemType { get; set; }

    public decimal? RoundedTLP { get; set; }

    public long? ParentToyotaLoyaltyProgramRedeemableItemId { get; set; }

    public decimal? Cost { get; set; }

    public int? RedeemType { get; set; }

    public int? RedeemableAt { get; set; }

    public Dictionary<string, string> PrintoutTitle { get; set; }

    public Dictionary<string, string> PrintoutDescription { get; set; }

    public string Code { get; set; }

    public IEnumerable<string> Franchises { get; set; }

    public IEnumerable<Franchises> Brands { get; set; }

    public IEnumerable<string> BrandIntegrationIDs { get; set; }

    public int Sort { get; set; }

    public bool SkipZeroTrust { get; set; }

    public int? ActiveFor { get; set; }

    public string ActiveForInterval { get; set; }

    public long? MaximumMileage { get; set; }

    public decimal? DistributorContribution { get; set; }

    public decimal? DealerContribution { get; set; }

    public long? ToyotaLoyaltyProgramRedeemableItemImportId { get; set; }

    public DateTime? LastReplicationDate { get; set; }

    public string MenuCode { get; set; }

    public IEnumerable<ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel> ModelCosts { get; set; }
}
