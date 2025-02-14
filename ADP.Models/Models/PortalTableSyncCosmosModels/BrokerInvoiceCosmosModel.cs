using System;
using ShiftSoftware.ADP.Models.DealerData;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class BrokerInvoiceCosmosModel : IDealerDataCSV
{
    public string id { get; set; } = default!;

    public string VIN { get; set; } = default!;

    public string ItemType => "BrokerInvoice";

    public long Id { get; set; }

    public DateTime InvoiceDate { get; set; }

    public decimal Price { get; set; }

    public string Vin { get; set; }

    public string Model { get; set; }

    public string ModelYear { get; set; }

    public string Engine { get; set; }

    public string VariantDesc { get; set; }

    public string Katashiki { get; set; }

    public string Color { get; set; }

    public string ColorDesc { get; set; }

    public string Trim { get; set; }

    public string TrimDesc { get; set; }

    public string CustomerSignature { get; set; }

    public string CustomerDocument { get; set; }

    public long? BrokerCustomerId { get; set; }

    public long? UserBranchId { get; set; }

    public DateTime SaveDate { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? ModificationDate { get; set; }

    public long? CreatedByUserId { get; set; }

    public long? ModifiedByUserId { get; set; }

    public bool Deleted { get; set; }

    public bool Archived { get; set; }

    public long? OriginalVersionId { get; set; }

    public long BrokerId { get; set; }

    public string VariantCode { get; set; }

    public Guid Guid { get; set; }

    public string Cylinders { get; set; }

    public string PlateNumber { get; set; }

    public string PlateNumberRegion { get; set; }

    public long UserId { get; set; }

    public long? UserUserId { get; set; }

    public string UserSignature { get; set; }

    public long? DivisionId { get; set; }

    public long InvoiceNo { get; set; }

    public int SaleType { get; set; }

    public string CustomerDocument2 { get; set; }

    public int InvoiceStatus { get; set; }

    public DateTime? FinalInvoiceDate { get; set; }

    public long? NonOfficialBrokerCustomerId { get; set; }

    public string CustomerDocument3 { get; set; }
}
