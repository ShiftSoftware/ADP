using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>Defines how a collection-backed condition selects source entries.</summary>
[Docable]
public class EligibilityConditionScope
{
    /// <summary>
    /// The collection selection strategy. This is the difference between "is this the case right
    /// now" and "has this ever happened", so it decides whether a condition about something the
    /// vehicle has done keeps matching once newer entries arrive.
    /// </summary>
    public EligibilityConditionSelection Selection { get; set; }

    /// <summary>
    /// The number of latest entries that take part in the comparison. Required by
    /// <see cref="EligibilityConditionSelection.Latest"/> and must be omitted by every other
    /// selection, which fails closed rather than quietly disregarding a number the author wrote.
    /// </summary>
    public int? Count { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EligibilityConditionOperator
{
    ContainsAll,
    Equals
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EligibilityConditionValueMatch
{
    /// <summary>Case-insensitive ordinal comparison of the whole value.</summary>
    Exact = 0,

    /// <summary>Case-insensitive ordinal suffix comparison, with no trimming or tokenizing.</summary>
    EndsWith = 1,

    /// <summary>
    /// Read the service milestone out of the value and compare it numerically, so the configured
    /// values are mileages rather than text. String matching cannot express this: a milestone is a
    /// number that a deployment writes into a longer code, and the same service can be written more
    /// than one way. Requires <see cref="EligibilityConditionModel.Qualifier"/> to say which of
    /// those writings count.
    /// </summary>
    Milestone = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EligibilityConditionSelection
{
    /// <summary>
    /// The most recent <see cref="EligibilityConditionScope.Count"/> entries. This is a moving
    /// window, so it answers "is this the case right now" — an entry that satisfies the condition
    /// today drops out of the window as soon as newer entries arrive.
    /// </summary>
    Latest = 0,

    /// <summary>
    /// Every entry, however old, and <see cref="EligibilityConditionScope.Count"/> must be omitted.
    /// This answers "has this ever happened", which is what a condition about a milestone the
    /// vehicle has passed needs: an award made for reaching it must not be withdrawn by the
    /// vehicle's next visit.
    /// </summary>
    All = 1,
}
