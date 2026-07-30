using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.Cases;

/// <summary>
/// What the case browser needs to render its sidebar, told honestly.
///
/// <para>The distinction between <see cref="Categories"/> and <see cref="RegistryIdentities"/> on
/// one hand and the browsable counts on the other is the whole point of this shape. A surface that
/// counts the cases it happens to hold and presents that as the corpus is wrong by orders of
/// magnitude and looks entirely plausible while doing it — measured on TIQ, where a queue-backed
/// browser reported 13,591 auto-merges against the 1,111,426 the resolve actually made.</para>
/// </summary>
public class CaseSummaryDTO
{
    /// <summary>Source profiles in the registry — the whole tenant corpus.</summary>
    public int RegistryProfiles { get; set; }

    /// <summary>Active identities after resolution.</summary>
    public int RegistryIdentities { get; set; }

    /// <summary>The resolve that produced all of this. Everything here is one run's view.</summary>
    public int RunID { get; set; }

    /// <summary>Per-category corpus totals and how much of each is browsable.</summary>
    public List<CaseCategoryDTO> Categories { get; set; } = new();

    /// <summary>Records staged for browsing (queue + catalog), by source system.</summary>
    public Dictionary<string, int> Sources { get; set; } = new();

    /// <summary>Pairs on the steward's work list. Smaller than the <c>StewardBand</c> category
    /// total: band pairs already merged transitively via a third record are dropped, because there
    /// is nothing left to ask.</summary>
    public int QueueDepth { get; set; }

    public int OpenFlags { get; set; }
    public int Audits { get; set; }
}

/// <summary>One sidebar row.</summary>
public class CaseCategoryDTO
{
    /// <summary>The <c>CaseCat</c> member name.</summary>
    public string Category { get; set; } = default!;

    /// <summary>Exact, corpus-wide.</summary>
    public long Total { get; set; }

    /// <summary>How many of them can actually be opened here. Lower than <see cref="Total"/>
    /// whenever the catalog cap bit — surface both or the cap reads as the population.</summary>
    public int Browsable { get; set; }
}

/// <summary>One row in the case list.</summary>
public class CaseListItemDTO
{
    public string PairKey { get; set; } = default!;
    public float Score { get; set; }

    /// <summary>Category names this pair carries.</summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>Rule names that fired (<c>MatchFlags</c> members) — the short "why".</summary>
    public List<string> Rules { get; set; } = new();

    public CaseSideDTO? A { get; set; }
    public CaseSideDTO? B { get; set; }

    /// <summary>Set when this pair is on the steward's work list, not merely catalogued.</summary>
    public bool Queued { get; set; }

    public string? StandingVerdict { get; set; }
    public bool Flagged { get; set; }
}

/// <summary>One side of a case — the normalized record as the matcher read it.</summary>
public class CaseSideDTO
{
    public string SourceSystem { get; set; } = default!;
    public string SourceRecordId { get; set; } = default!;
    public string? RawName { get; set; }
    public string? NormName { get; set; }
    public List<string> Phones { get; set; } = new();
    public List<string> WeakPhones { get; set; } = new();
    public string? NationalId { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public List<string> Emails { get; set; } = new();
    public bool NameWasMojibake { get; set; }
    public bool NameHadArabizi { get; set; }

    /// <summary>The identity this record resolved into, when the registry has assigned one.</summary>
    public long? IdentityID { get; set; }
}

/// <summary>A case with its derivation — the step-by-step the browser animates.</summary>
public class CaseDetailDTO
{
    public string PairKey { get; set; } = default!;
    public CaseSideDTO? A { get; set; }
    public CaseSideDTO? B { get; set; }

    /// <summary>The score the resolve recorded when it staged this pair.</summary>
    public float StagedScore { get; set; }

    /// <summary>The score the CURRENT engine gives, recomputed from the staged records.</summary>
    public double LiveScore { get; set; }

    /// <summary>True when the two disagree — the engine has changed since the staging resolve.
    /// Surfaced rather than hidden: a steward deciding on a stale score should know.</summary>
    public bool EngineDrift { get; set; }

    /// <summary>The live scorer's own account of how it reached <see cref="LiveScore"/>.</summary>
    public List<TraceStepDTO> Trace { get; set; } = new();

    /// <summary>How each side's raw source text became the text the matcher compared. The first
    /// half of the walkthrough — comparison steps only make sense against normalized values.</summary>
    public List<NormalizeStepDTO> NormalizeA { get; set; } = new();
    public List<NormalizeStepDTO> NormalizeB { get; set; } = new();

    /// <summary>Block keys both records carry — why the engine ever compared them. Recomputed from
    /// the records (block keys are a pure function of one record), not staged.</summary>
    public List<string> SharedBlockKeys { get; set; } = new();

