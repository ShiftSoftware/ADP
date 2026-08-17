using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// What an unmet condition means for the item it gates — whether the customer is being told
/// something, or whether the item was never theirs to see.
/// <para>
/// Declared per condition rather than inferred, because the two readings look identical from
/// inside the evaluator and only the author knows which applies. The default hides the item, so a
/// condition written before this property existed, or by an author who has not thought about it,
/// behaves exactly as it always did.
/// </para>
/// </summary>
[Docable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EligibilityConditionUnmetBehavior
{
    /// <summary>
    /// The item is not offered and nothing is shown. Correct whenever the condition states a fixed
    /// fact about the vehicle — a different brand, a different market, a different programme — where
    /// a card would advertise something the customer can never have, and can leak another market's
    /// catalog onto this dealer's screen.
    /// </summary>
    Hide = 0,

    /// <summary>
    /// The item is shown locked and unclaimable, with the outstanding prerequisites named. Correct
    /// only when the customer can still satisfy the condition by doing something.
    /// </summary>
    Lock = 1,

    /// <summary>
    /// The item is shown as missed: it was available and the window has closed. Distinct from
    /// <see cref="Lock"/> because there is nothing left to do about it, and distinct from
    /// <see cref="Hide"/> because saying so is the point — an item that silently disappears is the
    /// confusion this exists to remove.
    /// </summary>
    Miss = 2,
}
