namespace ShiftSoftware.ADP.Darlastic.Web;

/// <summary>
/// The Darlastic mark, as a MudBlazor icon, for hosts that want their Darlastic nav entries to read
/// as Darlastic rather than as three more Material glyphs.
///
/// <para><b>Canonical source:</b> the <c>brandmark</c> SVG in
/// <c>ADP.Darlastic.CaseBrowser/web/cases.html</c>. The path data below is that file's, verbatim —
/// three source records on the left converging into one golden record on the right, which is what
/// the module does.</para>
/// </summary>
public static class DarlasticBrand
{
    /// <summary>
    /// MudBlazor supplies its own <c>&lt;svg viewBox="0 0 24 24"&gt;</c> wrapper and injects this as
    /// inner markup, so the original 256-unit artwork is scaled by a transform (24/256 = 0.09375)
    /// rather than by rewriting every coordinate. Exact, and it survives an update to the artwork.
    ///
    /// <para>The white strokes of the original became <c>currentColor</c>: the mark was drawn for the
    /// case browser's black top bar, and left white it would be invisible in a light nav drawer.
    /// Inheriting the colour also means it picks up the drawer's active and hover states. The gold of
    /// the golden record stays fixed — it is the one part carrying meaning rather than contrast.</para>
    /// </summary>
    public const string Icon = """
        <g transform="scale(0.09375)">
          <g fill="none" stroke-width="11" stroke-linecap="round">
            <path d="M66 64 C132 64 150 128 186 128" stroke="currentColor"/>
            <path d="M66 128 L186 128" stroke="#F2C230"/>
            <path d="M66 192 C132 192 150 128 186 128" stroke="currentColor"/>
          </g>
          <circle cx="66" cy="64" r="15" fill="currentColor"/>
          <circle cx="66" cy="128" r="15" fill="currentColor"/>
          <circle cx="66" cy="192" r="15" fill="currentColor"/>
          <circle cx="186" cy="128" r="19" fill="#F2C230"/>
        </g>
        """;
}
