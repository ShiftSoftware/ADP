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
    /// <summary>Scopes a token to this surface. Changing it invalidates every outstanding token.</summary>
    public const string Descriptor = "ADP.Darlastic.CaseBrowser";

    public const string TokenParam = "token";
    public const string ExpiresParam = "expires";
    public const string ActorParam = "actor";

    public static bool Enabled(DarlasticApiOptions options) =>
        !string.IsNullOrWhiteSpace(options.CaseBrowserSigningKey);

    public static (string token, string expires) Mint(DarlasticApiOptions options, string actor) =>
        TokenService.GenerateSASToken(
            Descriptor, actor, DateTime.UtcNow.Add(options.CaseBrowserTokenLifetime), options.CaseBrowserSigningKey!);

    /// <summary>
    /// The actor a valid token names, or <see langword="null"/> when the request carries no usable
    /// token. Returning the actor rather than a bool keeps the caller from having to re-read and
    /// re-trust the query string separately — the name is only ever taken from data that was signed.
    /// </summary>
    public static string? ActorOf(HttpRequest request, DarlasticApiOptions options)
    {
        if (!Enabled(options)) return null;

        string? token = request.Query[TokenParam];
        string? expires = request.Query[ExpiresParam];
        string? actor = request.Query[ActorParam];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expires) || string.IsNullOrWhiteSpace(actor))
            return null;

        // ValidateSASToken re-derives the signature and checks the expiry, and compares in fixed
        // time. A tampered actor or a stretched expiry changes the signed data, so both are covered
        // by the signature rather than by trusting the query string.
        return TokenService.ValidateSASToken(Descriptor, actor, expires, token, options.CaseBrowserSigningKey!)
            ? actor
            : null;
    }

    /// <summary>The query string that carries a token onto a URL.</summary>
    public static string QueryString(string token, string expires, string actor) =>
        $"{TokenParam}={Uri.EscapeDataString(token)}" +
        $"&{ExpiresParam}={Uri.EscapeDataString(expires)}" +
        $"&{ActorParam}={Uri.EscapeDataString(actor)}";
}
