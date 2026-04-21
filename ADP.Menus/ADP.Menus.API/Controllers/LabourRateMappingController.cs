using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRateMapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class LabourRateMappingController : ShiftEntitySecureControllerAsync<LabourRateMappingRepository, LabourRateMapping, LabourRateMappingListDTO, LabourRateMappingDTO>
{
    public LabourRateMappingController(IOptions<MenuApiOptions> options)
        : base(options.Value.EnableMenuActionTreeAuthorization ? MenuActionTree.LabourRateMappings : null)
    {
    }
}
