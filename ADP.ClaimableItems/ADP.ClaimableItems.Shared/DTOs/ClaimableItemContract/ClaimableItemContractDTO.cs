using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

/// <summary>
/// A service contract for one VIN. Members marked "server" are worked out by the repository on every
/// save; whatever a client sends for them is overwritten.
/// </summary>
public class ClaimableItemContractDTO : ShiftEntityViewAndUpsertDTO, IHasDraftCheckBox<ClaimableItemContractDTO>
{
    public override string? ID { get; set; }

    public string VIN { get; set; } = default!;

    // Vehicle snapshot, as the lookup described the vehicle when the contract was written.
    public string? Katashiki { get; set; }
    public string? BasicModelCode { get; set; }
    public string? Variant { get; set; }
    public string? ModelCode { get; set; }
    public string? ModelDescription { get; set; }

    [BrandHashIdConverter]
    public ShiftEntitySelectDTO? Brand { get; set; }

    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractType ContractType { get; set; }

    /// <summary>Odometer reading in km when the contract is written.</summary>
    [Required]
    public long? Mileage { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    /// <summary>The contract's length. Standalone and group lines are valid for all of it.</summary>
    [Required]
    public int? Duration { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractDurationUnit? DurationUnit { get; set; }

    /// <summary>Server: <see cref="StartDate"/> + <see cref="Duration"/>.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Scales the menu's consumable charge. Passed to the menu lookup with the country.</summary>
    public decimal TransferRate { get; set; } = 1m;

    public string? MenuLanguage { get; set; }

    /// <summary>A fraction, 0 to 0.10. Added on top of each line's cost to give the customer price.</summary>
    public decimal PriceIncrement { get; set; }

    /// <summary>Server: the sum of the lines' <see cref="ClaimableItemContractLineDTO.Cost"/>.</summary>
    public decimal Total { get; set; }

    /// <summary>Server: the sum of the lines' <see cref="ClaimableItemContractLineDTO.PriceWithIncrement"/>.</summary>
    public decimal TotalWithIncrement { get; set; }

    public bool? IsDraft { get; set; }

    /// <summary>Server: when the contract was issued, in UTC. Set by issuing, never by a save.</summary>
    public DateTime? IssuedDate { get; set; }

    // Server: set by the cancel action, never by a save.
    public bool IsCancelled { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancelReason { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractStatus Status => IsCancelled
        ? ClaimableItemContractStatus.Cancelled
        : IsDraft == false ? ClaimableItemContractStatus.Issued : ClaimableItemContractStatus.Draft;

    /// <summary>The country whose menu prices the contract uses.</summary>
    [CountryHashIdConverter]
    public ShiftEntitySelectDTO? Country { get; set; }

    [CompanyHashIdConverter]
    public ShiftEntitySelectDTO? Company { get; set; }

    [CompanyBranchHashIdConverter]
    public ShiftEntitySelectDTO? CompanyBranch { get; set; }

    public List<ClaimableItemContractLineDTO> Lines { get; set; } = new();
}
