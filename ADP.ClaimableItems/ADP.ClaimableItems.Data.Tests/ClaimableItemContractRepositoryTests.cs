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
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared;
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
    private static readonly DateTime Today = new(2026, 6, 1);

    // ───── Save ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Saving_works_out_the_end_date_line_prices_and_totals()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);

        var dto = NewDto(priceIncrement: 0.05m, Periodic("P|1|10"), Standalone("S|1|7"));
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
    public async Task Saving_takes_the_vehicle_and_the_lines_from_the_menu_not_the_client()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);

        var line = Periodic("P|1|10");
        line.Cost = 1m;
        line.MenuCode = "FORGED";
        var dto = NewDto(0m, line);
        dto.Katashiki = "FORGED";
        dto.VIN = Vin.ToLowerInvariant();

        var contract = await InsertAsync(db, repository, dto);

        var saved = contract.Lines.Single();
        Assert.Equal(100m, saved.Cost);
        Assert.Equal("CON10K", saved.MenuCode);
        Assert.Equal(ClaimableItemContractLineType.Periodic, saved.LineType);
        Assert.Equal(10000, saved.ServiceIntervalValueInMeter);
        Assert.Equal("KAT-1", contract.Katashiki);
        Assert.Equal("Model 1", contract.ModelDescription);
        Assert.Equal(2, contract.BrandID);
        Assert.Equal(Vin, contract.VIN);
    }

    [Fact]
    public async Task The_line_type_comes_from_the_menu_so_a_disguised_periodic_repeat_is_refused()
    {
        await using var scope = NewScope();
        var (_, repository, _) = Resolve(scope);

        var disguised = Standalone("P|1|10");
        var dto = NewDto(0m, Periodic("P|1|10"), disguised);

        var error = await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(new ClaimableItemContract(), dto, ActionTypes.Insert, 7, null, true, false));
        Assert.Contains("only once", error.Message.Body);
    }

    [Fact]
    public async Task A_standalone_line_has_no_duration_of_its_own()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);

        var standalone = Standalone("S|1|7");
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
        var (db, repository, _) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10"), Periodic("P|1|20")));

        var dto = NewDto(0m, Periodic("P|1|30"));
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

    [Theory]
    [InlineData("P|9|99", "is not offered for this vehicle any more")]
    [InlineData("P|1|5", "which the vehicle has already reached")]
    [InlineData("S|1|8", "has parts without a price")]
    public async Task A_line_the_menu_does_not_offer_is_refused(string key, string message)
    {
        await using var scope = NewScope();
        var (_, repository, _) = Resolve(scope);

        var dto = NewDto(0m, key.StartsWith('P') ? Periodic(key) : Standalone(key));
        dto.Mileage = 6000;

        var error = await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(new ClaimableItemContract(), dto, ActionTypes.Insert, 7, null, true, false));
        Assert.Contains(message, error.Message.Body);
    }

    [Fact]
    public async Task A_periodic_menu_on_another_live_contract_is_refused()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);
        await AddOtherContractAsync(db, "P|1|10");

        var error = await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(new ClaimableItemContract(), NewDto(0m, Periodic("P|1|10")), ActionTypes.Insert, 7, null, true, false));
        Assert.Contains("already on another contract", error.Message.Body);
    }

    [Fact]
    public async Task The_same_periodic_menu_on_another_vins_contract_does_not_block()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);
        await AddOtherContractAsync(db, "P|1|10", vin: "JTMHX01J8L4198293");

        await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10")));
    }

    [Fact]
    public async Task A_standalone_menu_may_be_on_several_contracts()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);
        await AddOtherContractAsync(db, "S|1|7", ClaimableItemContractLineType.Standalone);

        await InsertAsync(db, repository, NewDto(0m, Standalone("S|1|7")));
    }

    [Fact]
    public async Task An_unofficial_vehicle_is_refused()
    {
        await using var scope = NewScope();
        var (_, repository, provider) = Resolve(scope);
        provider.IsOfficial = false;

        var error = await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(new ClaimableItemContract(), NewDto(0m, Periodic("P|1|10")), ActionTypes.Insert, 7, null, true, false));
        Assert.Contains("not one of the official vehicles", error.Message.Body);
    }

    [Fact]
    public async Task With_one_country_the_server_sets_the_country_and_a_transfer_rate_of_1()
    {
        await using var scope = NewScope();
        var (db, repository, provider) = Resolve(scope);
        provider.Countries = [new() { ID = 4, Name = "Only" }];

        var dto = NewDto(0m, Periodic("P|1|10"));
        dto.TransferRate = 3m;
        var contract = await InsertAsync(db, repository, dto);

        Assert.Equal(4, contract.CountryID);
        Assert.Equal(1m, contract.TransferRate);
        Assert.Equal((4L, 1m), provider.LastCall);
    }

    [Fact]
    public async Task An_issued_contract_cannot_be_changed_or_deleted()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10")));
        contract.IsDraft = false;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(contract, NewDto(0m, Periodic("P|1|10")), ActionTypes.Update, 7, null, true, false));
        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.DeleteAsync(contract, 7, true, false));
    }

    [Fact]
    public async Task A_cancelled_contract_cannot_be_changed()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10")));
        contract.IsCancelled = true;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(contract, NewDto(0m, Periodic("P|1|10")), ActionTypes.Update, 7, null, true, false));
    }

    [Fact]
    public async Task A_contract_cannot_be_issued_by_a_save_yet()
    {
        await using var scope = NewScope();
        var (_, repository, _) = Resolve(scope);

        var dto = NewDto(0m, Periodic("P|1|10"));
        dto.IsDraft = false;

        await Assert.ThrowsAsync<ShiftEntityException>(async () =>
            await repository.UpsertAsync(new ClaimableItemContract(), dto, ActionTypes.Insert, 7, null, true, false));
    }

    [Fact]
    public async Task A_draft_can_be_deleted()
    {
        await using var scope = NewScope();
        var (db, repository, _) = Resolve(scope);
        var contract = await InsertAsync(db, repository, NewDto(0m, Periodic("P|1|10")));

        await repository.DeleteAsync(contract, 7, true, false);
    }

    // ───── Lookup ───────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task With_no_country_configured_the_lookups_default_country_applies()
    {
        await using var scope = NewScope();
        var (service, provider) = ResolveService(scope);

        var lookup = await service.LookupAsync(Vin, null, countryId: 9, transferRate: 3m, null, null, Today);

        Assert.True(lookup.CountryIsFixed);
        Assert.Null(lookup.Country);
        Assert.Equal(1m, lookup.TransferRate);
        Assert.Equal(((long?)null, 1m), provider.LastCall);
    }

    [Fact]
    public async Task With_several_countries_the_user_picks_the_country_and_the_rate()
    {
        await using var scope = NewScope();
        var (service, provider) = ResolveService(scope);
        provider.Countries = [new() { ID = 4, Name = "A" }, new() { ID = 5, Name = "B" }];

        var unchosen = await service.LookupAsync(Vin, null, null, null, null, null, Today);
        Assert.Equal(ClaimableItemContractLookupStatus.CountryRequired, unchosen.Status);
        Assert.False(unchosen.CountryIsFixed);
        Assert.Equal(2, unchosen.Countries.Count);
        Assert.Empty(unchosen.Lines);
        Assert.Null(provider.LastCall);

        var chosen = await service.LookupAsync(Vin, null, 5, 1.2m, null, null, Today);
        Assert.Equal(ClaimableItemContractLookupStatus.Found, chosen.Status);
        Assert.Equal("5", chosen.Country!.Value);
        Assert.Equal((5L, 1.2m), provider.LastCall);
    }

    [Fact]
    public async Task An_unknown_country_is_not_accepted()
    {
        await using var scope = NewScope();
        var (service, provider) = ResolveService(scope);
        provider.Countries = [new() { ID = 4, Name = "A" }, new() { ID = 5, Name = "B" }];

        var lookup = await service.LookupAsync(Vin, null, 6, null, null, null, Today);

        Assert.Equal(ClaimableItemContractLookupStatus.CountryRequired, lookup.Status);
    }

    [Theory]
    [InlineData(ClaimableItemContractVehicleMenuStatus.NotFound, ClaimableItemContractLookupStatus.MenuNotFound)]
    [InlineData(ClaimableItemContractVehicleMenuStatus.Unavailable, ClaimableItemContractLookupStatus.MenuUnavailable)]
    public async Task A_vehicle_without_a_readable_menu_has_no_lines(ClaimableItemContractVehicleMenuStatus menuStatus, ClaimableItemContractLookupStatus expected)
    {
        await using var scope = NewScope();
        var (service, provider) = ResolveService(scope);
        provider.MenuStatus = menuStatus;

        var lookup = await service.LookupAsync(Vin, null, null, null, null, null, Today);

        Assert.Equal(expected, lookup.Status);
        Assert.Empty(lookup.Lines);
    }

    [Fact]
    public async Task The_lookup_marks_each_line_that_cannot_go_on_a_contract()
    {
        await using var scope = NewScope();
        var (service, _) = ResolveService(scope);
        await AddOtherContractAsync(Db(scope), "P|1|20");

        var lookup = await service.LookupAsync(Vin, mileage: 6000, null, null, null, null, Today);
        var reasons = lookup.Lines.ToDictionary(x => x.MenuLineKey, x => x.UnavailableReason);

        Assert.Equal(ClaimableItemContractMenuLineUnavailableReason.AtOrBelowMileage, reasons["P|1|5"]);
        Assert.Null(reasons["P|1|10"]);
        Assert.Equal(ClaimableItemContractMenuLineUnavailableReason.OnAnotherContract, reasons["P|1|20"]);
        Assert.Null(reasons["S|1|7"]);
        Assert.Equal(ClaimableItemContractMenuLineUnavailableReason.UnpricedParts, reasons["S|1|8"]);
    }

    [Fact]
    public async Task The_mileage_margin_widens_what_counts_as_already_passed()
    {
        await using var scope = NewScope(mileageMargin: 1000);
        var (service, _) = ResolveService(scope);

        var lookup = await service.LookupAsync(Vin, mileage: 9000, null, null, null, null, Today);

        Assert.Equal(ClaimableItemContractMenuLineUnavailableReason.AtOrBelowMileage, lookup.Lines.Single(x => x.MenuLineKey == "P|1|10").UnavailableReason);
    }

    [Fact]
    public async Task The_form_is_not_offered_lines_it_could_never_pick()
    {
        await using var scope = NewScope();
        var (service, _) = ResolveService(scope);
        await AddOtherContractAsync(Db(scope), "P|1|20");

        var lookup = ClaimableItemContractMenuService.ForForm(await service.LookupAsync(Vin, 6000, null, null, null, null, Today));

        Assert.Equal(["P|1|10", "P|1|20", "P|1|30", "S|1|7"], lookup.Lines.Select(x => x.MenuLineKey).Order());
    }

    [Theory]
    [InlineData(true, false, false, null, true)]             // issued and live: held
    [InlineData(false, false, false, null, false)]           // a draft holds nothing
    [InlineData(true, true, false, null, false)]             // cancelled contract
    [InlineData(true, false, true, null, false)]             // cancelled line
    [InlineData(true, false, false, "2026-05-31", false)]    // expired before today
    [InlineData(true, false, false, "2026-06-01", true)]     // expires today: still held
    public async Task Only_a_live_line_on_an_issued_contract_holds_its_periodic_menu(
        bool issued, bool contractCancelled, bool lineCancelled, string? expireDate, bool held)
    {
        await using var scope = NewScope();
        var (service, _) = ResolveService(scope);
        await AddOtherContractAsync(Db(scope), "P|1|10", issued: issued, contractCancelled: contractCancelled, lineCancelled: lineCancelled,
            expireDate: expireDate is null ? null : DateTime.Parse(expireDate));

        var taken = await service.GetTakenPeriodicMenuLineKeysAsync(Vin, null, Today);

        Assert.Equal(held, taken.Contains("P|1|10"));
    }

    // ───── Map and validator ────────────────────────────────────────────────────────────────────

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

    [Fact]
    public void A_periodic_menu_may_appear_once_but_a_standalone_may_repeat()
    {
        var validator = new ClaimableItemContractValidator(new KeyLocalizer());

        Assert.False(validator.Validate(NewDto(0m, Periodic("P|1|10"), Periodic("P|1|10"))).IsValid);
        Assert.True(validator.Validate(NewDto(0m, Standalone("S|1|7"), Standalone("S|1|7"))).IsValid);
    }

    [Fact]
    public void A_periodic_line_needs_a_duration()
    {
        var validator = new ClaimableItemContractValidator(new KeyLocalizer());
        var line = Periodic("P|1|10");
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

        Assert.Equal(valid, validator.Validate(NewDto((decimal)increment, Standalone("S|1|7"))).IsValid);
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

    private static ClaimableItemContractLineDTO Periodic(string key) => new()
    {
        MenuLineKey = key,
        LineType = ClaimableItemContractLineType.Periodic,
        Duration = 6,
        DurationUnit = ClaimableItemContractDurationUnit.Months,
    };

    private static ClaimableItemContractLineDTO Standalone(string key) => new()
    {
        MenuLineKey = key,
        LineType = ClaimableItemContractLineType.Standalone,
    };

    private static async Task<ClaimableItemContract> InsertAsync(ShiftDbContext db, ClaimableItemContractRepository repository, ClaimableItemContractDTO dto)
    {
        var contract = await repository.UpsertAsync(new ClaimableItemContract(), dto, ActionTypes.Insert, 7, null, disableDefaultDataLevelAccess: true, disableGlobalFilters: false);
        db.Add(contract);
        await db.SaveChangesAsync();
        return contract;
    }

    /// <summary>Another contract, for the same VIN unless one is given, written straight to the database.</summary>
    private static async Task AddOtherContractAsync(
        ShiftDbContext db,
        string key,
        ClaimableItemContractLineType lineType = ClaimableItemContractLineType.Periodic,
        bool issued = true,
        bool contractCancelled = false,
        bool lineCancelled = false,
        DateTime? expireDate = null,
        string vin = Vin)
    {
        db.Add(new ClaimableItemContract
        {
            VIN = vin,
            CustomerName = "Earlier customer",
            IsDraft = !issued,
            IsCancelled = contractCancelled,
            Lines = { new ClaimableItemContractLine { MenuLineKey = key, LineType = lineType, IsCancelled = lineCancelled, ExpireDate = expireDate } },
        });
        await db.SaveChangesAsync();
    }

    private static AsyncServiceScope NewScope(long mileageMargin = 0)
    {
        var services = ModuleMapper.Services();
        services.AddDbContext<ShiftDbContext, ContractDb>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));
        services.AddScoped<ICurrentUserProvider, NoUser>();
        services.AddScoped<IdentityClaimProvider>();
        services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, UnusedAccess>());
        services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
        services.AddSingleton(new ClaimableItemsDataOptions { ContractPeriodicMileageMargin = mileageMargin });
        services.AddScoped<FakeMenuProvider>();
        services.AddScoped<IClaimableItemContractMenuProvider>(sp => sp.GetRequiredService<FakeMenuProvider>());
        services.AddScoped<ClaimableItemContractMenuService>();
        return services.BuildServiceProvider().CreateAsyncScope();
    }

    private static ShiftDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

    private static (ShiftDbContext Db, ClaimableItemContractRepository Repository, FakeMenuProvider Provider) Resolve(AsyncServiceScope scope)
    {
        var db = Db(scope);
        var service = scope.ServiceProvider.GetRequiredService<ClaimableItemContractMenuService>();
        return (db, new ClaimableItemContractRepository(db, service), scope.ServiceProvider.GetRequiredService<FakeMenuProvider>());
    }

    private static (ClaimableItemContractMenuService Service, FakeMenuProvider Provider) ResolveService(AsyncServiceScope scope) =>
        (scope.ServiceProvider.GetRequiredService<ClaimableItemContractMenuService>(), scope.ServiceProvider.GetRequiredService<FakeMenuProvider>());

    /// <summary>
    /// One variant's contract menu: periodic services at 5,000 / 10,000 / 20,000 / 30,000 km, a standalone item,
    /// and a standalone item with an unpriced part.
    /// </summary>
    private class FakeMenuProvider : IClaimableItemContractMenuProvider
    {
        public List<ClaimableItemContractCountry> Countries { get; set; } = [];
        public bool IsOfficial { get; set; } = true;
        public ClaimableItemContractVehicleMenuStatus MenuStatus { get; set; } = ClaimableItemContractVehicleMenuStatus.Found;
        public (long? CountryId, decimal TransferRate)? LastCall { get; private set; }

        public ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync() =>
            ValueTask.FromResult<IReadOnlyList<ClaimableItemContractCountry>>(Countries);

        public Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language)
        {
            LastCall = (countryId, transferRate);

            return Task.FromResult(new ClaimableItemContractVehicleMenu
            {
                IsOfficial = IsOfficial,
                Katashiki = "KAT-1",
                BasicModelCode = "KAT",
                ModelDescription = "Model 1",
                BrandID = 2,
                MenuStatus = MenuStatus,
                Lines = MenuStatus == ClaimableItemContractVehicleMenuStatus.Found
                    ?
                    [
                        PeriodicLine("P|1|5", 5000, 60m),
                        PeriodicLine("P|1|10", 10000, 100m),
                        PeriodicLine("P|1|20", 20000, 120m),
                        PeriodicLine("P|1|30", 30000, 150m),
                        new() { MenuLineKey = "S|1|7", MenuCode = "WIPER", LineType = ClaimableItemContractLineType.Standalone, Price = 40m },
                        new() { MenuLineKey = "S|1|8", MenuCode = "BATTERY", LineType = ClaimableItemContractLineType.Standalone, Price = 10m, HasUnpricedParts = true },
                    ]
                    : [],
            });
        }

        private static ClaimableItemContractMenuLineDTO PeriodicLine(string key, int km, decimal price) => new()
        {
            MenuLineKey = key,
            MenuCode = $"CON{km / 1000}K",
            LineType = ClaimableItemContractLineType.Periodic,
            ServiceIntervalValueInMeter = km,
            Price = price,
        };
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
