using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_Invoice
{
    public long ID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset InvoiceDate { get; set; }
    public bool IsCompleted { get; set; }
    public long? InvoiceNumber { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public string CustomerIDNumber { get; set; }
}
