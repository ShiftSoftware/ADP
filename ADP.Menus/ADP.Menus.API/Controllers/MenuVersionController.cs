using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVersion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class MenuVersionController : ShiftEntitySecureControllerAsync<MenuVersionRepository, MenuVersion, MenuVersionListDTO, MenuVersionDTO>
{
    private readonly MenuApiOptions options;

    public MenuVersionController(MenuApiOptions options)
        : base(options.EnableMenuActionTreeAuthorization ? MenuActionTree.MenuVersions : null)
    {
        this.options = options;
    }

    // Creating a new menu version is gated by a dedicated boolean action so it can be granted independently
    // of generic write access to MenuVersions.
    [Authorize]
    [HttpPost]
    public override async Task<ActionResult<ShiftEntityResponse<MenuVersionDTO>>> Post([FromBody] MenuVersionDTO dto)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(MenuActionTree.Operations.CreateMenuVersion))
                return Forbid();
        }

        return await base.Post(dto);
    }
}
