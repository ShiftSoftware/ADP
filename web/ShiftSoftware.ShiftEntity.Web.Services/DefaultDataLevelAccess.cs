using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Brand;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Region;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Team;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;
using ShiftSoftware.TypeAuth.Core.Linq;

namespace ShiftSoftware.ShiftEntity.Web.Services;

public class DefaultDataLevelAccess : IDefaultDataLevelAccess
{
	private readonly ITypeAuthService typeAuthService;

	private readonly IdentityClaimProvider identityClaimProvider;

	private readonly IHashIdService hashIdService;

	public DefaultDataLevelAccess(ITypeAuthService typeAuthService, IdentityClaimProvider identityClaimProvider, IHashIdService hashIdService)
	{
		this.typeAuthService = typeAuthService;
		this.identityClaimProvider = identityClaimProvider;
		this.hashIdService = hashIdService;
	}

	private List<long?>? GetAccessibleItems<TDto>(DynamicReadWriteDeleteAction claim, params string[]? selfId)
	{
		return typeAuthService.GetReadableItems(claim, selfId).ConvertIds<long>((Func<string, long>)((string x) => hashIdService.Decode<TDto>(x)));
	}

	public List<long?>? GetAccessibleCountries()
	{
		DynamicReadWriteDeleteAction countries = DataLevelAccess.Countries;
		string[] array = new string[1];
		IdentityClaimProvider obj = identityClaimProvider;
		array[0] = ((obj != null) ? obj.GetHashedCountryID() : null);
		return GetAccessibleItems<CountryDTO>(countries, array);
	}

	public List<long?>? GetAccessibleRegions()
	{
		DynamicReadWriteDeleteAction regions = DataLevelAccess.Regions;
		string[] array = new string[1];
		IdentityClaimProvider obj = identityClaimProvider;
		array[0] = ((obj != null) ? obj.GetHashedRegionID() : null);
		return GetAccessibleItems<RegionDTO>(regions, array);
	}

	public List<long?>? GetAccessibleCities()
	{
		DynamicReadWriteDeleteAction cities = DataLevelAccess.Cities;
		string[] array = new string[1];
		IdentityClaimProvider obj = identityClaimProvider;
		array[0] = ((obj != null) ? obj.GetHashedCityID() : null);
		return GetAccessibleItems<CityDTO>(cities, array);
	}

	public List<long?>? GetAccessibleCompanies()
	{
		DynamicReadWriteDeleteAction companies = DataLevelAccess.Companies;
		string[] array = new string[1];
		IdentityClaimProvider obj = identityClaimProvider;
		array[0] = ((obj != null) ? obj.GetHashedCompanyID() : null);
		return GetAccessibleItems<CompanyDTO>(companies, array);
	}

	public List<long?>? GetAccessibleBranches()
	{
		DynamicReadWriteDeleteAction branches = DataLevelAccess.Branches;
		string[] array = new string[1];
		IdentityClaimProvider obj = identityClaimProvider;
		array[0] = ((obj != null) ? obj.GetHashedCompanyBranchID() : null);
		return GetAccessibleItems<CompanyBranchDTO>(branches, array);
	}

	public List<long?>? GetAccessibleTeams()
	{
		DynamicReadWriteDeleteAction teams = DataLevelAccess.Teams;
		IdentityClaimProvider obj = identityClaimProvider;
		return GetAccessibleItems<TeamDTO>(teams, (obj == null) ? null : obj.GetHashedTeamIDs()?.ToArray());
	}

	public List<long?>? GetAccessibleBrands()
	{
		return GetAccessibleItems<BrandDTO>(DataLevelAccess.Brands, Array.Empty<string>());
	}

