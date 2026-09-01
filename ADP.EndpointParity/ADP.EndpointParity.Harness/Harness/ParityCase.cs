namespace ShiftSoftware.ADP.EndpointParity.Harness;

// CAPTURE LAYER: HttpClient, System.Text.Json and string only.

/// <summary>Which principal a case runs under (verification.md section 8.7).</summary>
public enum ParityGrant
{
    /// <summary>Full action-tree access. The ordinary pass.</summary>
    FullAccess,

    /// <summary>
    /// The per-group restricted grant declared in parity.psd1. Each group has its own action
    /// tree, so "restricted" has no group-independent meaning and cannot be defined here.
    /// </summary>
    Restricted,
}

public enum ParityMode
{
    /// <summary>Write the goldens. Only legal on a tree whose behaviour is the reference.</summary>
    Capture,

    /// <summary>Replay and diff against the committed goldens. Never writes a golden.</summary>
    Verify,
}

/// <summary>
/// One request the runner will issue. Built by the group project from the route catalogue
/// (Bootstrap/RouteCatalog.cs) plus the seed, never from a hand-written URL list - a
/// hand-written list is precisely the list that omits the route that broke.
/// </summary>
public sealed record ParityCase
{
    /// <summary>Stable file-safe name; becomes the golden's filename.</summary>
    public required string Name { get; init; }

    /// <summary>LIST, DETAIL, REVISIONS, ASOF, PRINT, PRINTTOKEN, CREATE, READBACK, UPDATE, REMOVE, GONE.</summary>
    public required string Kind { get; init; }

    public required string Method { get; init; }

    public required string Url { get; init; }

    /// <summary>Request body as JSON text, already built by RequestFactory. Null for GET/DELETE.</summary>
    public string? Body { get; init; }

    /// <summary>
    /// The catalogue route template this case exercises, used for the coverage gate
    /// ("catalogue routes covered: n/n"). Null only for a case that is not route-driven.
    /// </summary>
    public string? RouteKey { get; init; }

    /// <summary>
    /// Set for a case whose URL is only known at run time - READBACK/UPDATE/REMOVE/GONE
    /// against an id the CREATE returned. The runner substitutes {newId} before issuing.
    /// </summary>
    public bool NeedsCreatedId { get; init; }

    /// <summary>
    /// Culture sent as Accept-Language. Rule 6: en-US on every request, plus one extra pass
    /// per group at a second culture, because number and date formatting differences would
    /// otherwise hide inside a single-culture baseline.
    /// </summary>
    public string Culture { get; init; } = "en-US";
}
