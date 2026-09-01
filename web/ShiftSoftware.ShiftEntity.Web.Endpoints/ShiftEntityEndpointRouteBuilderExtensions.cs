using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.AspNetCore.EndpointFilters;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftEntity.Web.Endpoints;

/// <summary>
/// Minimal-API entry points that mirror the two controller base classes one-for-one:
/// <list type="bullet">
///   <item><see cref="M:ShiftSoftware.ShiftEntity.Web.Endpoints.ShiftEntityEndpointRouteBuilderExtensions.MapShiftEntityCrud``4(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder,System.String)" />
///   — counterpart of <c>ShiftEntityControllerAsync</c> (no auth).</item>
///   <item><see cref="M:ShiftSoftware.ShiftEntity.Web.Endpoints.ShiftEntityEndpointRouteBuilderExtensions.MapShiftEntitySecureCrud``4(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder,System.String,ShiftSoftware.TypeAuth.Core.Actions.ReadWriteDeleteAction)" />
///   — counterpart of <c>ShiftEntitySecureControllerAsync</c>
///   (<c>RequireAuthorization</c> + TypeAuth filter per verb).</item>
/// </list>
/// Both share a single <see cref="T:ShiftSoftware.ShiftEntity.Web.ShiftEntityCrudHandler`4" />
/// so there is only one source of truth for CRUD logic.
///
/// Each method has an overload accepting
/// <see cref="T:System.Action`1" /> for overriding individual endpoint
/// handlers — the minimal-API equivalent of overriding virtual methods in a controller.
/// </summary>
public static class ShiftEntityEndpointRouteBuilderExtensions
{
	private static readonly ConcurrentDictionary<Type, ODataQueryContext> _odataContextCache = new ConcurrentDictionary<Type, ODataQueryContext>();

	/// <summary>
	/// Registers the CRUD/revisions/print endpoints with no authentication or
	/// authorization — the minimal-API counterpart of
	/// <see cref="T:ShiftSoftware.ShiftEntity.Web.ShiftEntityControllerAsync`4" />.
	/// </summary>
	public static RouteGroupBuilder MapShiftEntityCrud<Repository, Entity, ListDTO, ViewAndUpsertDTO>(this IEndpointRouteBuilder endpoints, string prefix) where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
	{
		RouteGroupBuilder routeGroupBuilder = endpoints.MapGroup(prefix);
		MapCrudEndpointsCore<Repository, Entity, ListDTO, ViewAndUpsertDTO>(routeGroupBuilder, prefix, secure: false, (ReadWriteDeleteAction?)null, (ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>?)null);
		return routeGroupBuilder;
	}

	/// <summary>
	/// Registers the CRUD/revisions/print endpoints with no authentication or
	/// authorization, with per-endpoint override support.
	/// </summary>
	public static RouteGroupBuilder MapShiftEntityCrud<Repository, Entity, ListDTO, ViewAndUpsertDTO>(this IEndpointRouteBuilder endpoints, string prefix, Action<ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>> configure) where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
	{
		ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO> shiftEntityEndpointConfig = new ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>();
		configure(shiftEntityEndpointConfig);
		RouteGroupBuilder routeGroupBuilder = endpoints.MapGroup(prefix);
		MapCrudEndpointsCore<Repository, Entity, ListDTO, ViewAndUpsertDTO>(routeGroupBuilder, prefix, secure: false, (ReadWriteDeleteAction?)null, shiftEntityEndpointConfig);
		return routeGroupBuilder;
	}

