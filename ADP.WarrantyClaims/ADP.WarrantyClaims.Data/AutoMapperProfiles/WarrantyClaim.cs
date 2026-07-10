using AutoMapper;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.AutoMapperProfiles;

public class WarrantyClaim : Profile
{
    public WarrantyClaim()
    {

        CreateMap<Entities.WarrantyClaim?, WarrantyClaimListDTO>()
            .ForMember(
                x => x.ProcessDate,
                x => x.MapFrom(y => y!.ProcessDate.HasValue ? new DateTimeOffset(y.ProcessDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
            )
            .ForMember(
                x => x.DistributorProcessDate,
                x => x.MapFrom(y => y!.DistributorProcessDate.HasValue ? new DateTimeOffset(y.DistributorProcessDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
            )
            .ForMember(
                x => x.HasAttachment,
                x => x.MapFrom(y => y!.Attachments == null || y!.Attachments == "[]" ? YesNoOptions.No : YesNoOptions.Yes)
            )
            // Before the generic rename this member carried the org claim-number name and AutoMapper
            // flattening (nav name + the pre-rename claim-number property) populated it by convention. The renamed
            // member no longer decomposes to a valid path (ReferenceWarrantyClaim + Number), so the
            // flattening is pinned explicitly.
            .ForMember(
                x => x.ReferenceWarrantyClaimNumber,
                x => x.MapFrom(y => y!.ReferenceWarrantyClaim!.ClaimNumber)
            );

        CreateMap<Entities.WarrantyClaim, WarrantyCertificateLineDTO>()
            .ForMember(x => x.WarrantyClaim, x => x.MapFrom(x => new ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntitySelectDTO
            {
                Value = x.ID.ToString(),
                Text = x.ClaimNumber.ToString()
            }));

        // Certificate moved to ADP.Cases.Data (Phase 2 Slice 4). The module entity has no claim
        // collections (the old ForMember Ignores are gone with them); the certificate repositories
        // populate CertificateDTO.WarrantyClaims / ReimbursementItemClaims in their ViewAsync
        // overrides (the entity->DTO name-match that used to fill them no longer has a source).
        CreateMap<CertificateDTO, Certificate>()
            .DefaultDtoToEntityAfterMap()
            .ReverseMap()
            .DefaultEntityToDtoAfterMap();

        CreateMap<WarrantyClaimLaborLine, WarrantyClaimLaborLineDTO>().ReverseMap();
        CreateMap<WarrantyClaimSubletLine, WarrantyClaimSubletLineDTO>().ReverseMap();
        CreateMap<WarrantyClaimPartLine, WarrantyClaimPartLineDTO>().ReverseMap();

        // NOTE: the Financial list maps (entity -> Distributor/DealerFinancialListDTO) live in the
        // Financial profile in this folder (moved from the host consumer in Phase 3 Slice 3.5, D23).

        CreateMap<Entities.WarrantyClaim, WarrantyClaimModel>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.ID.ToString()))
            .ForMember(dest => dest.ClaimStatus, opt => opt.MapFrom(src => src.ClaimStatus!.Value))
            .ForMember(dest => dest.VIN, opt => opt.MapFrom(src => src.VIN))
            .ForMember(dest => dest.DateOfReceipt, opt => opt.MapFrom(src => src.DateOfReceipt))
            //.ForMember(dest => dest.Brand, opt => opt.MapFrom(src =>
            //    src.Franchise == Franchises.Toyota.Key
            //        ? ShiftSoftware.ADP.Models.Enums.Brands.Toyota
            //        : ShiftSoftware.ADP.Models.Enums.Brands.Lexus))
            .ForMember(dest => dest.DealerClaimNumber, opt => opt.MapFrom(src => src.DealerClaimNo))
            .ForMember(dest => dest.CompanyID, opt => opt.MapFrom(src => src.CompanyID))
            .ForMember(dest => dest.ClaimNumber, opt => opt.MapFrom(src => src.ClaimNumber))
            .ForMember(dest => dest.DeliveryDate, opt => opt.MapFrom(src => src.DeliveryDate))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.ProcessDate, opt => opt.MapFrom(src => src.ProcessDate))
            .ForMember(dest => dest.Odometer, opt => opt.MapFrom(src => src.Odometer))
            .ForMember(dest => dest.RepairDate, opt => opt.MapFrom(src => src.RepairDate))
            .ForMember(dest => dest.BrandID, opt => opt.MapFrom(src =>
                src.Franchise == Franchises.Toyota.Key ? 2 : 3))
            .ForMember(dest => dest.DistributorComment, opt => opt.MapFrom(src => src.DistComment1))
            .ForMember(dest => dest.RepairCompletionDate, opt => opt.MapFrom(src => src.RepairCompletionDate))
            .ForMember(dest => dest.WarrantyType, opt => opt.MapFrom(src => src.WarrantyType))
            .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.InvoiceNo))
            .ForMember(dest => dest.RepairOrderNumber, opt => opt.MapFrom(src => src.RepairOrderNo))
            .ForMember(dest => dest.LaborOperationNumberMain, opt => opt.MapFrom(src => src.LaborOperationNoMain))
            .ForMember(dest => dest.ManufacturerStatus, opt => opt.MapFrom(src => src.ManufacturerStatus!.Value))
            .ForMember(dest => dest.DistributorProcessDate, opt => opt.MapFrom(src => src.DistributorProcessDate))
            .ForMember(dest => dest.LaborLines, opt => opt.MapFrom(src =>
                src.WarrantyClaimLaborLines.Select(y => new ShiftSoftware.ADP.Models.Vehicle.WarrantyClaimLaborLineModel
                {
                    DistributorHour = y.DistributorHour,
                    Hour = y.Hour,
                    ID = y.ID,
                    LaborCode = y.OperationNumber,
                    MainOperation = y.MainOperation,
                    PayCode = y.PayCode
                })));
    }
}
