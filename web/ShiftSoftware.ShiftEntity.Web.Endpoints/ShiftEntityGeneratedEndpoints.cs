using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftEntity.Web.Endpoints;

/// <summary>
/// Generates minimal-API CRUD endpoints from entity endpoint attributes (see
/// <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityEndpointDiscovery" />) by invoking the existing
/// <see cref="T:ShiftSoftware.ShiftEntity.Web.Endpoints.ShiftEntityEndpointRouteBuilderExtensions" /> map methods via reflection — no endpoint
/// logic is duplicated.
/// </summary>
internal static class ShiftEntityGeneratedEndpoints
{
	private static readonly MethodInfo MapEntityEndpointMethod = typeof(ShiftEntityGeneratedEndpoints).GetMethod("MapEntityEndpoint", BindingFlags.Static | BindingFlags.NonPublic);

	internal static void Generate(IEndpointRouteBuilder routeBuilder, IEnumerable<ShiftEntityEndpointSpec> specs, Type dbType)
	{
		foreach (ShiftEntityEndpointSpec spec in specs)
		{
			Type type = ResolveRepositoryType(spec, dbType);
			ValidateRepository(type, spec);
			ReadWriteDeleteAction val = (spec.Secure ? ShiftEntityEndpointActionResolver.ResolveAction(spec.ActionTreeType, spec.ActionName) : null);
			MapEntityEndpointMethod.MakeGenericMethod(type, spec.Entity, spec.ListDto, spec.ViewDto).Invoke(null, new object[4] { routeBuilder, spec.Route, spec.Secure, val });
		}
	}

	private static void MapEntityEndpoint<TRepository, TEntity, TListDTO, TViewAndUpsertDTO>(IEndpointRouteBuilder endpoints, string route, bool secure, ReadWriteDeleteAction? action) where TRepository : IShiftRepositoryAsync<TEntity, TListDTO, TViewAndUpsertDTO> where TEntity : ShiftEntity<TEntity>, new() where TListDTO : ShiftEntityDTOBase where TViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
	{
		if (secure)
		{
			endpoints.MapShiftEntitySecureCrud<TRepository, TEntity, TListDTO, TViewAndUpsertDTO>(route, action);
		}
		else
		{
			endpoints.MapShiftEntityCrud<TRepository, TEntity, TListDTO, TViewAndUpsertDTO>(route);
		}
	}

	internal static Type ResolveRepositoryType(ShiftEntityEndpointSpec spec, Type dbType)
	{
		return spec.Repository ?? typeof(ShiftRepository<, , , >).MakeGenericType(dbType, spec.Entity, spec.ListDto, spec.ViewDto);
	}

	private static void ValidateRepository(Type repositoryType, ShiftEntityEndpointSpec spec)
	{
		Type type = typeof(IShiftRepositoryAsync<, , >).MakeGenericType(spec.Entity, spec.ListDto, spec.ViewDto);
		if (!type.IsAssignableFrom(repositoryType))
		{
			throw new InvalidOperationException($"Repository '{repositoryType.FullName}' for the endpoint '{spec.Route}' on entity '{spec.Entity.FullName}' must implement {type.FullName}. A custom repository must be a ShiftRepository<DB, {spec.Entity.Name}, {spec.ListDto.Name}, {spec.ViewDto.Name}> (or a subclass).");
		}
	}
}
