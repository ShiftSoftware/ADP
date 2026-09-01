using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftEntity.Web;

public class ShiftEntitySecureControllerAsync<Repository, Entity, ListDTO, ViewAndUpsertDTO> : ShiftEntityControllerBase<Repository, Entity, ListDTO, ViewAndUpsertDTO> where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO>, IShiftRepositoryWithOptions<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
{
	private readonly ReadWriteDeleteAction? action;

	public ShiftEntitySecureControllerAsync(ReadWriteDeleteAction? action)
	{
		this.action = action;
	}

	[HttpGet]
	[Authorize]
	public virtual async Task<ActionResult<ODataDTO<ListDTO>>> Get(ODataQueryOptions<ListDTO> oDataQueryOptions)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanRead(action))
		{
			return Forbid();
		}
		try
		{
			return Ok(await GetOdataListingNonAction(oDataQueryOptions));
		}
		catch (ShiftEntityException ex)
		{
			ShiftEntityException ex2 = ex;
			return StatusCode(ex2.HttpStatusCode, new { ex2.Message, ex2.AdditionalData });
		}
	}

	[Authorize]
	[HttpGet("{key}")]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> GetSingle(string key, [FromQuery] DateTimeOffset? asOf)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanRead(action))
		{
			return Forbid();
		}
		return (await GetSingleNonAction(key, asOf)).Item1;
	}

	[HttpGet("print-token/{key}")]
	public virtual async Task<ActionResult> PrintToken(string key)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanRead(action))
		{
			return Forbid();
		}
		string urlDescriptor = base.Url.Action("PrintToken", new { key });
		return await PrintTokenNonAction(key, urlDescriptor);
	}

	[HttpGet("print/{key}")]
	[AllowAnonymous]
	public virtual async Task<ActionResult> Print(string key, [FromQuery] string? expires = null, [FromQuery] string? token = null)
	{
		string urlDescriptor = base.Url.Action("PrintToken", new { key });
		if (!ValidatePrintSASTokenNonAction(key, urlDescriptor, expires, token))
		{
			return Forbid();
		}
		return await PrintNonAction(key);
	}

	[Authorize]
	[HttpGet("{key}/revisions")]
	public virtual async Task<ActionResult<ODataDTO<List<RevisionDTO>>>> GetRevisions(string key, ODataQueryOptions<RevisionDTO> oDataQueryOptions)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanRead(action))
		{
			return Forbid();
		}
		return Ok(await GetRevisionListingNonAction(key, oDataQueryOptions));
	}

	[Authorize]
	[HttpPost]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> Post([FromBody] ViewAndUpsertDTO dto)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanWrite(action))
		{
			return Forbid();
		}
		return (await PostItemNonAction(dto, "GetSingle")).Item1;
	}

	[Authorize]
	[HttpPut("{key}")]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> Put(string key, [FromBody] ViewAndUpsertDTO dto)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanWrite(action))
		{
			return Forbid();
		}
		return (await PutItemNonAction(key, dto)).Item1;
	}

	[Authorize]
	[HttpDelete("{key}")]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> Delete(string key)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanDelete(action))
		{
			return Forbid();
		}
		return (await DeleteItemNonAction(key)).Item1;
	}

	[Authorize]
	[HttpGet("{key}/attention")]
	public virtual async Task<ActionResult> GetAttentionSignals(string key)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanRead(action))
		{
			return Forbid();
		}
		return ToActionResult(await _handler.GetAttentionSignalsAsync(base.HttpContext, key));
	}

	[Authorize]
	[HttpPost("{key}/attention/clear")]
	public virtual async Task<ActionResult> ClearAttentionSignals(string key, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AttentionClearFilter? filter = null)
	{
		ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (action != null && !requiredService.CanWrite(action))
		{
			return Forbid();
		}
		return ToActionResult(await _handler.ClearAttentionSignalsAsync(base.HttpContext, key, filter));
	}

	[NonAction]
	public async Task<List<Entity>> GetSelectedEntitiesAsync(SelectStateDTO<ListDTO> ids, bool skipAuthentication = false, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (!skipAuthentication)
		{
			ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
			if (action != null && !requiredService.CanRead(action))
			{
				return new List<Entity>();
			}
		}
		return await GetSelectedEntitiesAsyncBase(ids, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	[NonAction]
	public async Task<List<ListDTO>> GetSelectedListDTOsAsync(SelectStateDTO<ListDTO> ids, bool skipAuthentication = false, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (!skipAuthentication)
		{
			ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
			if (action != null && !requiredService.CanRead(action))
			{
				return new List<ListDTO>();
			}
		}
		return await GetSelectedListDTOsAsyncBase(ids, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	[NonAction]
	public async Task<List<Entity>> GetSelectedEntitiesAsync(ODataQueryOptions<ListDTO> oDataQueryOptions, bool skipAuthentication = false, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (!skipAuthentication)
		{
			ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
			if (action != null && !requiredService.CanRead(action))
			{
				return new List<Entity>();
			}
		}
		return await GetSelectedEntitiesAsyncBase(oDataQueryOptions, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	[NonAction]
	public async Task<List<ListDTO>> GetSelectedListDTOsAsync(ODataQueryOptions<ListDTO> oDataQueryOptions, bool skipAuthentication = false, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (!skipAuthentication)
		{
			ITypeAuthService requiredService = base.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
			if (action != null && !requiredService.CanRead(action))
			{
				return new List<ListDTO>();
			}
		}
		return await GetSelectedListDTOsAsyncBase(oDataQueryOptions, disableDefaultDataLevelAccess, disableGlobalFilters);
	}
}
