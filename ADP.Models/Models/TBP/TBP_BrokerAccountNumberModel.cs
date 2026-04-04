namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a broker's account number at a specific company.
/// Brokers may have different account numbers for different dealer companies they work with.
/// </summary>
[Docable]
public class TBP_BrokerAccountNumberModel
{
    /// <summary>
    /// The account number assigned to the broker at the company.
    /// </summary>
    public string AccountNumber { get; set; } = default!;

    /// <summary>
    /// The company ID this account number belongs to.
    /// </summary>
    public long? CompanyID { get; set; }
}
