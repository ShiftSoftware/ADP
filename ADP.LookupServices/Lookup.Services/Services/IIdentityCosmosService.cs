using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    public interface IIdentityCosmosService
    {
        Task<IEnumerable<CompanyModel>> GetCompaniesAsync();
        Task<IEnumerable<CompanyBranchModel>> GetCompanyBranchesAsync();
        Task<CompanyModel> GetCompanyAsync(long? id);
        Task<CompanyBranchModel> GetCompanyBranchAsync(long? id);
        Task<CountryModel> GetCountryAsync(long? id);
        Task<RegionModel> GetRegionAsync(long? id);
    }
}