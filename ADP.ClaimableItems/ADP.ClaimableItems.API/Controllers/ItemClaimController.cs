using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.ClaimableItems.API.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Repositories;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.Localization;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;
using System.Text;

namespace ShiftSoftware.ADP.ClaimableItems.API.Controllers;

/// <summary>
/// The item-claim controller, including the production hot path <c>POST {prefix}/ItemClaim/claim</c>
/// that the ADP vehicle-item-claim-form web component submits to. Moved from the original host application (Phase 2 Slice 5)
/// with the wire contract FROZEN: multipart <c>payload</c> (LookupServices ItemClaimDTO JSON) +
/// repeated <c>document</c> files; 200 → <c>{Success, ID}</c>; error → <c>{Success:false, Message}</c>;
/// HMAC signature validated against the host's <c>ADPSigningSecreteKey</c> configuration value.
/// Org-specific concerns are consumer seams: <see cref="IItemClaimQRValidator"/> (QR/voucher policy)
/// and <see cref="IItemClaimFailureLogger"/> (failed-claim persistence).
/// </summary>
[Route("[controller]")]
public class ItemClaimController : ShiftEntitySecureControllerAsync<ItemClaimRepository, ItemClaim, ItemClaimListDTO, ItemClaimDTO>
{
    private readonly IConfiguration configuration;
    private readonly IStringLocalizer<ClaimableItemsResource> loc;
    private readonly AzureStorageService azureStorageService;
    private readonly ItemClaimRepository repository;
    private readonly ITypeAuthService typeAuthService;
    private readonly IOptions<ClaimableItemsApiOptions> options;
    private readonly IItemClaimQRValidator? qrValidator;
    private readonly IItemClaimFailureLogger? failureLogger;

    public ItemClaimController(
        IConfiguration configuration,
        IStringLocalizer<ClaimableItemsResource> loc,
        AzureStorageService azureStorageService,
        ItemClaimRepository itemClaimRepository,
        ITypeAuthService typeAuthService,
        IOptions<ClaimableItemsApiOptions> options,
        IItemClaimQRValidator? qrValidator = null,
        IItemClaimFailureLogger? failureLogger = null
    ) : base(options.Value.EnableClaimableItemsActionTreeAuthorization ? options.Value.ClaimingAction : null)
    {
        this.configuration = configuration;
        this.loc = loc;
        this.azureStorageService = azureStorageService;
        this.repository = itemClaimRepository;
        this.typeAuthService = typeAuthService;
        this.options = options;
        this.qrValidator = qrValidator;
        this.failureLogger = failureLogger;
    }

    // Voucher QR codes are typed on Cyrillic keyboard layouts in the field — normalize the hex
    // characters + separators. Harmless for correctly-typed input; kept in the module bug-for-bug.
    private static readonly Dictionary<char, char> HexInputNormalizationMap = new()
    {
        ['ф'] = 'a',
        ['и'] = 'b',
        ['с'] = 'c',
        ['в'] = 'd',
        ['у'] = 'e',
        ['а'] = 'f',

        ['б'] = ',',
        ['ю'] = ','
    };

