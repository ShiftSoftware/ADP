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
/// Reads the vehicle and its contract menu from the distributor's vehicle lookup endpoint
/// (<c>Sample:VehicleLookupUrl</c>, with <c>{vin}</c> in it), asking it to include the model's contract menu. A
/// server-to-server call: the endpoint's function key, when it needs one, is <c>Sample:VehicleLookupFunctionKey</c>.
/// One country, so no country or transfer rate is sent: the endpoint prices the menu itself.
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
    private readonly string urlTemplate;
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
        urlTemplate = configuration["Sample:VehicleLookupUrl"] ?? "";
        functionKey = configuration["Sample:VehicleLookupFunctionKey"];
    }

    public async ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync() =>
        (await countryProvider.GetSupportedCountriesAsync())
            .Select(x => new ClaimableItemContractCountry { ID = x.Id, Name = x.Name })
            .ToList();

    public async Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language)
    {
        var url = urlTemplate.Replace("{vin}", Uri.EscapeDataString(vin));
        url += (url.Contains('?') ? "&" : "?") + "includeServiceMenu=true&menuTypes=Contract";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(functionKey))
            request.Headers.Add("x-functions-key", functionKey);

        if (!string.IsNullOrWhiteSpace(language))
            request.Headers.AcceptLanguage.ParseAdd(language);

        using var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"The vehicle lookup answered {(int)response.StatusCode} ({response.StatusCode}) for {vin}. A 401 means a missing or " +
                "wrong Sample:VehicleLookupFunctionKey.");

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
            Lines = menu?.Services.Select(ToMenuLine).ToList() ?? [],
        };
    }

    private static ClaimableItemContractMenuLineDTO ToMenuLine(MenuLine line) => new()
    {
        MenuLineKey = line.LineKey,
        MenuCode = line.Code,
        LabourCode = line.LabourCode,
        VariantID = line.VariantID,
        VariantName = line.VariantName,
        LineType = line.LineType switch
        {
            "Periodic" => ClaimableItemContractLineType.Periodic,
            "StandaloneUngrouped" => ClaimableItemContractLineType.Standalone,
            "StandaloneGrouped" => ClaimableItemContractLineType.StandaloneGroup,
            _ => throw new InvalidOperationException($"The vehicle lookup returned an unknown menu line type '{line.LineType}'."),
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

    // Only the parts of the endpoint's response that a contract needs.
    private class VehicleLookup
    {
        public bool IsAuthorized { get; set; }
        public string? BasicModelCode { get; set; }
        public VehicleIdentifiers? Identifiers { get; set; }
        public VehicleSpecification? VehicleSpecification { get; set; }
        public ServiceMenu? ServiceMenu { get; set; }
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

    private class ServiceMenu
    {
        public string? Status { get; set; }
        public List<MenuLine> Services { get; set; } = [];
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
