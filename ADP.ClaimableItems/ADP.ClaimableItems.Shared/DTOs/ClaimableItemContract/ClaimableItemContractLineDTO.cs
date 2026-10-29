using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

/// <summary>
/// One menu service on a contract. The menu members are a snapshot of the menu line the user picked,
/// identified by <see cref="MenuLineKey"/>.
/// </summary>
public class ClaimableItemContractLineDTO
{
    public long ID { get; set; }

    /// <summary>
    /// The menu line's stable identity. The menu code is not used for this: its text depends on the
    /// language the menu was read in.
    /// </summary>
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

    /// <summary>The menu price of the service.</summary>
    public decimal Cost { get; set; }

    /// <summary>Server: <see cref="Cost"/> × (1 + the contract's price increment).</summary>
    public decimal PriceWithIncrement { get; set; }

    /// <summary>Periodic lines only, and required on them. Standalone lines run for the whole contract.</summary>
    public int? Duration { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractDurationUnit? DurationUnit { get; set; }

    /// <summary>Server: set when the contract is issued.</summary>
    public DateTime? ExpireDate { get; set; }

    // Server: set by the cancel action, never by a save.
    public bool IsCancelled { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }

    public int Sort { get; set; }
}
