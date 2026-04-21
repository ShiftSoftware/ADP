using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class MenuVariantController : ShiftEntitySecureControllerAsync<MenuVariantRepository, MenuVariant, MenuVariantListDTO, MenuVariantDTO>
{
    private readonly MenuVariantRepository repository;
    private readonly MenuApiOptions options;

    public MenuVariantController(MenuVariantRepository repository, IOptions<MenuApiOptions> options)
        : base(options.Value.EnableMenuActionTreeAuthorization ? MenuActionTree.MenuVariants : null)
    {
        this.repository = repository;
        this.options = options.Value;
    }

    [HttpGet("ByMenu/{menuID}")]
    [Authorize]
    public async Task<IActionResult> ByMenu(string menuID)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanRead(MenuActionTree.MenuVariants))
                return Forbid();
        }

        var id = ShiftEntityHashIdService.Decode<MenuDTO>(menuID);
        var items = await (await repository.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted && x.MenuID == id)
            .OrderBy(x => x.Name)
            .Select(x => new MenuVariantListDTO
            {
                ID = x.ID.ToString(),
                MenuID = x.MenuID.ToString(),
                Name = x.Name,
                MenuPrefix = x.MenuPrefix,
                MenuPostfix = x.MenuPostfix,
                LabourRate = x.LabourRate,
                HasStandaloneItems = x.HasStandaloneItems
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpDelete("DeleteWithGuard/{key}")]
    [Authorize]
    public async Task<IActionResult> DeleteWithGuard(string key)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanDelete(MenuActionTree.MenuVariants))
                return Forbid();
        }

        var id = ShiftEntityHashIdService.Decode<MenuVariantDTO>(key);
        var entity = await (await repository.GetIQueryable(null, null, false, false))
            .Where(x => x.ID == id)
            .FirstOrDefaultAsync();

        if (entity is null)
            return NotFound();

        var variantsCount = await (await repository.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted && x.MenuID == entity.MenuID)
            .CountAsync();

        if (variantsCount <= 1)
            return BadRequest(new ShiftEntityResponse
            {
                Message = new ShiftSoftware.ShiftEntity.Model.Message("Conflict", "Can not delete the last variant in a menu group")
            });

        await repository.DeleteAsync(entity, false, null, false, false);
        return Ok();
    }
}
