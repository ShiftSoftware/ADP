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
using ShiftMapper;
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
    private readonly IMapper mapper;
    private readonly MenuApiOptions options;

    public VehicleModelController(
            VehicleModelRepository vehicleModelRepository,
            IHashIdService hashIdService,
            IMapper mapper,
            IOptions<MenuApiOptions> options
        ) : base(options.Value.EnableMenuActionTreeAuthorization ? MenuActionTree.VehicleModels : null)
    {
        this.vehicleModelRepository = vehicleModelRepository;
        this.hashIdService = hashIdService;
        this.mapper = mapper;
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

        // Declared in ADP.Menus.Data's MenuMapper.
        var result = mapper.Map<VehicleModel, VehicleModelMenuDTO>(vehicleModel);

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