    /// <summary>auto-merge | steward-queue | kept-separate, per the live score.</summary>
    public string Decision { get; set; } = default!;

    /// <summary>Years between the two DOBs when both carry one — the relatives watch-point.</summary>
    public int? DobGapYears { get; set; }

    public List<string> Categories { get; set; } = new();
    public List<string> Rules { get; set; } = new();
    public bool Queued { get; set; }
    public string? StandingVerdict { get; set; }
    public ReviewFlagDTO? Flag { get; set; }
    public List<LabelAuditDTO> Audits { get; set; } = new();

    /// <summary>Set when both records resolved into the same identity.</summary>
    public long? IdentityID { get; set; }
}

/// <summary>One normalization step for a single record.</summary>
public class NormalizeStepDTO
{
    /// <summary>repair | arabizi | name | phone | address.</summary>
    public string Stage { get; set; } = default!;
    public string Detail { get; set; } = default!;
}

/// <summary>One step of the scoring story, in engine execution order.</summary>
public class TraceStepDTO
{
    /// <summary>signal | base | gate | address | conflict | decide — the UI maps these to stations.</summary>
    public string Stage { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Detail { get; set; } = default!;
    public double? ConfidenceAfter { get; set; }
}

/// <summary>One row in the identities list.</summary>
public class IdentityListItemDTO
{
    public long IdentityID { get; set; }
    public string? GoldenName { get; set; }
    public int MemberCount { get; set; }
    public int SourceCount { get; set; }

    /// <summary>Cluster-level category names (<c>ClusterCat</c> members).</summary>
    public List<string> Categories { get; set; } = new();
}

/// <summary>One page of identities.</summary>
public class IdentityPageDTO
{
    public int Total { get; set; }
    public int Skip { get; set; }
    public List<IdentityListItemDTO> Identities { get; set; } = new();
}

/// <summary>How an identity was assembled: its members and the edges that joined them.</summary>
public class IdentityAssemblyDTO
{
    public long IdentityID { get; set; }
    public string? GoldenName { get; set; }
    public int MemberCount { get; set; }
    public int SourceCount { get; set; }
    public List<CaseSideDTO> Members { get; set; } = new();

    /// <summary>The auto-merge edges that built it, strongest first. Corroborating edges are
    /// included: how tightly a cluster holds together is what a steward weighs before splitting.</summary>
    public List<IdentityEdgeDTO> Edges { get; set; } = new();
}

public class IdentityEdgeDTO
{
    public string PairKey { get; set; } = default!;
    public float Score { get; set; }
    public List<string> Rules { get; set; } = new();
}

public class ReviewFlagDTO
{
    public long FlagID { get; set; }
    public string Target { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public string Comment { get; set; } = default!;
    public string Author { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public string? Response { get; set; }
    public string? ResponseBy { get; set; }
    public DateTime? ResponseUtc { get; set; }
    public bool IsOpen => Response is null;
}

public class LabelAuditDTO
{
    public long AuditID { get; set; }
    public string PairKey { get; set; } = default!;
    public string? OldLabel { get; set; }
    public string? NewLabel { get; set; }
    public string AuditedBy { get; set; } = default!;
    public DateTime AuditedUtc { get; set; }
    public string? PanelVotes { get; set; }
    public string? Rationale { get; set; }
    public string Status { get; set; } = default!;
}

/// <summary>Write shapes.</summary>
public class ReviewFlagInputDTO
{
    public string Target { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public string Comment { get; set; } = default!;

    /// <summary>Case detail incl. the trace as it read when flagged. Optional — the server
    /// recomputes and stores one when omitted, so a flag is never evidence-free.</summary>
    public string? Snapshot { get; set; }
}

public class ReviewFlagResponseInputDTO
{
    public string Target { get; set; } = default!;
    public string Response { get; set; } = default!;
}

public class LabelAuditInputDTO
{
    public string PairKey { get; set; } = default!;
    public string? OldLabel { get; set; }

    /// <summary>Empty means "pending an expert call" — recorded, never folded into the gold set.</summary>
    public string? NewLabel { get; set; }

    public string? PanelVotes { get; set; }
    public string? Rationale { get; set; }
}

/// <summary>One page of cases.</summary>
public class CasePageDTO
{
    public int Total { get; set; }
    public int Skip { get; set; }
    public List<CaseListItemDTO> Cases { get; set; } = new();

    /// <summary>True when the filter's category is capped — the page is a sample of a larger
    /// population, and the UI must say so rather than imply completeness.</summary>
    public bool Capped { get; set; }

    /// <summary>Corpus-wide total for the filtered category, when one is filtered.</summary>
    public long? CategoryTotal { get; set; }
}
