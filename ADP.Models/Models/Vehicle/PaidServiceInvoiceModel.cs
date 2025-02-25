using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaidServiceInvoiceModel : IPartitionedItem
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public DateTime ActivationDate { get; set; }
    public long InvoiceNumber { get; set; }
    public bool IsDeleted { get; set; }
    public string VIN { get; set; } = default!;
    public virtual IEnumerable<PaidServiceInvoiceLineModel> Items { get; set; }
    public Brands Brand { get; set; }
    public string BrandIntegrationID { get; set; }
    public PartitionedItemType ItemType => ModelTypes.PaidServiceInvoice;
}