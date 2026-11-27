using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.Cases.Shared.DTOs;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;
using ShiftSoftware.ADP.ClaimableItems.Web;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

/// <summary>What a save may move on an item claim, and the history it leaves.</summary>
public class ItemClaimHistoryTests
{
    [Fact]
    public async Task A_claims_life_is_recorded_whichever_repository_saves_it()
    {
        await using var host = await Host.CreateAsync();
        var id = await host.CreateAsync(ClaimStatus.PendingProcess);

        await host.ActAsync(id, UpdateStatusActionTypes.DistributorError, isDistributor: true, "no invoice");
        await host.SaveAsync(id, x => x.ReSubmitForDistributorReview = true, isDistributor: false);
        await host.NoteAsync(id, isDistributor: false, "invoice attached");
        await host.ActAsync(id, UpdateStatusActionTypes.DistributorAccepted, isDistributor: true);
        await host.CertifyAsync(id);

        var history = (await host.ViewAsync(id, isDistributor: false)).StatusHistory;

        Assert.Equal(
            [
                (ClaimStatus.Accepted, ClaimStatus.Certified, null, CaseRoles.Distributor),
                (ClaimStatus.PendingProcess, ClaimStatus.Accepted, null, CaseRoles.Distributor),
                (ClaimStatus.PendingProcess, ClaimStatus.PendingProcess, "invoice attached", CaseRoles.Dealer),
                (ClaimStatus.RejectedWithError, ClaimStatus.PendingProcess, null, CaseRoles.Dealer),
                (ClaimStatus.PendingProcess, ClaimStatus.RejectedWithError, "no invoice", CaseRoles.Distributor),
                (null, ClaimStatus.PendingProcess, null, CaseRoles.Dealer),
            ],
            history.Select(x => (x.FromStatus, x.ToStatus, x.Comment, x.ActorRole!)));

        Assert.All(history, x => Assert.Equal("Pat", x.ActorUserName));
    }

    [Theory]
    [InlineData(null, ClaimStatus.Draft)]
    [InlineData(ClaimStatus.Accepted, ClaimStatus.Draft)]
    [InlineData(ClaimStatus.Invoiced, ClaimStatus.Draft)]
    [InlineData(ClaimStatus.PendingProcess, ClaimStatus.PendingProcess)]
    public async Task A_new_claim_opens_as_a_draft_or_as_submitted(ClaimStatus? requested, ClaimStatus opened)
    {
        await using var host = await Host.CreateAsync();

        Assert.Equal(opened, (await host.ViewAsync(await host.CreateAsync(requested), isDistributor: false)).ClaimStatus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_save_moves_nothing_and_its_history_is_ignored(bool isDistributor)
    {
        await using var host = await Host.CreateAsync();
        var id = await host.CreateAsync(ClaimStatus.PendingProcess);
        await host.ActAsync(id, UpdateStatusActionTypes.DistributorRejected, isDistributor: true);
        var before = await host.ViewAsync(id, isDistributor);

        await host.SaveAsync(id, x =>
        {
            x.JobNumber = "edited";
            x.ClaimStatus = ClaimStatus.Accepted;
            x.DistributorErrorMessage = "fine";
            x.ReSubmitForDistributorReview = true;
            x.StatusHistory = [new CaseStatusHistoryDTO<ClaimStatus> { ToStatus = ClaimStatus.Invoiced }];
        }, isDistributor);

        var after = await host.ViewAsync(id, isDistributor);
        Assert.Equal("edited", after.JobNumber);
        Assert.Equal((ClaimStatus.RejectedPermanently, null), (after.ClaimStatus, after.DistributorErrorMessage));
        Assert.Equal(JsonSerializer.Serialize(before.StatusHistory), JsonSerializer.Serialize(after.StatusHistory));
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        private long campaignId;
        private long itemId;

        private Host(ServiceProvider provider) => this.provider = provider;

        public static async Task<Host> CreateAsync()
        {
            var name = Guid.NewGuid().ToString();
            var services = ModuleMapper.Services();
            services.AddDbContext<ShiftDbContext, ClaimsDb>(o => o.UseInMemoryDatabase(name));
            services.AddItemClaimHistory<ClaimsDb>();
            services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
            services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));
            services.AddSingleton<ICurrentUserProvider, Pat>();
            services.AddScoped<IdentityClaimProvider>();
            services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, EveryRow>());
            services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
            services.AddScoped<Side>();
            services.AddScoped<IClaimableItemsCapabilityProvider>(x => x.GetRequiredService<Side>());
            services.AddSingleton<SharedClaimService>();

            var host = new Host(services.BuildServiceProvider());

            using var scope = host.As(isDistributor: true);
            var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();
            var campaign = new Campaign { Name = "{}" };
            var item = new ClaimableItem { Name = "{}", Costs = "[]", Campaign = campaign };
            db.AddRange(campaign, item);
            await db.SaveChangesAsync();
            (host.campaignId, host.itemId) = (campaign.ID, item.ID);

            return host;
        }

