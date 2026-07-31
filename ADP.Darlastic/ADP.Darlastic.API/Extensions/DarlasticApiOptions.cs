namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

public class DarlasticApiOptions
{
    /// <summary>Route prefix Darlastic controllers are mounted under (e.g. "api/Darlastic").</summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// SQL schema the registry lives under in the host database. Must match the schema the host's
    /// migrations created and the engine writes (DARLASTIC_SQL side) — "Darlastic" everywhere so far.
    /// </summary>
    public string Schema { get; set; } = "Darlastic";

    /// <summary>
    /// When true, Darlastic endpoints are protected by per-action DarlasticActionTree permissions.
    /// When false (default), only authentication is required — for hosts whose identity server
    /// doesn't grant the Darlastic tree yet.
    /// </summary>
    public bool EnableDarlasticActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Whether <c>AddDarlasticApiServices</c> registers the registry model itself.
    ///
    /// <para>Leave <see langword="true"/> (the default) when the registry shares the host's
    /// application database: the host adds nothing and its existing migrations version the
    /// registry. That is the usual shape for a CRM/ticketing host.</para>
    ///
    /// <para>Set <see langword="false"/> when the registry lives in a DEDICATED database behind its
    /// own <see cref="ShiftSoftware.ShiftEntity.EFCore.ShiftDbContext"/>, and call
    /// <c>ConfigureDarlasticEntities</c> from that context's <c>OnModelCreating</c> instead. The
    /// contributor cannot serve that case: <c>ShiftDbContext</c> resolves contributors from the
    /// APPLICATION service provider, so a single registration reaches every <c>ShiftDbContext</c>
    /// in the process. In a host with a separate Darlastic database that silently pulls the whole
    /// registry into the application database's model too, and the next migration generated there
    /// creates a second, never-written copy of all 16 tables — a schema no engine populates and no
    /// reader queries, in the database whose migrations everyone trusts.</para>
    /// </summary>
    public bool RegisterModelContributor { get; set; } = true;

    /// <summary>
    /// HMAC key for the case browser's short-lived access tokens. <see langword="null"/> or empty
    /// (the default) turns the whole mechanism off: no token can be minted and none is accepted, so
    /// the surface behaves exactly as it did before — bearer authentication only.
    ///
    /// <para><b>Why a token at all.</b> The case browser is a static HTML page, not a Blazor
    /// component. A browser navigating to it sends no <c>Authorization</c> header, and the page has
    /// no auth stack to attach one to its own fetches — so on a bearer-authenticated host it is
    /// unreachable. A signed, expiring, single-purpose token minted by an authenticated caller and
    /// handed to the page is the same shape ShiftEntity already uses for printing
    /// (<c>TokenService.GenerateSASToken</c>).</para>
    ///
    /// <para><b>Know what you are choosing.</b> The token travels in the URL, so it can leak through
    /// browser history, access logs and <c>Referer</c> headers — genuinely weaker than a cookie, and
    /// this surface has writes (flag, unflag, audit), so a leaked link is not harmlessly read-only.
    /// Keep <see cref="CaseBrowserTokenLifetime"/> short. Any host that would rather not make that
    /// trade simply leaves this unset.</para>
    /// </summary>
    public string? CaseBrowserSigningKey { get; set; }

    /// <summary>
    /// How long a minted case browser token stays valid. Long enough to read a case without being
    /// interrupted, short enough that a leaked URL is worth little. The caller that mints it holds a
    /// real session, so it can mint another whenever it likes — this is a handoff, not a login.
    /// </summary>
    public TimeSpan CaseBrowserTokenLifetime { get; set; } = TimeSpan.FromMinutes(60);
}
