using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ShiftEntity.Web.Endpoints;

/// <summary>
/// Minimal-API counterpart of the controller pipeline's
/// <c>InvalidModelStateResponseFactory</c> (wired up in
/// <c>IMvcBuilderExtensions.AddShiftEntityWeb</c>). Runs DataAnnotations validation on
/// every non-null <see cref="T:ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntityViewAndUpsertDTO" /> argument of the endpoint
/// and, on failure, short-circuits with the exact same <see cref="T:ShiftSoftware.ShiftEntity.Model.ShiftEntityResponse`1" />
/// body the MVC factory produces — so clients see byte-compatible responses whether
/// they hit the controller path or the minimal API path.
/// </summary>
public sealed class ShiftEntityValidationEndpointFilter : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		foreach (object argument in context.Arguments)
		{
			ShiftEntityViewAndUpsertDTO val = (ShiftEntityViewAndUpsertDTO)((argument is ShiftEntityViewAndUpsertDTO) ? argument : null);
			if (val == null)
			{
				continue;
			}
			ValidationContext validationContext = new ValidationContext(val);
			List<ValidationResult> list = new List<ValidationResult>();
			if (Validator.TryValidateObject(val, validationContext, list, validateAllProperties: true))
			{
				continue;
			}
			Dictionary<string, string[]> source = (from x in list.SelectMany(delegate(ValidationResult r)
				{
					IEnumerable<string> source2;
					if (!r.MemberNames.Any())
					{
						IEnumerable<string> enumerable = new string[1] { string.Empty };
						source2 = enumerable;
					}
					else
					{
						source2 = r.MemberNames;
					}
					return source2.Select((string m) => new
					{
						Key = m,
						Error = (r.ErrorMessage ?? string.Empty)
					});
				})
				group x by x.Key).ToDictionary(g => g.Key, g => g.Select(x => x.Error).ToArray());
			ShiftEntityResponse<object> obj = new ShiftEntityResponse<object>();
			((ShiftEntityResponse)obj).Message = new Message
			{
				Title = "Model Validation Error",
				SubMessages = ((IEnumerable<KeyValuePair<string, string[]>>)source).Select((Func<KeyValuePair<string, string[]>, Message>)((KeyValuePair<string, string[]> x) => new Message
				{
					Title = x.Key,
					For = x.Key,
					SubMessages = ((IEnumerable<string>)x.Value).Select((Func<string, Message>)((string e) => new Message
					{
						Title = e
					})).ToList()
				})).ToList()
			};
			return Results.Json<ShiftEntityResponse<object>>(obj, (JsonSerializerOptions?)null, (string?)null, (int?)400);
		}
		return await next(context);
	}
}
