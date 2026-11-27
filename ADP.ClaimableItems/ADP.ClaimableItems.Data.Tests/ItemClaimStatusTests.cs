using System.Reflection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Web;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

/// <summary>Who may change the status of item claims through <see cref="ItemClaimRepository.UpdateClaimStatusAsync"/>.</summary>
public class ItemClaimStatusTests
{
    private static readonly ReadWriteDeleteAction Certifying = new("Certifying");

    [Fact]
    public async Task A_dealer_submits_a_claim_and_the_distributor_accepts_it()
    {
        var isDistributor = false;
        await using var services = Services(x => x.AddTransient<IClaimableItemsCapabilityProvider>(_ => new Capability(isDistributor)));
        await using var scope = services.CreateAsyncScope();
        var (_, repository, claim) = await ArrangeAsync(scope);
        claim.ClaimStatus = ClaimStatus.Draft;

        await repository.UpdateClaimStatusAsync([claim], UpdateStatusActionTypes.SubmitToDistributor, null);
        Assert.Equal(ClaimStatus.PendingProcess, claim.ClaimStatus);

        isDistributor = true;
        await repository.UpdateClaimStatusAsync([claim], UpdateStatusActionTypes.DistributorAccepted, null);
        Assert.Equal(ClaimStatus.Accepted, claim.ClaimStatus);
    }

    [Theory]
    [MemberData(nameof(DistributorActions))]
    public async Task A_dealer_is_refused_every_other_status_action_and_nothing_changes(UpdateStatusActionTypes action)
    {
        await using var services = Services(x => x.AddSingleton<IClaimableItemsCapabilityProvider>(new Capability(IsDistributor: false)));
        await using var scope = services.CreateAsyncScope();
        var (db, repository, claim) = await ArrangeAsync(scope);

        var refused = await Assert.ThrowsAsync<ShiftEntityException>(() => repository.UpdateClaimStatusAsync([claim], action, "note"));

        Assert.Equal(403, refused.HttpStatusCode);
        Assert.Equal(ClaimStatus.PendingProcess, claim.ClaimStatus);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    public static TheoryData<UpdateStatusActionTypes> DistributorActions =>
        [.. Enum.GetValues<UpdateStatusActionTypes>().Where(x => x != UpdateStatusActionTypes.SubmitToDistributor)];

    /// <summary>A host that registers no capability provider in its API.</summary>
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task Without_a_capability_provider_write_on_the_certifying_action_decides(bool actionConfigured, bool canWrite, bool allowed)
    {
        await using var services = Services(x =>
        {
            x.AddSingleton(new ClaimableItemsDataOptions { CertifyingAction = actionConfigured ? Certifying : null });
            x.AddSingleton(TypeAuth.Granting(canWrite));
        });
        await using var scope = services.CreateAsyncScope();
        var (_, repository, claim) = await ArrangeAsync(scope);

        var accept = () => repository.UpdateClaimStatusAsync([claim], UpdateStatusActionTypes.DistributorAccepted, null);

        if (allowed)
            await accept();
        else
            Assert.Equal(403, (await Assert.ThrowsAsync<ShiftEntityException>(accept)).HttpStatusCode);

        Assert.Equal(allowed ? ClaimStatus.Accepted : ClaimStatus.PendingProcess, claim.ClaimStatus);
    }

    private static async Task<(ShiftDbContext, ItemClaimRepository, ItemClaim)> ArrangeAsync(AsyncServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();
        var claim = new ItemClaim { VIN = "VIN00000000000001", ClaimStatus = ClaimStatus.PendingProcess };
        db.Add(claim);
        await db.SaveChangesAsync();

        return (db, scope.ServiceProvider.GetRequiredService<ItemClaimRepository>(), claim);
    }

    private static ServiceProvider Services(Action<IServiceCollection> host)
    {
        var services = ModuleMapper.Services();
        services.AddDbContext<ShiftDbContext, ClaimsDb>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));
        services.AddScoped<ICurrentUserProvider, NoUser>();
        services.AddScoped<IdentityClaimProvider>();
        services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, UnusedAccess>());
        services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
        services.AddSingleton<SharedClaimService>();
        host(services);

        return services.BuildServiceProvider();
    }

    private record Capability(bool IsDistributor) : IClaimableItemsCapabilityProvider;

    private class TypeAuth : DispatchProxy
    {
        private bool canWrite;

        public static ITypeAuthService Granting(bool canWrite)
        {
            var proxy = Create<ITypeAuthService, TypeAuth>();
            ((TypeAuth)(object)proxy).canWrite = canWrite;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method?.Name == nameof(ITypeAuthService.CanWrite) && ReferenceEquals(args![0], Certifying) ? this.canWrite : throw new NotSupportedException();
    }

    private class ClaimsDb(DbContextOptions<ClaimsDb> options) : ShiftDbContext(options);

    private class NoUser : ICurrentUserProvider { public ClaimsPrincipal? GetUser() => null; }

    private class UnusedAccess : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new NotSupportedException();
    }
}
