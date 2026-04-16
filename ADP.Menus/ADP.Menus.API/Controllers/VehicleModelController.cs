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
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class VehicleModelController : ShiftEntitySecureControllerAsync<VehicleModelRepository, VehicleModel, VehicleModelListDTO, VehicleModelDTO>
{
    private readonly VehicleModelRepository vehicleModelRepository;
    private readonly MenuApiOptions options;

    public VehicleModelController(
            VehicleModelRepository vehicleModelRepository,
            MenuApiOptions options
        ) : base(options.EnableMenuActionTreeAuthorization ? MenuActionTree.VehicleModels : null)
    {
        this.vehicleModelRepository = vehicleModelRepository;
        this.options = options;
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

        var id = ShiftEntityHashIdService.Decode<VehicleModelDTO>(key);

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

        var vehicleModelId = ShiftEntityHashIdService.Decode<VehicleModelDTO>(key);
        var replacementItemIds = (request?.ReplacementItemIDs ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => ShiftEntityHashIdService.Decode<ReplacementItemDTO>(x))
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
