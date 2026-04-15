using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.BrandMapping;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class BrandMappingController : ShiftEntitySecureControllerAsync<BrandMappingRepository, BrandMapping, BrandMappingListDTO, BrandMappingDTO>
{
    public BrandMappingController(MenuApiOptions options)
        : base(options.EnableMenuActionTreeAuthorization ? MenuActionTree.BrandMappings : null)
    {
    }
}
