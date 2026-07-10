using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.SSCCampaignCode;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Controllers;

/// <summary>
/// Lists the distinct SSC (Special Service Campaign) campaign codes present in the affected-VIN
/// Cosmos documents, for autocompletes over the warranty claims' campaign-code fields. Moved from
/// the original host application (Phase 3 Slice 3.5, D23) at its exact original route. Requires only
/// authentication (no TypeAuth node), exactly like the original.
/// </summary>
[Route("[controller]")]
[Authorize]
public class SSCCampaignCodeController : ControllerBase
{
    private readonly CosmosClient cosmosClient;

    public SSCCampaignCodeController(CosmosClient cosmosClient)
    {
        this.cosmosClient = cosmosClient;
    }

    [HttpGet]
    public async Task<ActionResult<ODataDTO<SSCCampaignCodeListDTO>>> Get(ODataQueryOptions<SSCCampaignCodeListDTO> oDataQueryOptions)
    {
        var container = cosmosClient.GetContainer(
            NoSQLConstants.Databases.CompanyData,
            NoSQLConstants.Containers.Vehicles
        );

        var queryDefinition = new QueryDefinition(
            "SELECT c.CampaignCode FROM c WHERE c.ItemType = @itemType GROUP BY c.CampaignCode"
        ).WithParameter("@itemType", (string)ModelTypes.SSCAffectedVIN);

        var iterator = container.GetItemQueryIterator<CampaignCodeResult>(queryDefinition);

        var campaignCodes = new List<SSCCampaignCodeListDTO>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();

            foreach (var item in response)
            {
                campaignCodes.Add(new SSCCampaignCodeListDTO
                {
                    ID = item.CampaignCode,
                    Code = item.CampaignCode
                });
            }
        }

        IQueryable<SSCCampaignCodeListDTO> data = campaignCodes.AsQueryable();

        if (oDataQueryOptions.Filter != null)
        {
            var filterRaw = oDataQueryOptions.Filter.RawValue;

            if (filterRaw != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(filterRaw, @"contains\(\w+,'([^']*)'\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var searchTerm = match.Groups[1].Value;
                    data = data.Where(x => x.Code != null && x.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        if (oDataQueryOptions.OrderBy != null)
            data = oDataQueryOptions.OrderBy.ApplyTo(data, new ODataQuerySettings());

        var count = data.Count();

        if (oDataQueryOptions.Skip != null)
            data = data.Skip(oDataQueryOptions.Skip.Value);

        var top = oDataQueryOptions.Top?.Value ?? 25;
        data = data.Take(top);

        return Ok(new ODataDTO<SSCCampaignCodeListDTO>
        {
            Count = count,
            Value = data.ToList()
        });
    }

    private class CampaignCodeResult
    {
        public string CampaignCode { get; set; } = default!;
    }
}
