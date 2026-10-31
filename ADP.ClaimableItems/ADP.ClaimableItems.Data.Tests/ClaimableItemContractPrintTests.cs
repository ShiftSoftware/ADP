using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

/// <summary>
/// Prints a service contract through <see cref="ClaimableItemContractRepository.PrintAsync"/> with the template embedded
/// in the module. The database is EF Core's in-memory provider and the company details have no logo, so nothing
/// outside the process is needed. FastReport draws with System.Drawing (GDI+), so on Linux these tests need libgdiplus.
/// </summary>
public class ClaimableItemContractPrintTests
{
    [Fact]
    public async Task An_issued_contract_prints_with_a_cancelled_line()
    {
        await using var services = BuildServices();
        var id = await SeedContractAsync(services, isDraft: false, contractCancelled: false);

        using var scope = services.CreateScope();
        await using var pdf = await scope.ServiceProvider.GetRequiredService<ClaimableItemContractRepository>().PrintAsync(id.ToString());

        AssertIsPdf(pdf);
    }

    [Fact]
    public async Task A_cancelled_contract_prints()
    {
        await using var services = BuildServices();
        var id = await SeedContractAsync(services, isDraft: false, contractCancelled: true);

        using var scope = services.CreateScope();
        await using var pdf = await scope.ServiceProvider.GetRequiredService<ClaimableItemContractRepository>().PrintAsync(id.ToString());

        AssertIsPdf(pdf);
    }

    [Fact]
    public async Task A_draft_is_not_printed()
    {
        await using var services = BuildServices();
        var id = await SeedContractAsync(services, isDraft: true, contractCancelled: false);

        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ClaimableItemContractRepository>();

        var error = await Assert.ThrowsAsync<ShiftEntityException>(async () => await repository.PrintAsync(id.ToString()));
        Assert.Contains("Only an issued contract", error.Message.Body);
    }

    private static void AssertIsPdf(Stream pdf)
    {
        var header = new byte[5];
        pdf.ReadExactly(header);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(header));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddDbContext<ShiftDbContext, PrintTestDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));

        // The repository constructor needs these. Printing does not use them.
        services.AddScoped<ICurrentUserProvider, NoSignedInUser>();
        services.AddScoped<IdentityClaimProvider>();
        services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, NotUsedWhilePrinting>());
        services.AddScoped(_ => DispatchProxy.Create<IClaimableItemContractMenuProvider, NotUsedWhilePrinting>());

        services.AddSingleton(new ClaimableItemsDataOptions { ContractTimeZone = TimeZoneInfo.Utc });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
        services.AddSingleton<ICompanyInfoProvider, FixedCompanyInfoProvider>();

        services.AddScoped<ClaimableItemContractMenuService>();
        services.AddScoped<ClaimableItemContractRepository>();

        return services.BuildServiceProvider();
    }

    /// <summary>A contract with two periodic services and a standalone one, the 20,000 km service cancelled.</summary>
    private static async Task<long> SeedContractAsync(ServiceProvider services, bool isDraft, bool contractCancelled)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        var start = DateTime.UtcNow.Date;
        var contract = new ClaimableItemContract
        {
            VIN = "JTMHX01J6M4217945",
            CustomerName = "Customer",
            CustomerPhone = "+000 000 0000",
            ModelDescription = "Model",
            Katashiki = "KAT-1",
            Variant = "Variant",
            ContractType = ClaimableItemContractType.PurchasedServiceContract,
            Mileage = 5000,
            StartDate = start,
            Duration = 1,
            DurationUnit = ClaimableItemContractDurationUnit.Years,
            EndDate = start.AddYears(1),
            PriceIncrement = 0.05m,
            IsDraft = isDraft,
            IssuedDate = isDraft ? null : DateTime.UtcNow,
            IsCancelled = contractCancelled,
            CompanyID = 1,
            CompanyBranchID = 2,
            Lines =
            {
                Line("P|1|10", "CON10K", ClaimableItemContractLineType.Periodic, 10000, 105m, start.AddMonths(6), cancelled: false),
                Line("P|1|20", "CON20K", ClaimableItemContractLineType.Periodic, 20000, 126m, start.AddMonths(12), cancelled: true),
                Line("S|1|7", "WIPER", ClaimableItemContractLineType.Standalone, null, 42m, start.AddYears(1), cancelled: false),
            },
        };

        db.Add(contract);
        await db.SaveChangesAsync();

        return contract.ID;
    }

    private static ClaimableItemContractLine Line(string key, string code, ClaimableItemContractLineType type, int? km, decimal price, DateTime expires, bool cancelled) => new()
    {
        MenuLineKey = key,
        MenuCode = code,
        Description = $"{code} service",
        LineType = type,
        ServiceIntervalValueInMeter = km,
        PriceWithIncrement = price,
        ExpireDate = expires,
        IsCancelled = cancelled,
    };

    private class PrintTestDbContext(DbContextOptions<PrintTestDbContext> options) : ShiftDbContext(options);

    private class NoSignedInUser : ICurrentUserProvider
    {
        public ClaimsPrincipal? GetUser() => null;
    }

    private class NotUsedWhilePrinting : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"{targetMethod?.Name} is not expected to be called while printing.");
    }

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
