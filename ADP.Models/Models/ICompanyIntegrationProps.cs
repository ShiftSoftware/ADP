namespace ShiftSoftware.ADP.Models;

public interface ICompanyIntegrationProps
{
    /// <summary>
    /// An External Identifier that can be used for system to system Integration
    /// </summary>
    public string CompanyIntegrationID { get; set; }
}