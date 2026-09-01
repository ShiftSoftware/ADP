using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;

namespace Microsoft.Extensions.DependencyInjection;

public static class IMvcBuilderExtensions
{
	/// <summary>
	/// Registers ShiftEntity Web services with the given configuration.
	/// Multiple calls to <c>services.Configure&lt;ShiftEntityOptions&gt;(...)</c> are additive.
	/// </summary>
	public static IMvcBuilder AddShiftEntityWeb(this IMvcBuilder builder, Action<ShiftEntityOptions> configure)
	{
		IServiceCollectionExtensions.AddShiftEntity(builder.Services, configure);
		return AddShiftEntityWebCore(builder);
	}

	/// <summary>
	/// Registers ShiftEntity Web infrastructure without configuring options.
	/// Options can be registered separately via <c>services.Configure&lt;ShiftEntityOptions&gt;(o =&gt; { ... })</c>.
	/// </summary>
	public static IMvcBuilder AddShiftEntityWeb(this IMvcBuilder builder)
	{
		IServiceCollectionExtensions.AddShiftEntity(builder.Services);
		return AddShiftEntityWebCore(builder);
	}

	private static IMvcBuilder AddShiftEntityWebCore(IMvcBuilder builder)
	{
		builder.Services.AddShiftEntityWebSharedCore();
		builder.Services.AddSingleton((Func<IServiceProvider, IConfigureOptions<ApiBehaviorOptions>>)delegate(IServiceProvider sp)
		{
			ShiftEntityOptions shiftEntityOptions = sp.GetRequiredService<ShiftEntityOptions>();
			return new ConfigureNamedOptions<ApiBehaviorOptions>(Microsoft.Extensions.Options.Options.DefaultName, delegate(ApiBehaviorOptions options)
			{
				if (shiftEntityOptions._WrapValidationErrorResponseWithShiftEntityResponse)
				{
					options.InvalidModelStateResponseFactory = delegate(ActionContext context)
					{
						Dictionary<string, ModelErrorCollection> source = Enumerable.Select(context.ModelState, (KeyValuePair<string, ModelStateEntry> x) => new
						{
							Key = x.Key,
							Errors = x.Value?.Errors
						}).ToDictionary(x => x.Key, x => x.Errors);
						ShiftEntityResponse<object> obj = new ShiftEntityResponse<object>();
						((ShiftEntityResponse)obj).Additional = ((IEnumerable<KeyValuePair<string, ModelErrorCollection>>)source).ToDictionary((Func<KeyValuePair<string, ModelErrorCollection>, string>)((KeyValuePair<string, ModelErrorCollection> x) => x.Key), (Func<KeyValuePair<string, ModelErrorCollection>, object>)((KeyValuePair<string, ModelErrorCollection> x) => x.Value?.Select((ModelError s) => s.ErrorMessage)));
						return new BadRequestObjectResult(obj);
					};
				}
			});
		});
		return builder;
	}
}
