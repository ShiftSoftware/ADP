using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>Carries the access tree the mounted host's principal should present.</summary>
public sealed class ParityPrincipalOptions
{
    public string AccessTreeJson { get; set; } = "{}";

    /// <summary>
    /// Data-level scoping ids. These are NOT decoration: ShiftEntity applies data-level access
    /// filtering from these claims, so a seeded row whose CompanyID does not match is silently
    /// excluded from every list - the list comes back <c>{"Count":0,"Value":[]}</c> with a 200 and
    /// the baseline looks perfectly healthy while proving nothing. The mounted host registers
    /// hash ids with acceptUnencodedIds, so plain numbers are correct here, and the seed writes
    /// the SAME numbers into its rows.
    /// </summary>
    public string CompanyId { get; set; } = "1";
    public string CompanyBranchId { get; set; } = "1";
    public string CountryId { get; set; } = "1";
    public string RegionId { get; set; } = "1";
    public string CityId { get; set; } = "1";
}

/// <summary>
/// Authenticates every request to a MOUNTED host as a fixed parity principal.
///
/// <para>
/// <b>This is not authentication. It is a test principal, given a name so it is visible in review</b>
/// - the same device, and the same rationale, as the repo's own
/// <c>ADP.Darlastic.Sample.API/DevAuthenticationHandler</c>. A mounted host has no identity server
/// to issue a real token, but the module's controllers are <c>[Authorize]</c>, so without a scheme
/// every request fails with "No authenticationScheme was specified" - which is exactly how the
/// first ClaimableItems capture came back: ten 5xx and not one usable transcript.
/// </para>
///
/// <para>
/// It emits the SAME claim set a real ShiftIdentity token carries, including
/// <c>ShiftSoftware/TypeAuth/Claims/AccessTree</c>. That matters: it is what makes the RESTRICTED
/// pass meaningful on a mounted host rather than a second identical run, because the two grants
/// then differ in exactly the way they do on a sample host - in the access tree and nowhere else.
/// </para>
/// </summary>
public sealed class ParityAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ParityAuth";

    private readonly ParityPrincipalOptions principal;

    public ParityAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ParityPrincipalOptions principal)
        : base(options, logger, encoder)
    {
        this.principal = principal;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Mirrors the claim set observed on a real token minted by the Surveys sample's own
        // identity server, so a mounted-host principal and a sample-host principal differ only
        // in how they were issued.
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, ParityAuth.UserId),
            new Claim(ClaimTypes.Name, "SuperUser"),
            new Claim(ClaimTypes.GivenName, "Super User"),
            new Claim("ShiftSoftware/ShiftEntity/Claims/RegionId", principal.RegionId),
            new Claim("ShiftSoftware/ShiftEntity/Claims/CompanyId", principal.CompanyId),
            new Claim("ShiftSoftware/ShiftEntity/Claims/CompanyBranchId", principal.CompanyBranchId),
            new Claim("ShiftSoftware/ShiftEntity/Claims/CompanyType", "NotSpecified"),
            new Claim("ShiftSoftware/ShiftEntity/Claims/CountryId", principal.CountryId),
            new Claim("ShiftSoftware/ShiftEntity/Claims/CityId", principal.CityId),
            new Claim("ExternalToken", "false"),
            new Claim(ParityAuth.AccessTreeClaim, principal.AccessTreeJson),
        ], SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
