using System;

namespace ShiftSoftware.ADP.Models;

internal interface IInvoiceProps
{
    public DateTime? InvoiceDate { get; set; }
    public int? InvoiceNumber { get; set; }
    public string InvoiceCurrency { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public string SaleType { get; set; }
}