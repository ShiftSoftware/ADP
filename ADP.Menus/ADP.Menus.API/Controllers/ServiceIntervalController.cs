using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class ServiceIntervalController : ShiftEntitySecureControllerAsync<ServiceIntervalRepository, ServiceInterval, ServiceIntervalListDTO, ServiceIntervalDTO>
{
    public ServiceIntervalController(MenuApiOptions options)
        : base(options.EnableMenuActionTreeAuthorization ? MenuActionTree.ServiceIntervals : null)
    {
    }
}
