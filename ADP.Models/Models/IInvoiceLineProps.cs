using System;

namespace ShiftSoftware.ADP.Models;

internal interface IInvoiceLineProps
{
    public string Status { get; set; }
    public int LineNumber { get; set; }
    public string LineStatus { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
}