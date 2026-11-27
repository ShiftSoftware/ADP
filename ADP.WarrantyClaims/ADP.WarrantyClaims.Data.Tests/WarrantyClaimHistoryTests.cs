using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.DTOs;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>What a save may move on a claim, and the history it leaves.</summary>
public class WarrantyClaimHistoryTests
{
    [Fact]
    public async Task A_claims_life_is_recorded_whichever_repository_saves_it()
    {
        await using var host = new Host();
        var id = await host.CreateAsync(isDistributor: false);

        await host.ActAsync(id, UpdateStatusActionTypes.SubmitToDistributor, isDistributor: false);
        await host.ActAsync(id, UpdateStatusActionTypes.DistributorError, isDistributor: true, "wrong labor code");
        await host.ActAsync(id, UpdateStatusActionTypes.DistributorError, isDistributor: true, "still wrong");
        await host.ActAsync(id, UpdateStatusActionTypes.DistributorAccepted, isDistributor: true, " fine now ");
        await host.CertifyAsync(id);

        var history = (await host.ViewAsync(id, isDistributor: false)).StatusHistory;

        Assert.Equal(
            [
                (ClaimStatus.Accepted, ClaimStatus.Certified, null, CaseRoles.Distributor),
                (ClaimStatus.RejectedWithError, ClaimStatus.Accepted, "fine now", CaseRoles.Distributor),
                (ClaimStatus.RejectedWithError, ClaimStatus.RejectedWithError, "still wrong", CaseRoles.Distributor),
                (ClaimStatus.PendingProcess, ClaimStatus.RejectedWithError, "wrong labor code", CaseRoles.Distributor),
                (ClaimStatus.Draft, ClaimStatus.PendingProcess, null, CaseRoles.Dealer),
                (null, ClaimStatus.Draft, null, CaseRoles.Dealer),
            ],
            history.Select(x => (x.FromStatus, x.ToStatus, x.Comment, x.ActorRole!)));

        Assert.All(history, x => Assert.Equal("Pat", x.ActorUserName));
    }

