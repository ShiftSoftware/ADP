using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines invoice-related properties for models that represent or are associated with a sales invoice.
/// </summary>
internal interface IInvoiceProps
{
    /// <summary>
    /// The date of the invoice.
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// The invoice number.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// The currency used on the invoice.
    /// </summary>
    public Currencies? InvoiceCurrency { get; set; }

    /// <summary>
    /// The status of the invoice (e.g., Paid, Pending).
    /// </summary>
    public string InvoiceStatus { get; set; }

    /// <summary>
    /// The dealer account number on the invoice.
    /// </summary>
    public string AccountNumber { get; set; }

    /// <summary>
    /// The customer's account number at the dealer.
    /// </summary>
    public string CustomerAccountNumber { get; set; }

    /// <summary>
    /// The customer ID associated with this invoice.
    /// </summary>
    public string CustomerID { get; set; }

    /// <summary>
    /// The type of sale (e.g., Retail, Fleet, Internal).
    /// </summary>
    public string SaleType { get; set; }
}