using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyCurrencyRate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyLaborRate;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// What a dealer gets of a claim through the claim repository, and what it may do with the rates. The distributor's
/// labor rate and totals stay with the distributor: a response that leaks them is still a 200 of the same shape.
/// </summary>
public class DealerClaimExposureTests
{
    private static readonly string[] WithheldFromDealerList = ["HourTotalDistributor", "TotalClaimAmountDistributor"];

    [Fact]
    public async Task The_claim_list_withholds_exactly_the_distributor_figures()
    {
        await using var host = await Host.CreateAsync();

        var distributor = await (await host.Claims(isDistributor: true).OdataList(host.Db.Set<Entities.WarrantyClaim>())).SingleAsync();
        var dealer = await (await host.Claims(isDistributor: false).OdataList(host.Db.Set<Entities.WarrantyClaim>())).SingleAsync();

        Assert.Equal(1.5m, distributor.HourTotalDistributor);
        Assert.Equal(45, distributor.TotalClaimAmountDistributor);

        var differing = typeof(WarrantyClaimListDTO).GetProperties()
            .Where(p => p.CanRead && JsonSerializer.Serialize(p.GetValue(dealer)) != JsonSerializer.Serialize(p.GetValue(distributor)))
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal);

        Assert.Equal(WithheldFromDealerList, differing);
    }

    [Fact]
    public async Task A_dealer_can_not_filter_the_list_by_a_distributor_figure()
    {
        await using var host = await Host.CreateAsync();

        var dealer = await host.Claims(isDistributor: false).OdataList(host.Db.Set<Entities.WarrantyClaim>());

        Assert.Empty(await dealer.Where(x => x.TotalClaimAmountDistributor > 1).ToListAsync());
        Assert.Single(await dealer.Where(x => x.TotalClaimAmount > 1).ToListAsync());
    }

    [Fact]
    public async Task A_dealers_view_of_a_claim_and_its_currency_have_no_distributor_labor_rate()
    {
        await using var host = await Host.CreateAsync();
        var claim = await host.ClaimAsync();

        Assert.Equal(30, (await host.Claims(isDistributor: true).ViewAsync(claim)).LaborRateDistributor);

        var dealer = await host.Claims(isDistributor: false).ViewAsync(claim);

        Assert.Null(dealer.LaborRateDistributor);
        Assert.Equal(20, dealer.LaborRate);
        Assert.Equal(0, dealer.LaborTotalAmountDistributor);

        Assert.Equal(30, (await host.Claims(isDistributor: true).GetClaimCurrencyAsync(null, null, null)).Currency.LaborRateDistributor);

        var dealerCurrency = (await host.Claims(isDistributor: false).GetClaimCurrencyAsync(null, null, null)).Currency;

        Assert.Null(dealerCurrency.LaborRateDistributor);
        Assert.Equal(20, dealerCurrency.LaborRate);
    }

    [Fact]
    public async Task Only_the_distributor_reads_the_labor_rates_and_changes_the_rates()
    {
        await using var host = await Host.CreateAsync();

        var laborRates = new WarrantyLaborRateRepository(host.Db, new Capability(IsDistributor: false));
        var currencyRates = new WarrantyCurrencyRateRepository(host.Db, new Capability(IsDistributor: false));

        async Task ForbiddenAsync(Func<Task> act) =>
            Assert.Equal(403, (await Assert.ThrowsAsync<ShiftEntityException>(act)).HttpStatusCode);

        await ForbiddenAsync(async () => await laborRates.GetIQueryable(null, null, true, true));
        await ForbiddenAsync(() => { laborRates.GetRevisionsAsync(1); return Task.CompletedTask; });
        await ForbiddenAsync(async () => await laborRates.UpsertAsync(new(), new WarrantyLaborRateDTO(), ActionTypes.Insert, 1, null, true, true));
        await ForbiddenAsync(async () => await laborRates.DeleteAsync(new(), 1, true, true));
        await ForbiddenAsync(async () => await currencyRates.UpsertAsync(new(), new WarrantyCurrencyRateDTO(), ActionTypes.Insert, 1, null, true, true));
        await ForbiddenAsync(async () => await currencyRates.DeleteAsync(new(), 1, true, true));

        var asDistributor = new WarrantyLaborRateRepository(host.Db, new Capability(IsDistributor: true));
        Assert.Equal(2, await (await asDistributor.GetIQueryable(null, null, true, true)).CountAsync());
    }

    [Fact]
    public async Task The_current_labor_rate_is_the_latest_row_that_has_started()
    {
        await using var host = await Host.CreateAsync();
        var rates = host.Db.Set<Entities.WarrantyLaborRate>();

        Assert.Equal(20, (await WarrantyLaborRateRepository.CurrentAsync(rates, "USD", new DateTime(2026, 6, 1)))!.LaborRate);
        Assert.Equal(99, (await WarrantyLaborRateRepository.CurrentAsync(rates, "USD", new DateTime(2999, 1, 1)))!.LaborRate);
        Assert.Null(await WarrantyLaborRateRepository.CurrentAsync(rates, "USD", new DateTime(2019, 12, 31)));
        Assert.Null(await WarrantyLaborRateRepository.CurrentAsync(rates, "XTS", new DateTime(2026, 6, 1)));
    }

    private record Capability(bool IsDistributor) : IWarrantyClaimsCapabilityProvider;

    private sealed class Host : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        private readonly AsyncServiceScope scope;

        private Host(ServiceProvider provider)
        {
            this.provider = provider;
            this.scope = provider.CreateAsyncScope();
        }

        public ShiftDbContext Db => this.scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        public WarrantyClaimRepository Claims(bool isDistributor) =>
            new(this.Db, new WarrantyClaimService(), new Capability(isDistributor), sharedClaimService: null!, deliveryDateService: null!);

        public Task<Entities.WarrantyClaim> ClaimAsync() =>
            this.Db.Set<Entities.WarrantyClaim>().Include(x => x.WarrantyClaimLaborLines).SingleAsync();

        public static async Task<Host> CreateAsync()
        {
            var services = ModuleMapper.Services();
            services.AddDbContext<ShiftDbContext, ClaimsDb>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
            services.AddSingleton<IModelBuildingContributor>(new WarrantyClaimsModelBuildingContributor(schema: null));
            services.AddScoped<ICurrentUserProvider, NoUser>();
            services.AddScoped<IdentityClaimProvider>();
            services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, UnusedAccess>());
            services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));

            var host = new Host(services.BuildServiceProvider());

            host.Db.Add(new Entities.WarrantyClaim
            {
                ClaimNumber = "1", DealerCode = "D", WarrantyType = "VE", Franchise = "F", RepairOrderNo = "RO", DataID = "D", Condition = "C", Cause = "C", Remedy = "R",
                VIN = "AAABBBBB000000001", VIN_WMI = "AAA", VIN_VDS = "BBBBB", VIN_CD = "0", VIN_VIS = "00000001",
                LaborRate = 20, LaborRateUSD = 20, LaborRateDistributor = 30,
                HourTotal = 1.5m, HourTotalDistributor = 1.5m, TotalClaimAmount = 30, TotalClaimAmountDistributor = 45,
                WarrantyClaimLaborLines = [new() { PayCode = "1", OperationNumber = "OP", Hour = 1.5m, DistributorHour = 1.5m }],
            });
            host.Db.Add(new Entities.WarrantyLaborRate { Currency = "USD", LaborRate = 20, LaborRateDistributor = 30, EffectiveDate = new DateTime(2020, 1, 1) });
            host.Db.Add(new Entities.WarrantyLaborRate { Currency = "USD", LaborRate = 99, LaborRateDistributor = 99, EffectiveDate = new DateTime(2998, 1, 1) });
            await host.Db.SaveChangesAsync();

            return host;
        }

        public async ValueTask DisposeAsync()
        {
            await this.scope.DisposeAsync();
            await this.provider.DisposeAsync();
        }
    }

    private class ClaimsDb(DbContextOptions<ClaimsDb> options) : ShiftDbContext(options);

    private class NoUser : ICurrentUserProvider { public ClaimsPrincipal? GetUser() => null; }

    private class UnusedAccess : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new NotSupportedException();
    }
}
