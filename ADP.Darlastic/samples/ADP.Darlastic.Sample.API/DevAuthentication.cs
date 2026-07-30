using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ShiftSoftware.ADP.Darlastic.Sample.API;

/// <summary>
/// Authenticates every request as a fixed local developer.
///
/// <para><b>This is not authentication. It is the absence of authentication, given a name so it is
/// visible in code review.</b> The module's endpoints are <c>[Authorize]</c>, and a runnable sample
/// has to satisfy that somehow; the honest options were to stand up a full ShiftIdentity dashboard
/// (what the Menus sample does — hundreds of lines of seeding for a demo) or to make the bypass
/// explicit and impossible to enable by accident. This is the second.</para>
///
/// <para>Two independent gates, both required, checked in <c>Program.cs</c> before this handler is
/// ever registered: the environment must be Development, AND <c>Sample:AllowDevAuth</c> must be
/// explicitly true. A deployment that forgets either one fails to start rather than serving the
/// registry to anonymous callers — which is the failure mode that actually matters, because this
/// reads real customer records.</para>
///
/// <para>Real hosts do not use this. TCA mounts the module behind the CRM's identity server and TIQ
/// behind its own; the action-tree grants then decide who may see the queue and who may act on it.</para>
/// </summary>
public sealed class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SampleDevAuth";

    /// <summary>Who the audit rows will name. Deliberately obvious in a decision log.</summary>
    public const string DevUser = "sample-dev@localhost";

    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, DevUser),
            new Claim(ClaimTypes.Email, DevUser),
            new Claim(ClaimTypes.NameIdentifier, DevUser),
        ], SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
