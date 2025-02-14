using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class BrokerCosmosModel
{
    public string id { get; set; } = default!;

    public long Id { get; set; }

    public string Name { get; set; }

    public long DivisionId { get; set; }

    public string DealerIntegrationID { get; set; }

    public string Image { get; set; }

    public long? FooterLeftId { get; set; }

    public long? FooterRightId { get; set; }

    public long? UserBranchId { get; set; }

    public DateTime SaveDate { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? ModificationDate { get; set; }

    public long? CreatedByUserId { get; set; }

    public long? ModifiedByUserId { get; set; }

    public bool Deleted { get; set; }

    public bool Archived { get; set; }

    public long? OriginalVersionId { get; set; }

    public bool ForceDealer { get; set; }

    public DateTime? AccountStartDate { get; set; }

    public string LocationCode { get; set; }

    public int Region { get; set; }

    public DateTime? TerminationDate { get; set; }

    public List<string> AccountNumbers { get; set; }

    public string RegionIntegrationID { get; set; }
}
