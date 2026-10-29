using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

/// <summary>
/// A contract-type line from the vehicle's service menu, as offered for a contract. Picking it copies
/// these members onto a <see cref="ClaimableItemContractLineDTO"/>, with <see cref="Price"/> as its cost.
/// </summary>
public class ClaimableItemContractMenuLineDTO
{
    /// <summary>The menu line's stable identity; the menu code's text depends on the language.</summary>
    public string MenuLineKey { get; set; } = default!;
    public string? MenuCode { get; set; }
    public string? LabourCode { get; set; }
    public long? VariantID { get; set; }
    public string? VariantName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractLineType LineType { get; set; }

    public string? ServiceIntervalCode { get; set; }

    /// <summary>The km the service is due at. Kilometres, despite the name, as on the menu line.</summary>
    public int? ServiceIntervalValueInMeter { get; set; }

    public string? Description { get; set; }

    public decimal LabourTotalPrice { get; set; }
    public decimal PartsTotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>What the customer pays for the service on the menu, after discount.</summary>
    public decimal Price { get; set; }

    public bool HasUnpricedParts { get; set; }

    /// <summary>Null when the line can be put on the contract.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractMenuLineUnavailableReason? UnavailableReason { get; set; }
}
