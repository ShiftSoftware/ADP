using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// Immutable snapshot per publish. Stores the fully-resolved <c>SurveyDto</c> JSON
/// (all <c>bankRef</c> / <c>templateRef</c> expanded inline per Decision #11). Instances
/// pin to a specific version — editing the draft or renaming bank keys never affects
/// in-flight responses' validation.
/// </summary>
public class SurveyVersion : ShiftEntity<SurveyVersion>
{
    public long SurveyID { get; set; }
    public Survey Survey { get; set; } = default!;

    /// <summary>Monotonic within a <see cref="Survey"/>. Unique on (SurveyID, Version).</summary>
    public int Version { get; set; }

    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>Fully-resolved <c>SurveyDto</c> serialized via <c>SurveySchemaSerializer.Options</c>.</summary>
    public string ResolvedJson { get; set; } = default!;

    /// <summary>SHA-256 of <see cref="ResolvedJson"/>, hex-encoded. Lets us skip a re-publish if nothing meaningful changed.</summary>
    public string Hash { get; set; } = default!;

    public virtual ICollection<SurveyInstance> Instances { get; set; } = new HashSet<SurveyInstance>();
}
