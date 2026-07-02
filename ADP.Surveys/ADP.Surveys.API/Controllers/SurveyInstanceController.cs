using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Data.Repositories;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// READ-ONLY OData list surface for survey instances — what the dashboard's
/// Responses ShiftList binds to. Inherits the framework controller purely for its
/// list pipeline (OData over the ProjectTo'd <see cref="SurveyInstanceListDTO"/>,
/// hashid-aware filters, envelope); every other verb is deliberately 405:
/// instances are created by trigger ingest / the test-run action and mutated only
/// by the public submit + scheduler paths. Detail lives on
/// <c>SurveyResponses/instance/{publicId}</c>.
/// </summary>
[Route("[controller]")]
[ApiController]
public class SurveyInstanceController : ShiftEntitySecureControllerAsync<SurveyInstanceRepository, SurveyInstance, SurveyInstanceListDTO, SurveyInstanceAdminDTO>
{
    private readonly SurveyApiOptions options;

    public SurveyInstanceController(SurveyApiOptions options) : base(null)
    {
        this.options = options;
    }

    public override async Task<ActionResult<ODataDTO<SurveyInstanceListDTO>>> Get(ODataQueryOptions<SurveyInstanceListDTO> oDataQueryOptions)
    {
        // The base gates on a ReadWriteDeleteAction; responses viewing is governed
        // by the standalone boolean action instead (same gate as the detail /
        // test-instance endpoints on SurveyResponsesController).
        if (options.EnableSurveysActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(SurveysActionTree.Operations.ViewResponses))
                return Forbid();
        }

        return await base.Get(oDataQueryOptions);
    }

    // ─── Everything below is intentionally not part of this resource ─────────

    public override Task<ActionResult<ShiftEntityResponse<SurveyInstanceAdminDTO>>> GetSingle(string key, [FromQuery] DateTimeOffset? asOf)
        => MethodNotAllowed<ShiftEntityResponse<SurveyInstanceAdminDTO>>();

    public override Task<ActionResult<ShiftEntityResponse<SurveyInstanceAdminDTO>>> Post([FromBody] SurveyInstanceAdminDTO dto)
        => MethodNotAllowed<ShiftEntityResponse<SurveyInstanceAdminDTO>>();

    public override Task<ActionResult<ShiftEntityResponse<SurveyInstanceAdminDTO>>> Put(string key, [FromBody] SurveyInstanceAdminDTO dto)
        => MethodNotAllowed<ShiftEntityResponse<SurveyInstanceAdminDTO>>();

    public override Task<ActionResult<ShiftEntityResponse<SurveyInstanceAdminDTO>>> Delete(string key)
        => MethodNotAllowed<ShiftEntityResponse<SurveyInstanceAdminDTO>>();

    public override Task<ActionResult<ODataDTO<List<RevisionDTO>>>> GetRevisions(string key, ODataQueryOptions<RevisionDTO> oDataQueryOptions)
        => MethodNotAllowed<ODataDTO<List<RevisionDTO>>>();

    public override Task<ActionResult> Print(string key, [FromQuery] string? expires = null, [FromQuery] string? token = null)
        => Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed));

    public override Task<ActionResult> PrintToken(string key)
        => Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed));

    private Task<ActionResult<T>> MethodNotAllowed<T>()
        => Task.FromResult<ActionResult<T>>(StatusCode(StatusCodes.Status405MethodNotAllowed));
}