        private IServiceScope As(bool isDistributor)
        {
            var scope = this.provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<Side>().IsDistributor = isDistributor;
            return scope;
        }

        public async Task<long> CreateAsync(ClaimStatus? status)
        {
            using var scope = this.As(isDistributor: false);
            var claims = scope.ServiceProvider.GetRequiredService<ItemClaimRepository>();

            var claim = await claims.UpsertAsync(
                new(),
                new ItemClaimDTO
                {
                    VIN = "VIN00000000000001", ClaimDate = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero), ClaimStatus = status,
                    Campaign = new() { Value = this.campaignId.ToString() }, ClaimableItem = new() { Value = this.itemId.ToString() },
                },
                ActionTypes.Insert, null, null, true, true);
            claims.Add(claim);
            await claims.SaveChangesAsync();

            return claim.ID;
        }

        public async Task SaveAsync(long id, Action<ItemClaimDTO> edit, bool isDistributor)
        {
            using var scope = this.As(isDistributor);
            var claims = scope.ServiceProvider.GetRequiredService<ItemClaimRepository>();
            var claim = (await claims.FindAsync(id, null, true, true))!;

            var dto = await claims.ViewAsync(claim);
            edit(dto);
            await claims.UpsertAsync(claim, dto, ActionTypes.Update, null, null, true, true);
            await claims.SaveChangesAsync();
        }

        public async Task ActAsync(long id, UpdateStatusActionTypes action, bool isDistributor, string? text = null)
        {
            using var scope = this.As(isDistributor);
            await scope.ServiceProvider.GetRequiredService<ItemClaimRepository>().ApplyActionAsync(id, action, text);
        }

        public async Task NoteAsync(long id, bool isDistributor, string text)
        {
            using var scope = this.As(isDistributor);
            await scope.ServiceProvider.GetRequiredService<ItemClaimRepository>().AddNoteAsync(id, text);
        }

        public async Task CertifyAsync(long id)
        {
            using var scope = this.As(isDistributor: true);
            var certificates = scope.ServiceProvider.GetRequiredService<ItemClaimCertificateRepository>();

            certificates.Add(await certificates.UpsertAsync(
                new Certificate(),
                new ItemClaimCertificateDTO
                {
                    CertificateDate = new DateTime(2026, 6, 1), PeriodStartDate = new DateTime(2026, 5, 1), PeriodEndDate = new DateTime(2026, 5, 31),
                    ReimbursementItemClaims = [new() { ID = id.ToString() }],
                },
                ActionTypes.Insert, null, null, true, true));
            await certificates.SaveChangesAsync();
        }

        public async Task<ItemClaimDTO> ViewAsync(long id, bool isDistributor)
        {
            using var scope = this.As(isDistributor);
            var claims = scope.ServiceProvider.GetRequiredService<ItemClaimRepository>();

            return await claims.ViewAsync((await claims.FindAsync(id, null, true, true))!);
        }

        public ValueTask DisposeAsync() => this.provider.DisposeAsync();
    }

    private class Side : IClaimableItemsCapabilityProvider { public bool IsDistributor { get; set; } }

    private class ClaimsDb(DbContextOptions<ClaimsDb> options) : ShiftDbContext(options);

    private class Pat : ICurrentUserProvider
    {
        public ClaimsPrincipal? GetUser() => new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "7"), new(ClaimTypes.GivenName, "Pat")], "test"));
    }

    private class EveryRow : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.ReturnType == typeof(bool) ? true : args!.Single(method.ReturnType.IsInstanceOfType);
    }
}
