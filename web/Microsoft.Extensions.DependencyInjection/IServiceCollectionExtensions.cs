using System;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.HashIds;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
	/// <summary>
	/// Wires the DI-aware HashId <see cref="T:System.Text.Json.Serialization.Metadata.JsonTypeInfo" /> modifier into both AspNetCore JSON
	/// pipelines so identity-hashed properties (Brand/Country/User/...) serialize through
	/// <c>IHashIdService.GetHasherFor</c> at type-info build time, picking up the correct
	/// identity salt from <see cref="T:ShiftSoftware.ShiftEntity.Core.HashIdOptions" /> instead of the legacy
	/// attribute-construction-time path. <c>AddShiftEntityWeb</c> and
	/// <c>AddShiftEntityFunctions</c> both call this internally; only call it directly from
	/// custom hosts that don't go through either entry point.
	/// </summary>
	public static IServiceCollection AddShiftEntityHashIdJsonSupport(this IServiceCollection services)
	{
		services.AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>().Configure(delegate(Microsoft.AspNetCore.Mvc.JsonOptions o, IHashIdService hashIdService)
		{
			IJsonTypeInfoResolver resolver = o.JsonSerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
			o.JsonSerializerOptions.TypeInfoResolver = resolver.WithAddedModifier(HashIdJsonTypeInfoResolverModifier.Create(hashIdService));
		});
		services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>().Configure(delegate(Microsoft.AspNetCore.Http.Json.JsonOptions o, IHashIdService hashIdService)
		{
			IJsonTypeInfoResolver resolver = o.SerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
			o.SerializerOptions.TypeInfoResolver = resolver.WithAddedModifier(HashIdJsonTypeInfoResolverModifier.Create(hashIdService));
		});
		return services;
	}

	/// <summary>
	/// Registers ShiftEntity services for an Azure Functions Worker (Isolated) host that uses
	/// the AspNetCore extension (<c>ConfigureFunctionsWebApplication</c>). Wires the same
	/// HashId JSON middleware, JSON naming policy, Azure Storage converters and HTTP/identity
	/// plumbing as <c>AddShiftEntityWeb</c>, but skips the MVC controller pipeline pieces
	/// (<see cref="T:Microsoft.AspNetCore.Mvc.ApiBehaviorOptions" /> / <c>[ApiController]</c> validation factory) that
	/// have no effect outside MVC.
	/// </summary>
	public static IServiceCollection AddShiftEntityFunctions(this IServiceCollection services, Action<ShiftEntityOptions> configure)
	{
		IServiceCollectionExtensions.AddShiftEntity(services, configure);
		return services.AddShiftEntityWebSharedCore();
	}

	/// <summary>
	/// Registers ShiftEntity Functions infrastructure without configuring options.
	/// Options can be registered separately via <c>services.Configure&lt;ShiftEntityOptions&gt;(o =&gt; { ... })</c>.
	/// </summary>
	public static IServiceCollection AddShiftEntityFunctions(this IServiceCollection services)
	{
		IServiceCollectionExtensions.AddShiftEntity(services);
		return services.AddShiftEntityWebSharedCore();
	}

	/// <summary>
	/// Shared registrations used by both <c>AddShiftEntityWeb</c> (MVC) and
	/// <c>AddShiftEntityFunctions</c> (Functions Worker AspNetCore). Caller is responsible for
	/// running the core <c>AddShiftEntity</c> step before this — both entry points do so.
	/// </summary>
	internal static IServiceCollection AddShiftEntityWebSharedCore(this IServiceCollection services)
	{
		services.AddHttpContextAccessor().AddLocalization();
		services.AddShiftEntityHashIdJsonSupport();
		services.AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>().Configure(delegate(Microsoft.AspNetCore.Mvc.JsonOptions o, ShiftEntityOptions shiftEntityOptions, AzureStorageService azureStorageService)
		{
			o.JsonSerializerOptions.PropertyNamingPolicy = shiftEntityOptions.JsonNamingPolicy;
			if (shiftEntityOptions.azureStorageOptions.Count > 0)
			{
				o.RegisterAzureStorageServiceConverters(azureStorageService);
			}
		});
		services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>().Configure(delegate(Microsoft.AspNetCore.Http.Json.JsonOptions o, ShiftEntityOptions shiftEntityOptions, AzureStorageService azureStorageService)
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			o.SerializerOptions.PropertyNamingPolicy = shiftEntityOptions.JsonNamingPolicy;
			if (shiftEntityOptions.azureStorageOptions.Count > 0)
			{
				o.SerializerOptions.Converters.Add((JsonConverter)new JsonShiftFileDTOConverter(azureStorageService));
			}
		});
		services.AddScoped<IDefaultDataLevelAccess, DefaultDataLevelAccess>();
		services.AddScoped<IdentityClaimProvider>();
		services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
		IServiceCollectionExtensions.AddShiftEntityDataLevelAccess(services);
		return services;
	}
}
