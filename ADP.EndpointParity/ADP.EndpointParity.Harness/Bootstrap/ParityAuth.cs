using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Tokens;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// Mints the two principals every group is captured under.
///
/// <para>
/// <b>Two principals, not one, and both built here in Step 00.</b> Four later step files make
/// the restricted pass mandatory; a mandatory gate that no step builds is a gate discovered at
/// the riskiest step, after the baselines are committed. A restricted baseline captured after
/// the code changes is not a baseline.
/// </para>
///
/// <para>
/// The claim set below is not invented - it is copied from a real token minted by the sample's
/// own identity server (POST /api/Auth/Login as the seeded SuperUser), so a minted parity token
/// is indistinguishable from a real one except in its access tree. The observed shape is:
/// <code>
/// alg RS256, iss = Settings:TokenSettings:Issuer
/// nameidentifier / name / givenname
/// ShiftSoftware/ShiftEntity/Claims/{RegionId,CompanyId,CompanyBranchId,CompanyType,CountryId,CityId}
/// ExternalToken = "false"
/// ShiftSoftware/TypeAuth/Claims/AccessTree = {"&lt;TreeName&gt;":[1,2,3,4], ...}
/// </code>
/// </para>
///
/// <para>
/// <b>The access tree is where the two principals differ, and nowhere else.</b> That is
/// deliberate: it makes the restricted pass a clean control on row- and field-scoping rather
/// than a second variable. The numbers are the TypeAuth <c>Access</c> enum - Read=1, Write=2,
/// Delete=3, Maximum=4 - so full access is [1,2,3,4] per tree and a restricted grant is a
/// subset, declared per group in parity.psd1 because each group has its own action tree and
/// "restricted" has no group-independent meaning.
/// </para>
/// </summary>
public static class ParityAuth
{
    public const string AccessTreeClaim = "ShiftSoftware/TypeAuth/Claims/AccessTree";

    /// <summary>The deterministic identity every parity token carries (Rule 3: compare literally).</summary>
    public const string UserId = "QOa4j";

    /// <summary>
    /// Builds the access-tree claim value for a grant.
    /// </summary>
    /// <param name="fullAccessTrees">
    /// Every action tree the host registers, e.g. ShiftIdentityActions, AzureStorageActionTree,
    /// GeneralActionTree and the group's own tree.
    /// </param>
    /// <param name="restricted">
    /// For <see cref="Harness.ParityGrant.Restricted"/>: tree name to the access levels that
    /// grant keeps. A tree absent from this map is granted nothing at all.
    /// </param>
    public static string BuildAccessTree(
        Harness.ParityGrant grant,
        IReadOnlyCollection<string> fullAccessTrees,
        IReadOnlyDictionary<string, int[]> restricted)
    {
        var tree = new JsonObject();

        if (grant == Harness.ParityGrant.FullAccess)
        {
            // [1,2,3,4] = Read, Write, Delete, Maximum - what SetFullAccessAsync produces.
            foreach (var name in fullAccessTrees.OrderBy(n => n, StringComparer.Ordinal))
                tree[name] = new JsonArray(1, 2, 3, 4);
        }
        else
        {
            foreach (var kv in restricted.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var levels = new JsonArray();
                foreach (var level in kv.Value) levels.Add(level);
                tree[kv.Key] = levels;
            }
        }

        // The claim value is a JSON STRING containing JSON, exactly as the identity server emits it.
        return tree.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Mints an RS256 JWT the host's ShiftIdentity configuration will accept.
    /// </summary>
    /// <param name="issuer">Settings:TokenSettings:Issuer from the host's configuration.</param>
    /// <param name="privateKeyBase64">Settings:TokenSettings:PrivateKey - a base64 PKCS#1 RSA private key.</param>
    public static string MintToken(string issuer, string privateKeyBase64, string accessTreeJson)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId),
            new(ClaimTypes.Name, "SuperUser"),
            new(ClaimTypes.GivenName, "Super User"),
            new("ShiftSoftware/ShiftEntity/Claims/RegionId", "Qpa4j"),
            new("ShiftSoftware/ShiftEntity/Claims/CompanyId", "QOa4d"),
            new("ShiftSoftware/ShiftEntity/Claims/CompanyBranchId", "Qpa4j"),
            new("ShiftSoftware/ShiftEntity/Claims/CompanyType", "NotSpecified"),
            new("ShiftSoftware/ShiftEntity/Claims/CountryId", "QOa4d"),
            new("ShiftSoftware/ShiftEntity/Claims/CityId", "QOa4d"),
            new("ExternalToken", "false"),
            new(AccessTreeClaim, accessTreeJson),
        };

        // A FIXED expiry, not now+15min. Rule 1's spirit: make values deterministic rather than
        // normalizing them away. The exp claim never reaches a response body, but a fixed value
        // means two capture runs mint byte-identical tokens, which removes one more reason for
        // two identical runs to differ.
        var token = new JwtSecurityToken(
            issuer: issuer,
            claims: claims,
            notBefore: null,
            expires: new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
