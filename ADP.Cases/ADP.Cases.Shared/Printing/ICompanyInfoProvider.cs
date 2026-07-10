namespace ShiftSoftware.ADP.Cases.Shared.Printing;

/// <summary>
/// Consumer seam supplying company identity for module printouts (Phase 3 Slice 3.2). The module
/// ships NO default implementation — printing requires the consumer to register one
/// (<c>services.AddScoped&lt;ICompanyInfoProvider, YourCompanyInfoProvider&gt;()</c>); module print
/// methods throw a descriptive <see cref="System.InvalidOperationException"/> when it is missing.
/// The original host implements this over its CompanyInfoService (identity Cosmos + signed logo
/// URLs); implementations should return placeholder values rather than throw when a lookup fails,
/// so a printout still renders.
/// </summary>
public interface ICompanyInfoProvider
{
    /// <summary>The distributor (the organization running this system).</summary>
    Task<CompanyPrintInfo> GetDistributorAsync(string language);

    /// <summary>The manufacturer / franchisor the distributor settles claims with.</summary>
    Task<CompanyPrintInfo> GetManufacturerAsync(string language);

    /// <summary>A dealer company by its company ID.</summary>
    Task<CompanyPrintInfo> GetDealerAsync(string language, long companyId);

    /// <summary>A company branch by its branch ID.</summary>
    Task<CompanyPrintInfo> GetBranchAsync(string language, long companyBranchId);
}