    public static string NormalizeHexQrInput(string input)
    {
        var sb = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            if (HexInputNormalizationMap.TryGetValue(ch, out var mapped))
            {
                sb.Append(mapped);
            }
            else
            {
                sb.Append(ch); // Accept a-z, 0-9, commas that are already correct
            }
        }
        return sb.ToString();
    }

    [HttpPost("claim")]
    public async Task<ActionResult<ShiftEntityResponse<ItemClaimDTO>>> Claim([FromForm] string payload, [FromForm] IFormFile[] document)
    {
        // The original endpoint carried [TypeAuth(..., Claiming, Write)] — the node is consumer-owned
        // (D9), so the equivalent check runs imperatively against the options-supplied action.
        if (options.Value.EnableClaimableItemsActionTreeAuthorization &&
            options.Value.ClaimingAction is not null &&
            !typeAuthService.Can(options.Value.ClaimingAction, Access.Write))
        {
            return Forbid();
        }

        try
        {
            var claimDTO = System.Text.Json.JsonSerializer.Deserialize<ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup.ItemClaimDTO>(
                payload,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;


            if (claimDTO.QRCode is not null)
                claimDTO.QRCode = NormalizeHexQrInput(claimDTO.QRCode!).ToLower();

            if (DateTime.UtcNow > claimDTO.ServiceItem.SignatureExpiry)
            {
                return Unauthorized(new { Success = false, Message = loc["Expired Signature. Please reload the items and claim again."].Value });
            }

            var adpSecreteSigningKey = this.configuration.GetValue<string>("ADPSigningSecreteKey")!;

            if (!claimDTO.ServiceItem!.ValidateSignature(claimDTO.VIN, adpSecreteSigningKey))
            {
                return Unauthorized(new { Success = false, Message = loc["Invalid signature."].Value });
            }

            if (!string.IsNullOrWhiteSpace(claimDTO.ServiceItem?.VehicleInspectionID))
            {
                // Inspection-based claims carry a voucher QR whose validity rules are org policy —
                // delegated to the consumer's validator. Fail closed when none is registered.
                if (this.qrValidator is null)
                    throw new InvalidOperationException(
                        "An inspection-based claim was received but no IItemClaimQRValidator is registered. Register one in the consumer's DI.");

                var qrError = await this.qrValidator.ValidateAsync(claimDTO, adpSecreteSigningKey);

                if (qrError is not null)
                {
                    return Unauthorized(new { Success = false, Message = qrError });
                }
            }

            List<ShiftFileDTO> attachments = new();

            foreach (var doc in document)
            {
                // Handle the file as needed (example: read to memory)
                if (doc != null && doc.Length > 0)
                {
                    using var stream = new MemoryStream();

                    await doc.CopyToAsync(stream);

                    var blobName = await this.azureStorageService.UploadAsync(
                        fileName: doc.FileName,
                        stream: stream
                    );

                    attachments.Add(new ShiftFileDTO
                    {
                        Name = doc.FileName,
                        ContentType = doc.ContentType,
                        Size = doc.Length,
                        Blob = blobName,
                    });
                }
            }

            var dto = new ItemClaimDTO
            {
                ClaimDate = DateTimeOffset.UtcNow,
                VIN = claimDTO.VIN!,
                InvoiceNumber = claimDTO.Invoice,
                JobNumber = claimDTO.JobNumber,
                QRCode = claimDTO.QRCode,
                Cost = claimDTO.ServiceItem?.Cost ?? 0m,
                ClaimableItem = new ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntitySelectDTO { Value = claimDTO.ServiceItem!.ServiceItemID.ToString() },
                PackageCode = claimDTO.ServiceItem!.PackageCode,
                Campaign = new ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntitySelectDTO { Value = claimDTO.ServiceItem!.CampaignID!.ToString()! },
                VehicleInspectionResult = claimDTO.ServiceItem?.VehicleInspectionID is null ? null : new ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntitySelectDTO { Value = claimDTO.ServiceItem!.VehicleInspectionID!.ToString() },
                Attachments = attachments,
                ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.PendingProcess,
                ModelDescription = $"{claimDTO.VehicleSpecification?.ModelCode}{(claimDTO.VehicleSpecification?.ModelDescription is null ? "" : $" - {claimDTO.VehicleSpecification?.ModelDescription}")}",
                Katashiki = claimDTO.Identifiers?.Katashiki,
                CampaignVINEntry = claimDTO.ServiceItem?.CampaignVinEntryID is null ? null : new ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntitySelectDTO { Value = claimDTO.ServiceItem!.CampaignVinEntryID!.ToString() },
            };

            if (dto.VehicleInspectionResult is null && dto.CampaignVINEntry is null && string.IsNullOrWhiteSpace(dto.Katashiki))
            {
                //This should never happen, as ADP calculates eligibility based on either Katashiki, and the user serves the same item details back to this endpoint.
                //But we have seen some edge cases in the field where free service items come without Katashiki. To avoid that, we add this check.
                throw new Exception("Unknown edge case: Free Service Items coming without Katashiki");
            }

            try
            {
                var item = await this.repository.UpsertAsync(
                    entity: new(),
                    dto: dto,
                    actionType: ShiftSoftware.ShiftEntity.Core.ActionTypes.Insert,
                    userId: this.HttpContext.GetUserID(),
                    idempotencyKey: null,
                    disableDefaultDataLevelAccess: false,
                    disableGlobalFilters: false
                );

                this.repository.Add(item);

                await this.repository.SaveChangesAsync();

                var tokenResult = await this.PrintToken(item.ID.ToString()) as OkObjectResult;

                return Ok(new
                {
                    Success = true,
                    ID = item.ID.ToString(),
                    //PrintURL intentionally not returned — matches the deployed host behavior (it was
                    //commented out there; the web component's print box only fires when it's present).
                });
            }
            catch (ShiftEntityException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
        catch (Exception ex)
        {
            if (this.failureLogger is not null)
                await this.failureLogger.LogAsync(payload, document.Select(d => (object)new { d.FileName, d.ContentType, d.Length }), ex);

            return BadRequest(new { Success = false, Message = "Something went wrong. The issue is logged and a technician will investigate it." });
        }
    }

    [HttpPost("UpdateStatus/{actionType}/{inputText?}")]
    public async Task<IActionResult> ProcessBatchAction([FromBody] ShiftSoftware.ShiftEntity.Model.Dtos.SelectStateDTO<ItemClaimListDTO> selectedItems, [FromRoute] ShiftSoftware.ADP.Cases.Shared.Enums.UpdateStatusActionTypes actionType, [FromRoute] string? inputText)
    {
        var items = await this.GetSelectedEntitiesAsync(selectedItems);

        try
        {
            await this.repository.UpdateClaimStatusAsync(items, actionType, inputText);

            // NOTE: the original host implementation wrapped the response as WarrantyClaimListDTO instances (a wire
            // quirk); the Entity payload is a list of default-valued DTOs the client never reads.
            // The module returns default ItemClaimListDTOs instead — reviewed micro-deviation.
            return Ok(new ShiftEntityResponse<IEnumerable<ItemClaimListDTO>>()
            {
                Entity = items.Select(x => new ItemClaimListDTO { })
            });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<ItemClaimDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }
}
