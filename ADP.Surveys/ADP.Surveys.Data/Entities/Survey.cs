using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// One row per logical survey (across all its versions). Holds the currently-editable
/// draft as a JSON blob (the full <c>SurveyDto</c>) plus metadata. Published snapshots
/// live in <see cref="SurveyVersion"/>.
///
/// Why draft-as-JSON: the authored schema is polymorphic over 14 question types, two
/// screen forms, and three logic condition shapes. Re-normalizing that into 20+ SQL
/// tables buys us no query advantage because drafts are always loaded / saved as a whole
/// unit by the builder; BI queries run against published versions and the response tables,
/// not drafts.
/// </summary>
[TemporalShiftEntity]
public class Survey : ShiftEntity<Survey>
{
    /// <summary>Human-readable survey name — shown in the builder's list view.</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The editable <c>SurveyDto</c> serialized via <c>SurveySchemaSerializer.Options</c>.
    /// Empty string while the survey has not yet been populated by the builder.
    /// </summary>
    public string DraftJson { get; set; } = "";

    /// <summary>Highest version number that has been published. Null before first publish.</summary>
    public int? PublishedVersionNumber { get; set; }

    public virtual ICollection<SurveyVersion> Versions { get; set; } = new HashSet<SurveyVersion>();
    public virtual ICollection<SurveyInstance> Instances { get; set; } = new HashSet<SurveyInstance>();
}
