using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Data.Repositories;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

[Route("[controller]")]
[ApiController]
public class ScreenTemplateController : ShiftEntitySecureControllerAsync<ScreenTemplateRepository, ScreenTemplate, ScreenTemplateListDTO, ScreenTemplateAdminDTO>
{
    public ScreenTemplateController(SurveyApiOptions options)
        : base(options.EnableSurveysActionTreeAuthorization ? SurveysActionTree.ScreenTemplates : null)
    {
    }
}
