using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;

public class ReplacementItemDTO : ShiftEntityViewAndUpsertDTO
{
    [ReplacementItemHashId]
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string FriendlyName { get; set; } = default!;

    [Required]
    [Range(1,3, ErrorMessage ="Please select a correct value.")]
    public ReplacementItemType Type { get; set; }

    public bool AllowMultiplePartNumbers { get; set; }

    public decimal? DefaultPartPriceMarginPercentage { get; set; }

    [Required]
    public string StandaloneOperationCode { get; set; } = default!;

    [Required]
    public string StandaloneLabourCode { get; set; } = default!;

    [StandaloneReplacementItemGroupHashId]
    public ShiftEntitySelectDTO? StandaloneReplacementItemGroup { get; set; } = default!;

    public IEnumerable<ServiceIntervalGroupReplacaementItemDTO> ServiceIntervalGroups { get; set; }
}

public class ServiceIntervalGroupReplacaementItemDTO
{
    [ServiceIntervalGroupHashId]
    public string ID { get; set; }

    public ServiceIntervalGroupReplacaementItemDTO()
    {
        
    }

    public ServiceIntervalGroupReplacaementItemDTO(string id)
    {
        ID = id;
    }
}
