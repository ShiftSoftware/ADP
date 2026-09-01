using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.HashIds;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;

namespace System.Linq;

public static class IQueryableExtensions
{
	public static async ValueTask<ODataDTO<T>> ToOdataDTO<T>(this IQueryable<T> data, ODataQueryOptions<T> oDataQueryOptions, HttpRequest httpRequest, bool isAsync = true, bool applySoftDeleteFilter = true, Func<IQueryable<T>, ValueTask<IQueryable<T>>>? applyPostODataProcessing = null) where T : ShiftEntityDTOBase
	{
		httpRequest.HttpContext.RequestServices.GetRequiredService<ShiftEntityOptions>();
		ITypeAuthService typeAuth = httpRequest.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
		if (((ODataQueryOptions)oDataQueryOptions).Top == null && typeAuth != null && !typeAuth.CanAccess(GeneralActionTree.DataGridExport))
		{
			throw new ShiftEntityException(new Message("Unrestricted Data Access Not Allowed", "Please specify a page size using the $top query parameter. You do not have permission to load unrestricted data sets."), 400, (Dictionary<string, object>)null);
		}
		if (((ODataQueryOptions)oDataQueryOptions).Filter != null)
		{
			FilterQueryOption filter = ((ODataQueryOptions)oDataQueryOptions).Filter;
			FilterClause val = ((filter != null) ? filter.FilterClause : null);
			IHashIdService requiredService = httpRequest.HttpContext.RequestServices.GetRequiredService<IHashIdService>();
			FilterClause filter2 = new FilterClause(((QueryNode)val.Expression).Accept<SingleValueNode>((QueryNodeVisitor<SingleValueNode>)(object)new HashIdQueryNodeVisitor<T>(requiredService)), val.RangeVariable);
			ODataUriParser val2 = new ODataUriParser(((ODataQueryOptions)oDataQueryOptions).Context.Model, new Uri("", UriKind.Relative));
			ODataUri obj = val2.ParseUri();
			obj.Filter = filter2;
			string text = ODataUriExtensions.BuildUri(obj, val2.UrlKeyDelimiter).ToString();
			QueryString queryString = new QueryString(text.Substring(text.IndexOf("?")));
			httpRequest.QueryString = queryString;
			ODataQueryOptions<T> val3 = new ODataQueryOptions<T>(((ODataQueryOptions)oDataQueryOptions).Context, httpRequest);
			data = ((ODataQueryOptions)val3).Filter.ApplyTo((IQueryable)data, new ODataQuerySettings
			{
				EnsureStableOrdering = true
			}) as IQueryable<T>;
		}
		if (applySoftDeleteFilter)
		{
			data = data.ApplyDefaultSoftDeleteFilter();
		}
		if (applyPostODataProcessing != null)
		{
			IQueryable<T> queryable = ((!isAsync) ? applyPostODataProcessing(data).Result : (await applyPostODataProcessing(data)));
			data = queryable;
		}
		if (((ODataQueryOptions)oDataQueryOptions).OrderBy != null)
		{
			data = ((ODataQueryOptions)oDataQueryOptions).OrderBy.ApplyTo<T>(data, new ODataQuerySettings
			{
				EnsureStableOrdering = true
			});
		}
		int num = ((!isAsync) ? data.Count() : (await EntityFrameworkQueryableExtensions.CountAsync<T>(data, default(CancellationToken))));
		int num2 = num;
		if (((ODataQueryOptions)oDataQueryOptions).Skip != null)
		{
			data = data.Skip(((ODataQueryOptions)oDataQueryOptions).Skip.Value);
		}
		int num3 = num2;
		if (((ODataQueryOptions)oDataQueryOptions).Top != null)
		{
			num3 = ((ODataQueryOptions)oDataQueryOptions).Top.Value;
		}
		int? num4 = null;
		if (typeAuth != null)
		{
			decimal? num5 = typeAuth.AccessValue(GeneralActionTree.DataGridMaxTop);
			if (num5.HasValue)
			{
				num4 = (int)num5.Value;
			}
		}
		if (num4 > 0 && num3 > num4)
		{
			throw new ShiftEntityException(new Message("Query Limit Exceeded", $"The requested number of records ({num3}) exceeds the maximum allowed limit of {num4}. Please reduce the page size."), 400, (Dictionary<string, object>)null);
		}
		if (num3 != num2)
		{
			data = data.Take(num3);
		}
		ODataDTO<T> val4 = new ODataDTO<T>
		{
			Count = num2
		};
		ODataDTO<T> val5 = val4;
		List<T> value = ((!isAsync) ? data.ToList() : (await EntityFrameworkQueryableExtensions.ToListAsync<T>(data, default(CancellationToken))));
		val5.Value = value;
		return val4;
	}

	private static IQueryable<EntityType> ApplyDefaultSoftDeleteFilter<EntityType>(this IQueryable<EntityType> query) where EntityType : ShiftEntityDTOBase
	{
		if (!IQueryableExtensions.HasWhereOnProperty<EntityType>(query, (Expression<Func<EntityType, object>>)((EntityType x) => ((ShiftEntityDTOBase)x).IsDeleted)))
		{
			query = query.Where((EntityType x) => !((ShiftEntityDTOBase)x).IsDeleted);
		}
		return query;
	}
}
