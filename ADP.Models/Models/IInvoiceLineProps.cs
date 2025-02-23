using System;

namespace ShiftSoftware.ADP.Models;

internal interface IInvoiceLineProps
{
    public string Status { get; set; }
    public int LineNumber { get; set; }
    public string LineStatus { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateInserted { get; set; }
}