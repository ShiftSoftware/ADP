using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData.Query;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftEntity.Web;

public class ShiftEntityControllerBase<Repository, Entity, ListDTO, ViewAndUpsertDTO> : ControllerBase where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
{
	protected readonly ShiftEntityCrudHandler<Repository, Entity, ListDTO, ViewAndUpsertDTO> _handler = new ShiftEntityCrudHandler<Repository, Entity, ListDTO, ViewAndUpsertDTO>();

	[NonAction]
	internal Task<ODataDTO<ListDTO>> GetOdataListingNonAction(ODataQueryOptions<ListDTO> oDataQueryOptions, Expression<Func<Entity, bool>>? where = null)
	{
		return _handler.GetListAsync(base.HttpContext, oDataQueryOptions, where);
	}

	[NonAction]
	internal Task<ODataDTO<RevisionDTO>> GetRevisionListingNonAction(string key, ODataQueryOptions<RevisionDTO> oDataQueryOptions)
	{
		return _handler.GetRevisionsAsync(base.HttpContext, key, oDataQueryOptions);
	}

	[NonAction]
	internal async Task<(ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>> ActionResult, Entity? Entity)> GetSingleNonAction(string key, DateTimeOffset? asOf)
	{
		var (crudResult, item) = await _handler.GetSingleAsync(base.HttpContext, key, asOf);
		if (crudResult.IsTemporal)
		{
			base.Response.Headers.Append("Versioning", "Temporal");
		}
		return (ActionResult: ToActionResult(crudResult), Entity: item);
	}

	[NonAction]
	internal async Task<(ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>> ActionResult, Entity? Entity)> PostItemNonAction(ViewAndUpsertDTO dto, string getActionName)
	{
		var (crudResult, item) = await _handler.PostAsync(base.HttpContext, dto, BuildValidationErrorsFromModelState());
		if (crudResult.CreatedAtKey != null)
		{
			return (ActionResult: CreatedAtAction(getActionName, new
			{
				key = crudResult.CreatedAtKey
			}, crudResult.Body), Entity: item);
		}
		return (ActionResult: ToActionResult(crudResult), Entity: item);
	}

	[NonAction]
	internal async Task<(ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>> ActionResult, Entity? Entity)> PutItemNonAction(string key, ViewAndUpsertDTO dto)
	{
		var (result, item) = await _handler.PutAsync(base.HttpContext, key, dto, BuildValidationErrorsFromModelState());
		return (ActionResult: ToActionResult(result), Entity: item);
	}

	[NonAction]
	internal async Task<(ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>> ActionResult, Entity? Entity)> DeleteItemNonAction(string key)
	{
		var (result, item) = await _handler.DeleteAsync(base.HttpContext, key);
		return (ActionResult: ToActionResult(result), Entity: item);
	}

	[NonAction]
	internal async Task<ActionResult> PrintNonAction(string key)
	{
		CrudResult crudResult = await _handler.PrintAsync(base.HttpContext, key);
		if (crudResult.Stream != null)
		{
			return new FileStreamResult(crudResult.Stream, crudResult.ContentType ?? "application/octet-stream");
		}
		return StatusCode(crudResult.StatusCode, crudResult.Body);
	}

	[NonAction]
	internal async Task<ActionResult> PrintTokenNonAction(string key, string urlDescriptor)
	{
		CrudResult crudResult = await _handler.PrintTokenAsync(base.HttpContext, key, urlDescriptor);
		if (crudResult.StatusCode == 200)
		{
			return Ok(crudResult.Body);
		}
		return StatusCode(crudResult.StatusCode, crudResult.Body);
	}

	[NonAction]
	internal bool ValidatePrintSASTokenNonAction(string key, string urlDescriptor, string? expires, string? token)
	{
		return _handler.ValidatePrintSASToken(base.HttpContext, key, urlDescriptor, expires, token);
	}

	internal Task<List<ListDTO>> GetSelectedListDTOsAsyncBase(ODataQueryOptions<ListDTO> oDataQueryOptions, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		return _handler.GetSelectedListDTOsAsync(base.HttpContext, oDataQueryOptions, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	internal Task<List<ListDTO>> GetSelectedListDTOsAsyncBase(SelectStateDTO<ListDTO> ids, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		return _handler.GetSelectedListDTOsAsync(base.HttpContext, ids, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	internal Task<List<Entity>> GetSelectedEntitiesAsyncBase(ODataQueryOptions<ListDTO> oDataQueryOptions, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		return _handler.GetSelectedEntitiesAsync(base.HttpContext, oDataQueryOptions, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	internal Task<List<Entity>> GetSelectedEntitiesAsyncBase(SelectStateDTO<ListDTO> ids, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		return _handler.GetSelectedEntitiesAsync(base.HttpContext, ids, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	private IReadOnlyDictionary<string, string[]>? BuildValidationErrorsFromModelState()
	{
		if (base.ModelState.IsValid)
		{
			return null;
		}
		return base.ModelState.ToDictionary<KeyValuePair<string, ModelStateEntry>, string, string[]>((KeyValuePair<string, ModelStateEntry> x) => x.Key, (KeyValuePair<string, ModelStateEntry> x) => (x.Value != null) ? x.Value.Errors.Select((ModelError e) => e.ErrorMessage).ToArray() : Array.Empty<string>());
	}

	protected ActionResult ToActionResult(CrudResult result)
	{
		return result.StatusCode switch
		{
			200 => Ok(result.Body), 
			400 => BadRequest(result.Body), 
			404 => NotFound(result.Body), 
			_ => StatusCode(result.StatusCode, result.Body), 
		};
	}
}