	/// <summary>
	/// Registers the CRUD/revisions/print endpoints behind <c>RequireAuthorization</c>
	/// plus a TypeAuth endpoint filter per verb (Read on GET, Write on POST/PUT,
	/// Delete on DELETE) — the minimal-API counterpart of
	/// <see cref="T:ShiftSoftware.ShiftEntity.Web.ShiftEntitySecureControllerAsync`4" />.
	///
	/// Pass <paramref name="action" /> = <c>null</c> to require authentication without
	/// any TypeAuth permission check (matches the secure controller with a null action).
	/// </summary>
	public static RouteGroupBuilder MapShiftEntitySecureCrud<Repository, Entity, ListDTO, ViewAndUpsertDTO>(this IEndpointRouteBuilder endpoints, string prefix, ReadWriteDeleteAction? action) where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
	{
		RouteGroupBuilder routeGroupBuilder = endpoints.MapGroup(prefix).RequireAuthorization();
		MapCrudEndpointsCore<Repository, Entity, ListDTO, ViewAndUpsertDTO>(routeGroupBuilder, prefix, secure: true, action, (ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>?)null);
		return routeGroupBuilder;
	}

	/// <summary>
	/// Registers the CRUD/revisions/print endpoints behind <c>RequireAuthorization</c>
	/// plus a TypeAuth endpoint filter per verb, with per-endpoint override support.
	///
	/// Pass <paramref name="action" /> = <c>null</c> to require authentication without
	/// any TypeAuth permission check.
	/// </summary>
	public static RouteGroupBuilder MapShiftEntitySecureCrud<Repository, Entity, ListDTO, ViewAndUpsertDTO>(this IEndpointRouteBuilder endpoints, string prefix, ReadWriteDeleteAction? action, Action<ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>> configure) where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
	{
		ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO> shiftEntityEndpointConfig = new ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>();
		configure(shiftEntityEndpointConfig);
		RouteGroupBuilder routeGroupBuilder = endpoints.MapGroup(prefix).RequireAuthorization();
		MapCrudEndpointsCore<Repository, Entity, ListDTO, ViewAndUpsertDTO>(routeGroupBuilder, prefix, secure: true, action, shiftEntityEndpointConfig);
		return routeGroupBuilder;
	}

