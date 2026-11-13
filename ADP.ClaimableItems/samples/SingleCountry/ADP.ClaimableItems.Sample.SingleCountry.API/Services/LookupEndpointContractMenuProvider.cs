using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.ClaimableItems.Sample.SingleCountry.API.Services;

/// <summary>
/// Reads the vehicle and the contract menus from the single-country menus sample's lookup functions
/// (<c>Sample:MenuLookupUrl</c>, the functions host's base URL): <c>api/vehicle/{vin}</c> for a VIN and
/// <c>api/menu/{basicModelCode}</c> for a model the user picked. Server-to-server calls: the function key, when
/// the host needs one, is <c>Sample:MenuLookupFunctionKey</c>. One country, so no country or transfer rate is
/// sent: the functions price the menu themselves.
/// </summary>
public class LookupEndpointContractMenuProvider : IClaimableItemContractMenuProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient http;
    private readonly IMenuCountryProvider countryProvider;
    private readonly IHashIdService hashIdService;
    private readonly string baseUrl;
    private readonly string? functionKey;

    public LookupEndpointContractMenuProvider(
        HttpClient http,
        IMenuCountryProvider countryProvider,
        IHashIdService hashIdService,
        IConfiguration configuration)
    {
        this.http = http;
        this.countryProvider = countryProvider;
        this.hashIdService = hashIdService;
        baseUrl = (configuration["Sample:MenuLookupUrl"] ?? "").TrimEnd('/');
        functionKey = configuration["Sample:MenuLookupFunctionKey"];
    }

    public async ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync() =>
        (await countryProvider.GetSupportedCountriesAsync())
            .Select(x => new ClaimableItemContractCountry { ID = x.Id, Name = x.Name })
            .ToList();

    public async Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language)
    {
        using var response = await GetAsync($"api/vehicle/{Uri.EscapeDataString(vin)}", language);

        // A lookup with no vehicle data answers 503: every VIN is then one the user picks a model for.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.ServiceUnavailable)
            return new ClaimableItemContractVehicleMenu { MenuStatus = ClaimableItemContractVehicleMenuStatus.NotFound };

        EnsureSuccess(response, vin);

        var lookup = await response.Content.ReadFromJsonAsync<VehicleLookup>(Json)
            ?? throw new InvalidOperationException($"The vehicle lookup returned nothing for {vin}.");

        var menu = lookup.ServiceMenu;

        return new ClaimableItemContractVehicleMenu
        {
            IsOfficial = lookup.IsAuthorized,
            Katashiki = lookup.Identifiers?.Katashiki,
            BasicModelCode = lookup.BasicModelCode,
            Variant = lookup.Identifiers?.Variant,
            ModelCode = lookup.VehicleSpecification?.ModelCode,
            ModelDescription = lookup.VehicleSpecification?.ModelDescription,
            // The identity server's hash, decoded with the identity hash-id settings this API registers.
            BrandID = lookup.Identifiers?.BrandID is { Length: > 0 } brandId ? hashIdService.Decode(brandId, new BrandHashIdConverter()) : null,
            MenuStatus = menu?.Status switch
            {
                "Found" => ClaimableItemContractVehicleMenuStatus.Found,
                "NoBasicModelCode" or "NotFound" => ClaimableItemContractVehicleMenuStatus.NotFound,
                _ => ClaimableItemContractVehicleMenuStatus.Unavailable,
            },
            Lines = menu?.Services.Select(x => ToMenuLine(x, x.VariantID, x.VariantName)).ToList() ?? [],
        };
    }

    public async Task<ClaimableItemContractVehicleMenu> GetModelMenuAsync(string basicModelCode, long? countryId, decimal transferRate, string language)
    {
        // A code may hold a slash; the route is a catch-all, so the slash stays a slash.
        var path = string.Join('/', basicModelCode.Split('/').Select(Uri.EscapeDataString));

        using var response = await GetAsync($"api/menu/{path}", language);

        // 503: the menu store is not provisioned. 409: the menu's data is incomplete.
        if (response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.Conflict)
            return new ClaimableItemContractVehicleMenu { MenuStatus = ClaimableItemContractVehicleMenuStatus.Unavailable };

        EnsureSuccess(response, basicModelCode);

        var menu = await response.Content.ReadFromJsonAsync<ModelMenu>(Json)
            ?? throw new InvalidOperationException($"The menu lookup returned nothing for {basicModelCode}.");

        return new ClaimableItemContractVehicleMenu
        {
            BasicModelCode = menu.BasicModelCode,
            MenuStatus = menu.NotFound ? ClaimableItemContractVehicleMenuStatus.NotFound : ClaimableItemContractVehicleMenuStatus.Found,
            Lines = menu.Variants
                .SelectMany(variant => variant.PeriodicServices.Concat(variant.StandaloneServices)
                    .Select(line => ToMenuLine(line, variant.VariantID, variant.VariantName)))
                .ToList(),
        };
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string language)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{path}?menuTypes=Contract&language={Uri.EscapeDataString(language)}");

        if (!string.IsNullOrWhiteSpace(functionKey))
            request.Headers.Add("x-functions-key", functionKey);

        return await http.SendAsync(request);
    }

    private static void EnsureSuccess(HttpResponseMessage response, string subject)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"The menu lookup answered {(int)response.StatusCode} ({response.StatusCode}) for {subject}. A 401 means a missing or " +
                "wrong Sample:MenuLookupFunctionKey.");
    }

    private static ClaimableItemContractMenuLineDTO ToMenuLine(MenuLine line, long variantId, string? variantName) => new()
    {
        MenuLineKey = line.LineKey,
        MenuCode = line.Code,
        LabourCode = line.LabourCode,
        VariantID = variantId,
        VariantName = variantName,
        LineType = line.LineType switch
        {
            "Periodic" => ClaimableItemContractLineType.Periodic,
            "StandaloneUngrouped" => ClaimableItemContractLineType.Standalone,
            "StandaloneGrouped" => ClaimableItemContractLineType.StandaloneGroup,
            _ => throw new InvalidOperationException($"The menu lookup returned an unknown menu line type '{line.LineType}'."),
        },
        ServiceIntervalCode = line.ServiceIntervalCode,
        ServiceIntervalValueInMeter = line.ServiceIntervalValueInMeter,
        Description = line.Description,
        LabourTotalPrice = line.LabourTotalPrice,
        PartsTotalPrice = line.PartsTotalPrice,
        DiscountAmount = line.DiscountAmount,
        Price = line.TotalPrice,
        HasUnpricedParts = line.HasUnpricedParts,
    };

    // Only the parts of the functions' responses that a contract needs.
    private class VehicleLookup
    {
        public bool IsAuthorized { get; set; }
        public string? BasicModelCode { get; set; }
        public VehicleIdentifiers? Identifiers { get; set; }
        public VehicleSpecification? VehicleSpecification { get; set; }
        public VehicleMenu? ServiceMenu { get; set; }
    }

    private class VehicleIdentifiers
    {
        public string? Katashiki { get; set; }
        public string? Variant { get; set; }
        public string? BrandID { get; set; }
    }

    private class VehicleSpecification
    {
        public string? ModelCode { get; set; }
        public string? ModelDescription { get; set; }
    }

    private class VehicleMenu
    {
        public string? Status { get; set; }
        public List<MenuLine> Services { get; set; } = [];
    }

    private class ModelMenu
    {
        public string? BasicModelCode { get; set; }
        public bool NotFound { get; set; }
        public List<ModelMenuVariant> Variants { get; set; } = [];
    }

    private class ModelMenuVariant
    {
        public long VariantID { get; set; }
        public string? VariantName { get; set; }
        public List<MenuLine> PeriodicServices { get; set; } = [];
        public List<MenuLine> StandaloneServices { get; set; } = [];
    }

    private class MenuLine
    {
        public string LineKey { get; set; } = default!;
        public string? Code { get; set; }
        public string? LabourCode { get; set; }
        public long VariantID { get; set; }
        public string? VariantName { get; set; }
        public string? LineType { get; set; }
        public string? ServiceIntervalCode { get; set; }
        public int? ServiceIntervalValueInMeter { get; set; }
        public string? Description { get; set; }
        public decimal LabourTotalPrice { get; set; }
        public decimal PartsTotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public bool HasUnpricedParts { get; set; }
    }
}
