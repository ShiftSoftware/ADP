using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Controllers;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.API.Verification;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Data.Extensions;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.Verification;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// Verification against an in-memory registry: what a report opens and ends, exclusivity, merges, expiry, the
/// "verified for this visit" rules, and the HTTP surface. The unique indexes that settle two simultaneous
/// reports exist only in a relational database, so that race is not exercised here. Synthetic values only.
/// </summary>
public class VerificationTests
{
    private const long A = 100, B = 200, C = 300;
    private const string Phone = "7000000001";
    private const string OtherPhone = "7000000002";
    private const string Vin = "SYNTHVIN000000011";

    private static VerificationReport Verified(long identity, string kind, string? value, string method = "test-method") =>
        new(identity, kind, value, method, VerificationOutcome.Verified, "tester");

    [Fact]
    public async Task Off_by_default_adds_no_tables_and_refuses()
    {
        var defaults = new DarlasticApiOptions();
        Assert.False(defaults.Verification.Enabled);
        Assert.False(defaults.Verification.AcceptReportsOverHttp);
        Assert.Null(defaults.Verification.ValueHashKey);

        using var registry = new Registry(verification: false);
        Assert.Null(registry.Db.Model.FindEntityType(typeof(VerifiedClaim)));
        Assert.Null(registry.Db.Model.FindEntityType(typeof(VerificationEntry)));

        var service = registry.Service(new DarlasticApiOptions());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReportAsync(Verified(A, VerificationKind.Phone, Phone)));

