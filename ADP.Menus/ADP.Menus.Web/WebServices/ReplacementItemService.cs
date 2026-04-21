using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.Menus.Web.WebServices;

public class ReplacementItemService
{
    private readonly HttpClient http;
    private readonly string prefix;

    public ReplacementItemService(HttpClient http, IOptions<MenuWebOptions> options)
    {
        this.http = http;
        this.prefix = options.Value.ResolvedRoutePrefix;
    }

    public async Task<Dictionary<string, VehicelModelReplacementItemUI>?> GetListAsync()
    {
        var url = $"{prefix}ReplacementItem?filter= {nameof(ReplacementItemListDTO.IsDeleted)} eq false";
        var response = await http.GetFromJsonAsync<ODataDTO<ReplacementItemListDTO>>(url);
        return response?.Value?
             .Select(x => new VehicelModelReplacementItemUI
             {
                 ID = x.ID,
                 Name = x.Name,
                 Type = x.Type,
                 AllowMultiplePartNumbers = x.AllowMultiplePartNumbers,
                 DefaultPartPriceMarginPercentage = x.DefaultPartPriceMarginPercentage,
                 StandaloneLabourCode = x.StandaloneLabourCode,
                 StandaloneOperationCode = x.StandaloneOperationCode,
                 StandaloneReplacementItemGroup = x.StandaloneReplacementItemGroup
             })
             .OrderBy(x => x.StandaloneReplacementItemGroup?.Value)
             .ToDictionary(x => x.ID!, x => x);
    }

    public async Task<HttpResponse<VehicleModelMenuDTO>> GetVehicleModelById(string id)
    {
        var url = $"{prefix}VehicleModel/GetById/{id}";
        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new HttpResponse<VehicleModelMenuDTO>(await response.Content.ReadAsStringAsync(), response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<VehicleModelMenuDTO>();
        return new HttpResponse<VehicleModelMenuDTO>(data!, response.StatusCode);
    }

    public async Task<List<ReplacementItemMenuUsageDTO>> CheckReplacementItemMenuUsageAsync(string vehicleModelKey, IEnumerable<string> replacementItemIds)
    {
        var ids = replacementItemIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return new List<ReplacementItemMenuUsageDTO>();

        var url = $"{prefix}VehicleModel/CheckReplacementItemMenuUsage/{vehicleModelKey}";
        var response = await http.PostAsJsonAsync(url, new ReplacementItemMenuUsageRequestDTO { ReplacementItemIDs = ids });

        if (!response.IsSuccessStatusCode)
            return new List<ReplacementItemMenuUsageDTO>();

        return await response.Content.ReadFromJsonAsync<List<ReplacementItemMenuUsageDTO>>() ?? [];
    }
}
