using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_Invoice
{
    public long ID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset InvoiceDate { get; set; }
}
