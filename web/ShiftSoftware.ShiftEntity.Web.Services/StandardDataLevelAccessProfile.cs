using System;
using System.Linq.Expressions;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.DataLevelAccess;
using ShiftSoftware.ShiftEntity.Model.Flags;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Region;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftEntity.Web.Services;

/// <summary>
/// The standard data-level profile on the v2 engine: declares, per marker interface, the same dimension the legacy
/// <see cref="T:ShiftSoftware.ShiftEntity.Web.Services.DefaultDataLevelAccess" /> enforces — same TypeAuth action, same single key column, same hashid DTO
/// type-key, same self claim, honoring the same <see cref="T:ShiftSoftware.ShiftEntity.Core.DefaultDataLevelAccessOptions" /> disable flags. Parity
/// with legacy is the charter (Phase 4): an entity moved onto the profile must see byte-for-byte the rows it sees
/// today (the cross-column OR and the other v2 capabilities remain explicit, per-entity declarations).
/// </summary>
/// <remarks>
/// Built one dimension per slice — 4.1 Company, 4.2 Country; Region/Branch/Brand/City/Team follow (4.3–4.7) — each
/// proven against the real legacy implementation by the parity tests. The profile only <em>declares</em> dimensions;
/// nothing routes entities onto it automatically yet (that flip is decided once all seven are at parity). Note an
/// entity whose markers are all flag-disabled (or that has no markers) gets <em>no</em> dimensions — compiling a
/// policy from such an empty declaration throws by design (fail closed), so the future auto-wiring must declare a
/// policy only when at least one dimension landed.
/// </remarks>
public static class StandardDataLevelAccessProfile
{
	/// <summary>
	/// Adds the standard dimension for each marker interface <typeparamref name="TEntity" /> implements (unless the
	/// corresponding <paramref name="options" /> flag disables it), exactly as the legacy default filters would
	/// enforce it — one block per dimension, in the legacy dimension order. Currently covers:
	/// <b>Country</b> (<see cref="T:ShiftSoftware.ShiftEntity.Model.Flags.IEntityHasCountry`1" />, slice 4.2),
	/// <b>Region</b> (<see cref="T:ShiftSoftware.ShiftEntity.Model.Flags.IEntityHasRegion`1" />, slice 4.3),
	/// <b>Company</b> (<see cref="T:ShiftSoftware.ShiftEntity.Model.Flags.IEntityHasCompany`1" />, slice 4.1),
	/// <b>Branch</b> (<see cref="T:ShiftSoftware.ShiftEntity.Model.Flags.IEntityHasCompanyBranch`1" />, slice 4.4) and
	/// <b>City</b> (<see cref="T:ShiftSoftware.ShiftEntity.Model.Flags.IEntityHasCity`1" />, slice 4.6).
	/// </summary>
	public static DataLevelAccessBuilder<TEntity> AddStandardDimensions<TEntity>(this DataLevelAccessBuilder<TEntity> access, DefaultDataLevelAccessOptions options)
	{
		if (access == null)
		{
			throw new ArgumentNullException("access");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if (!options.DisableDefaultCountryFilter && typeof(IEntityHasCountry<TEntity>).IsAssignableFrom(typeof(TEntity)))
		{
			((DataLevelDimensionBuilder<CountryDTO>)(object)access.On((DynamicAction)(object)DataLevelAccess.Countries).Key((Expression<Func<TEntity, long?>>)((TEntity x) => ((IEntityHasCountry<TEntity>)x).CountryID))).HashId<CountryDTO>().Self("ShiftSoftware/ShiftEntity/Claims/CountryId");
		}
		if (!options.DisableDefaultRegionFilter && typeof(IEntityHasRegion<TEntity>).IsAssignableFrom(typeof(TEntity)))
		{
			((DataLevelDimensionBuilder<RegionDTO>)(object)access.On((DynamicAction)(object)DataLevelAccess.Regions).Key((Expression<Func<TEntity, long?>>)((TEntity x) => ((IEntityHasRegion<TEntity>)x).RegionID))).HashId<RegionDTO>().Self("ShiftSoftware/ShiftEntity/Claims/RegionId");
		}
		if (!options.DisableDefaultCompanyFilter && typeof(IEntityHasCompany<TEntity>).IsAssignableFrom(typeof(TEntity)))
		{
			((DataLevelDimensionBuilder<CompanyDTO>)(object)access.On((DynamicAction)(object)DataLevelAccess.Companies).Key((Expression<Func<TEntity, long?>>)((TEntity x) => ((IEntityHasCompany<TEntity>)x).CompanyID))).HashId<CompanyDTO>().Self("ShiftSoftware/ShiftEntity/Claims/CompanyId");
		}
		if (!options.DisableDefaultCompanyBranchFilter && typeof(IEntityHasCompanyBranch<TEntity>).IsAssignableFrom(typeof(TEntity)))
		{
			((DataLevelDimensionBuilder<CompanyBranchDTO>)(object)access.On((DynamicAction)(object)DataLevelAccess.Branches).Key((Expression<Func<TEntity, long?>>)((TEntity x) => ((IEntityHasCompanyBranch<TEntity>)x).CompanyBranchID))).HashId<CompanyBranchDTO>().Self("ShiftSoftware/ShiftEntity/Claims/CompanyBranchId");
		}
		if (!options.DisableDefaultCityFilter && typeof(IEntityHasCity<TEntity>).IsAssignableFrom(typeof(TEntity)))
		{
			((DataLevelDimensionBuilder<CityDTO>)(object)access.On((DynamicAction)(object)DataLevelAccess.Cities).Key((Expression<Func<TEntity, long?>>)((TEntity x) => ((IEntityHasCity<TEntity>)x).CityID))).HashId<CityDTO>().Self("ShiftSoftware/ShiftEntity/Claims/CityId");
		}
		return access;
	}
}