    [Fact]
    public async Task The_manufacturers_side_is_recorded_for_the_distributor_only()
    {
        await using var host = new Host();
        var id = await host.CreateAsync(isDistributor: true);

        await host.ActAsync(id, UpdateStatusActionTypes.ManufacturerRejected, isDistributor: true);

        var line = (await host.ViewAsync(id, isDistributor: true)).StatusHistory[0];
        Assert.True(line.IsNote);
        Assert.Equal((ClaimStatus.PendingProcess, "Manufacturer: N/A → Rejected"), (line.ToStatus, line.Comment));

        Assert.Equal([ClaimStatus.PendingProcess], (await host.ViewAsync(id, isDistributor: false)).StatusHistory.Select(x => x.ToStatus));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_save_moves_nothing_and_its_history_is_ignored(bool isDistributor)
    {
        await using var host = new Host();
        var id = await host.CreateAsync(isDistributor: false);
        await host.ActAsync(id, UpdateStatusActionTypes.SubmitToDistributor, isDistributor: false);
        await host.ActAsync(id, UpdateStatusActionTypes.DistributorError, isDistributor: true, "wrong labor code");
        var before = await host.ViewAsync(id, isDistributor);

        var dto = await host.ViewAsync(id, isDistributor);
        dto.ClaimStatus = ClaimStatus.Accepted;
        dto.ManufacturerStatus = WarrantyManufacturerClaimStatus.Paid;
        dto.ProcessDate = new DateTime(2020, 1, 1);
        dto.DistributorErrorMessage = "fine";
        dto.InvoiceNo = "INV-1";
        dto.Remedy = "edited";
        dto.StatusHistory = [new CaseStatusHistoryDTO<ClaimStatus> { ToStatus = ClaimStatus.Invoiced }];
        await host.SaveAsync(id, dto, isDistributor);

        var after = await host.ViewAsync(id, isDistributor);
        Assert.Equal("edited", after.Remedy);
        Assert.Equal(
            (before.ClaimStatus, before.ManufacturerStatus, before.ProcessDate, before.DistributorErrorMessage),
            (after.ClaimStatus, after.ManufacturerStatus, after.ProcessDate, after.DistributorErrorMessage));
        Assert.Equal(isDistributor ? "INV-1" : null, after.InvoiceNo);
        Assert.Equal(JsonSerializer.Serialize(before.StatusHistory), JsonSerializer.Serialize(after.StatusHistory));
    }

    [Fact]
    public async Task A_copy_starts_its_own_history()
    {
        await using var host = new Host();
        var id = await host.CreateAsync(isDistributor: false);
        await host.ActAsync(id, UpdateStatusActionTypes.SubmitToDistributor, isDistributor: false);

        var copy = await host.ViewAsync(id, isDistributor: true);
        var copyId = await host.CreateAsync(isDistributor: true, copy);

        Assert.Null(Assert.Single((await host.ViewAsync(copyId, isDistributor: true)).StatusHistory).FromStatus);
    }

    [Fact]
    public async Task A_dealer_notes_only_on_a_claim_it_may_still_edit()
    {
        await using var host = new Host();
        var id = await host.CreateAsync(isDistributor: false);

        await host.NoteAsync(id, isDistributor: false, " waiting for the part ");
        var line = (await host.ViewAsync(id, isDistributor: false)).StatusHistory[0];
        Assert.Equal((true, ClaimStatus.Draft, "waiting for the part"), (line.IsNote, line.ToStatus, line.Comment));

        await host.ActAsync(id, UpdateStatusActionTypes.SubmitToDistributor, isDistributor: false);

        Assert.Equal(403, (await Assert.ThrowsAsync<ShiftEntityException>(() => host.NoteAsync(id, isDistributor: false, "late"))).HttpStatusCode);
        await host.NoteAsync(id, isDistributor: true, "received");
        await Assert.ThrowsAsync<ShiftEntityException>(() => host.NoteAsync(id, isDistributor: true, "  "));
    }

    [Fact]
    public async Task The_replicated_document_has_no_history()
    {
        await using var host = new Host();
        var id = await host.CreateAsync(isDistributor: false);

        using var scope = host.As(isDistributor: true);
        var claim = await scope.ServiceProvider.GetRequiredService<ShiftDbContext>().Set<WarrantyClaim>().Include(x => x.WarrantyClaimLaborLines).SingleAsync(x => x.ID == id);

        Assert.DoesNotContain("History", JsonSerializer.Serialize(WarrantyClaimsReplicationExtensions.ToWarrantyClaimModel(claim)));
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly ServiceProvider provider;

        public Host()
        {
            var name = Guid.NewGuid().ToString();
            var services = ModuleMapper.Services();
            services.AddDbContext<ShiftDbContext, ClaimsDb>(o => o.UseInMemoryDatabase(name));
            services.AddWarrantyClaimHistory<ClaimsDb>();
            services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
            services.AddSingleton<IModelBuildingContributor>(new WarrantyClaimsModelBuildingContributor(schema: null));
            services.AddSingleton<ICurrentUserProvider, Pat>();
            services.AddScoped<IdentityClaimProvider>();
            services.AddSingleton(DispatchProxy.Create<IDefaultDataLevelAccess, EveryRow>());
            services.AddSingleton<IHashIdService>(new HashIdService(Options.Create(new ShiftEntityOptions())));
            services.AddScoped<Side>();
            services.AddScoped<IWarrantyClaimsCapabilityProvider>(x => x.GetRequiredService<Side>());
            services.AddSingleton<SharedClaimService>();
            services.AddScoped<WarrantyClaimService>();
            services.AddScoped<DeliveryDateService>();

            this.provider = services.BuildServiceProvider();
        }

        public IServiceScope As(bool isDistributor)
        {
            var scope = this.provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<Side>().IsDistributor = isDistributor;
            return scope;
        }

        public async Task<long> CreateAsync(bool isDistributor, WarrantyClaimDTO? dto = null)
        {
            using var scope = this.As(isDistributor);
            var claims = scope.ServiceProvider.GetRequiredService<WarrantyClaimRepository>();

            var claim = await claims.UpsertAsync(new(), dto ?? NewClaim(), ActionTypes.Insert, null, null, true, true);
            claims.Add(claim);
            await claims.SaveChangesAsync();

            return claim.ID;
        }

        public async Task SaveAsync(long id, WarrantyClaimDTO dto, bool isDistributor)
        {
            using var scope = this.As(isDistributor);
            var claims = scope.ServiceProvider.GetRequiredService<WarrantyClaimRepository>();

            await claims.UpsertAsync((await Find(claims, id))!, dto, ActionTypes.Update, null, null, true, true);
            await claims.SaveChangesAsync();
        }

        public async Task ActAsync(long id, UpdateStatusActionTypes action, bool isDistributor, string? text = null)
        {
            using var scope = this.As(isDistributor);
            var claims = scope.ServiceProvider.GetRequiredService<WarrantyClaimRepository>();

            if (ClaimActionAccess.IsFormAction(action))
                await claims.ApplyActionAsync(id, action, text);
            else
                await claims.UpdateClaimStatusAsync([(await Find(claims, id))!], action, text);
        }

        public async Task NoteAsync(long id, bool isDistributor, string text)
        {
            using var scope = this.As(isDistributor);
            await scope.ServiceProvider.GetRequiredService<WarrantyClaimRepository>().AddNoteAsync(id, text);
        }

        public async Task CertifyAsync(long id)
        {
            using var scope = this.As(isDistributor: true);
            var certificates = scope.ServiceProvider.GetRequiredService<WarrantyCertificateRepository>();

            certificates.Add(await certificates.UpsertAsync(
                new Certificate(),
                new CertificateDTO
                {
                    CertificateDate = new DateTime(2026, 6, 1), PeriodStartDate = new DateTime(2026, 5, 1), PeriodEndDate = new DateTime(2026, 5, 31),
                    WarrantyClaims = [new() { WarrantyClaim = new() { Value = id.ToString() } }],
                },
                ActionTypes.Insert, null, null, true, true));
            await certificates.SaveChangesAsync();
        }

        public async Task<WarrantyClaimDTO> ViewAsync(long id, bool isDistributor)
        {
            using var scope = this.As(isDistributor);
            var claims = scope.ServiceProvider.GetRequiredService<WarrantyClaimRepository>();

            return await claims.ViewAsync((await Find(claims, id))!);
        }

        private static Task<WarrantyClaim?> Find(WarrantyClaimRepository claims, long id) => claims.FindAsync(id, null, true, true);

        private static WarrantyClaimDTO NewClaim() => new()
        {
            WarrantyType = "VE", Franchise = "F", OperationType = OperationTypes.NotSpecified, PreDelivery = true, RepairDate = new DateTime(2026, 5, 10),
            RepairOrderNo = "RO", Condition = "C", Cause = "C", Remedy = "R", VIN_WMI = "AAA", VIN_VDS = "BBBBB", VIN_CD = "0", VIN_VIS = "00000001",
            PRR1 = 1, LaborExchangeRate = 1, PartExchangeRate = 1, SubletExchangeRate = 1,
        };

        public ValueTask DisposeAsync() => this.provider.DisposeAsync();
    }

    private class Side : IWarrantyClaimsCapabilityProvider { public bool IsDistributor { get; set; } }

    private class ClaimsDb(DbContextOptions<ClaimsDb> options) : ShiftDbContext(options);

    private class Pat : ICurrentUserProvider
    {
        public ClaimsPrincipal? GetUser() => new(new ClaimsIdentity(
            [
                new(ClaimTypes.NameIdentifier, "7"),
                new(ClaimTypes.GivenName, "Pat"),
                new(Constants.CompanyIdClaim, "1"),
                new(Constants.CompanyBranchIdClaim, "1"),
            ], "test"));
    }

    private class EveryRow : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.ReturnType == typeof(bool) ? true : args!.Single(method.ReturnType.IsInstanceOfType);
    }
}
