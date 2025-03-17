using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models;

internal interface IInvoiceProps
{
    public DateTime? InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; }
    public Currencies InvoiceCurrency { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }
    public string SaleType { get; set; }
}