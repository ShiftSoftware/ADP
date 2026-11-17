#nullable enable

using ShiftSoftware.ADP.Lookup.Services.Enums;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ShiftSoftware.ADP.Lookup.Services;

/// <summary>
/// The token a vehicle lookup returns in <c>VehicleWarrantyDTO.ActivationToken</c> when it offers warranty activation
/// to the requesting company. It binds the VIN, that company and an expiry, signed with
/// <c>LookupOptions.SigningSecretKey</c>, so a host's activation endpoint can accept an activation only where a
/// lookup offered one, without evaluating the vehicle again.
/// </summary>
public static class WarrantyActivationToken
{
    private const string Purpose = "warranty-activation";

    /// <summary>
    /// Creates a token for <paramref name="vin"/> and <paramref name="companyID"/> that is valid until
    /// <paramref name="expiresAtUtc"/> (to the second). The lookup issues these itself; hosts call this only in tests.
    /// </summary>
    public static string Create(string vin, long companyID, DateTime expiresAtUtc, string secretKey)
    {
        var expires = ToUnixSeconds(expiresAtUtc);

        return $"{expires.ToString(CultureInfo.InvariantCulture)}.{Sign(vin, companyID, expires, secretKey)}";
    }

    /// <summary>
    /// Whether <paramref name="token"/> was issued for <paramref name="vin"/> and <paramref name="companyID"/> with
    /// <paramref name="secretKey"/> and has not expired at <paramref name="utcNow"/> (the current time when null).
    /// </summary>
    public static bool Validate(string? token, string vin, long companyID, string secretKey, DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(vin) || string.IsNullOrWhiteSpace(secretKey))
            return false;

        var separator = token!.IndexOf('.');

        if (separator <= 0 ||
            !long.TryParse(token.Substring(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var expires))
            return false;

        if (ToUnixSeconds(utcNow ?? DateTime.UtcNow) > expires)
            return false;

        return FixedTimeEquals(Sign(vin, companyID, expires, secretKey), token.Substring(separator + 1));
    }

    internal static string? Issue(WarrantyActivationStatus status, string vin, long? requestingCompanyID, LookupOptions options)
    {
        if (status != WarrantyActivationStatus.Required ||
            requestingCompanyID is null ||
            string.IsNullOrWhiteSpace(vin) ||
            string.IsNullOrWhiteSpace(options?.SigningSecretKey))
            return null;

        return Create(vin, requestingCompanyID.Value, options!.GetUtcNow().Add(options.SignatureValidityDuration), options.SigningSecretKey);
    }

    private static string Sign(string vin, long companyID, long expires, string secretKey)
    {
        var text = string.Join(
            ",",
            Purpose,
            vin.Trim().ToUpperInvariant(),
            companyID.ToString(CultureInfo.InvariantCulture),
            expires.ToString(CultureInfo.InvariantCulture)
        );

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));

        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static long ToUnixSeconds(DateTime utc) =>
        new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();

    // CryptographicOperations.FixedTimeEquals is not available on netstandard2.0.
    private static bool FixedTimeEquals(string expected, string provided)
    {
        if (expected.Length != provided.Length)
            return false;

        var difference = 0;

        for (var i = 0; i < expected.Length; i++)
            difference |= expected[i] ^ provided[i];

        return difference == 0;
    }
}
