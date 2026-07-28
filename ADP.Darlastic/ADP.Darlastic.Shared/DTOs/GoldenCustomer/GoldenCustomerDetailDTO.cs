namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// One resolved identity, by ID — the survived attributes a consumer renders as "the customer".
///
/// <para>Unlike the list (which serves the <c>GoldenCustomer</c> view), this reads the staged
/// payload straight out of <c>ProjectionState</c> on its clustered primary key. That is the whole
/// reason a by-ID read is cheap here while it was refused on the sources endpoint: filtering the
/// view by id means <c>CAST(ArtifactKey AS bigint)</c>, which is non-SARGable and OPENJSONs the
/// entire golden slice to return one row. Seeking <c>(ArtifactType, ArtifactKey)</c> touches
/// exactly one page.</para>
///
/// <para><b><see cref="AwaitingResolve"/> is the point of this DTO.</b> In a batch-first system an
/// interactive create mints an identity immediately but no golden until the next resolve run —
/// the identity has a row in <c>Identity</c> and a <c>SourceProfile</c> binding, and nothing in
/// <c>ProjectionState</c>, so it is absent from the golden view entirely rather than merely blank.
/// Returning 404 there would be wrong: the identity exists, it just has no survived attributes yet.
/// So this endpoint answers <i>"yes, and here is the state it's in"</i>, and the caller renders its
/// own record's values meanwhile.</para>
///
/// <para>Why the caller and not this endpoint supplies those values: the registry does not store
/// them. <c>SourceProfile</c> carries identity assignment and a content hash, never attributes —
/// only the originating host's own table holds a just-created record's name and phone. That is a
/// deliberate boundary, not a gap to close by teaching this endpoint about host tables.</para>
/// </summary>
public class GoldenCustomerDetailDTO
{
    /// <summary>The LIVE identity ID — after redirect-chasing. The cross-system GoldenCustomerID.</summary>
    public string? ID { get; set; }

    /// <summary>
    /// The ID the caller asked for. Differs from <see cref="ID"/> when that identity was merged
    /// into another: a merge redirects and never deletes, so a stale ID stays resolvable forever.
    /// A consumer holding the old ID can notice and update its own reference.
    /// </summary>
    public string? RequestedID { get; set; }

    /// <summary>True when the requested ID was merged away — <see cref="ID"/> is its survivor.</summary>
    public bool WasRedirected { get; set; }

    /// <summary>
    /// The identity exists but has no staged golden yet — minted interactively since the last
    /// resolve run. Every attribute below will be null; render the originating record's own values
    /// and tell the operator this identity has not been unified yet.
    /// </summary>
    public bool AwaitingResolve { get; set; }

    public IdentityStatus Status { get; set; }

    // Survived attributes, extracted from the staged payload with the same last-wins-per-type rule
    // the Cosmos drain uses — so this endpoint, the view and the drained documents never disagree.
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? IDNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>How many source records this identity unifies. 0 while awaiting resolve.</summary>
    public int SourceCount { get; set; }

    /// <summary>The resolve run that minted this identity, and the last one that changed it.</summary>
    public int CreatedRunID { get; set; }
    public int LastChangedRunID { get; set; }
}
