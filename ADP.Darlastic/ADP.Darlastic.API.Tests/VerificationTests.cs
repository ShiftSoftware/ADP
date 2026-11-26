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

    // ── levels, typed documents, one holder, review ──────────────────────────────────────────────

    private const string NationalId = "national-id", Passport = "passport";

    /// <summary>A host whose person kind is an identity document: typed, one customer's for good, hashed, and
    /// "looked-at" needing a review.</summary>
    private static DarlasticApiOptions Documents()
    {
        var options = Enabled();
        options.Verification.ValueHashKey = "synthetic-test-key";
        var person = options.Verification.Kinds[VerificationKind.Person];
        person.OneHolder = true;
        person.ValueRequired = true;
        person.ValueTypes = new(StringComparer.OrdinalIgnoreCase) { NationalId, Passport };
        person.ReviewedMethods = new(StringComparer.OrdinalIgnoreCase) { "looked-at" };
        return options;
    }

    private static VerificationReport Document(long identity, string type, string number, string outcome = VerificationOutcome.Verified,
        string method = "reader", string actor = "desk") =>
        new(identity, VerificationKind.Person, number, method, outcome, actor, ValueType: type);

    [Fact]
    public async Task A_submitted_claim_waits_for_a_review_that_moves_it_to_verified()
    {
        using var registry = new Registry();
        var options = Documents();
        var service = registry.Service(options);

        var submitted = await service.ReportAsync(Document(A, NationalId, "SYNTH-1", VerificationOutcome.Submitted, "looked-at"));

        Assert.Equal(VerificationLevel.Submitted, submitted.Opened!.Level);
        var waiting = Assert.Single(await service.ClaimsAsync(A));
        Assert.False(waiting.Holds);
        Assert.True(waiting.Pending);

        // The default rule asks for verified: a submitted claim does not meet it, and says why.
        var status = await service.StatusAsync(A);
        Assert.False(status.Verified);
        Assert.Equal("pending-review", Assert.Single(status.Requirements).Missing);

        var (queue, total) = await service.AwaitingReviewAsync();
        Assert.Equal(1, total);
        Assert.Equal(submitted.Opened.ClaimID, Assert.Single(queue).Claim.ClaimID);
        Assert.Equal("desk", queue[0].Submitted.Actor);

        var approved = await service.ReviewAsync(submitted.Opened.ClaimID, approve: true, "back-office", "matches the photo");

        Assert.Equal(VerificationLevel.Verified, approved.Opened!.Level);
        Assert.Equal("looked-at", approved.Opened.Method);
        Assert.Equal(VerificationEndReason.Promoted, Assert.Single(approved.Ended).EndReason);
        Assert.Equal("back-office", approved.Entry.Actor);
        Assert.Equal(submitted.Opened.ClaimID, approved.Entry.ReviewOfClaimID);
        Assert.Equal("matches the photo", approved.Entry.Note);
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);
        Assert.True((await service.StatusAsync(A)).Verified);
        Assert.Empty((await service.AwaitingReviewAsync()).Items);

        // Decided once: the same claim cannot be reviewed again.
        var again = await Assert.ThrowsAsync<VerificationException>(() => service.ReviewAsync(submitted.Opened.ClaimID, true, "back-office"));
        Assert.Equal(VerificationProblem.UnknownClaim, again.Problem);
    }

    [Fact]
    public async Task A_rejected_claim_ends_and_a_rule_can_accept_the_submitted_level()
    {
        using var registry = new Registry();
        var options = Documents();
        options.Verification.Rules["walk-in"] = [new(VerificationKind.Person, Level: VerificationLevel.Submitted)];
        var service = registry.Service(options);

        var submitted = await service.ReportAsync(Document(A, Passport, "SYNTH-2", VerificationOutcome.Submitted, "looked-at"));

        var walkIn = await service.StatusAsync(A, "walk-in");
        Assert.True(walkIn.Verified);
        Assert.Equal(VerificationLevel.Submitted, Assert.Single(walkIn.Requirements).Claim!.Level);
        Assert.False((await service.StatusAsync(A)).Verified);

        var rejected = await service.ReviewAsync(submitted.Opened!.ClaimID, approve: false, "back-office", "unreadable");

        Assert.Null(rejected.Opened);
        Assert.Equal(VerificationOutcome.Rejected, rejected.Entry.Outcome);
        Assert.Equal(VerificationEndReason.Rejected, Assert.Single(rejected.Ended).EndReason);
        Assert.Empty(await service.ClaimsAsync(A));
        Assert.False((await service.StatusAsync(A, "walk-in")).Verified);
    }

    [Fact]
    public async Task A_method_that_needs_a_review_cannot_report_verified_and_nobody_reviews_their_own_report()
    {
        using var registry = new Registry();
        var options = Documents();
        var service = registry.Service(options);

        var direct = await Assert.ThrowsAsync<VerificationException>(() =>
            service.ReportAsync(Document(A, NationalId, "SYNTH-3", VerificationOutcome.Verified, "looked-at")));
        Assert.Equal(VerificationProblem.MethodNeedsReview, direct.Problem);

        var submitted = await service.ReportAsync(Document(A, NationalId, "SYNTH-3", VerificationOutcome.Submitted, "looked-at", actor: "desk"));
        var own = await Assert.ThrowsAsync<VerificationException>(() => service.ReviewAsync(submitted.Opened!.ClaimID, true, "desk"));
        Assert.Equal(VerificationProblem.ReviewBySubmitter, own.Problem);

        options.Verification.AllowReviewBySubmitter = true;
        Assert.NotNull((await registry.Service(options).ReviewAsync(submitted.Opened!.ClaimID, true, "desk")).Opened);
    }

    [Fact]
    public async Task A_customer_holds_several_documents_each_with_its_type()
    {
        using var registry = new Registry();
        var service = registry.Service(Documents());

        await service.ReportAsync(Document(A, NationalId, "SYNTH-4"));
        await service.ReportAsync(Document(A, Passport, "SYNTH-5"));
        // The same number under another type is another document.
        await service.ReportAsync(Document(A, Passport, "SYNTH-4"));

        var claims = await service.ClaimsAsync(A);
        Assert.Equal(3, claims.Count);
        Assert.All(claims, c => Assert.True(c.Holds));
        Assert.Equal([NationalId, Passport, Passport], claims.Select(c => c.Claim.ValueType).Order());
        Assert.Equal(3, claims.Select(c => c.Claim.ValueKey).Distinct().Count());
        Assert.All(claims, c => Assert.Equal(64, c.Claim.ValueKey.Length));

        // Reading the same document again renews its period and leaves the others.
        var renewed = await service.ReportAsync(Document(A, NationalId, " synth 4 "));
        Assert.Equal(VerificationEndReason.Renewed, Assert.Single(renewed.Ended).EndReason);
        Assert.Equal(3, (await service.ClaimsAsync(A)).Count);

        // One document is taken back by its claim number.
        var passport = claims.First(c => c.Claim.ValueType == Passport).Claim.ClaimID;
        var revoked = await service.RevokeClaimAsync(passport, "desk");
        Assert.Equal(VerificationEndReason.Revoked, Assert.Single(revoked.Ended).EndReason);
        Assert.Equal(2, (await service.ClaimsAsync(A)).Count);

        var noType = await Assert.ThrowsAsync<VerificationException>(() =>
            service.ReportAsync(new(A, VerificationKind.Person, "SYNTH-4", "reader", VerificationOutcome.Verified, "desk")));
        Assert.Equal(VerificationProblem.ValueTypeInvalid, noType.Problem);
        var unknownType = await Assert.ThrowsAsync<VerificationException>(() => service.ReportAsync(Document(A, "library-card", "SYNTH-4")));
        Assert.Equal(VerificationProblem.ValueTypeInvalid, unknownType.Problem);
        var typedPhone = await Assert.ThrowsAsync<VerificationException>(() =>
            service.ReportAsync(new(A, VerificationKind.Phone, Phone, "otp", VerificationOutcome.Verified, "desk", ValueType: "mobile")));
        Assert.Equal(VerificationProblem.ValueTypeInvalid, typedPhone.Problem);
    }

    [Fact]
    public async Task A_document_another_customer_holds_is_held_for_review_and_ends_nothing()
    {
        using var registry = new Registry();
        var service = registry.Service(Documents());
        var first = await service.ReportAsync(Document(A, NationalId, "SYNTH-6"));

        var second = await service.ReportAsync(Document(B, NationalId, "synth-6"));

        Assert.Equal(VerificationOutcome.Held, second.Entry.Outcome);
        Assert.Equal(VerificationLevel.Held, second.Opened!.Level);
        Assert.Equal(first.Opened!.ClaimID, second.Opened.ConflictClaimID);
        Assert.Equal(first.Opened.ClaimID, second.ConflictsWith!.ClaimID);
        Assert.Empty(second.Ended);

        // A keeps the document; B is not verified by a held attempt, under any rule.
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);
        var held = Assert.Single(await service.ClaimsAsync(B));
        Assert.False(held.Holds);
        Assert.True(held.Pending);
        var status = await service.StatusAsync(B);
        Assert.False(status.Verified);
        Assert.Equal("not-verified", Assert.Single(status.Requirements).Missing);

        // The reviewer sees who holds it.
        var item = Assert.Single((await service.AwaitingReviewAsync()).Items);
        Assert.Equal(B, item.LiveIdentityID);
        Assert.Equal(A, item.ConflictLiveIdentityID);
        Assert.Equal(first.Opened.ClaimID, item.ConflictsWith!.ClaimID);

        // Trying again changes nothing: one held claim.
        var again = await service.ReportAsync(Document(B, NationalId, "SYNTH-6"));
        Assert.Null(again.Opened);
        Assert.Equal(second.Opened.ClaimID, again.Unchanged!.ClaimID);
        Assert.Single(await service.ClaimsAsync(B));

        // While A holds it, approving B is refused.
        var refused = await Assert.ThrowsAsync<VerificationException>(() => service.ReviewAsync(second.Opened.ClaimID, true, "back-office"));
        Assert.Equal(VerificationProblem.ConflictUnresolved, refused.Problem);
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);

        // The other document of B is untouched by all this.
        Assert.Equal(VerificationLevel.Verified, (await service.ReportAsync(Document(B, Passport, "SYNTH-7"))).Opened!.Level);
        Assert.True((await service.StatusAsync(B)).Verified);
    }

    [Fact]
    public async Task A_held_document_is_settled_by_merging_the_customers_or_by_ending_the_other_claim()
    {
        // A missed merge: once the two are one customer, the held claim can be approved.
        using (var registry = new Registry())
        {
            var service = registry.Service(Documents());
            await service.ReportAsync(Document(A, NationalId, "SYNTH-8"));
            var held = await service.ReportAsync(Document(B, NationalId, "SYNTH-8"));

            await registry.RedirectAsync(from: A, to: B);

            var item = Assert.Single((await service.AwaitingReviewAsync()).Items);
            Assert.Equal(item.LiveIdentityID, item.ConflictLiveIdentityID);
            var approved = await service.ReviewAsync(held.Opened!.ClaimID, true, "back-office");
            Assert.Equal(B, approved.Opened!.IdentityID);
            var one = Assert.Single(await service.ClaimsAsync(B));
            Assert.True(one.Holds);
        }

        // A mistake on the first customer: their claim is revoked, then the second is approved.
        using (var registry = new Registry())
        {
            var service = registry.Service(Documents());
            var first = await service.ReportAsync(Document(A, NationalId, "SYNTH-9"));
            var held = await service.ReportAsync(Document(B, NationalId, "SYNTH-9"));

            await service.RevokeClaimAsync(first.Opened!.ClaimID, "back-office");
            await service.ReviewAsync(held.Opened!.ClaimID, true, "back-office");

            Assert.Empty(await service.ClaimsAsync(A));
            Assert.True(Assert.Single(await service.ClaimsAsync(B)).Holds);
            // And now it is B's: A trying again is the one held.
            Assert.Equal(VerificationLevel.Held, (await service.ReportAsync(Document(A, NationalId, "SYNTH-9"))).Opened!.Level);
        }

        // A submitted document takes the value too: a reader on another customer is held behind it.
        using (var registry = new Registry())
        {
            var service = registry.Service(Documents());
            await service.ReportAsync(Document(A, NationalId, "SYNTH-10", VerificationOutcome.Submitted, "looked-at"));
            Assert.Equal(VerificationLevel.Held, (await service.ReportAsync(Document(B, NationalId, "SYNTH-10"))).Opened!.Level);
        }
    }

    [Fact]
    public async Task One_holder_without_a_key_is_refused_and_a_submitted_report_never_steps_a_verified_claim_down()
    {
        using var registry = new Registry();
        var keyless = Documents();
        keyless.Verification.ValueHashKey = null;
        var refused = await Assert.ThrowsAsync<VerificationException>(() => registry.Service(keyless).ReportAsync(Document(A, NationalId, "SYNTH-11")));
        Assert.Equal(VerificationProblem.NotConfigured, refused.Problem);

        var service = registry.Service(Documents());
        var verified = await service.ReportAsync(Document(A, NationalId, "SYNTH-11"));
        var submitted = await service.ReportAsync(Document(A, NationalId, "SYNTH-11", VerificationOutcome.Submitted, "looked-at"));

        Assert.Null(submitted.Opened);
        Assert.Empty(submitted.Ended);
        Assert.Equal(verified.Opened!.ClaimID, submitted.Unchanged!.ClaimID);
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);
    }

    [Fact]
    public async Task A_submitted_phone_takes_nothing_until_a_review_approves_it()
    {
        using var registry = new Registry();
        var service = registry.Service();
        await service.ReportAsync(Verified(A, VerificationKind.Phone, Phone));

        var submitted = await service.ReportAsync(new(B, VerificationKind.Phone, Phone, "called", VerificationOutcome.Submitted, "desk"));

        Assert.Empty(submitted.Ended);
        Assert.False(submitted.Opened!.Exclusive);
        Assert.True(Assert.Single(await service.ClaimsAsync(A)).Holds);

        var approved = await service.ReviewAsync(submitted.Opened.ClaimID, true, "back-office");

        Assert.Contains(approved.Ended, c => c.IdentityID == A && c.EndReason == VerificationEndReason.Superseded);
        Assert.Empty(await service.ClaimsAsync(A));
        Assert.True(Assert.Single(await service.ClaimsAsync(B)).Holds);
    }

    [Fact]
    public async Task Over_http_the_review_queue_and_decisions_need_the_review_access_and_a_held_report_never_names_the_holder()
    {
        using var registry = new Registry();
        var options = Documents();
        options.Verification.AcceptReportsOverHttp = true;
        var controller = registry.Controller(options);

        await controller.Report(new() { IdentityID = "100", Kind = "person", Value = "SYNTH-12", ValueType = NationalId, Method = "reader" });
        var held = Assert.IsType<VerificationResultDTO>(Assert.IsType<OkObjectResult>((await controller.Report(
            new() { IdentityID = "200", Kind = "person", Value = "SYNTH-12", ValueType = NationalId, Method = "reader" })).Result).Value);

        Assert.True(held.Held);
        Assert.Equal(VerificationOutcome.Held, held.Outcome);
        Assert.Equal(VerificationLevel.Held, held.Level);
        Assert.Empty(held.Ended);   // nothing in the answer says who holds it

        var claims = Assert.IsType<CustomerVerificationDTO>(Assert.IsType<OkObjectResult>((await controller.Get(200)).Result).Value);
        var claim = Assert.Single(claims.Claims);
        Assert.True(claim.Pending);
        Assert.False(claim.Holds);
        Assert.Equal(NationalId, claim.ValueType);
        Assert.Null(claim.Value);

        var queue = Assert.IsType<VerificationReviewQueueDTO>(Assert.IsType<OkObjectResult>((await controller.Review()).Result).Value);
        var item = Assert.Single(queue.Items);
        Assert.Equal("200", item.IdentityID);
        Assert.Equal("100", item.ConflictIdentityID);
        Assert.True(item.ConflictOpen);
        Assert.Null(item.Value);

        Assert.IsType<ConflictObjectResult>((await controller.Decide(new() { ClaimID = item.ClaimID, Approve = true })).Result);
        Assert.IsType<NotFoundResult>((await controller.Decide(new() { ClaimID = 987654, Approve = true })).Result);

        // A claim is revoked by its number only through its own customer.
        Assert.IsType<NotFoundResult>((await controller.Report(new() { IdentityID = "300", Outcome = "revoked", ClaimID = item.ClaimID, Kind = "person", Method = "m" })).Result);
        Assert.IsType<OkObjectResult>((await controller.Report(new() { IdentityID = "200", Outcome = "revoked", ClaimID = item.ClaimID, Kind = "person", Method = "m" })).Result);
        Assert.Empty(Assert.IsType<VerificationReviewQueueDTO>(Assert.IsType<OkObjectResult>((await controller.Review()).Result).Value).Items);

        // With the action tree on and a caller who holds nothing, the queue and a decision are forbidden.
        options.EnableDarlasticActionTreeAuthorization = true;
        var gated = registry.Controller(options);
        Assert.IsType<ForbidResult>((await gated.Review()).Result);
        Assert.IsType<ForbidResult>((await gated.Decide(new() { ClaimID = 1, Approve = false })).Result);
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
