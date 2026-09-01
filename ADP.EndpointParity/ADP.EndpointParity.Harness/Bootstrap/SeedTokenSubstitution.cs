using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// Substitutes <c>@@Entity[n]@@</c> tokens in a hand-authored create body with the REAL hash id
/// of a seeded row.
///
/// <para>
/// Needed because a minimal-valid body frequently has to reference another row - a
/// <c>ShiftEntitySelectDTO</c> pointing at a vehicle model, a parent's <c>MenuID</c>, a service
/// interval group - and a hash id only exists once the seed has been applied and the application
/// has encoded it. Hard-coding one would break the moment the salt or the seeded long changed;
/// inventing one produces a body that 4xxs before reaching the mapper, which is precisely the
/// silent-coverage failure the 100% CREATE gate exists to catch.
/// </para>
///
/// <para>
/// A token that cannot be resolved is left in place deliberately, so the request fails loudly and
/// visibly rather than being silently sent as <c>null</c>.
/// </para>
/// </summary>
public static class SeedTokenSubstitution
{
    private static readonly Regex Token = new(@"@@(?<entity>[A-Za-z0-9_]+)\[(?<index>\d+)\]@@",
        RegexOptions.Compiled);

    public static string Apply(string body, IReadOnlyDictionary<string, IReadOnlyList<string>> seededHashIds) =>
        Token.Replace(body, match =>
        {
            var entity = match.Groups["entity"].Value;
            var index = int.Parse(match.Groups["index"].Value);

            return seededHashIds.TryGetValue(entity, out var ids) && index < ids.Count
                ? ids[index]
                : match.Value;   // unresolved: leave it, so the failure is loud
        });
}
