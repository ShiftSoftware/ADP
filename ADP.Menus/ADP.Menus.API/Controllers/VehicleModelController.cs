using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class VehicleModelController : ShiftEntitySecureControllerAsync<VehicleModelRepository, VehicleModel, VehicleModelListDTO, VehicleModelDTO>
{
    private readonly VehicleModelRepository vehicleModelRepository;
    private readonly IHashIdService hashIdService;
    private readonly MenuApiOptions options;

    public VehicleModelController(
            VehicleModelRepository vehicleModelRepository,
            IHashIdService hashIdService,
            IOptions<MenuApiOptions> options
        ) : base(options.Value.EnableMenuActionTreeAuthorization ? MenuActionTree.VehicleModels : null)
    {
        this.vehicleModelRepository = vehicleModelRepository;
        this.hashIdService = hashIdService;
        this.options = options.Value;
    }

    [HttpGet("GetById/{key}")]
    [Authorize]
    public async Task<ActionResult<VehicleModelMenuDTO>> GetById(string key)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanRead(MenuActionTree.VehicleModels))
                return Forbid();
        }

        var id = hashIdService.Decode<VehicleModelDTO>(key);

        var vehicleModel = await (await vehicleModelRepository.GetIQueryable(null, null, false, false)).AsNoTracking()
            .Where(x => x.ID == id)
            .Include(x => x.ReplacementItemVehicleModels!).ThenInclude(x => x.ReplacementItem).ThenInclude(x => x.StandaloneReplacementItemGroup)
            .Include(x => x.ReplacementItemVehicleModels!).ThenInclude(x => x.DefaultParts)
            .Include(x => x.LabourDetails)
            .Include(x => x.LabourRates)
            .FirstOrDefaultAsync();

        if (vehicleModel is null)
            return NotFound($"Can't find vehicle model");

        var result = new VehicleModelMenuDTO
        {
            ReplacementItems = vehicleModel.ReplacementItemVehicleModels?.Where(x => !x.IsDeleted).Select(x => new MenuReplacementItemDTO
            {
                ID = x.ReplacementItemID.ToString(),
                Name = x.ReplacementItem.Name,
                Type = x.ReplacementItem.Type,
                AllowMultiplePartNumbers = x.ReplacementItem.AllowMultiplePartNumbers,
                StandaloneAllowedTime = x.StandaloneAllowedTime,
                DefaultPartPriceMarginPercentage = x.DefaultPartPriceMarginPercentage,
                DefaultParts = x.DefaultParts.Where(p => !p.IsDeleted).OrderBy(p => p.SortOrder).Select(p => new ReplacementItemDefaultPartDTO
                {
                    ID = p.ID,
                    PartNumber = p.PartNumber,
                    DefaultPeriodicQuantity = p.DefaultPeriodicQuantity,
                    DefaultStandaloneQuantity = p.DefaultStandaloneQuantity
                }).ToList(),
                ReplacementItemVehicleModelID = x.ID,
                StandaloneOperationCode = x.ReplacementItem.StandaloneOperationCode,
                StandaloneLabourCode = x.ReplacementItem.StandaloneLabourCode,
                StandaloneReplacementItemGroup = x.ReplacementItem?.StandaloneReplacementItemGroup is not null ?
                    new ShiftEntitySelectDTO { Value = x.ReplacementItem?.StandaloneReplacementItemGroup?.ID.ToString()!, Text = x.ReplacementItem?.StandaloneReplacementItemGroup?.Name } :
                    null,
            }).ToList() ?? new(),
            LabourRate = vehicleModel.LabourRate,
            LabourRates = vehicleModel.LabourRates
                .Where(x => !x.IsDeleted)
                .Select(x => new Shared.DTOs.LabourRate.LabourRateByCountryDTO
                {
                    CountryID = x.CountryID,
                    LabourRate = x.LabourRate
                }).ToList(),
            LabourDetails = vehicleModel.LabourDetails.Select(x => new LabourDetailsDTO
            {
                AllowedTime = x.AllowedTime,
                Consumable = x.Consumable,
                ServiceIntervalGroupID = x.ServiceIntervalGroupID.ToString()
            }).ToList(),
            BrandID = vehicleModel.BrandID.HasValue ? vehicleModel.BrandID.ToString() : null,
            VehicleModelName = vehicleModel.Name,
            VehicleModelID = vehicleModel.ID.ToString()
        };

        return Ok(result);
    }

    [HttpGet("ReplacementItemUsage/{key}/{replacementItemKey}")]
    [Authorize]
    public async Task<ActionResult<List<MenuVariantReplacementItemUsageDTO>>> GetReplacementItemUsage(string key, string replacementItemKey)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanRead(MenuActionTree.VehicleModels))
                return Forbid();
        }

        var vehicleModelId = hashIdService.Decode<VehicleModelDTO>(key);
        var replacementItemId = hashIdService.Decode<ReplacementItemDTO>(replacementItemKey);

        var usage = await vehicleModelRepository.GetReplacementItemUsageAsync(vehicleModelId, replacementItemId);
        return Ok(usage);
    }

    [HttpPost("PropagateReplacementItem/{key}")]
    [Authorize]
    public async Task<ActionResult<PropagateReplacementItemResponseDTO>> PropagateReplacementItem(string key, [FromBody] PropagateReplacementItemRequestDTO request)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanWrite(MenuActionTree.VehicleModels))
                return Forbid();
        }

        if (request is null)
            return BadRequest("Request body is required.");

        var vehicleModelId = hashIdService.Decode<VehicleModelDTO>(key);
        var replacementItemId = hashIdService.Decode<ReplacementItemDTO>(request.ReplacementItemID);

        var pendingCleared = await vehicleModelRepository.PropagateReplacementItemAsync(vehicleModelId, replacementItemId, request);
        return Ok(new PropagateReplacementItemResponseDTO { PendingCleared = pendingCleared });
    }

    [HttpPost("CheckReplacementItemMenuUsage/{key}")]
    [Authorize]
    public async Task<ActionResult<List<ReplacementItemMenuUsageDTO>>> CheckReplacementItemMenuUsage(string key, [FromBody] ReplacementItemMenuUsageRequestDTO request)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanRead(MenuActionTree.VehicleModels))
                return Forbid();
        }

        var vehicleModelId = hashIdService.Decode<VehicleModelDTO>(key);
        var replacementItemIds = (request?.ReplacementItemIDs ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => hashIdService.Decode<ReplacementItemDTO>(x))
            .ToList();

        if (replacementItemIds.Count == 0)
            return Ok(new List<ReplacementItemMenuUsageDTO>());

        var usage = await vehicleModelRepository.GetReplacementItemMenuUsageAsync(vehicleModelId, replacementItemIds);

        var result = usage
            .Select(x => new ReplacementItemMenuUsageDTO
            {
                ReplacementItemID = x.ReplacementItemID.ToString(),
                ReplacementItemName = x.ReplacementItemName,
                MenuLabels = x.MenuLabels
            })
            .ToList();

        return Ok(result);
    }
}
