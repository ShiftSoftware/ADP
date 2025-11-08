using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaidServiceInvoiceModel : IPartitionedItem, IBrandProps, ICompanyProps, IIntegrationProps
{
    public string id { get; set; } = default!;
    public DateTime InvoiceDate { get; set; }
    public DateTime StartDate { get; set; }
    public long InvoiceNumber { get; set; }
    public bool IsDeleted { get; set; }
    public string VIN { get; set; } = default!;
    public virtual IEnumerable<PaidServiceInvoiceLineModel> Lines { get; set; }
    public Brands Brand { get; set; }
    public long? BrandID { get; set; }
    public string BrandHashID { get; set; }
    public string ItemType => ModelTypes.PaidServiceInvoice;
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string IntegrationID { get; set; }
}