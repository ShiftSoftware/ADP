using ShiftSoftware.ADP.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Why an item is on screen without being claimable, and what the customer would have to do about it.
/// <para>
/// Present only on items that are not being offered. An item absent from this block is an ordinary
/// one — whether it is claimable is <see cref="VehicleServiceItemDTO.Claimable"/>, as it always was.
/// </para>
/// <para>
/// Deliberately not a new <c>StatusEnum</c> member. Status drives ordering, expiry and mileage
/// sequencing throughout the evaluator, so a new member there would change behaviour far from where
/// it was added. This block only describes.
/// </para>
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceItemLockDTO
{
    /// <summary>Which of the two unclaimable states this item is in.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehicleServiceItemLockState State { get; set; }

    /// <summary>
    /// The services the customer must have had for this item to unlock, each with whether it has
    /// happened. Empty when the item is unclaimable for a reason that decomposes into no steps.
    /// </summary>
    public List<VehicleServiceItemPrerequisiteDTO> Prerequisites { get; set; } = new();
}
