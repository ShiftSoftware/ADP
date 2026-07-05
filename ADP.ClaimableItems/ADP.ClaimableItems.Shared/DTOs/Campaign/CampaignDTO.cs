using FluentValidation;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;

public class CampaignDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
    public string? UniqueReference { get; set; } = default!;
    public DateTime? StartDate { get; set; }
    public DateTime? ExpireDate { get; set; }

    public string? CertificatePrintoutHeader { get; set; }
    public string? CertificatePrintoutBody { get; set; }
    public bool CertificatePrintoutSignStampVisibility { get; set; }
    public bool CertificatePrintoutKatashikiAndModelColumnVisibility { get; set; }
    public string? DistributorCertificateNumberPrefix { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public List<ShiftEntitySelectDTO> Brands { get; set; } = new();


    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public List<ShiftEntitySelectDTO> Companies { get; set; } = new();


    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public List<ShiftEntitySelectDTO> Countries { get; set; } = new();


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemCampaignActivationTrigger ActivationTrigger { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemCampaignActivationTypes ActivationType { get; set; }

    public ShiftEntitySelectDTO? VehicleInspectionType { get; set; }
}

public class CampaignValidator : AbstractValidator<CampaignDTO>
{
    public CampaignValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.");


        RuleFor(x => x.ActivationTrigger)
            .NotEmpty()
            .WithMessage("Required.");

        RuleFor(x => x.ActivationType)
            .NotEmpty()
            .WithMessage("Required.");

        RuleFor(x => x.VehicleInspectionType)
            .NotEmpty()
            .WithMessage("Required.")
            .When(x => x.ActivationTrigger == ClaimableItemCampaignActivationTrigger.VehicleInspection);
    }
}
