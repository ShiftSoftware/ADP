using System;
using System.Collections.Generic;
using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class TLPPackageInvoiceCosmosModel : IDealerDataCSV
{
    public string id { get; set; } = default!;

    public long Id { get; set; }

    public int TIQId { get; set; }

    public string Phone { get; set; }

    public DateTime StartDate { get; set; }

    public int Duration { get; set; }

    public string Interval { get; set; }

    public long? TLPItemPackageId { get; set; }

    public decimal Total { get; set; }

    public bool Closed { get; set; }

    public long InvoiceNo { get; set; }

    public long? UserBranchId { get; set; }

    public DateTime SaveDate { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? ModificationDate { get; set; }

    public long? CreatedByUserId { get; set; }

    public long? ModifiedByUserId { get; set; }

    public bool Deleted { get; set; }

    public bool Archived { get; set; }

    public long? OriginalVersionId { get; set; }

    public string CustomerName { get; set; }

    public int Franchise { get; set; }

    public string Model { get; set; }

    public long Mileage { get; set; }

    public long ToyotaAppUserId { get; set; }

    public int USDToIQD { get; set; }

    public decimal PriceIncrement { get; set; }

    public int ContractType { get; set; }

    public DateTime? LastReplicationDate { get; set; }

    public string VIN { get; set; } = default!;

    public string ItemType => "TLPPackageInvoice";

    public virtual IEnumerable<TLPPackageInvoiceTLPItemCosmosModel> Items { get; set; }

    public Franchises Brand { get; set; }
    public string BrandIntegrationID { get; set; }
}
