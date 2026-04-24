using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.Menus.Web.WebServices;

public class ServiceIntervalService
{
    private readonly HttpClient http;
    private readonly string prefix;

    public ServiceIntervalService(HttpClient http, IOptions<MenuWebOptions> options)
    {
        this.http = http;
        this.prefix = options.Value.ResolvedRoutePrefix;
    }

    public async Task<List<ServiceIntervalListSelectableUI>?> GetListAsync()
    {
        var url = $"{prefix}ServiceInterval";
        var response = await http.GetFromJsonAsync<ODataDTO<ServiceIntervalListDTO>>(url);

        return response?.Value?
            .Select(x => new ServiceIntervalListSelectableUI
            {
                Description = x.Description,
                ID = x.ID,
                FullName = x.FullName,
                IsDeleted = x.IsDeleted,
                IsSelected = false,
                Code = x.Code,
                ValueInMeter = x.ValueInMeter
            }).ToList();
    }
}