	public bool HasDefaultDataLevelAccess<EntityType>(DefaultDataLevelAccessOptions defaultDataLevelAccessOptions, EntityType? entity, Access access) where EntityType : ShiftEntity<EntityType>, new()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		bool disableDefaultCountryFilter = defaultDataLevelAccessOptions.DisableDefaultCountryFilter;
		bool disableDefaultRegionFilter = defaultDataLevelAccessOptions.DisableDefaultRegionFilter;
		bool disableDefaultCompanyFilter = defaultDataLevelAccessOptions.DisableDefaultCompanyFilter;
		bool disableDefaultCompanyBranchFilter = defaultDataLevelAccessOptions.DisableDefaultCompanyBranchFilter;
		bool disableDefaultBrandFilter = defaultDataLevelAccessOptions.DisableDefaultBrandFilter;
		bool disableDefaultCityFilter = defaultDataLevelAccessOptions.DisableDefaultCityFilter;
		bool disableDefaultTeamFilter = defaultDataLevelAccessOptions.DisableDefaultTeamFilter;
		if (!disableDefaultCountryFilter && entity is IEntityHasCountry<EntityType> val && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Countries, access, (!(val?.CountryID).HasValue) ? null : hashIdService.Encode<CountryDTO>(val.CountryID.Value), new string[1] { identityClaimProvider.GetHashedCountryID() }))
		{
			return false;
		}
		if (!disableDefaultRegionFilter && entity is IEntityHasRegion<EntityType> val2 && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Regions, access, (!(val2?.RegionID).HasValue) ? null : hashIdService.Encode<RegionDTO>(val2.RegionID.Value), new string[1] { identityClaimProvider.GetHashedRegionID() }))
		{
			return false;
		}
		if (!disableDefaultCompanyFilter && entity is IEntityHasCompany<EntityType> val3 && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Companies, access, (!(val3?.CompanyID).HasValue) ? null : hashIdService.Encode<CompanyDTO>(val3.CompanyID.Value), new string[1] { identityClaimProvider.GetHashedCompanyID() }))
		{
			return false;
		}
		if (!disableDefaultCompanyBranchFilter && entity is IEntityHasCompanyBranch<EntityType> val4 && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Branches, access, (!(val4?.CompanyBranchID).HasValue) ? null : hashIdService.Encode<CompanyBranchDTO>(val4.CompanyBranchID.Value), new string[1] { identityClaimProvider.GetHashedCompanyBranchID() }))
		{
			return false;
		}
		if (!disableDefaultBrandFilter && entity is IEntityHasBrand<EntityType> val5 && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Brands, access, (!(val5?.BrandID).HasValue) ? null : hashIdService.Encode<BrandDTO>(val5.BrandID.Value), Array.Empty<string>()))
		{
			return false;
		}
		if (!disableDefaultCityFilter && entity is IEntityHasCity<EntityType> val6 && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Cities, access, (!(val6?.CityID).HasValue) ? null : hashIdService.Encode<CityDTO>(val6.CityID.Value), new string[1] { identityClaimProvider.GetHashedCityID() }))
		{
			return false;
		}
		if (!disableDefaultTeamFilter && entity is IEntityHasTeam<EntityType> val7 && !typeAuthService.Can((ActionBase)(object)DataLevelAccess.Teams, access, (!(val7?.TeamID).HasValue) ? null : hashIdService.Encode<TeamDTO>(val7.TeamID.Value), identityClaimProvider.GetHashedTeamIDs()?.ToArray()))
		{
			return false;
		}
		return true;
	}

	public IQueryable<EntityType> ApplyDefaultDataLevelFilters<EntityType>(DefaultDataLevelAccessOptions DefaultDataLevelAccessOptions, IQueryable<EntityType> query) where EntityType : notnull
	{
		bool disableDefaultCountryFilter = DefaultDataLevelAccessOptions.DisableDefaultCountryFilter;
		bool disableDefaultRegionFilter = DefaultDataLevelAccessOptions.DisableDefaultRegionFilter;
		bool disableDefaultCompanyFilter = DefaultDataLevelAccessOptions.DisableDefaultCompanyFilter;
		bool disableDefaultCompanyBranchFilter = DefaultDataLevelAccessOptions.DisableDefaultCompanyBranchFilter;
		bool disableDefaultBrandFilter = DefaultDataLevelAccessOptions.DisableDefaultBrandFilter;
		bool disableDefaultCityFilter = DefaultDataLevelAccessOptions.DisableDefaultCityFilter;
		bool disableDefaultTeamFilter = DefaultDataLevelAccessOptions.DisableDefaultTeamFilter;
		bool num = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasCountry<EntityType>)));
		bool flag = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasRegion<EntityType>)));
		bool flag2 = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasCompany<EntityType>)));
		bool flag3 = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasCompanyBranch<EntityType>)));
		bool flag4 = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasBrand<EntityType>)));
		bool flag5 = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasCity<EntityType>)));
		bool flag6 = typeof(EntityType).GetInterfaces().Any((Type x) => x.IsAssignableFrom(typeof(IEntityHasTeam<EntityType>)));
		if (num && !disableDefaultCountryFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleCountries(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasCountry<EntityType>)x).CountryID
			});
		}
		if (flag && !disableDefaultRegionFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleRegions(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasRegion<EntityType>)x).RegionID
			});
		}
		if (flag2 && !disableDefaultCompanyFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleCompanies(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasCompany<EntityType>)x).CompanyID
			});
		}
		if (flag3 && !disableDefaultCompanyBranchFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleBranches(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasCompanyBranch<EntityType>)x).CompanyBranchID
			});
		}
		if (flag4 && !disableDefaultBrandFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleBrands(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasBrand<EntityType>)x).BrandID
			});
		}
		if (flag5 && !disableDefaultCityFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleCities(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasCity<EntityType>)x).CityID
			});
		}
		if (flag6 && !disableDefaultTeamFilter)
		{
			query = AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleTeams(), new Expression<Func<EntityType, long?>>[1]
			{
				(EntityType x) => ((IEntityHasTeam<EntityType>)x).TeamID
			});
		}
		return query;
	}

	public IQueryable<EntityType> ApplyDefaultCountryFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasCountry<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleCountries(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasCountry<EntityType>)x).CountryID
		});
	}

	public IQueryable<EntityType> ApplyDefaultRegionFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasRegion<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleRegions(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasRegion<EntityType>)x).RegionID
		});
	}

	public IQueryable<EntityType> ApplyDefaultCompanyFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasCompany<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleCompanies(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasCompany<EntityType>)x).CompanyID
		});
	}

	public IQueryable<EntityType> ApplyDefaultBranchFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasCompanyBranch<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleBranches(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasCompanyBranch<EntityType>)x).CompanyBranchID
		});
	}

	public IQueryable<EntityType> ApplyDefaultBrandFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasBrand<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleBrands(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasBrand<EntityType>)x).BrandID
		});
	}

	public IQueryable<EntityType> ApplyDefaultCityFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasCity<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleCities(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasCity<EntityType>)x).CityID
		});
	}

	public IQueryable<EntityType> ApplyDefaultTeamFilter<EntityType>(IQueryable<EntityType> query) where EntityType : IEntityHasTeam<EntityType>
	{
		return AccessibleItemsQueryableExtensions.WhereIn<EntityType, long?>(query, GetAccessibleTeams(), new Expression<Func<EntityType, long?>>[1]
		{
			(EntityType x) => ((IEntityHasTeam<EntityType>)x).TeamID
		});
	}
}
