using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.API.Verification;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.Verification;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Verification over HTTP: what is verified for a customer, whether they are verified for a visit, and
/// (where the host allows it) recording a result. Every route answers 404 while the host has verification off.
///
/// <para>Values a caller sends (a phone, a VIN) travel in the request body, never in a URL. A verified value
/// that is personal data is shown masked to a caller without the host's reveal action. A status answer never
/// says whether another customer holds a value.</para>
/// </summary>
[Route("[controller]")]
[ApiController]
public class VerificationController : ControllerBase
{
    private readonly VerificationService service;
    private readonly DarlasticApiOptions options;
    private readonly IPiiMasker masker;
    private readonly PiiOptions piiOptions;

    public VerificationController(VerificationService service, IOptions<DarlasticApiOptions> options,
        IPiiMasker masker, IOptions<PiiOptions> piiOptions)
    {
        this.service = service;
        this.options = options.Value;
        this.masker = masker;
        this.piiOptions = piiOptions.Value;
    }

    private bool Denied(Access access)
    {
        if (!options.EnableDarlasticActionTreeAuthorization) return false;
        var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        return !typeAuth.Can(options.Actions.ResolvedVerification, access);
    }

    private string Actor => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.Identity?.Name ?? "unknown";

    private static bool TryId(string? text, out long id) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out id);

    /// <summary>The claims that hold for a customer; with <paramref name="history"/> the ended ones too.</summary>
    [HttpGet("{identityId:long}")]
    [Authorize]
    public async Task<ActionResult<CustomerVerificationDTO>> Get(long identityId, bool history = false, CancellationToken ct = default)
    {
        if (!service.Enabled) return NotFound();
        if (Denied(Access.Read)) return Forbid();

        try
        {
            var live = await service.LiveIdentityAsync(identityId, ct);
            if (live is null) return NotFound();

            var pii = RecordPiiView.ForCaller(HttpContext, masker, piiOptions);
            var claims = await service.ClaimsAsync(live.Value, history, ct);
            return Ok(new CustomerVerificationDTO
            {
                IdentityID = Text(live.Value),
                Claims = [.. claims.Select(c => ToDto(c, pii))],
            });
        }
        catch (VerificationException problem) { return Refused(problem); }
    }

    /// <summary>Whether a customer is verified under the host's rule for a context, requirement by requirement.</summary>
    [HttpPost("status")]
    [Authorize]
    public async Task<ActionResult<VerificationStatusDTO>> Status([FromBody] VerificationStatusRequestDTO input, CancellationToken ct = default)
    {
        if (!service.Enabled) return NotFound();
        if (Denied(Access.Read)) return Forbid();
        if (input is null || !TryId(input.IdentityID, out var identityId)) return BadRequest("A status question names its customer.");

        try
        {
            var status = await service.StatusAsync(identityId, input.Context, input.Values, ct);
            return Ok(new VerificationStatusDTO
            {
                IdentityID = Text(status.IdentityID),
                Context = status.Context,
                Verified = status.Verified,
                Requirements = [.. status.Requirements.Select(r => new VerificationRequirementStatusDTO
                {
                    Kind = r.Requirement.Kind,
                    SpecificValue = r.Requirement.SpecificValue,
                    Met = r.Met,
                    Missing = r.Missing,
                    ClaimID = r.Claim?.ClaimID,
                    Method = r.Claim?.Method,
                    SinceUtc = r.Claim?.StartUtc,
                })],
            });
        }
        catch (VerificationException problem) { return Refused(problem); }
    }

    /// <summary>
    /// Record a result. A <c>revoked</c> report needs the delete action and is always accepted; a
    /// <c>verified</c> or <c>failed</c> one needs the write action and a host that accepts reports over HTTP
    /// (<see cref="DarlasticVerificationOptions.AcceptReportsOverHttp"/>), otherwise 404: such a host reports
    /// from its own server code after its own check.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<VerificationResultDTO>> Report([FromBody] VerificationReportDTO input, CancellationToken ct = default)
    {
        if (!service.Enabled) return NotFound();

        var outcome = string.IsNullOrWhiteSpace(input?.Outcome) ? VerificationOutcome.Verified : input.Outcome.Trim().ToLowerInvariant();
        bool revoke = outcome == VerificationOutcome.Revoked;

        if (Denied(revoke ? Access.Delete : Access.Write)) return Forbid();
        if (!revoke && !options.Verification.AcceptReportsOverHttp) return NotFound();
        if (input is null || !TryId(input.IdentityID, out var identityId)) return BadRequest("A report names its customer.");

        try
        {
            var result = await service.ReportAsync(new VerificationReport(
                identityId, input.Kind, input.Value, input.Method, outcome, Actor,
                input.Location, input.SourceSystem, input.SourceRecordId, input.EvidenceReference), ct);

            return Ok(new VerificationResultDTO
            {
                EntryID = result.Entry.EntryID,
                IdentityID = Text(result.Entry.IdentityID),
                Kind = result.Entry.Kind,
                Outcome = result.Entry.Outcome,
                ClaimID = result.Opened?.ClaimID,
                Ended = [.. result.Ended.Select(c => new VerificationEndedClaimDTO
                {
                    ClaimID = c.ClaimID,
                    IdentityID = Text(c.IdentityID),
                    Reason = c.EndReason!,
                })],
            });
        }
        catch (VerificationException problem) { return Refused(problem); }
    }

    private ActionResult Refused(VerificationException problem) =>
        problem.Problem == VerificationProblem.UnknownIdentity ? NotFound() : BadRequest(new { problem.Message });

    private VerifiedClaimDTO ToDto(ClaimState state, RecordPiiView pii)
    {
        var claim = state.Claim;
        options.Verification.Kinds.TryGetValue(claim.Kind, out var kind);
        return new VerifiedClaimDTO
        {
            ClaimID = claim.ClaimID,
            IdentityID = Text(claim.IdentityID),
            Kind = claim.Kind,
            // A hashed value is never shown; a kind the host no longer configures shows nothing either.
            Value = kind is null || kind.HashValue || claim.ValueKey.Length == 0 ? null
                : kind.Pii is { } piiKind ? pii.Show(piiKind, claim.ValueKey)
                : claim.ValueKey,
            Method = claim.Method,
            StartUtc = claim.StartUtc,
            EndUtc = claim.EndUtc,
            EndReason = claim.EndReason,
            ExpiresUtc = state.ExpiresUtc,
            Holds = state.Holds,
            Location = claim.Location,
            SourceSystem = claim.SourceSystem,
            SourceRecordId = claim.SourceRecordId,
        };
    }

    private static string Text(long id) => id.ToString(CultureInfo.InvariantCulture);
}
