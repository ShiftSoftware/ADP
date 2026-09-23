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
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class MenuVariantController : ShiftEntitySecureControllerAsync<MenuVariantRepository, MenuVariant, MenuVariantListDTO, MenuVariantDTO>
{
    private readonly MenuVariantRepository repository;
    private readonly IHashIdService hashIdService;
    private readonly MenuApiOptions options;

    public MenuVariantController(MenuVariantRepository repository, IHashIdService hashIdService, IOptions<MenuApiOptions> options)
        : base(options.Value.EnableMenuActionTreeAuthorization ? MenuActionTree.MenuVariants : null)
    {
        this.repository = repository;
        this.hashIdService = hashIdService;
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

        var id = hashIdService.Decode<MenuDTO>(menuID);
        var variants = (await repository.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted && x.MenuID == id)
            .OrderBy(x => x.Name);

        // The repository's own list projection, so this endpoint and the variant grid agree on the shape.
        var items = await repository.MapToList(variants).ToListAsync();

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

        var id = hashIdService.Decode<MenuVariantDTO>(key);
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

        long? userId = null;

        try
        {
            userId = HttpContext.RequestServices.GetRequiredService<IdentityClaimProvider>().GetUserID();
        }
        catch
        {

        }
        await repository.DeleteAsync(entity, userId, false, false);
        return Ok();
    }
}
