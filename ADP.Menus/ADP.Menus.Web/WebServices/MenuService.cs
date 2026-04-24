using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.Menus.Web.WebServices;

public class MenuService
{
    private readonly HttpClient http;
    private readonly string prefix;
    private sealed class StockPayloadDTO
    {
        public string? PartNumber { get; set; }
        public decimal? Price { get; set; }
        public List<StockPriceByCountryDTO>? CountryPrices { get; set; }
    }

    public MenuService(HttpClient http, IOptions<MenuWebOptions> options)
    {
        this.http = http;
        this.prefix = options.Value.ResolvedRoutePrefix;
    }

    public async Task<StockDTO?> GetStockByPartNumberAsync(string partNumber)
    {
        var url = $"{prefix}Menu/StockByPartNumber/{partNumber}";
        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;


        return await response.Content.ReadFromJsonAsync<StockDTO>();
    }

    public async Task<IEnumerable<StockDTO>> GetStockByPartNumbersAsync(IEnumerable<string> partNumbers)
    {
        var url = $"{prefix}Menu/StockByPartNumbers?";

        // Add partnumbers as query string
        foreach (var partNumber in partNumbers)
            url+=$"partNumbers={partNumber}&";

        if (url.EndsWith("&"))
            url = url.Remove(url.Length - 1);

        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new List<StockDTO>();

        var payload = await response.Content.ReadFromJsonAsync<IEnumerable<StockPayloadDTO>>()
            ?? Enumerable.Empty<StockPayloadDTO>();

        return payload.Select(x => new StockDTO
        {
            PartNumber = x.PartNumber ?? string.Empty,
            Price = x.Price,
            CountryPrices = x.CountryPrices ?? []
        });
    }

    public async Task<IEnumerable<MenuVariantListDTO>> GetVariantsByMenuAsync(string menuID)
    {
        var response = await http.GetAsync($"{prefix}MenuVariant/ByMenu/{menuID}");
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IEnumerable<MenuVariantListDTO>>() ?? [];
    }

    public async Task<ShiftEntityResponse<MenuVariantDTO>?> GetVariantByIdAsync(string variantID)
    {
        var response = await http.GetAsync($"{prefix}MenuVariant/{variantID}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ShiftEntityResponse<MenuVariantDTO>>();
    }

    public async Task<HttpResponseMessage> SaveVariantAsync(MenuVariantDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ID))
            return await http.PostAsJsonAsync($"{prefix}MenuVariant", dto);

        return await http.PutAsJsonAsync($"{prefix}MenuVariant/{dto.ID}", dto);
    }

    public async Task<HttpResponseMessage> DeleteVariantWithGuardAsync(string variantID)
    {
        return await http.DeleteAsync($"{prefix}MenuVariant/DeleteWithGuard/{variantID}");
    }
}
