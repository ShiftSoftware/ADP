using ShiftSoftware.ADP.Cases.Shared.Printing;

namespace ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.API.Services;

/// <summary>
/// The company details on the contract print. A real host reads them from its identity data; the sample takes the
/// distributor from <c>Sample:Distributor</c> and names dealers and branches by their ID.
/// </summary>
public class SampleCompanyInfoProvider : ICompanyInfoProvider
{
    private readonly CompanyPrintInfo distributor;

    public SampleCompanyInfoProvider(IConfiguration configuration)
    {
        var section = configuration.GetSection("Sample:Distributor");

        distributor = new CompanyPrintInfo(
            section["Name"] ?? "Distributor",
            section["Address"] ?? "",
            section["Phone"] ?? "",
            section["Website"] ?? "",
            Logo: string.IsNullOrWhiteSpace(section["Logo"]) ? null : section["Logo"],
            ShortCode: section["ShortCode"] ?? "");
    }

    public Task<CompanyPrintInfo> GetDistributorAsync(string language) => Task.FromResult(distributor);

    public Task<CompanyPrintInfo> GetManufacturerAsync(string language) => Task.FromResult(distributor);

    public Task<CompanyPrintInfo> GetDealerAsync(string language, long companyId) =>
        Task.FromResult(distributor with { Name = $"Dealer {companyId}", Logo = null });

    public Task<CompanyPrintInfo> GetBranchAsync(string language, long companyBranchId) =>
        Task.FromResult(distributor with { Name = $"Branch {companyBranchId}", Logo = null });
}
