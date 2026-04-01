namespace ShiftSoftware.ADP.Models;

/// <summary>
/// Defines customer identification properties for models that are linked to a customer record.
/// </summary>
public interface ICustomerProps
{
    /// <summary>
    /// The customer ID from the dealer's system.
    /// </summary>
    public string CustomerID { get; set; }

    /// <summary>
    /// The Golden Customer ID linking to the unified customer identity.
    /// </summary>
    public string GoldenCustomerID { get; set; }
}