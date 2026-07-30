using System.Text;

namespace ShiftSoftware.ADP.Darlastic.CaseBrowser;

/// <summary>
/// Access to the case browser's UI as shipped inside this package.
///
/// <para>There is exactly one copy of <c>cases.html</c> and every host serves it from here — the
/// spike's local <c>queue</c>/<c>cases</c> modes, the sample host, and (once mounted) a tenant
/// host. A second copy on disk somewhere would drift from the API contract silently, which is the
/// failure this accessor exists to prevent.</para>
/// </summary>
public static class CaseBrowserUI
{
    /// <summary>The UI, from this assembly's embedded resources.</summary>
    public static string Html()
    {
        var asm = typeof(CaseBrowserUI).Assembly;
        var name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("cases.html", StringComparison.OrdinalIgnoreCase));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s, Encoding.UTF8);
        return r.ReadToEnd();
    }

    /// <summary>
    /// The UI with its API base path rewritten. The page defaults to the spike's local routes
    /// (<c>/api/...</c>); a host mounting the module points it at
    /// <c>CaseBrowserCompatController</c> instead (<c>/api/Darlastic/CaseBrowserCompat</c>), which
    /// serves the same contract from the registry. This is how that gets injected without a second
    /// copy of the file.
    ///
    /// <para>Note the base is the COMPAT controller, not <c>CaseBrowserController</c>. The latter is
    /// the module's own DTO-shaped API; this page predates it and speaks a different contract, and
    /// the compat controller is the typed mapping between them.</para>
    /// </summary>
    public static string Html(string apiBase)
    {
        string html = Html();
        // The page reads window.DARLASTIC_API_BASE when present; injecting it ahead of the page's
        // own script means no build step and no templating engine.
        string inject = $"<script>window.DARLASTIC_API_BASE = {System.Text.Json.JsonSerializer.Serialize(apiBase)};</script>";
        int head = html.IndexOf("<script", StringComparison.OrdinalIgnoreCase);
        return head < 0 ? inject + html : html.Insert(head, inject);
    }
}
