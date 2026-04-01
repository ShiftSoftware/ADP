using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Supported currencies used across invoices, pricing, and financial transactions in the platform.
/// </summary>
[Docable]
public enum Currencies
{
    /// <summary>
    /// No currency specified.
    /// </summary>
    [Description("Not Specified")]
    NotSpecified = 0,

    /// <summary>
    /// United States Dollar (USD).
    /// </summary>
    [Description("United States Dollar")]
    USD = 1,

    /// <summary>
    /// Iraqi Dinar (IQD).
    /// </summary>
    [Description("Iraqi Dinar")]
    IQD = 2,

    /// <summary>
    /// Uzbekistani Som (UZS).
    /// </summary>
    [Description("Uzbekistani Som")]
    UZS = 3,

    /// <summary>
    /// Turkmenistani Manat (TMT).
    /// </summary>
    [Description("Turkmenistani Manat")]
    TMT = 4,

    /// <summary>
    /// Tajikistani Somoni (TJS).
    /// </summary>
    [Description("Tajikistani Somoni")]
    TJS = 5,
}