        var controller = registry.Controller(new DarlasticApiOptions());
        Assert.IsType<NotFoundResult>((await controller.Get(A)).Result);
        Assert.IsType<NotFoundResult>((await controller.Status(new() { IdentityID = "100" })).Result);
        Assert.IsType<NotFoundResult>((await controller.Report(new() { IdentityID = "100", Kind = "phone", Value = Phone, Method = "m" })).Result);
    }

    [Fact]
    public async Task A_verified_report_opens_a_claim_and_logs_it()
    {
        using var registry = new Registry();
        var service = registry.Service();

        var result = await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone, "otp"));

        Assert.NotNull(result.Opened);
        Assert.Empty(result.Ended);
        var claim = Assert.Single(await service.ClaimsAsync(A));
        Assert.True(claim.Holds);
        Assert.Equal(Phone, claim.Claim.ValueKey);
        Assert.Equal("otp", claim.Claim.Method);
        Assert.True(claim.Claim.Exclusive);
        Assert.Equal(result.Entry.EntryID, claim.Claim.StartedByEntryID);
        Assert.Equal(1, await registry.Db.Set<VerificationEntry>().CountAsync());
    }

    [Theory]
    [InlineData("+964 700 000 0001")]
    [InlineData("07000000001")]
    [InlineData("00964-7000000001")]
    public async Task A_phone_is_keyed_the_way_the_registry_keys_phones(string typed)
    {
        using var registry = new Registry();
        var service = registry.Service();

        await service.ReportAsync(Verified(A, VerificationKind.Phone, typed));

        Assert.Equal(Phone, Assert.Single(await service.ClaimsAsync(A)).Claim.ValueKey);
    }

    [Fact]
    public async Task An_exclusive_value_moves_to_the_customer_verified_last()
    {
        using var registry = new Registry();
        var service = registry.Service();
        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));

        var result = await service.ReportAsync(Verified(B, VerificationKind.Phone, Phone));

        var ended = Assert.Single(result.Ended);
        Assert.Equal(A, ended.IdentityID);
        Assert.Equal(VerificationEndReason.Superseded, ended.EndReason);
        Assert.Empty(await service.ClaimsAsync(A));
        Assert.Single(await service.ClaimsAsync(B));

        var history = Assert.Single(await service.ClaimsAsync(A, history: true));
        Assert.False(history.Holds);
        Assert.NotNull(history.Claim.EndUtc);
        Assert.Equal(result.Entry.EntryID, history.Claim.EndedByEntryID);

        // Another phone of A's is untouched by B verifying this one.
        await service.ReportAsync(Verified(A, VerificationKind.Phone, OtherPhone));
        Assert.Equal(OtherPhone, Assert.Single(await service.ClaimsAsync(A)).Claim.ValueKey);
    }

    [Fact]
    public async Task Verifying_again_for_the_same_customer_starts_a_new_period()
    {
        using var registry = new Registry();
        var service = registry.Service();
        await service.ReportAsync(Verified(A, VerificationKind.Vehicle, Vin));

        var result = await service.ReportAsync(Verified(A, VerificationKind.Vehicle, Vin.ToLowerInvariant()));

        Assert.Equal(VerificationEndReason.Renewed, Assert.Single(result.Ended).EndReason);
        Assert.Single(await service.ClaimsAsync(A));
        Assert.Equal(2, (await service.ClaimsAsync(A, history: true)).Count);
    }

    [Fact]
    public async Task A_kind_that_is_not_exclusive_holds_for_several_customers()
    {
        using var registry = new Registry();
        var service = registry.Service();

        await service.ReportAsync(Verified(A, VerificationKind.Person, null));
        var second = await service.ReportAsync(Verified(B, VerificationKind.Person, null));

        Assert.Empty(second.Ended);
        Assert.Single(await service.ClaimsAsync(A));
        Assert.Single(await service.ClaimsAsync(B));
    }

    [Fact]
    public async Task Revoked_ends_the_claim_and_failed_changes_nothing()
    {
        using var registry = new Registry();
        var service = registry.Service();
        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));

        var failed = await service.ReportAsync(Verified(B, VerificationKind.Phone, Phone) with { Outcome = VerificationOutcome.Failed });
        Assert.Null(failed.Opened);
        Assert.Empty(failed.Ended);
        Assert.Single(await service.ClaimsAsync(A));
        Assert.Empty(await service.ClaimsAsync(B));

        // Revoking for B does not touch A's claim on the same value.
        var wrong = await service.ReportAsync(Verified(B, VerificationKind.Phone, Phone) with { Outcome = VerificationOutcome.Revoked });
        Assert.Empty(wrong.Ended);
        Assert.Single(await service.ClaimsAsync(A));

        var revoked = await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone) with { Outcome = VerificationOutcome.Revoked });
        Assert.Null(revoked.Opened);
        Assert.Equal(VerificationEndReason.Revoked, Assert.Single(revoked.Ended).EndReason);
        Assert.Empty(await service.ClaimsAsync(A));
        Assert.Equal(4, await registry.Db.Set<VerificationEntry>().CountAsync());
    }

    [Fact]
    public async Task Claims_are_read_through_a_merge()
    {
        using var registry = new Registry();
        var service = registry.Service();
        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));
        await service.ReportAsync(Verified(A, VerificationKind.Person, null));

        await registry.RedirectAsync(from: A, to: B);

        // B, and A's old number, both answer with what was verified on A.
        Assert.Equal(2, (await service.ClaimsAsync(B)).Count);
        Assert.Equal(2, (await service.ClaimsAsync(A)).Count);
        Assert.True((await service.StatusAsync(A)).Verified);
        Assert.Equal(B, (await service.StatusAsync(A)).IdentityID);

        // Verifying the same phone on the survivor is the same customer again, not a takeover.
        var again = await service.ReportAsync(Verified(B, VerificationKind.Phone, Phone));
        Assert.Equal(VerificationEndReason.Renewed, Assert.Single(again.Ended).EndReason);
        Assert.Equal(B, again.Opened!.IdentityID);

        // A report on the old number lands on the live customer.
        var onOld = await service.ReportAsync(Verified(A, VerificationKind.Vehicle, Vin));
        Assert.Equal(B, onOld.Opened!.IdentityID);
    }

    [Fact]
    public async Task What_cannot_be_recorded_is_refused_with_its_reason()
    {
        using var registry = new Registry();
        var options = Enabled();
        options.Verification.Kinds[VerificationKind.Phone].Methods.Add("otp");
        var service = registry.Service(options);

        async Task Refused(VerificationProblem problem, VerificationReport report) =>
            Assert.Equal(problem, (await Assert.ThrowsAsync<VerificationException>(() => service.ReportAsync(report))).Problem);

        await Refused(VerificationProblem.UnknownKind, Verified(A, "passport-stamp", "x"));
        await Refused(VerificationProblem.UnknownOutcome, Verified(A, VerificationKind.Phone, Phone, "otp") with { Outcome = "maybe" });
        await Refused(VerificationProblem.MethodMissing, Verified(A, VerificationKind.Vehicle, Vin, " "));
        await Refused(VerificationProblem.MethodNotAllowed, Verified(A, VerificationKind.Phone, Phone, "staff-check"));
        await Refused(VerificationProblem.ValueMissing, Verified(A, VerificationKind.Phone, "  ", "otp"));
        await Refused(VerificationProblem.ValueInvalid, Verified(A, VerificationKind.Vehicle, "NOTAVIN"));
        await Refused(VerificationProblem.UnknownIdentity, Verified(999, VerificationKind.Vehicle, Vin));
        await Refused(VerificationProblem.TooLong, Verified(A, VerificationKind.Vehicle, Vin, new string('m', 65)));

        Assert.Equal(0, await registry.Db.Set<VerificationEntry>().CountAsync());
        Assert.Equal(0, await registry.Db.Set<VerifiedClaim>().CountAsync());
    }

    [Fact]
    public async Task A_claim_expires_after_its_kinds_validity()
    {
        using var registry = new Registry();
        var options = Enabled();
        options.Verification.Kinds[VerificationKind.Phone].Validity = TimeSpan.FromDays(30);
        var clock = new Clock(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var service = registry.Service(options, clock);

        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));
        clock.Now = clock.Now.AddDays(29);
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);

        clock.Now = clock.Now.AddDays(2);
        Assert.Empty(await service.ClaimsAsync(A));
        var expired = Assert.Single(await service.ClaimsAsync(A, history: true));
        Assert.False(expired.Holds);
        Assert.Null(expired.Claim.EndUtc);
        Assert.Equal(new DateTime(2030, 1, 31), expired.ExpiresUtc);

        // Verifying again closes the expired period and holds again.
        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);
    }

    [Fact]
    public async Task Verified_for_a_visit_follows_the_hosts_rule_for_that_context()
    {
        using var registry = new Registry();
        var options = Enabled();
        options.Verification.Rules["service"] =
        [
            new(VerificationKind.Person),
            new(VerificationKind.Phone),
            new(VerificationKind.Vehicle, SpecificValue: true),
        ];
        var service = registry.Service(options);
        var visit = new Dictionary<string, string> { [VerificationKind.Vehicle] = Vin };

        var nothing = await service.StatusAsync(A, "service", visit);
        Assert.False(nothing.Verified);
        Assert.All(nothing.Requirements, r => Assert.Equal("not-verified", r.Missing));

        await service.ReportAsync(Verified(A, VerificationKind.Person, null));
        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));
        await service.ReportAsync(Verified(A, VerificationKind.Vehicle, "SYNTHVIN000000022"));

        // The default rule asks for the person only; an unknown context falls back to it.
        Assert.True((await service.StatusAsync(A)).Verified);
        Assert.True((await service.StatusAsync(A, "sales")).Verified);

        // The service rule wants the vehicle that is in for service, not any verified vehicle.
        var otherCar = await service.StatusAsync(A, "service", visit);
        Assert.False(otherCar.Verified);
        Assert.Equal("not-verified", Assert.Single(otherCar.Requirements, r => !r.Met).Missing);
        Assert.Equal("value-missing", Assert.Single((await service.StatusAsync(A, "service")).Requirements, r => !r.Met).Missing);
        Assert.Equal("value-invalid", Assert.Single((await service.StatusAsync(A, "service",
            new Dictionary<string, string> { [VerificationKind.Vehicle] = "NOTAVIN" })).Requirements, r => !r.Met).Missing);

        await service.ReportAsync(Verified(A, VerificationKind.Vehicle, Vin));
        Assert.True((await service.StatusAsync(A, "service", visit)).Verified);

        // The vehicle verified for another customer: this one no longer meets the rule, and is not told why.
        await service.ReportAsync(Verified(B, VerificationKind.Vehicle, Vin));
        Assert.Equal("not-verified", Assert.Single((await service.StatusAsync(A, "service", visit)).Requirements, r => !r.Met).Missing);

        // A rule that asks for nothing verifies nobody.
        options.Verification.Rules[""] = [];
        Assert.False((await service.StatusAsync(A)).Verified);
    }

    [Fact]
    public async Task A_document_number_is_never_stored_only_its_keyed_hash_when_the_host_keeps_a_key()
    {
        const string document = "SYNTH-DOC-0001";

        using (var registry = new Registry())
        {
            var service = registry.Service();
            await service.ReportAsync(Verified(A, VerificationKind.Person, document));
            Assert.Equal("", Assert.Single(await service.ClaimsAsync(A)).Claim.ValueKey);
            Assert.Equal("", (await registry.Db.Set<VerificationEntry>().SingleAsync()).ValueKey);
        }

        using (var registry = new Registry())
        {
            var options = Enabled();
            options.Verification.ValueHashKey = "synthetic-test-key";
            var service = registry.Service(options);

            await service.ReportAsync(Verified(A, VerificationKind.Person, document));
            await service.ReportAsync(Verified(B, VerificationKind.Person, " synth doc 0001 "));

            var a = Assert.Single(await service.ClaimsAsync(A)).Claim.ValueKey;
            var b = Assert.Single(await service.ClaimsAsync(B)).Claim.ValueKey;
            Assert.Equal(64, a.Length);
            Assert.DoesNotContain("SYNTH", a);
            Assert.Equal(a, b);   // one document on two customers is visible as one key

            var controller = registry.Controller(options);
            var dto = Assert.IsType<CustomerVerificationDTO>(Assert.IsType<OkObjectResult>((await controller.Get(A)).Result).Value);
            Assert.Null(Assert.Single(dto.Claims).Value);
        }
    }

    [Fact]
    public async Task Over_http_a_report_is_accepted_only_when_the_host_allows_it_and_revoking_always()
    {
        using var registry = new Registry();
        var options = Enabled();
        var report = new VerificationReportDTO { IdentityID = "100", Kind = "phone", Value = Phone, Method = "staff-check" };

        Assert.IsType<NotFoundResult>((await registry.Controller(options).Report(report)).Result);
        Assert.Equal(0, await registry.Db.Set<VerificationEntry>().CountAsync());

        options.Verification.AcceptReportsOverHttp = true;
        var recorded = Assert.IsType<VerificationResultDTO>(
            Assert.IsType<OkObjectResult>((await registry.Controller(options).Report(report)).Result).Value);
        Assert.Equal(VerificationOutcome.Verified, recorded.Outcome);
        Assert.NotNull(recorded.ClaimID);

        Assert.IsType<BadRequestObjectResult>((await registry.Controller(options).Report(
            new() { IdentityID = "100", Kind = "vehicle", Value = "NOTAVIN", Method = "staff-check" })).Result);
        Assert.IsType<NotFoundResult>((await registry.Controller(options).Report(
            new() { IdentityID = "999", Kind = "phone", Value = Phone, Method = "staff-check" })).Result);

        options.Verification.AcceptReportsOverHttp = false;
        var revoked = Assert.IsType<VerificationResultDTO>(Assert.IsType<OkObjectResult>((await registry.Controller(options).Report(
            new() { IdentityID = "100", Kind = "phone", Value = Phone, Method = "steward", Outcome = "revoked" })).Result).Value);
        Assert.Equal(VerificationEndReason.Revoked, Assert.Single(revoked.Ended).Reason);
    }

    [Fact]
    public async Task Over_http_a_phone_is_masked_and_every_route_needs_its_access()
    {
        using var registry = new Registry();
        var options = Enabled();
        await registry.Service(options).ReportAsync(Verified(A, VerificationKind.Phone, Phone));
        await registry.Service(options).ReportAsync(Verified(A, VerificationKind.Vehicle, Vin));

        // The test principal holds no reveal action: the phone comes back masked, the VIN as it is.
        var dto = Assert.IsType<CustomerVerificationDTO>(
            Assert.IsType<OkObjectResult>((await registry.Controller(options).Get(A)).Result).Value);
        var phone = Assert.Single(dto.Claims, c => c.Kind == VerificationKind.Phone);
        Assert.NotEqual(Phone, phone.Value);
        Assert.False(string.IsNullOrEmpty(phone.Value));
        Assert.Equal(Vin, Assert.Single(dto.Claims, c => c.Kind == VerificationKind.Vehicle).Value);

        var status = Assert.IsType<VerificationStatusDTO>(Assert.IsType<OkObjectResult>(
            (await registry.Controller(options).Status(new() { IdentityID = "100" })).Result).Value);
        Assert.False(status.Verified);
        Assert.IsType<NotFoundResult>((await registry.Controller(options).Get(999)).Result);

        options.EnableDarlasticActionTreeAuthorization = true;
        options.Verification.AcceptReportsOverHttp = true;
        var gated = registry.Controller(options);
        Assert.IsType<ForbidResult>((await gated.Get(A)).Result);
        Assert.IsType<ForbidResult>((await gated.Status(new() { IdentityID = "100" })).Result);
        Assert.IsType<ForbidResult>((await gated.Report(new() { IdentityID = "100", Kind = "phone", Value = Phone, Method = "m" })).Result);
        Assert.IsType<ForbidResult>((await gated.Report(new() { IdentityID = "100", Kind = "phone", Value = Phone, Method = "m", Outcome = "revoked" })).Result);
    }

    private static DarlasticApiOptions Enabled()
    {
        var options = new DarlasticApiOptions();
        options.Verification.Enabled = true;
        return options;
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>An in-memory registry with three customers, as a host DbContext maps it.</summary>
    private sealed class Registry : IDisposable
    {
        private readonly ServiceProvider services;
        private readonly IServiceScope scope;

        public Registry(bool verification = true)
        {
            var collection = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            // EF caches one model per context type, so the registry without verification has a type of its own.
            if (verification)
                collection.AddDbContext<ShiftDbContext, RegistryTestDbContext>(o => o.UseInMemoryDatabase(databaseName));
            else
                collection.AddDbContext<ShiftDbContext, PlainRegistryTestDbContext>(o => o.UseInMemoryDatabase(databaseName));
            collection.AddSingleton<IModelBuildingContributor>(new DarlasticModelBuildingContributor(schema: null, verification));
            collection.AddSingleton(DispatchProxy.Create<ITypeAuthService, GoldenCustomerByVinTests.DeniesEverything>());
            services = collection.BuildServiceProvider();
            scope = services.CreateScope();

            foreach (var id in new[] { A, B, C })
                Db.Add(new GoldenIdentity { IdentityID = id, Status = IdentityStatus.Active, CreatedRunID = 1, LastChangedRunID = 1 });
            Db.SaveChanges();
            Db.ChangeTracker.Clear();
        }

        public ShiftDbContext Db => scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        public VerificationService Service(DarlasticApiOptions? options = null, TimeProvider? clock = null) =>
            new(Db, Options.Create(options ?? Enabled()), clock);

        public VerificationController Controller(DarlasticApiOptions options)
        {
            var pii = new PiiOptions();
            return new VerificationController(Service(options), Options.Create(options),
                new DefaultPiiMasker(Options.Create(pii)), Options.Create(pii))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider },
                },
            };
        }

        public async Task RedirectAsync(long from, long to)
        {
            Db.Add(new IdentityRedirect { OldIdentityID = from, NewIdentityID = to, RunID = 2 });
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            scope.Dispose();
            services.Dispose();
        }
    }

    private sealed class RegistryTestDbContext(DbContextOptions<RegistryTestDbContext> options) : ShiftDbContext(options);
    private sealed class PlainRegistryTestDbContext(DbContextOptions<PlainRegistryTestDbContext> options) : ShiftDbContext(options);
}