	private static void MapCrudEndpointsCore<Repository, Entity, ListDTO, ViewAndUpsertDTO>(RouteGroupBuilder group, string prefix, bool secure, ReadWriteDeleteAction? action, ShiftEntityEndpointConfig<Repository, Entity, ListDTO, ViewAndUpsertDTO>? config) where Repository : IShiftRepositoryAsync<Entity, ListDTO, ViewAndUpsertDTO> where Entity : ShiftEntity<Entity>, new() where ListDTO : ShiftEntityDTOBase where ViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
	{
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Expected O, but got Unknown
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Expected O, but got Unknown
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Expected O, but got Unknown
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Expected O, but got Unknown
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Expected O, but got Unknown
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Expected O, but got Unknown
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Expected O, but got Unknown
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Expected O, but got Unknown
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Expected O, but got Unknown
		ShiftEntityCrudHandler<Repository, Entity, ListDTO, ViewAndUpsertDTO> handler = new ShiftEntityCrudHandler<Repository, Entity, ListDTO, ViewAndUpsertDTO>();
		Func<HttpContext, Task<IResult>> defaultGetList = async delegate(HttpContext ctx)
		{
			ODataQueryOptions<ListDTO> oDataQueryOptions = ShiftEntityEndpointRouteBuilderExtensions.BuildODataQueryOptions<ListDTO>(ctx.Request);
			return Results.Ok<ODataDTO<ListDTO>>(await handler.GetListAsync(ctx, oDataQueryOptions));
		};
		Func<HttpContext, string, DateTimeOffset?, Task<IResult>> defaultGetSingle = async delegate(HttpContext ctx, string key, DateTimeOffset? asOf)
		{
			CrudResult item = (await handler.GetSingleAsync(ctx, key, asOf)).Item1;
			if (item.IsTemporal)
			{
				ctx.Response.Headers.Append("Versioning", "Temporal");
			}
			return ToMinimalApiResult(item);
		};
		Func<HttpContext, string, Task<IResult>> defaultGetRevisions = async delegate(HttpContext ctx, string key)
		{
			ODataQueryOptions<RevisionDTO> oDataQueryOptions = BuildODataQueryOptions<RevisionDTO>(ctx.Request);
			return Results.Ok<ODataDTO<RevisionDTO>>(await handler.GetRevisionsAsync(ctx, key, oDataQueryOptions));
		};
		Func<HttpContext, ViewAndUpsertDTO, Task<IResult>> defaultPost = async delegate(HttpContext ctx, ViewAndUpsertDTO dto)
		{
			CrudResult item = (await handler.PostAsync(ctx, dto)).Item1;
			return (item.CreatedAtKey != null) ? Results.Created(prefix.TrimEnd('/') + "/" + item.CreatedAtKey, item.Body) : ToMinimalApiResult(item);
		};
		Func<HttpContext, string, ViewAndUpsertDTO, Task<IResult>> defaultPut = async (HttpContext ctx, string key, ViewAndUpsertDTO dto) => ToMinimalApiResult((await handler.PutAsync(ctx, key, dto)).Item1);
		Func<HttpContext, string, Task<IResult>> defaultDelete = async (HttpContext ctx, string key) => ToMinimalApiResult((await handler.DeleteAsync(ctx, key)).Item1);
		string normalizedPrefix = "/" + prefix.Trim('/');
		Func<HttpContext, string, Task<IResult>> defaultPrint = async delegate(HttpContext ctx, string key)
		{
			if (secure)
			{
				string expires = ctx.Request.Query["expires"].ToString();
				string token = ctx.Request.Query["token"].ToString();
				if (!handler.ValidatePrintSASToken(ctx, key, PrintTokenDescriptor(key), expires, token))
				{
					return Results.Forbid();
				}
			}
			CrudResult crudResult = await handler.PrintAsync(ctx, key);
			return (crudResult.Stream != null) ? Results.Stream(crudResult.Stream, crudResult.ContentType ?? "application/octet-stream") : ToMinimalApiResult(crudResult);
		};
		Func<HttpContext, string, Task<IResult>> defaultPrintToken = async (HttpContext ctx, string key) => ToMinimalApiResult(await handler.PrintTokenAsync(ctx, key, PrintTokenDescriptor(key)));
		RouteHandlerBuilder builder = group.MapGet("", (Func<HttpContext, Task<IResult>>)(async (HttpContext ctx) => (config?._getListOverride == null) ? (await defaultGetList(ctx)) : (await config._getListOverride(defaultGetList, ctx))));
		RouteHandlerBuilder builder2 = group.MapGet("/{key}", (Func<HttpContext, string, DateTimeOffset?, Task<IResult>>)(async (HttpContext ctx, string key, DateTimeOffset? asOf) => (config?._getSingleOverride == null) ? (await defaultGetSingle(ctx, key, asOf)) : (await config._getSingleOverride(defaultGetSingle, ctx, key, asOf))));
		RouteHandlerBuilder builder3 = group.MapGet("/{key}/revisions", (Func<HttpContext, string, Task<IResult>>)(async (HttpContext ctx, string key) => (config?._getRevisionsOverride == null) ? (await defaultGetRevisions(ctx, key)) : (await config._getRevisionsOverride(defaultGetRevisions, ctx, key))));
		RouteHandlerBuilder builder4 = group.MapPost("", (Func<HttpContext, ViewAndUpsertDTO, Task<IResult>>)(async (HttpContext ctx, ViewAndUpsertDTO dto) => (config?._postOverride == null) ? (await defaultPost(ctx, dto)) : (await config._postOverride(defaultPost, ctx, dto)))).AddEndpointFilter<ShiftEntityValidationEndpointFilter>();
		RouteHandlerBuilder builder5 = group.MapPut("/{key}", (Func<HttpContext, string, ViewAndUpsertDTO, Task<IResult>>)(async (HttpContext ctx, string key, ViewAndUpsertDTO dto) => (config?._putOverride == null) ? (await defaultPut(ctx, key, dto)) : (await config._putOverride(defaultPut, ctx, key, dto)))).AddEndpointFilter<ShiftEntityValidationEndpointFilter>();
		RouteHandlerBuilder builder6 = group.MapDelete("/{key}", (Func<HttpContext, string, Task<IResult>>)(async (HttpContext ctx, string key) => (config?._deleteOverride == null) ? (await defaultDelete(ctx, key)) : (await config._deleteOverride(defaultDelete, ctx, key))));
		RouteHandlerBuilder builder7 = group.MapGet("/print/{key}", (Func<HttpContext, string, Task<IResult>>)(async (HttpContext ctx, string key) => (config?._printOverride == null) ? (await defaultPrint(ctx, key)) : (await config._printOverride(defaultPrint, ctx, key))));
		if (secure)
		{
			builder7.AllowAnonymous();
		}
		RouteHandlerBuilder builder8 = null;
		if (secure)
		{
			builder8 = group.MapGet("/print-token/{key}", (Func<HttpContext, string, Task<IResult>>)(async (HttpContext ctx, string key) => (config?._printTokenOverride == null) ? (await defaultPrintToken(ctx, key)) : (await config._printTokenOverride(defaultPrintToken, ctx, key))));
		}
		RouteHandlerBuilder builder9 = group.MapGet("/{key}/attention", (Func<HttpContext, string, Task<IResult>>)(async (HttpContext ctx, string key) => ToMinimalApiResult(await handler.GetAttentionSignalsAsync(ctx, key))));
		RouteHandlerBuilder builder10 = group.MapPost("/{key}/attention/clear", (Func<HttpContext, string, AttentionClearFilter, Task<IResult>>)(async (HttpContext ctx, string key, AttentionClearFilter? filter) => ToMinimalApiResult(await handler.ClearAttentionSignalsAsync(ctx, key, filter))));
		if (secure && action != null)
		{
			ShiftEntityActionMap? service = ((IEndpointRouteBuilder)group).ServiceProvider.GetService<ShiftEntityActionMap>();
			if (service != null)
			{
				service.Register(typeof(Entity).Name, action);
			}
			builder.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)1));
			builder2.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)1));
			builder3.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)1));
			builder8.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)1));
			builder4.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)2));
			builder5.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)2));
			builder6.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)3));
			builder9.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)1));
			builder10.AddEndpointFilter((IEndpointFilter)new TypeAuthEndpointFilter((ActionBase)(object)action, (Access)2));
		}
		string PrintTokenDescriptor(string k)
		{
			return normalizedPrefix + "/print-token/" + k;
		}
	}

	private static IResult ToMinimalApiResult(CrudResult result)
	{
		if (result.Stream != null)
		{
			return Results.Stream(result.Stream, result.ContentType ?? "application/octet-stream");
		}
		return Results.Json(result.Body, (JsonSerializerOptions?)null, (string?)null, (int?)result.StatusCode);
	}

	/// <summary>
	/// Builds an <see cref="T:Microsoft.AspNetCore.OData.Query.ODataQueryOptions`1" /> from the current request's query
	/// string. Minimal API has no first-class binder for <see cref="T:Microsoft.AspNetCore.OData.Query.ODataQueryOptions`1" />,
	/// so we construct it manually from an EDM model built via
	/// <see cref="T:Microsoft.OData.ModelBuilder.ODataConventionModelBuilder" />. The per-type EDM context is cached
	/// to avoid rebuilding the model on every request.
	/// </summary>
	internal static ODataQueryOptions<T> BuildODataQueryOptions<T>(HttpRequest request) where T : class
	{
		return new ODataQueryOptions<T>(_odataContextCache.GetOrAdd(typeof(T), (Func<Type, ODataQueryContext>)delegate
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Expected O, but got Unknown
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			ODataConventionModelBuilder val = new ODataConventionModelBuilder();
			((ODataModelBuilder)val).EntitySet<T>(typeof(T).Name + "Set");
			return new ODataQueryContext(((ODataModelBuilder)val).GetEdmModel(), typeof(T), new ODataPath(Array.Empty<ODataPathSegment>()));
		}), request);
	}
}
