using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Core.HashIds;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.EFCore.Attention;
using ShiftSoftware.ShiftEntity.EFCore.Entities;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Print;
using ShiftSoftware.ShiftEntity.Web.Attention;

namespace ShiftSoftware.ShiftEntity.Web;

/// <summary>
/// Framework-agnostic CRUD/revisions/print/selection logic extracted from
/// <see cref="T:ShiftSoftware.ShiftEntity.Web.ShiftEntityControllerBase`4" />.
///
/// Consumed by both the controller base (which wraps <see cref="T:ShiftSoftware.ShiftEntity.Web.CrudResult" /> as
/// <c>ActionResult</c>) and the minimal-API <c>MapShiftEntityCrud</c> /
/// <c>MapShiftEntitySecureCrud</c> extensions (which wrap it as <c>IResult</c>).
///
/// The handler is stateless and uses the supplied <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> for DI
/// resolution, user ID, and request headers — no dependency on <c>ControllerBase</c>,
/// <c>ModelState</c>, <c>Url.Action</c>, or any MVC-specific type.
/// </summary>
public class ShiftEntityCrudHandler<Repository, Entity, ListDTO, ViewAndUpsertDTO> where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
{
	public async Task<ODataDTO<ListDTO>> GetListAsync(HttpContext httpContext, ODataQueryOptions<ListDTO> oDataQueryOptions, Expression<Func<Entity, bool>>? where = null)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		IQueryable<Entity> queryable = await ((IShiftOdataList<ListDTO, _003F>)repository/*cast due to .constrained prefix*/).GetIQueryable((DateTimeOffset?)null, (List<string>)null, (RepositoryBypass)0);
		if (where != null)
		{
			queryable = queryable.Where(where);
		}
		return await (await ((IShiftOdataList<ListDTO, _003F>)repository/*cast due to .constrained prefix*/).OdataList((IQueryable<ListDTO>)queryable)).ToOdataDTO(oDataQueryOptions, httpContext.Request, isAsync: true, applySoftDeleteFilter: true, (Func<IQueryable<ListDTO>, ValueTask<IQueryable<ListDTO>>>?)((IShiftOdataList<ListDTO, _003F>)(object)repository).ApplyPostODataProcessing);
	}

	public async Task<ODataDTO<RevisionDTO>> GetRevisionsAsync(HttpContext httpContext, string key, ODataQueryOptions<RevisionDTO> oDataQueryOptions)
	{
		Repository requiredService = httpContext.RequestServices.GetRequiredService<Repository>();
		IHashIdService requiredService2 = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		return await ((IShiftEntityFind<_003F>)requiredService/*cast due to .constrained prefix*/).GetRevisionsAsync(requiredService2.Decode<ViewAndUpsertDTO>(key)).ToOdataDTO<RevisionDTO>(oDataQueryOptions, httpContext.Request, isAsync: true, applySoftDeleteFilter: false, (Func<IQueryable<RevisionDTO>, ValueTask<IQueryable<RevisionDTO>>>?)null);
	}

	public async Task<(CrudResult Result, Entity? Entity)> GetSingleAsync(HttpContext httpContext, string key, DateTimeOffset? asOf)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		IHashIdService requiredService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		Entity item;
		try
		{
			item = await ((IShiftEntityFind<_003F>)repository/*cast due to .constrained prefix*/).FindAsync(requiredService.Decode<ViewAndUpsertDTO>(key), asOf, (RepositoryBypass)0);
		}
		catch (ShiftEntityException ex)
		{
			ShiftEntityException ex2 = ex;
			return (Result: HandleException(ex2), Entity: default(Entity));
		}
		if (item == null)
		{
			ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>();
			((ShiftEntityResponse)obj).Message = new Message
			{
				Title = "Not Found",
				Body = "Can't find entity with ID '" + key + "'"
			};
			((ShiftEntityResponse)obj).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
			return (Result: CrudResult.NotFound(obj), Entity: default(Entity));
		}
		bool isTemporal = ((object)item).GetType().GetCustomAttributes(typeof(TemporalShiftEntity)).Any();
		ShiftEntityResponse<_003F> obj2 = new ShiftEntityResponse<_003F>(await ((IShiftEntityViewAsync<ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).ViewAsync((ViewAndUpsertDTO)item));
		((ShiftEntityResponse)obj2).Message = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).ResponseMessage;
		((ShiftEntityResponse)obj2).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
		return (Result: CrudResult.Ok(obj2, isTemporal), Entity: item);
	}

	public async Task<(CrudResult Result, Entity? Entity)> PostAsync(HttpContext httpContext, ViewAndUpsertDTO dto, IReadOnlyDictionary<string, string[]>? validationErrors = null)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		if (validationErrors != null && validationErrors.Count > 0)
		{
			return (Result: CrudResult.BadRequest(BuildValidationErrorResponse(validationErrors, ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData)), Entity: default(Entity));
		}
		Guid? idempotencyKey = null;
		Repository val;
		Entity newItem;
		try
		{
			if (typeof(Entity).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasIdempotencyKey<Entity>))))
			{
				string text = httpContext.Request.Headers["Idempotency-Key"].ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					idempotencyKey = Guid.Parse(text);
				}
			}
			ref Repository reference = ref repository;
			val = default(Repository);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			Entity val2 = new Entity();
			long? userID = httpContext.GetUserID();
			Guid? guid = idempotencyKey;
			newItem = await ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)reference/*cast due to .constrained prefix*/).UpsertAsync((ListDTO)val2, dto, (ActionTypes)1, userID, guid, (RepositoryBypass)0);
		}
		catch (ShiftEntityException ex)
		{
			ShiftEntityException ex2 = ex;
			return (Result: HandleException(ex2), Entity: default(Entity));
		}
		((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).Add((ListDTO)newItem);
		try
		{
			await ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).SaveChangesAsync();
		}
		catch (DuplicateIdempotencyKeyException)
		{
			ref Repository reference2 = ref repository;
			val = default(Repository);
			if (val == null)
			{
				val = reference2;
				reference2 = ref val;
			}
			Guid value = idempotencyKey.Value;
			Entity existingItem = await ((IShiftEntityFind<_003F>)reference2/*cast due to .constrained prefix*/).FindByIdempotencyKeyAsync(value, (DateTimeOffset?)null, (RepositoryBypass)0);
			ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>(await ((IShiftEntityViewAsync<ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).ViewAsync((ViewAndUpsertDTO)existingItem));
			((ShiftEntityResponse)obj).Message = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).ResponseMessage;
			((ShiftEntityResponse)obj).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
			return (Result: CrudResult.Ok(obj), Entity: existingItem);
		}
		catch (ShiftEntityException ex4)
		{
			ShiftEntityException ex5 = ex4;
			return (Result: HandleException(ex5), Entity: default(Entity));
		}
		ShiftEntityResponse<_003F> obj2 = new ShiftEntityResponse<_003F>(await ((IShiftEntityViewAsync<ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).ViewAsync((ViewAndUpsertDTO)newItem));
		((ShiftEntityResponse)obj2).Message = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).ResponseMessage;
		((ShiftEntityResponse)obj2).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
		string createdAtKey = httpContext.RequestServices.GetRequiredService<IHashIdService>().Encode<ViewAndUpsertDTO>(((ShiftEntityBase)(object)newItem).ID);
		return (Result: CrudResult.Created(obj2, createdAtKey), Entity: newItem);
	}

	public async Task<(CrudResult Result, Entity? Entity)> PutAsync(HttpContext httpContext, string key, ViewAndUpsertDTO dto, IReadOnlyDictionary<string, string[]>? validationErrors = null)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		IHashIdService requiredService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		if (validationErrors != null && validationErrors.Count > 0)
		{
			return (Result: CrudResult.BadRequest(BuildValidationErrorResponse(validationErrors, ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData)), Entity: default(Entity));
		}
		Repository val;
		Entity item;
		try
		{
			ref Repository reference = ref repository;
			val = default(Repository);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			long num = requiredService.Decode<ViewAndUpsertDTO>(key);
			item = await ((IShiftEntityFind<_003F>)reference/*cast due to .constrained prefix*/).FindAsync(num, (DateTimeOffset?)null, (RepositoryBypass)0);
		}
		catch (ShiftEntityException ex)
		{
			ShiftEntityException ex2 = ex;
			return (Result: HandleException(ex2), Entity: default(Entity));
		}
		if (item == null)
		{
			ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>();
			((ShiftEntityResponse)obj).Message = new Message
			{
				Title = "Not Found",
				Body = "Can't find entity with ID '" + key + "'"
			};
			((ShiftEntityResponse)obj).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
			return (Result: CrudResult.NotFound(obj), Entity: default(Entity));
		}
		try
		{
			if (((ShiftEntity<_003F>)item).LastSaveDate != ((ShiftEntityViewAndUpsertDTO)dto).LastSaveDate)
			{
				throw new ShiftEntityException(new Message("Conflict", $"The submitted item version ({((ShiftEntityViewAndUpsertDTO)dto).LastSaveDate}) has been modified by another process. It does not match the loaded item version ({((ShiftEntity<_003F>)item).LastSaveDate}). Please reload the item and try again."), 409, (Dictionary<string, object>)null);
			}
			ref Repository reference2 = ref repository;
			val = default(Repository);
			if (val == null)
			{
				val = reference2;
				reference2 = ref val;
			}
			long? userID = httpContext.GetUserID();
			await ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)reference2/*cast due to .constrained prefix*/).UpsertAsync((ListDTO)item, dto, (ActionTypes)2, userID, (Guid?)null, (RepositoryBypass)0);
		}
		catch (ShiftEntityException ex3)
		{
			ShiftEntityException ex4 = ex3;
			return (Result: HandleException(ex4), Entity: default(Entity));
		}
		try
		{
			await ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).SaveChangesAsync();
		}
		catch (ShiftEntityException ex5)
		{
			ShiftEntityException ex6 = ex5;
			return (Result: HandleException(ex6), Entity: default(Entity));
		}
		ShiftEntityResponse<_003F> obj2 = new ShiftEntityResponse<_003F>(await ((IShiftEntityViewAsync<ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).ViewAsync((ViewAndUpsertDTO)item));
		((ShiftEntityResponse)obj2).Message = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).ResponseMessage;
		((ShiftEntityResponse)obj2).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
		return (Result: CrudResult.Ok(obj2), Entity: item);
	}

	public async Task<(CrudResult Result, Entity? Entity)> DeleteAsync(HttpContext httpContext, string key)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		IHashIdService requiredService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		ref Repository reference = ref repository;
		Repository val = default(Repository);
		if (val == null)
		{
			val = reference;
			reference = ref val;
		}
		long num = requiredService.Decode<ViewAndUpsertDTO>(key);
		Entity item = await ((IShiftEntityFind<_003F>)reference/*cast due to .constrained prefix*/).FindAsync(num, (DateTimeOffset?)null, (RepositoryBypass)0);
		if (item == null)
		{
			ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>();
			((ShiftEntityResponse)obj).Message = new Message
			{
				Title = "Not Found",
				Body = "Can't find entity with ID '" + key + "'"
			};
			((ShiftEntityResponse)obj).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
			return (Result: CrudResult.NotFound(obj), Entity: default(Entity));
		}
		try
		{
			ref Repository reference2 = ref repository;
			val = default(Repository);
			if (val == null)
			{
				val = reference2;
				reference2 = ref val;
			}
			Entity val2 = item;
			long? userID = httpContext.GetUserID();
			await ((IShiftEntityDeleteAsync<_003F>)reference2/*cast due to .constrained prefix*/).DeleteAsync(val2, userID, (RepositoryBypass)0);
		}
		catch (ShiftEntityException ex)
		{
			ShiftEntityException ex2 = ex;
			return (Result: HandleException(ex2), Entity: default(Entity));
		}
		try
		{
			await ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).SaveChangesAsync();
		}
		catch (ShiftEntityException ex3)
		{
			ShiftEntityException ex4 = ex3;
			return (Result: HandleException(ex4), Entity: default(Entity));
		}
		if (((ShiftEntity<_003F>)item).ReloadAfterSave)
		{
			ref Repository reference3 = ref repository;
			val = default(Repository);
			if (val == null)
			{
				val = reference3;
				reference3 = ref val;
			}
			long iD = ((ShiftEntityBase)(object)item).ID;
			item = await ((IShiftEntityFind<_003F>)reference3/*cast due to .constrained prefix*/).FindAsync(iD, (DateTimeOffset?)null, (RepositoryBypass)0);
		}
		ShiftEntityResponse<_003F> obj2 = new ShiftEntityResponse<_003F>(await ((IShiftEntityViewAsync<ViewAndUpsertDTO, _003F>)repository/*cast due to .constrained prefix*/).ViewAsync((ViewAndUpsertDTO)item));
		((ShiftEntityResponse)obj2).Message = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).ResponseMessage;
		((ShiftEntityResponse)obj2).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
		return (Result: CrudResult.Ok(obj2), Entity: item);
	}

	public async Task<CrudResult> PrintAsync(HttpContext httpContext, string key)
	{
		Repository requiredService = httpContext.RequestServices.GetRequiredService<Repository>();
		try
		{
			return CrudResult.File(await ((IShiftEntityFind<_003F>)requiredService/*cast due to .constrained prefix*/).PrintAsync(key), "application/pdf");
		}
		catch (ShiftEntityException ex)
		{
			ShiftEntityException ex2 = ex;
			return CrudResult.Status(ex2.HttpStatusCode, (object?)new ShiftEntityResponse
			{
				Message = ex2.Message,
				Additional = ex2.AdditionalData
			});
		}
	}

	/// <summary>
	/// Generates a SAS token for the print endpoint. <paramref name="urlDescriptor" /> must
	/// be the same string used by the print endpoint when validating the token (typically
	/// the absolute path of the print-token route).
	/// </summary>
	public async Task<CrudResult> PrintTokenAsync(HttpContext httpContext, string key, string urlDescriptor)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		IHashIdService requiredService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		if (await ((IShiftEntityFind<_003F>)repository/*cast due to .constrained prefix*/).FindAsync(requiredService.Decode<ViewAndUpsertDTO>(key), (DateTimeOffset?)null, (RepositoryBypass)0) == null)
		{
			ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>();
			((ShiftEntityResponse)obj).Message = new Message
			{
				Title = "Not Found",
				Body = "Can't find entity with ID '" + key + "'"
			};
			((ShiftEntityResponse)obj).Additional = ((IShiftRepositoryAsync<ListDTO, ViewAndUpsertDTO, _003F>)repository).AdditionalResponseData;
			return CrudResult.NotFound(obj);
		}
		ShiftEntityPrintOptions requiredService2 = httpContext.RequestServices.GetRequiredService<ShiftEntityPrintOptions>();
		var (text, text2) = TokenService.GenerateSASToken(urlDescriptor, key, DateTime.UtcNow.AddSeconds(requiredService2.TokenExpirationInSeconds), requiredService2.SASTokenKey);
		return CrudResult.Ok("expires=" + text2 + "&token=" + text);
	}

	/// <summary>
	/// Validates a SAS token against <paramref name="urlDescriptor" /> (must match the
	/// descriptor used by <see cref="M:ShiftSoftware.ShiftEntity.Web.ShiftEntityCrudHandler`4.PrintTokenAsync(Microsoft.AspNetCore.Http.HttpContext,System.String,System.String)" />) and returns true if valid.
	/// </summary>
	public bool ValidatePrintSASToken(HttpContext httpContext, string key, string urlDescriptor, string? expires, string? token)
	{
		if (string.IsNullOrEmpty(expires) || string.IsNullOrEmpty(token))
		{
			return false;
		}
		ShiftEntityPrintOptions requiredService = httpContext.RequestServices.GetRequiredService<ShiftEntityPrintOptions>();
		return TokenService.ValidateSASToken(urlDescriptor, key, expires, token, requiredService.SASTokenKey);
	}

	/// <summary>
	/// Returns the stored attention signals for one entity. Single source of truth shared by the
	/// controller (<c>ShiftEntitySecureControllerAsync.GetAttentionSignals</c>) and the minimal-API
	/// <c>MapShiftEntitySecureCrud</c> endpoint, so the two surfaces can't drift.
	/// <para>
	/// Returns an empty list (200) when the entity hasn't opted into attention — the route is
	/// exposed on every surface, so a 404 there reads as a real error in the browser; an empty
	/// list is indistinguishable to the client from an opted-in entity with no signals yet.
	/// </para>
	/// </summary>
	public async Task<CrudResult> GetAttentionSignalsAsync(HttpContext httpContext, string key)
	{
		if (!typeof(IHasAttention).IsAssignableFrom(typeof(Entity)))
		{
			return CrudResult.Ok(new List<StoredAttentionSignal>());
		}
		IHashIdService hashIdService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		long entityId = hashIdService.Decode<ViewAndUpsertDTO>(key);
		string entityTypeName = typeof(Entity).Name;
		bool flag = typeof(IHasIndexedAttention).IsAssignableFrom(typeof(Entity));
		object obj = httpContext.RequestServices.GetRequiredService<Repository>();
		object obj2 = ((obj is ShiftRepositoryBase) ? obj : null);
		object obj3 = ((obj2 != null) ? ((ShiftRepositoryBase)obj2).GetDbContext() : null);
		ShiftDbContext db = (ShiftDbContext)((obj3 is ShiftDbContext) ? obj3 : null);
		if (db == null)
		{
			return CrudResult.Status(500, null);
		}
		List<StoredAttentionSignal> body;
		if (flag)
		{
			body = (await EntityFrameworkQueryableExtensions.ToListAsync<AttentionSignalEntry>((IQueryable<AttentionSignalEntry>)((IQueryable<AttentionSignalEntry>)((DbContext)db).Set<AttentionSignalEntry>()).Where((Expression<Func<AttentionSignalEntry, bool>>)((AttentionSignalEntry x) => x.EntityType == entityTypeName && x.EntityId == entityId)).OrderByDescending((Expression<Func<AttentionSignalEntry, AttentionSeverity>>)((AttentionSignalEntry x) => x.Severity)).ThenByDescending((Expression<Func<AttentionSignalEntry, DateTimeOffset>>)((AttentionSignalEntry x) => x.RaisedAt)), default(CancellationToken))).Select(delegate(AttentionSignalEntry x)
			{
				StoredAttentionSignal obj5 = x.ToStoredSignal()._003CClone_003E_0024();
				obj5.set_EntityId(hashIdService.Encode<ViewAndUpsertDTO>(x.EntityId));
				return obj5;
			}).ToList();
		}
		else
		{
			object obj4 = await ((DbContext)db).FindAsync(typeof(Entity), new object[1] { entityId });
			if (obj4 == null)
			{
				return CrudResult.NotFound(null);
			}
			body = AttentionSignalJsonHelper.Deserialize((string)((MemberEntry)((DbContext)db).Entry(obj4).Property("AttentionSignalsJson")).CurrentValue);
		}
		return CrudResult.Ok(body);
	}

	/// <summary>
	/// Clears all active attention signals for one entity. Single source of truth shared by the
	/// controller and the minimal-API endpoint. No-op (200) when the entity hasn't opted in.
	/// </summary>
	public async Task<CrudResult> ClearAttentionSignalsAsync(HttpContext httpContext, string key, AttentionClearFilter? filter = null)
	{
		if (!typeof(IHasAttention).IsAssignableFrom(typeof(Entity)))
		{
			return CrudResult.Ok(null);
		}
		IHashIdService requiredService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		long entityId = requiredService.Decode<ViewAndUpsertDTO>(key);
		string entityTypeName = typeof(Entity).Name;
		long? userID = httpContext.RequestServices.GetRequiredService<IdentityClaimProvider>().GetUserID();
		object obj = httpContext.RequestServices.GetRequiredService<Repository>();
		object obj2 = ((obj is ShiftRepositoryBase) ? obj : null);
		object obj3 = ((obj2 != null) ? ((ShiftRepositoryBase)obj2).GetDbContext() : null);
		ShiftDbContext val = (ShiftDbContext)((obj3 is ShiftDbContext) ? obj3 : null);
		if (val == null)
		{
			return CrudResult.Status(500, null);
		}
		try
		{
			DateTimeOffset? lastSaveDate = await AttentionPipeline.ClearSignals(val, entityTypeName, entityId, userID, filter);
			IAttentionRealtimeBroadcaster service = httpContext.RequestServices.GetService<IAttentionRealtimeBroadcaster>();
			if (service != null)
			{
				IAttentionOriginProvider? service2 = httpContext.RequestServices.GetService<IAttentionOriginProvider>();
				string originConnectionId = ((service2 != null) ? service2.OriginConnectionId : null);
				try
				{
					await service.BroadcastClearedAsync(entityTypeName, entityId, originConnectionId);
				}
				catch
				{
				}
			}
			ClearAttentionResponse val2 = new ClearAttentionResponse();
			val2.set_LastSaveDate(lastSaveDate);
			return CrudResult.Ok((object?)val2);
		}
		catch (InvalidOperationException ex)
		{
			return CrudResult.NotFound(ex.Message);
		}
	}

	private async Task<IQueryable<Entity>> GetQueryForSelectionAsync(HttpContext httpContext, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
	{
		return (await ((IShiftOdataList<ListDTO, _003F>)httpContext.RequestServices.GetRequiredService<Repository>()/*cast due to .constrained prefix*/).GetIQueryable((DateTimeOffset?)null, (List<string>)null, disableDefaultDataLevelAccess, disableGlobalFilters)).Where((Entity x) => !((ShiftEntity<_003F>)x).IsDeleted);
	}

	private async Task<List<ListDTO>> GetListDTOForSelectionAsync(HttpContext httpContext, List<string?>? selectedIds, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
	{
		Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
		IQueryable<Entity> queryable = await GetQueryForSelectionAsync(httpContext, disableDefaultDataLevelAccess, disableGlobalFilters);
		IQueryable<ListDTO> queryable2 = await ((IShiftOdataList<ListDTO, _003F>)repository/*cast due to .constrained prefix*/).OdataList((IQueryable<ListDTO>)queryable);
		if (queryable2 != null)
		{
			if (selectedIds != null)
			{
				queryable2 = queryable2.Where((ListDTO x) => selectedIds.Contains(((ShiftEntityDTOBase)x).ID));
			}
			return await EntityFrameworkQueryableExtensions.ToListAsync<ListDTO>(queryable2, default(CancellationToken));
		}
		return new List<ListDTO>();
	}

	public async Task<List<ListDTO>> GetSelectedListDTOsAsync(HttpContext httpContext, ODataQueryOptions<ListDTO> oDataQueryOptions, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (((oDataQueryOptions != null) ? ((ODataQueryOptions)oDataQueryOptions).Filter : null) != null)
		{
			IHashIdService requiredService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
			FilterClause filter = new FilterClause(((QueryNode)((ODataQueryOptions)oDataQueryOptions).Filter.FilterClause.Expression).Accept<SingleValueNode>((QueryNodeVisitor<SingleValueNode>)(object)new HashIdQueryNodeVisitor<_003F>(requiredService)), ((ODataQueryOptions)oDataQueryOptions).Filter.FilterClause.RangeVariable);
			ODataUriParser val = new ODataUriParser(((ODataQueryOptions)oDataQueryOptions).Context.Model, new Uri("", UriKind.Relative));
			ODataUri obj = val.ParseUri();
			obj.Filter = filter;
			string text = ODataUriExtensions.BuildUri(obj, val.UrlKeyDelimiter).ToString();
			QueryString queryString = new QueryString(text.Substring(text.IndexOf("?")));
			QueryString originalQueryString = httpContext.Request.QueryString;
			try
			{
				httpContext.Request.QueryString = queryString;
				ODataQueryOptions<ListDTO> rebuiltOptions = (ODataQueryOptions<ListDTO>)(object)new ODataQueryOptions<_003F>(((ODataQueryOptions)oDataQueryOptions).Context, httpContext.Request);
				Repository repository = httpContext.RequestServices.GetRequiredService<Repository>();
				IQueryable<Entity> queryable = await GetQueryForSelectionAsync(httpContext, disableDefaultDataLevelAccess, disableGlobalFilters);
				IQueryable<ListDTO> queryable2 = await ((IShiftOdataList<ListDTO, _003F>)repository/*cast due to .constrained prefix*/).OdataList((IQueryable<ListDTO>)queryable);
				queryable2 = ((ODataQueryOptions)rebuiltOptions).Filter.ApplyTo((IQueryable)queryable2, new ODataQuerySettings()) as IQueryable<ListDTO>;
				return await EntityFrameworkQueryableExtensions.ToListAsync<ListDTO>(queryable2, default(CancellationToken));
			}
			finally
			{
				httpContext.Request.QueryString = originalQueryString;
			}
		}
		return await GetListDTOForSelectionAsync(httpContext, null, disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	public async Task<List<ListDTO>> GetSelectedListDTOsAsync(HttpContext httpContext, SelectStateDTO<ListDTO> ids, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (((SelectStateDTO<_003F>)(object)ids).All && !string.IsNullOrWhiteSpace(((SelectStateDTO<_003F>)(object)ids).Filter))
		{
			return await GetSelectedListDTOsAsync(httpContext, BuildODataQueryOptionsFromFilter(httpContext, ((SelectStateDTO<_003F>)(object)ids).Filter), disableDefaultDataLevelAccess, disableGlobalFilters);
		}
		return await GetListDTOForSelectionAsync(httpContext, ((SelectStateDTO<_003F>)(object)ids).All ? null : ((SelectStateDTO<_003F>)(object)ids)?.Items?.Select((ListDTO x) => ((ShiftEntityDTOBase)x).ID)?.ToList(), disableDefaultDataLevelAccess, disableGlobalFilters);
	}

	public async Task<List<Entity>> GetSelectedEntitiesAsync(HttpContext httpContext, ODataQueryOptions<ListDTO> oDataQueryOptions, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		IQueryable<Entity> query = await GetQueryForSelectionAsync(httpContext, disableDefaultDataLevelAccess, disableGlobalFilters);
		if (((ODataQueryOptions)oDataQueryOptions).Filter != null)
		{
			List<long> filteredIds = (await GetSelectedListDTOsAsync(httpContext, oDataQueryOptions, disableDefaultDataLevelAccess, disableGlobalFilters)).Select((ListDTO x) => HashIdExtensions.ToLong(((ShiftEntityDTOBase)x).ID)).ToList();
			query = query.Where((Entity x) => filteredIds.Contains(((ShiftEntityBase)x).ID));
		}
		if (query != null)
		{
			return await EntityFrameworkQueryableExtensions.ToListAsync<Entity>(query, default(CancellationToken));
		}
		return new List<Entity>();
	}

	public async Task<List<Entity>> GetSelectedEntitiesAsync(HttpContext httpContext, SelectStateDTO<ListDTO> ids, bool disableDefaultDataLevelAccess = false, bool disableGlobalFilters = false)
	{
		if (((SelectStateDTO<_003F>)(object)ids).All && !string.IsNullOrWhiteSpace(((SelectStateDTO<_003F>)(object)ids).Filter))
		{
			return await GetSelectedEntitiesAsync(httpContext, BuildODataQueryOptionsFromFilter(httpContext, ((SelectStateDTO<_003F>)(object)ids).Filter), disableDefaultDataLevelAccess, disableGlobalFilters);
		}
		IQueryable<Entity> queryable = await GetQueryForSelectionAsync(httpContext, disableDefaultDataLevelAccess, disableGlobalFilters);
		if (!((SelectStateDTO<_003F>)(object)ids).All)
		{
			IEnumerable<long> longIds = ((SelectStateDTO<_003F>)(object)ids).Items.Select((ListDTO x) => HashIdExtensions.ToLong(((ShiftEntityDTOBase)x).ID));
			queryable = queryable.Where((Entity x) => longIds.Contains(((ShiftEntityBase)x).ID));
		}
		return await EntityFrameworkQueryableExtensions.ToListAsync<Entity>(queryable, default(CancellationToken));
	}

	private static ODataQueryOptions<ListDTO> BuildODataQueryOptionsFromFilter(HttpContext httpContext, string filter)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		ODataConventionModelBuilder val = new ODataConventionModelBuilder();
		((ODataModelBuilder)val).EntitySet<ListDTO>("ListDTOs");
		IEdmModel edmModel = ((ODataModelBuilder)val).GetEdmModel();
		httpContext.Request.QueryString = httpContext.Request.QueryString.Add("$filter", filter);
		return (ODataQueryOptions<ListDTO>)(object)new ODataQueryOptions<_003F>(new ODataQueryContext(edmModel, typeof(ListDTO), new ODataPath(Array.Empty<ODataPathSegment>())), httpContext.Request);
	}

	internal static CrudResult HandleException(ShiftEntityException ex)
	{
		int httpStatusCode = ex.HttpStatusCode;
		ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>();
		((ShiftEntityResponse)obj).Message = ex.Message;
		((ShiftEntityResponse)obj).Additional = ex.AdditionalData;
		return CrudResult.Status(httpStatusCode, obj);
	}

	internal static ShiftEntityResponse<ViewAndUpsertDTO> BuildValidationErrorResponse(IReadOnlyDictionary<string, string[]> validationErrors, Dictionary<string, object>? additionalResponseData)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		ShiftEntityResponse<_003F> obj = new ShiftEntityResponse<_003F>();
		((ShiftEntityResponse)obj).Message = new Message
		{
			Title = "Model Validation Error",
			SubMessages = validationErrors.Select<KeyValuePair<string, string[]>, Message>((KeyValuePair<string, string[]> x) => new Message
			{
				Title = x.Key,
				For = x.Key,
				SubMessages = ((IEnumerable<string>)x.Value).Select((Func<string, Message>)((string e) => new Message
				{
					Title = e
				})).ToList()
			}).ToList()
		};
		((ShiftEntityResponse)obj).Additional = additionalResponseData;
		return (ShiftEntityResponse<ViewAndUpsertDTO>)(object)obj;
	}
}
