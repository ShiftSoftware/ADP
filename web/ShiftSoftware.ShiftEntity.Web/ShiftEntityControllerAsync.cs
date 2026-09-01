using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftEntity.Web;

public class ShiftEntityControllerAsync<Repository, Entity, ListDTO, ViewAndUpsertDTO> : ShiftEntityControllerBase<Repository, Entity, ListDTO, ViewAndUpsertDTO> where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
{
	[HttpGet]
	public virtual async Task<ActionResult<ODataDTO<ListDTO>>> Get(ODataQueryOptions<ListDTO> oDataQueryOptions)
	{
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

	[HttpGet("{key}")]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> GetSingle(string key, [FromQuery] DateTimeOffset? asOf)
	{
		return (await GetSingleNonAction(key, asOf)).Item1;
	}

	[HttpGet("{key}/revisions")]
	public virtual async Task<ActionResult<ODataDTO<List<RevisionDTO>>>> GetRevisions(string key, ODataQueryOptions<RevisionDTO> oDataQueryOptions)
	{
		return Ok(await GetRevisionListingNonAction(key, oDataQueryOptions));
	}

	[HttpPost]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> Post([FromBody] ViewAndUpsertDTO dto)
	{
		return (await PostItemNonAction(dto, "GetSingle")).Item1;
	}

	[HttpPut("{key}")]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> Put(string key, [FromBody] ViewAndUpsertDTO dto)
	{
		return (await PutItemNonAction(key, dto)).Item1;
	}

	[HttpDelete("{key}")]
	public virtual async Task<ActionResult<ShiftEntityResponse<ViewAndUpsertDTO>>> Delete(string key)
	{
		return (await DeleteItemNonAction(key)).Item1;
	}

	[HttpGet("print/{key}")]
	public virtual async Task<ActionResult> Print(string key, [FromQuery] string? expires = null, [FromQuery] string? token = null)
	{
		return await PrintNonAction(key);
	}
}
