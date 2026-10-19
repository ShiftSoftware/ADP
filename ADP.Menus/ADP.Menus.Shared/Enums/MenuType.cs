using System.ComponentModel;

namespace ShiftSoftware.ADP.Menus.Shared.Enums;

/// <summary>
/// How a menu variant is offered to the customer. A label only — it changes no generated price.
///
/// <para>Mirrors <c>ShiftSoftware.ADP.Models.Enums.MenuType</c>, which the replicated documents and the
/// generator carry. It is redeclared here rather than referenced because this project ships to the
/// Blazor client and ADP.Models would drag its dependencies there; the two must keep the same names and
/// values, and a test holds them to it.</para>
///
/// <para>Zero is <see cref="Paid"/>, so a variant that never set it reads as paid.</para>
/// </summary>
public enum MenuType
{
    [Description("Paid")]
    Paid = 0,

    [Description("Free")]
    Free = 1,

    [Description("Contract")]
    Contract = 2,
}
