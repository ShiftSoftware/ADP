using System.Reflection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.ClaimableItems.Shared.Localization;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

public class ClaimableItemContractRepositoryTests
{
    private const string Vin = "JTMHX01J6M4217945";

    [Fact]
    public async Task Saving_works_out_the_end_date_line_prices_and_totals()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);

        var dto = NewDto(priceIncrement: 0.05m,
            Periodic("P|1|10", cost: 100m, months: 6),
            Standalone("S|1|7", cost: 40m));
        var contract = await InsertAsync(db, repository, dto);

        Assert.True(contract.IsDraft);
        Assert.Equal(new DateTime(2027, 1, 1), contract.EndDate);
        Assert.Equal(105m, contract.Lines.Single(x => x.MenuLineKey == "P|1|10").PriceWithIncrement);
        Assert.Equal(42m, contract.Lines.Single(x => x.MenuLineKey == "S|1|7").PriceWithIncrement);
        Assert.Equal(140m, contract.Total);
        Assert.Equal(147m, contract.TotalWithIncrement);
        Assert.Equal([0, 1], contract.Lines.OrderBy(x => x.Sort).Select(x => x.Sort));
    }

    [Fact]
    public async Task A_standalone_line_has_no_duration_of_its_own()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);

        var standalone = Standalone("S|1|7", cost: 40m);
        standalone.Duration = 3;
        standalone.DurationUnit = ClaimableItemContractDurationUnit.Months;
        var contract = await InsertAsync(db, repository, NewDto(0m, standalone));

        var line = contract.Lines.Single();
        Assert.Null(line.Duration);
        Assert.Null(line.DurationUnit);
    }

    [Fact]
    public async Task Saving_a_draft_replaces_its_lines_and_ignores_server_owned_members()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10", 100m, 6), Periodic("P|1|20", 120m, 6)));

        var dto = NewDto(0m, Periodic("P|1|30", 150m, 12));
        dto.IsCancelled = true;
        dto.CancelReason = "client says so";
        dto.Lines[0].ExpireDate = new DateTime(2030, 1, 1);
        dto.Lines[0].IsCancelled = true;

        var loaded = await db.Set<ClaimableItemContract>().Include(x => x.Lines).SingleAsync(x => x.ID == contract.ID);
        await repository.UpsertAsync(loaded, dto, ActionTypes.Update, 7, null, disableDefaultDataLevelAccess: true, disableGlobalFilters: false);
        await db.SaveChangesAsync();

        var saved = await db.Set<ClaimableItemContract>().Include(x => x.Lines).SingleAsync(x => x.ID == contract.ID);
        var line = Assert.Single(saved.Lines);
        Assert.Equal("P|1|30", line.MenuLineKey);
        Assert.Null(line.ExpireDate);
        Assert.False(line.IsCancelled);
        Assert.False(saved.IsCancelled);
        Assert.Null(saved.CancelReason);
        Assert.Equal(1, await db.Set<ClaimableItemContractLine>().CountAsync());
    }

    [Fact]
    public async Task An_issued_contract_cannot_be_changed_or_deleted()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10", 100m, 6)));
        contract.IsDraft = false;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(contract, NewDto(0m, Periodic("P|1|10", 100m, 6)), ActionTypes.Update, 7, null, true, false));
        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.DeleteAsync(contract, 7, true, false));
    }

    [Fact]
    public async Task A_cancelled_contract_cannot_be_changed()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10", 100m, 6)));
        contract.IsCancelled = true;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(contract, NewDto(0m, Periodic("P|1|10", 100m, 6)), ActionTypes.Update, 7, null, true, false));
    }

    [Fact]
    public async Task A_contract_cannot_be_issued_by_a_save_yet()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);

        var dto = NewDto(0m, Periodic("P|1|10", 100m, 6));
        dto.IsDraft = false;

        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(new ClaimableItemContract(), dto, ActionTypes.Insert, 7, null, true, false));
    }

    [Fact]
    public async Task A_draft_can_be_deleted()
    {
        await using var scope = NewScope();
        var (db, repository) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10", 100m, 6)));

        await repository.DeleteAsync(contract, 7, true, false);
    }

    [Fact]
    public void The_view_carries_the_lines_and_the_draft_flag()
    {
        var mapper = ModuleMapper.For<ClaimableItemContract, ClaimableItemContractListDTO, ClaimableItemContractDTO>();
        var stored = new ClaimableItemContract
        {
            VIN = Vin,
            CustomerName = "Customer",
            IsDraft = false,
            Lines = { new ClaimableItemContractLine { MenuLineKey = "P|1|10", LineType = ClaimableItemContractLineType.Periodic, Cost = 10m } },
        };

        var view = mapper.MapToView(stored);

        Assert.False(view.IsDraft);
        Assert.Equal(ClaimableItemContractStatus.Issued, view.Status);
        Assert.Equal("P|1|10", Assert.Single(view.Lines).MenuLineKey);
    }

    // ───── Validator ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_periodic_menu_may_appear_once_but_a_standalone_may_repeat()
    {
        var validator = new ClaimableItemContractValidator(new KeyLocalizer());

        Assert.False(validator.Validate(NewDto(0m, Periodic("P|1|10", 1m, 6), Periodic("P|1|10", 1m, 6))).IsValid);
        Assert.True(validator.Validate(NewDto(0m, Standalone("S|1|7", 1m), Standalone("S|1|7", 1m))).IsValid);
    }

    [Fact]
    public void A_periodic_line_needs_a_duration()
    {
        var validator = new ClaimableItemContractValidator(new KeyLocalizer());
        var line = Periodic("P|1|10", 1m, 6);
        line.Duration = null;

        Assert.False(validator.Validate(NewDto(0m, line)).IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0.10, true)]
    [InlineData(0.11, false)]
    [InlineData(-0.01, false)]
    public void The_price_increment_is_between_0_and_10_percent(double increment, bool valid)
    {
        var validator = new ClaimableItemContractValidator(new KeyLocalizer());

        Assert.Equal(valid, validator.Validate(NewDto((decimal)increment, Standalone("S|1|7", 1m))).IsValid);
    }

    [Fact]
    public void A_contract_needs_at_least_one_line()
    {
        var validator = new ClaimableItemContractValidator(new KeyLocalizer());

        Assert.False(validator.Validate(NewDto(0m)).IsValid);
    }

    // ───── Helpers ─────────────────────────────────────────────────────────────────────────────

    private static ClaimableItemContractDTO NewDto(decimal priceIncrement, params ClaimableItemContractLineDTO[] lines) => new()
    {
        VIN = Vin,
        CustomerName = "Customer",
        ContractType = ClaimableItemContractType.PurchasedServiceContract,
        Mileage = 5000,
        StartDate = new DateTime(2026, 1, 1),
        Duration = 1,
        DurationUnit = ClaimableItemContractDurationUnit.Years,
        PriceIncrement = priceIncrement,
        Lines = lines.ToList(),
    };

    private static ClaimableItemContractLineDTO Periodic(string key, decimal cost, int months) => new()
    {
        MenuLineKey = key,
        LineType = ClaimableItemContractLineType.Periodic,
        Cost = cost,
        Duration = months,
        DurationUnit = ClaimableItemContractDurationUnit.Months,
    };

    private static ClaimableItemContractLineDTO Standalone(string key, decimal cost) => new()
    {
        MenuLineKey = key,
        LineType = ClaimableItemContractLineType.Standalone,
        Cost = cost,
    };

    private static async Task<ClaimableItemContract> InsertAsync(ShiftDbContext db, ClaimableItemContractRepository repository, ClaimableItemContractDTO dto)
    {
        var contract = await repository.UpsertAsync(new ClaimableItemContract(), dto, ActionTypes.Insert, 7, null, disableDefaultDataLevelAccess: true, disableGlobalFilters: false);
        db.Add(contract);
        await db.SaveChangesAsync();
        return contract;
    }

    private static AsyncServiceScope NewScope()
    {
        var services = ModuleMapper.Services();
        services.AddDbContext<ShiftDbContext, ContractDb>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));
        services.AddScoped<ICurrentUserProvider, NoUser>();
        services.AddScoped<IdentityClaimProvider>();
        services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, UnusedAccess>());
        services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }

    private static (ShiftDbContext Db, ClaimableItemContractRepository Repository) Resolve(AsyncServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();
        return (db, new ClaimableItemContractRepository(db));
    }

    private class ContractDb(DbContextOptions<ContractDb> options) : ShiftDbContext(options);
    private class NoUser : ICurrentUserProvider { public ClaimsPrincipal? GetUser() => null; }
    private class UnusedAccess : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new NotSupportedException();
    }

    private class KeyLocalizer : IStringLocalizer<ClaimableItemsResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
