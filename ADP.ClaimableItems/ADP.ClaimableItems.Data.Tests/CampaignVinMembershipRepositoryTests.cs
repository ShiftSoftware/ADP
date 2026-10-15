using System.Security.Claims;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

public class CampaignVinMembershipRepositoryTests
{
    [Theory]
    [InlineData(ClaimableItemCampaignActivationTrigger.VehicleInspection)]
    [InlineData(ClaimableItemCampaignActivationTrigger.ManualVinEntry)]
    [InlineData(ClaimableItemCampaignActivationTrigger.WarrantyActivation)]
    public async Task Repository_accepts_membership_without_changing_the_campaign_trigger(ClaimableItemCampaignActivationTrigger trigger)
    {
        var services = ModuleMapper.Services();
        services.AddDbContext<ShiftDbContext, MembershipDb>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));
        services.AddScoped<ICurrentUserProvider, NoUser>();
        services.AddScoped<IdentityClaimProvider>();
        services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, UnusedAccess>());
        services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();
        db.Add(new Campaign { ID = 500, Name = "{}", ActivationTrigger = trigger, VehicleInspectionTypeID = 10 });
        await db.SaveChangesAsync();
        var repository = new CampaignVinEntryRepository(db);
        var entry = await repository.UpsertAsync(new CampaignVinEntry(), new CampaignVinEntryDTO
        {
            VIN = "JTDBR32E0X0000001", Campaign = new ShiftEntitySelectDTO { Value = "500" }, RecordedDate = new DateTime(2026, 10, 13),
        }, ActionTypes.Update, 7, null, disableDefaultDataLevelAccess: true, disableGlobalFilters: false);
        Assert.Equal(500, entry.CampaignID);
        Assert.Equal(trigger, (await db.Set<Campaign>().SingleAsync()).ActivationTrigger);
        Assert.Equal(10, (await db.Set<Campaign>().SingleAsync()).VehicleInspectionTypeID);
    }

    private class MembershipDb(DbContextOptions<MembershipDb> options) : ShiftDbContext(options);
    private class NoUser : ICurrentUserProvider { public ClaimsPrincipal? GetUser() => null; }
    private class UnusedAccess : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new NotSupportedException();
    }
}
