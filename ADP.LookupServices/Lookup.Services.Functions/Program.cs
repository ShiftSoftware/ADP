using Lookup.Services.Functions.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ShiftEntity.Core.Services;
using System.Globalization;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables().AddUserSecrets<Program>(optional: true, reloadOnChange: true);
        var config = builder.Build();
    })
    .ConfigureServices((hostBuilder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var azureStorageAccounts = new List<AzureStorageOption>();
        hostBuilder.Configuration.Bind("AzureStorageAccounts", azureStorageAccounts);

        services.AddMvc().AddShiftEntityWeb(x =>
        {
            x.AddAzureStorage(azureStorageAccounts.ToArray());
            x.JsonNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddScoped<IdentityCosmosService>();

        var cosmosConnectionString = hostBuilder.Configuration.GetConnectionString("CosmosDB")!;
        if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
            services.AddSingleton<CosmosClient>(new CosmosClient(cosmosConnectionString,
                new CosmosClientOptions { AllowBulkExecution = true }));

        var redeemableItemImageUrl = hostBuilder.Configuration.GetValue<string>("ServiceItemImageStorage:Url")!;
        var redeemableItemImageContainer = hostBuilder.Configuration.GetValue<string>("ServiceItemImageStorage:Container")!;
        var paintThicknessContainerUrl = hostBuilder.Configuration.GetValue<string>("PaintThickness:ContianerURL");
        var accessoryContainerUrl = hostBuilder.Configuration.GetValue<string>("Accessory:ContianerURL");

        services.AddLookupService<CosmosClient>(x =>
        {
            x.ServiceItemImageUrlResolver = (x) =>
            {
                string result = null;

                try
                {
                    var image = Utility.GetLocalizedText(x.Value, x.Language);

                    if (!string.IsNullOrWhiteSpace(image))
                        result = Utility.GenerateBlobStorageFullUrl(redeemableItemImageUrl, redeemableItemImageContainer, image);
                }

                catch (Exception) { }

                return new(result);
            };

            x.PaintThickneesImageUrlResolver = (x) => new(paintThicknessContainerUrl + "/" + x.Value?.Replace("#", "%23"));

            x.AccessoryImageUrlResolver = (x) => new(accessoryContainerUrl + "/" + x.Value);

            x.PartLocationNameResolver = async (x) =>
            {
                string? locationName = null;

                if (x.Value?.LocationID == "UZ-UZ-B")
                    return "Besten Stock";

                return locationName;
            };

            x.CountryNameResolver = async (h) =>
            {
                if (string.IsNullOrWhiteSpace(h.Value))
                    return null;

                var identityComsosService = h.Services.GetRequiredService<IdentityCosmosService>();
                var country = await identityComsosService.GetTCACountryAsync(h.Value);

                if (country is null)
                    return null;

                var localizedCountryName = Utility.GetLocalizedText(country?.Name, h.Language);

                return localizedCountryName;
            };

            x.RegionNameResolver = async (h) =>
            {
                if (string.IsNullOrWhiteSpace(h.Value))
                    return null;

                var identityComsosService = h.Services.GetRequiredService<IdentityCosmosService>();
                var region = await identityComsosService.GetTCARegionAsync(h.Value);

                if (region is null)
                    return null;

                var localizedRegionName = Utility.GetLocalizedText(region?.Name, h.Language); ;

                return localizedRegionName;
            };

            x.CompanyNameResolver = async (h) =>
            {
                if (string.IsNullOrWhiteSpace(h.Value))
                    return null;

                var identityComsosService = h.Services.GetRequiredService<IdentityCosmosService>();
                var company = await identityComsosService.GetCompanyAsync(h.Value);

                if (company is null)
                    return null;

                var localizedCompanyName = Utility.GetLocalizedText(company?.Name, h.Language);

                return localizedCompanyName;
            };

            x.CompanyLogoResolver = async (h) =>
            {
                if (string.IsNullOrWhiteSpace(h.Value))
                    return null;

                var identityComsosService = h.Services.GetRequiredService<IdentityCosmosService>();
                var company = await identityComsosService.GetCompanyAsync(h.Value);

                return company?.Logo;
            };

            x.CompanyBranchNameResolver = async (h) =>
            {
                if (string.IsNullOrWhiteSpace(h.Value?.CompanyID) || string.IsNullOrWhiteSpace(h.Value?.BranchID))
                    return null;

                var identityComsosService = h.Services.GetRequiredService<IdentityCosmosService>();
                long.TryParse(h.Value.BranchID, out long branchId);
                var branch = await identityComsosService.GetCompanyBranchAsync(h.Value.CompanyID, branchId.ToString());

                if (branch is null)
                    return null;

                var localizedCompanyName = Utility.GetLocalizedText(branch?.Name, h.Language);

                return localizedCompanyName;
            };

            x.PartLookupPriceResolver = h =>
            {
                List<PartPriceDTO> prices = new();

                var usCulture = new CultureInfo("en-US");
                usCulture.NumberFormat.CurrencyDecimalSeparator = ".";
                usCulture.NumberFormat.CurrencyGroupSeparator = ",";
                usCulture.NumberFormat.CurrencySymbol = "$";
                usCulture.NumberFormat.CurrencyPositivePattern = 0;
                usCulture.NumberFormat.CurrencyNegativePattern = 0;

                foreach (var price in h?.Value?.Prices)
                {
                    prices.Add(new PartPriceDTO
                    {
                        CountryID = price.CountryID,
                        CountryName = price.CountryName,
                        RegionID = price.RegionID,
                        RegionName = price.RegionName,
                        WarrantyPrice = new PriceDTO(price.WarrantyPrice?.Value)
                        {
                            CurrecntySymbol = "$",
                            CultureName = "en-US",
                            Culture = usCulture
                        },
                        PurchasePrice = new PriceDTO(price.PurchasePrice?.Value)
                        {
                            CurrecntySymbol = price.CountryID == "UZ" ? "лв" : "$",
                            CultureName = price.CountryID == "UZ" ? "uz-UZ" : "en-US",
                            Culture = price.CountryID == "UZ" ? new CultureInfo("uz-UZ") : usCulture
                        },
                        RetailPrice = new PriceDTO(price.RetailPrice?.Value)
                        {
                            CurrecntySymbol = price.CountryID == "UZ" ? "лв" : "$",
                            CultureName = price.CountryID == "UZ" ? "uz-UZ" : "en-US",
                            Culture = price.CountryID == "UZ" ? new CultureInfo("uz-UZ") : usCulture
                        }
                    });
                }

                return new(prices);
            };

            x.PartLookupStocksResolver = h =>
            {
                List<StockPartDTO> stockParts = new();

                if (h.Value?.Count() == 0)
                    stockParts = [
                            new StockPartDTO
                            {
                                LocationName = "Besten Stock",
                                QuantityLookUpResult= QuantityLookUpResults.NotAvailable,
                                LocationID = "UZ-UZ-BS"
                            },
                            new StockPartDTO
                            {
                                LocationName = "Free Zone Stock",
                                QuantityLookUpResult= QuantityLookUpResults.NotAvailable,
                                LocationID = "UZ-UZ-FZS"
                            }
                        ];
                else if (h.Value?.Count() == 1)
                {
                    var stockLocationId = h.Value?.FirstOrDefault()?.LocationID;
                    stockParts = h.Value?.ToList() ?? new();

                    stockParts.Add(
                            new StockPartDTO
                            {
                                LocationName = stockLocationId == "UZ-UZ-BS" ? "Free Zone Stock" : "Besten Stock",
                                QuantityLookUpResult = QuantityLookUpResults.NotAvailable,
                                LocationID = stockLocationId == "UZ-UZ-BS" ? "UZ-UZ-FZS" : "UZ-UZ-BS"
                            }
                        );
                }
                else
                    stockParts = h.Value?.ToList() ?? new();

                return new(stockParts.OrderBy(x => x.LocationID));
            };
        });
    })
    .Build();



host.Run();
