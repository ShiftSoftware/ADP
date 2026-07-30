using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The case browser page itself, served by whichever host mounts the module.
///
/// <para>A host gets the whole surface from <c>AddDarlasticApiServices</c> and one link — no static
/// files to deploy, no build step, no second copy of the UI to keep in step with the API. The page
/// is the same embedded <c>cases.html</c> the standalone tool serves (linked, not copied — see the
/// csproj), pointed at <see cref="CaseBrowserCompatController"/>.</para>
///
/// <para>The API base is derived from the host's own configured route prefix rather than hard-coded,
/// because a host is free to mount the module anywhere. A page shipped with the wrong base loads
/// perfectly and then 404s every one of its own data calls — it looks deployed, and it is not.</para>
/// </summary>
[Route("[controller]")]
public class CaseBrowserUiController : Controller
{
    private readonly DarlasticApiOptions options;

    public CaseBrowserUiController(IOptions<DarlasticApiOptions> options) => this.options = options.Value;

    /// <summary>
    /// The page. Anonymous because it carries no data and no secret — every byte of it is the same
    /// for every caller, and the credential arrives separately in the query string. Requiring a
    /// bearer token here would be theatre: a browser navigation cannot send one, so the page would
    /// simply be unreachable while the data stayed exactly as protected as it is now.
    /// </summary>
    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Index()
    {
        string prefix = (options.RoutePrefix ?? "api").Trim('/');
        return Content(Page($"/{prefix}/CaseBrowserCompat"), "text/html; charset=utf-8", Encoding.UTF8);
    }

    /// <summary>
    /// Mints a short-lived token for the page. Authenticated — this is the point where the caller's
    /// real session is spent, and everything downstream is that one act, signed and time-boxed.
    ///
    /// <para>Meant to be called by a host UI that already holds a session (a Blazor page's
    /// <c>HttpClient</c>, say), which is what lets the page inherit that app's token refresh without
    /// implementing any: when this token nears expiry the caller simply mints another.</para>
    /// </summary>
    [HttpGet("token")]
    [Authorize]
    public IActionResult Token()
    {
        if (!CaseBrowserSas.Enabled(options))
            // Not "forbidden" — the host has not opted in, so there is nothing to hand out. Said
            // plainly, because a 401/403 here would send someone hunting for a permissions problem.
            return Problem(
                title: "Case browser tokens are not enabled",
                detail: "Set DarlasticApiOptions.CaseBrowserSigningKey on this host to enable them.",
                statusCode: StatusCodes.Status501NotImplemented);

        string actor = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown";

        var (token, expires) = CaseBrowserSas.Mint(options, actor);
        string prefix = (options.RoutePrefix ?? "api").Trim('/');

        return new JsonResult(new
        {
            token,
            expires,
            actor,
            // Handed back assembled so a caller never has to know how the parameters are spelled.
            url = $"/{prefix}/CaseBrowserUi?{CaseBrowserSas.QueryString(token, expires, actor)}",
            lifetimeSeconds = (int)options.CaseBrowserTokenLifetime.TotalSeconds,
        });
    }

    /// <summary>The embedded page with <c>window.DARLASTIC_API_BASE</c> injected ahead of its own
    /// script — which is all the page needs to talk to a host instead of the standalone server.</summary>
    private static string Page(string apiBase)
    {
        var asm = Assembly.GetExecutingAssembly();
        string name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("cases.html", StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string html = reader.ReadToEnd();

        string inject = $"<script>window.DARLASTIC_API_BASE = {System.Text.Json.JsonSerializer.Serialize(apiBase)};</script>";
        int head = html.IndexOf("<script", StringComparison.OrdinalIgnoreCase);
        return head < 0 ? inject + html : html.Insert(head, inject);
    }
}
