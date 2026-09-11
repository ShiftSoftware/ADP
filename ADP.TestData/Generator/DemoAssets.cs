using System.Text.RegularExpressions;

namespace ADP.TestData.Generator;

/// <summary>
/// The neutral demo images the fixtures point at. They ship inside the npm package
/// (<c>src/features/mocks/data/assets/</c> → <c>dist/mocks/assets/</c>, the same copy step that
/// publishes the fixtures themselves), so one CDN URL works wherever a fixture is loaded — the dev
/// showcase, the docs site and any host running <c>isDev</c> — with no storage account, no token and
/// no expiry. In a dev build the components map the CDN prefix back to the dev server's copy, so the
/// pictures render locally before the version that carries them is published.
/// <para>
/// Every mapping is deterministic: the same stored key always yields the same file, so regenerated
/// fixtures stay byte-stable. Each family of images serves exactly one use — a paint-panel drawing is
/// never a company badge, a badge never an accessory — so no generic picture is reused across
/// unrelated things. The drawings are described in <c>assets/README.md</c> next to the files.
/// </para>
/// </summary>
public static class DemoAssets
{
    /// <summary>
    /// <c>@latest</c> rather than the version being prepared: the assets only ever grow, and a fixture
    /// generated today must not 404 the day the next version ships. The consequence — the URLs resolve
    /// on the CDN only once the first release after the assets landed is published — is why dev builds
    /// map the prefix locally instead.
    /// </summary>
    public const string CdnBaseUrl = "https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/mocks/assets/";

    /// <summary>The number of distinct company badges in <c>assets/badges/</c> (<c>badge-1.svg</c> … <c>badge-N.svg</c>).</summary>
    public const int BadgeCount = 6;

    /// <summary>The accessory drawings in <c>assets/accessories/</c>, by the stem a stored image key names them with.</summary>
    public static readonly IReadOnlySet<string> AccessoryDrawings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "side-steps", "roof-rails", "roof-rack", "floor-mats", "mud-guards", "tow-bar",
    };

    /// <summary>The panel drawings in <c>assets/paint-panels/</c>, by slug (<c>type[-position][-side]</c>); each has a <c>-detail</c> twin.</summary>
    public static readonly IReadOnlySet<string> PaintPanelDrawings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "hood", "roof", "tail-gate",
        "fender-front-left", "fender-front-right", "fender-rear-left", "fender-rear-right",
        "door-front-left", "door-front-right", "door-rear-left", "door-rear-right",
    };

    private static readonly Regex TrailingIndex = new(@"[_-](\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// A paint-thickness image key → the drawing of that panel. Stored keys are upload paths whose file
    /// stem names the panel the way inspection tools do — <c>[Side_][Position_]Type_N.jpg</c>, e.g.
    /// <c>Left_Front_Fender_1.jpg</c>, <c>Hood_2.jpg</c>, <c>Tail_Gate_1.jpg</c> — so the stem is parsed
    /// into a slug (<c>fender-front-left</c>) and the trailing index picks the view: an odd index is the
    /// plan view (top-down car, panel highlighted), an even one the close-up with the gauge marker, so
    /// the two photos an inspection typically stores per panel do not come out identical. A stem that
    /// names no known panel falls back to the unhighlighted car outline (<c>panel.svg</c>): still a
    /// paint-panel picture, never a broken image, and the key stays visible in the fixture.
    /// </summary>
    public static string? PaintPanelImageUrl(string? imageKey)
    {
        if (string.IsNullOrWhiteSpace(imageKey))
            return null;

        var stem = FileStem(imageKey);
        var indexMatch = TrailingIndex.Match(stem);
        var index = indexMatch.Success ? int.Parse(indexMatch.Groups[1].Value) : 1;
        if (indexMatch.Success)
            stem = stem[..indexMatch.Index];

        var slug = PaintPanelSlugs(stem).FirstOrDefault(PaintPanelDrawings.Contains);
        if (slug is null)
            return CdnBaseUrl + "paint-panels/panel.svg";

        return CdnBaseUrl + "paint-panels/" + slug + (index % 2 == 0 ? "-detail" : string.Empty) + ".svg";
    }

    /// <summary>
    /// A company id → one of the fictional badges, deterministically (<c>badge-((id - 1) mod N) + 1</c>).
    /// Ids beyond the badge count share a badge; the marks are abstract on purpose — company names differ
    /// per environment, so a badge must not spell one.
    /// </summary>
    public static string? CompanyBadgeUrl(long? companyId)
    {
        if (companyId is not { } id)
            return null;

        var badge = (int)(((id - 1) % BadgeCount + BadgeCount) % BadgeCount) + 1;

        return CdnBaseUrl + "badges/badge-" + badge + ".svg";
    }

    /// <summary>
    /// An accessory image key → the drawing of that product. The stem of the stored key names the drawing
    /// (<c>Uploads/accessories/&lt;VIN&gt;/side-steps.jpg</c> → <c>accessories/side-steps.svg</c>); an
    /// unknown stem falls back to the generic parts carton (<c>accessory.svg</c>), so a real host's
    /// re-keyed uploads render something that still reads as a part rather than a broken image. A null
    /// key stays null — that accessory has no picture, which is a state the panel shows on its own.
    /// </summary>
    public static string? AccessoryImageUrl(string? imageKey)
    {
        if (string.IsNullOrWhiteSpace(imageKey))
            return null;

        var stem = TrailingIndex.Replace(FileStem(imageKey), string.Empty).ToLowerInvariant().Replace('_', '-');

        return CdnBaseUrl + "accessories/" + (AccessoryDrawings.Contains(stem) ? stem : "accessory") + ".svg";
    }

    /// <summary>
    /// The file name without its extension, whatever separators the key uses. Keys are storage paths, not
    /// local files, so this is done by hand rather than through <see cref="Path"/>.
    /// </summary>
    private static string FileStem(string key)
    {
        var name = key.Trim();
        var cut = name.LastIndexOfAny(new[] { '/', '\\' });
        if (cut >= 0)
            name = name[(cut + 1)..];

        var dot = name.LastIndexOf('.');

        return dot > 0 ? name[..dot] : name;
    }

    /// <summary>
    /// <c>Left_Front_Fender</c> → <c>fender-front-left</c>: the type is whatever is left once the side and
    /// position tokens are taken out, two-word types joined (<c>Tail_Gate</c> → <c>tail-gate</c>,
    /// <c>TailGate</c> too). Yielded most specific first — with side and position, then each alone, then
    /// the bare type — so a key that qualifies a panel the set does not split (<c>Front_Hood</c>,
    /// <c>Middle_Roof</c>) still lands on its drawing.
    /// </summary>
    private static IEnumerable<string> PaintPanelSlugs(string stem)
    {
        var tokens = stem.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .ToList();

        string? side = null, position = null;
        var typeTokens = new List<string>();

        foreach (var token in tokens)
        {
            if (token is "left" or "right")
                side = token;
            else if (token is "front" or "rear" or "middle")
                position = token;
            else
                typeTokens.Add(token);
        }

        var type = string.Join("-", typeTokens) switch
        {
            "tailgate" => "tail-gate",
            var t => t,
        };

        if (type.Length == 0)
            yield break;

        if (position is not null && side is not null)
            yield return $"{type}-{position}-{side}";
        if (position is not null)
            yield return $"{type}-{position}";
        if (side is not null)
            yield return $"{type}-{side}";
        yield return type;
    }
}
