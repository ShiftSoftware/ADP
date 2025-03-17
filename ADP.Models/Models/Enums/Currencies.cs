using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum Currencies
{
    [Description("Not Specified")]
    NotSpecified = 0,

    [Description("United States Dollar")]
    USD = 1,

    [Description("Iraqi Dinar")]
    IQD = 2,

    [Description("Uzbekistani Som")]
    UZS = 3,

    [Description("Turkmenistani Manat")]
    TMT = 4,

    [Description("Tajikistani Somoni")]
    TJS = 5,
}