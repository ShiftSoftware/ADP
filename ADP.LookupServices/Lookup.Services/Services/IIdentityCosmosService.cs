using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services
{
    /// <summary>
    /// Service interface for retrieving identity data (companies, branches, countries, regions) from Cosmos DB.
    /// </summary>
    public interface IIdentityCosmosService
    {
        /// <summary>Retrieves all companies.</summary>
        Task<IEnumerable<CompanyModel>> GetCompaniesAsync();
        /// <summary>Retrieves all company branches.</summary>
        Task<IEnumerable<CompanyBranchModel>> GetCompanyBranchesAsync();
        /// <summary>Retrieves a company by ID.</summary>
        Task<CompanyModel> GetCompanyAsync(long? id);
        /// <summary>Retrieves a company branch by ID.</summary>
        Task<CompanyBranchModel> GetCompanyBranchAsync(long? id);
        /// <summary>Retrieves a country by ID.</summary>
        Task<CountryModel> GetCountryAsync(long? id);
        /// <summary>Retrieves a region by ID.</summary>
        Task<RegionModel> GetRegionAsync(long? id);
    }
}