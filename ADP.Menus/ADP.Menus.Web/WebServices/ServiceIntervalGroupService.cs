using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.Menus.Web.WebServices;

public class ServiceIntervalGroupService
{
    private readonly HttpClient http;
    private readonly string prefix;

    public ServiceIntervalGroupService(HttpClient http, IOptions<MenuWebOptions> options)
    {
        this.http = http;
        this.prefix = options.Value.ResolvedRoutePrefix;
    }

    public async Task<List<ServiceIntervalGroupListDTO>?> GetListAsync()
    {
        var url = $"{prefix}ServiceIntervalGroup?filter= {nameof(ServiceIntervalGroupListDTO.IsDeleted)} eq false";
        var response = await http.GetFromJsonAsync<ODataDTO<ServiceIntervalGroupListDTO>>(url);
        return response?.Value;
    }

    public async Task<Dictionary<string, string>?> GetDictionaryLabourDetailsAsync()
    {
        return (await GetListAsync())?.ToDictionary(x => x.ID!, x => x.Name);
    }
}
