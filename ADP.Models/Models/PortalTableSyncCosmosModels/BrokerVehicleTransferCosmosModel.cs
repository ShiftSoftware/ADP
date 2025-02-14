using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class BrokerVehicleTransferCosmosModel
{
    public long ID { get; set; }

    public DateTime TransferDate { get; set; }

    public decimal Price { get; set; }

    public string VIN { get; set; }

    public string Model { get; set; }

    public string ModelYear { get; set; }

    public string Engine { get; set; }

    public string Variant_Desc { get; set; }

    public string Variant_Code { get; set; }

    public string Katashiki { get; set; }

    public string Color { get; set; }

    public string ColorDesc { get; set; }

    public string Trim { get; set; }

    public string TrimDesc { get; set; }

    public string Cylinders { get; set; }

    public string PlateNumber { get; set; }

    public string PlateNumberRegion { get; set; }

    public long TransferNo { get; set; }

    public Guid Guid { get; set; }

    public long UserId { get; set; }

    public string UserSignature { get; set; }

    public long? DivisionId { get; set; }

    public long BrokerId { get; set; }

    public string BrokerName { get; set; }

    public long SoldToBrokerId { get; set; }

    public int SaleType { get; set; }

    public long? UserBranchID { get; set; }

    public DateTime SaveDate { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? ModificationDate { get; set; }

    public long? CreatedByUserID { get; set; }

    public long? ModifiedByUserID { get; set; }

    public bool Deleted { get; set; }

    public bool Archived { get; set; }

    public long? OriginalVersionId { get; set; }

    public long? User_UserId { get; set; }

    public string VSRegion { get; set; }

    public DateTime? LastReplicationDate { get; set; }

    public string id { get; set; }
    public string ItemType => "BrokerVehicleTransfer";
}
