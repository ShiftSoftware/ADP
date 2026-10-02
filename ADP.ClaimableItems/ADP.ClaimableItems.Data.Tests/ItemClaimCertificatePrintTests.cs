using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

/// <summary>
/// Prints an item-claim certificate and an invoice through <see cref="ItemClaimCertificateRepository.PrintAsync"/>, with
/// the templates embedded in the module. The database is EF Core's in-memory provider, and the company details have no
/// logo, so nothing outside the process is needed. FastReport draws with System.Drawing (GDI+), so on Linux these tests
/// need libgdiplus.
/// </summary>
public class ItemClaimCertificatePrintTests
{
    /// <summary>
    /// The campaign hides the Model / Katashiki column and shows the sign and stamp band, so every line of the
    /// certificate-only part of the print code runs.
    /// </summary>
    [Fact]
    public async Task A_certificate_prints_with_the_campaign_printout_settings()
    {
        await using var services = BuildServices();
        var certificateId = await SeedInvoicedCertificate(services);

        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ItemClaimCertificateRepository>();

        await using var pdf = await repository.PrintAsync(certificateId.ToString());

        AssertIsPdf(pdf);
    }

    /// <summary>
    /// The invoice template has no campaign header or body, no sign and stamp band and no Model / Katashiki column. The
    /// print code used to fill these in invoice mode too, so every invoice print failed with a NullReferenceException.
    /// </summary>
    [Fact]
    public async Task An_invoice_prints()
    {
        await using var services = BuildServices();
        var certificateId = await SeedInvoicedCertificate(services);

        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ItemClaimCertificateRepository>();
        // The invoice controller sets this flag. In invoice mode the repository prints the invoice template.
        repository.IsInvoiceMode = true;

        await using var pdf = await repository.PrintAsync(certificateId.ToString());

        AssertIsPdf(pdf);
    }

    private static void AssertIsPdf(Stream pdf)
    {
        var header = new byte[5];
        pdf.ReadExactly(header);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(header));
    }

    /// <summary>The services a host registers for printing, with test stand-ins. Each call makes a new database.</summary>
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // The name is made once, outside the options callback. The callback runs again for every scope, and each
        // scope must open the same database.
        var databaseName = Guid.NewGuid().ToString();

        // The module entities reach the model through the same contributors that a host registers.
        services.AddDbContext<ShiftDbContext, PrintTestDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));

        // The repository constructors need these. Printing does not use them.
        services.AddScoped<ICurrentUserProvider, NoSignedInUser>();
        services.AddScoped<IdentityClaimProvider>();
        services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, NotUsedWhilePrinting>());
        services.AddSingleton<SharedClaimService>();

        // No hash ID configuration is registered, so the print ID is the certificate ID as plain text.
        services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
        services.AddSingleton<ICompanyInfoProvider, FixedCompanyInfoProvider>();

        services.AddScoped<CampaignRepository>();
        services.AddScoped<ItemClaimRepository>();
        services.AddScoped<ItemClaimCertificateRepository>();

        return services.BuildServiceProvider();
    }

    /// <summary>An invoiced certificate with two claims, from a campaign that has printout settings.</summary>
    private static async Task<long> SeedInvoicedCertificate(ServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        var campaign = new Campaign
        {
            Name = """{"en":"Campaign"}""",
            CertificatePrintoutHeader = "Certificate {CertificateNo} for {Dealer}, {StartDate} to {EndDate}",
            CertificatePrintoutBody = "{Distributor} pays {Dealer}. Invoice date: {InvoiceDate}.",
            CertificatePrintoutSignStampVisibility = true,
            CertificatePrintoutKatashikiAndModelColumnVisibility = false,
        };

        // The certificate keeps the campaign as a plain ID, with no navigation, so the campaign is saved first.
        db.Add(campaign);
        await db.SaveChangesAsync();

        var certificate = new Certificate
        {
            CertificateType = CertificateTypes.ClaimableItemClaim,
            CampaignID = campaign.ID,
            CompanyID = 1,
            CertificateNo = 1,
            CertificateDate = new DateTime(2026, 9, 1),
            PeriodStartDate = new DateTime(2026, 8, 1),
            PeriodEndDate = new DateTime(2026, 8, 31),
            InvoiceDate = new DateTime(2026, 9, 15),
        };

        var item = new ClaimableItem { Name = """{"en":"Service"}""", Costs = "[]", Campaign = campaign };

        db.AddRange(
            NewClaim(certificate, campaign, item, "VIN00000000000001", 120m),
            NewClaim(certificate, campaign, item, "VIN00000000000002", 80m));

        await db.SaveChangesAsync();

        return certificate.ID;
    }

    private static ItemClaim NewClaim(Certificate certificate, Campaign campaign, ClaimableItem item, string vin, decimal cost) => new()
    {
        ReimbursementCertificate = certificate,
        Campaign = campaign,
        ClaimableItem = item,
        CompanyID = 1,
        VIN = vin,
        ClaimDate = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero),
        Cost = cost,
        ModelDescription = "Model",
        Katashiki = "Katashiki",
    };

    private class PrintTestDbContext(DbContextOptions<PrintTestDbContext> options) : ShiftDbContext(options);

    private class NoSignedInUser : ICurrentUserProvider
    {
        public ClaimsPrincipal? GetUser() => null;
    }

    /// <summary>
    /// Stands in for a service that a repository constructor needs but printing never calls. Every call throws, so a
    /// test fails clearly if printing starts to use the service.
    /// </summary>
    private class NotUsedWhilePrinting : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"{targetMethod?.Name} is not expected to be called while printing.");
    }

    /// <summary>Fixed company details. There is no logo, so the print does not try to download one.</summary>
    private class FixedCompanyInfoProvider : ICompanyInfoProvider
    {
        private static Task<CompanyPrintInfo> Company(string name) =>
            Task.FromResult(new CompanyPrintInfo(name, $"{name} address", "+000 000 0000", "example.com", Logo: null, ShortCode: name));

        public Task<CompanyPrintInfo> GetDistributorAsync(string language) => Company("Distributor");
        public Task<CompanyPrintInfo> GetManufacturerAsync(string language) => Company("Manufacturer");
        public Task<CompanyPrintInfo> GetDealerAsync(string language, long companyId) => Company("Dealer");
        public Task<CompanyPrintInfo> GetBranchAsync(string language, long companyBranchId) => Company("Branch");
    }
}
