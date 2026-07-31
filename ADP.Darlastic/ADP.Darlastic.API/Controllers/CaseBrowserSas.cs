using Microsoft.AspNetCore.Http;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ShiftEntity.Core.Services;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Short-lived signed access for the case browser page, using ShiftEntity's own SAS helper so this
/// is the same mechanism the framework already uses for printing rather than a second invention.
///
/// <para>The signature covers <c>(descriptor, actor, expires)</c>. The descriptor is constant and
/// specific to this surface, which is what stops a token minted here from being replayed against any
/// other SAS-protected resource — and stops one minted elsewhere from opening this one.</para>
///
/// <para>The actor rides inside the signed data on purpose. Review flags and label audits exist to
/// record <em>who said what</em>; a token that only proved "someone authenticated" would leave every
/// row authored by "unknown" and quietly destroy the provenance those tables are for. It does mean a
/// username appears in the URL, which is a deliberate trade for an internal tool.</para>
/// </summary>
internal static class CaseBrowserSas
{
    /// <summary>
    /// Scopes a token to this surface, and — by being one of two — to what the caller could do at the
    /// moment it was minted. Changing either value invalidates every outstanding token of that kind.
    ///
    /// <para>The alternative was to pack a scope marker into the signed <c>id</c> alongside the actor,
    /// which would put <c>rw:</c> in front of a person's name in the URL and, worse, in front of the
    /// name written into <c>ReviewFlag.FlaggedBy</c> unless every read remembered to strip it. Two
    /// descriptors keep the signed identity a plain actor and leave no way to forget.</para>
    /// </summary>
    public const string ReadDescriptor = "ADP.Darlastic.CaseBrowser";

    /// <summary>Read-plus-write counterpart of <see cref="ReadDescriptor"/>.</summary>
    public const string WriteDescriptor = "ADP.Darlastic.CaseBrowser.Write";

    public const string TokenParam = "token";
    public const string ExpiresParam = "expires";
    public const string ActorParam = "actor";

    public static bool Enabled(DarlasticApiOptions options) =>
        !string.IsNullOrWhiteSpace(options.CaseBrowserSigningKey);

    /// <summary>
    /// Signs a token for <paramref name="actor"/> carrying no more than the access the minting caller
    /// held. <paramref name="canWrite"/> comes from the action tree at mint time, which is the only
    /// moment a real session is present to ask about.
    /// </summary>
    public static (string token, string expires) Mint(DarlasticApiOptions options, string actor, bool canWrite) =>
        TokenService.GenerateSASToken(
            canWrite ? WriteDescriptor : ReadDescriptor,
            actor, DateTime.UtcNow.Add(options.CaseBrowserTokenLifetime), options.CaseBrowserSigningKey!);

    /// <summary>
    /// What a valid token grants, or <see langword="null"/> when the request carries no usable token.
    /// Returning the grant rather than a bool keeps the caller from having to re-read and re-trust the
    /// query string separately — the name and the access level are only ever taken from signed data.
    /// </summary>
    public static CaseBrowserGrant? GrantOf(HttpRequest request, DarlasticApiOptions options)
    {
        if (!Enabled(options)) return null;

        string? token = request.Query[TokenParam];
        string? expires = request.Query[ExpiresParam];
        string? actor = request.Query[ActorParam];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expires) || string.IsNullOrWhiteSpace(actor))
            return null;

        // ValidateSASToken re-derives the signature and checks the expiry, and compares in fixed
        // time. A tampered actor or a stretched expiry changes the signed data, so both are covered
        // by the signature rather than by trusting the query string. The descriptor is signed too,
        // so a read token cannot be re-presented as a write one — try the stronger grant first and
        // fall back, rather than letting the query string say which kind it is.
        string key = options.CaseBrowserSigningKey!;
        if (TokenService.ValidateSASToken(WriteDescriptor, actor, expires, token, key)) return new(actor, true);
        if (TokenService.ValidateSASToken(ReadDescriptor, actor, expires, token, key)) return new(actor, false);
        return null;
    }

    /// <summary>The query string that carries a token onto a URL.</summary>
    public static string QueryString(string token, string expires, string actor) =>
        $"{TokenParam}={Uri.EscapeDataString(token)}" +
        $"&{ExpiresParam}={Uri.EscapeDataString(expires)}" +
        $"&{ActorParam}={Uri.EscapeDataString(actor)}";
}

/// <summary>
/// Who a case browser token names and what it lets them do. <paramref name="CanWrite"/> is a property
/// of the token itself — a steward with read-only access gets a token that cannot flag or audit, so
/// the handoff to a page with no auth stack of its own does not quietly widen what they may do.
/// </summary>
internal sealed record CaseBrowserGrant(string Actor, bool CanWrite);
