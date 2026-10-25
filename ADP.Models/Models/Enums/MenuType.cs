using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// How a service menu variant is offered to the customer. Authored per variant and carried, never priced
/// on: it changes no generated price, so a consumer that renders "free" or "under contract" instead of a
/// total decides that for itself.
///
/// <para>Zero is <see cref="Paid"/> so a variant that never set it — a default-constructed object, or a
/// stored row or document written before the field existed — reads as paid. That is the safe direction:
/// <see cref="Free"/> is the claim that gives something away.</para>
///
/// <para>Serialized as a string, so the value reads the same on every endpoint.</para>
/// </summary>
[Docable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MenuType
{
    /// <summary>The customer pays for the menu. The default.</summary>
    Paid = 0,

    /// <summary>The menu is offered free of charge.</summary>
    Free = 1,

    /// <summary>The menu is covered by a service contract.</summary>
    Contract = 2,
}
