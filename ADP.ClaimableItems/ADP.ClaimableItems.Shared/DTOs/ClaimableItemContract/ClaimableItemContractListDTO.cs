using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

public class ClaimableItemContractListDTO : ShiftEntityListDTO, IHasDraftColumn<ClaimableItemContractListDTO>
{
    public override string? ID { get; set; }

    public string VIN { get; set; } = default!;
    public string? ModelDescription { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractType ContractType { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Total { get; set; }
    public decimal TotalWithIncrement { get; set; }

    public bool IsDraft { get; set; }
    public bool IsCancelled { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemContractStatus Status => IsCancelled
        ? ClaimableItemContractStatus.Cancelled
        : IsDraft ? ClaimableItemContractStatus.Draft : ClaimableItemContractStatus.Issued;

    [CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    [CompanyBranchHashIdConverter]
    public string? CompanyBranchID { get; set; }

    [CountryHashIdConverter]
    public string? CountryID { get; set; }
}
