namespace ShiftSoftware.ADP.Models;

internal interface ICompanyProps
{
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }
}