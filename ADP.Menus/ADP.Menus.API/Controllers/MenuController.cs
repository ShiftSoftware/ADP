using ShiftSoftware.ADP.Menus.API.Extensions;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Data.Repositories;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Brand;
using ShiftSoftware.TypeAuth.Core;
using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.API.Controllers;

[Route("[controller]")]
[ApiController]
public class MenuController : ShiftEntitySecureControllerAsync<MenuRepository, MenuEntity, MenuListDTO, MenuDTO>
{
    private readonly MenuRepository menuRepo;
    private readonly ServiceIntervalGroupRepository serviceIntervalGroupRepo;
    private readonly LabourRateMappingRepository labourRateMappingRepo;
    private readonly BrandMappingRepository brandMappingRepo;
    private readonly ReplacementItemRepository replacementItemRepo;
    private readonly StandaloneReplacementItemGroupRepository standaloneReplacementItemGroupRepo;
    private readonly IMenuPartPriceService partPriceService;
    private readonly IMenuReportExporter reportExporter;
    private readonly IMenuCountryProvider countryProvider;
    private readonly MenuApiOptions options;

    public MenuController(
            MenuRepository menuRepo,
            ServiceIntervalGroupRepository serviceIntervalGroupRepo,
            LabourRateMappingRepository labourRateMappingRepo,
            BrandMappingRepository brandMappingRepo,
            ReplacementItemRepository replacementItemRepo,
            StandaloneReplacementItemGroupRepository standaloneReplacementItemGroupRepo,
            IMenuPartPriceService partPriceService,
            IMenuReportExporter reportExporter,
            IMenuCountryProvider countryProvider,
            MenuApiOptions options
        ) : base(options.EnableMenuActionTreeAuthorization ? MenuActionTree.Menus : null)
    {
        this.options = options;
        this.menuRepo = menuRepo;
        this.serviceIntervalGroupRepo = serviceIntervalGroupRepo;
        this.labourRateMappingRepo = labourRateMappingRepo;
        this.brandMappingRepo = brandMappingRepo;
        this.replacementItemRepo = replacementItemRepo;
        this.standaloneReplacementItemGroupRepo = standaloneReplacementItemGroupRepo;
        this.partPriceService = partPriceService;
        this.reportExporter = reportExporter;
        this.countryProvider = countryProvider;
    }

    private async Task<(long countryId, decimal transferRate, bool usePrimaryLabourRate)> NormalizeCountryAndTransferRateAsync(long countryId, decimal transferRate)
    {
        var configured = (await countryProvider.GetSupportedCountriesAsync()) ?? Array.Empty<CountryInfo>();
        if (configured.Count == 0)
            return (0, 1m, true);
        if (configured.Count == 1)
            return (configured[0].Id, 1m, true);
        return (countryId, transferRate, false);
    }

    [HttpGet("ExportMenusToExcel")]
    [Authorize]
    public async Task<IActionResult> ExportMenusToExcel([FromQuery]IEnumerable<string>? brands, [FromQuery] long countryId, [FromQuery] decimal transferRate, [FromQuery] string? language = null)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(MenuActionTree.Reports.ExportMenusToExcel))
                return Forbid();
        }

        bool usePrimaryLabourRate;
        (countryId, transferRate, usePrimaryLabourRate) = await NormalizeCountryAndTransferRateAsync(countryId, transferRate);

        if (options.Languages.Count > 0 && !string.IsNullOrEmpty(language) && !options.Languages.Any(x => x.Culture == language))
            return BadRequest(new ShiftEntityResponse { Message = new Message { Title = "Invalid language", Body = $"Language '{language}' is not configured." } });

        var labourRateMappings = (await (await labourRateMappingRepo.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted).AsNoTracking().ToListAsync()).ToDictionary(x => new CompositeKey<long?, decimal>(x.BrandID, x.LabourRate), x => x);

        var brandMapping = (await (await brandMappingRepo.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted).AsNoTracking().ToListAsync())
            .ToDictionary(x => x.BrandID, x => x);

        try
        {
            var brandIDs = brands?.Select(x => ShiftEntityHashIdService.Decode<BrandDTO>(x)).ToList() ?? [];

            var lines = await GenerateLinesAsync(labourRateMappings, brandIDs, brandMapping, countryId, transferRate, language: language, usePrimaryLabourRate: usePrimaryLabourRate);

            var byteArray = await reportExporter.ExportMenusToExcel(new MenuExportContext
            {
                Lines = lines,
                BrandMappings = brandMapping,
                LabourRateMappings = labourRateMappings,
                CountryId = countryId,
                TransferRate = transferRate,
            });

            return File(byteArray, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Menus.xlsx");
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse { Message = ex.Message });
        }
    }

    [HttpGet("ExportRTSCodesToExcel")]
    [Authorize]
    public async Task<IActionResult> ExportRTSCodesToExcel([FromQuery] IEnumerable<string>? brands, [FromQuery] long countryId, [FromQuery] decimal transferRate)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(MenuActionTree.Reports.ExportRTSCodesToExcel))
                return Forbid();
        }

        bool usePrimaryLabourRate;
        (countryId, transferRate, usePrimaryLabourRate) = await NormalizeCountryAndTransferRateAsync(countryId, transferRate);

        var brandMapping = (await (await brandMappingRepo.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted).AsNoTracking().ToListAsync())
            .ToDictionary(x => x.BrandID, x => x);

        var brandIDs = brands?.Select(x => ShiftEntityHashIdService.Decode<BrandDTO>(x)).ToList() ?? [];

        var menuItems = (await (await serviceIntervalGroupRepo.GetIQueryable(null, null, false, false)).AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.MenuLabourDetails).ThenInclude(x => x.MenuVariant).ThenInclude(x => x.Menu)
            .SelectMany(x => x.MenuLabourDetails
                .Where(s => !s.IsDeleted)
                .Where(s => !s.MenuVariant.IsDeleted)
                .Where(s => !s.MenuVariant.Menu.IsDeleted)
                .Where(s => s.MenuVariant.PeriodicAvailabilities.Any(pa => x.ServiceIntervals.Any(p => p.ID == pa.ServiceIntervalID)))
                .Where(s=> s.MenuVariant.Menu.VehicleModel != null && s.MenuVariant.Menu.VehicleModel.BrandID.HasValue ? brandIDs.Contains(s.MenuVariant.Menu.VehicleModel!.BrandID.Value) : false)
                .Select(s => new RTSCodeExportModel
                {
                    LabourCode = x.LabourCode,
                    AllowedTime = s.AllowedTime,
                    LabourRate = s.MenuVariant.LabourRate,
                    Description = x.LabourDescription,
                    BrandID = s.MenuVariant.Menu.VehicleModel!.BrandID
                })
            )
            .ToListAsync());

        var standaloneItems = await (await replacementItemRepo.GetIQueryable(null, null, false, false)).AsNoTracking()
            .Include(x => x.ReplacementItemVehicleModels).ThenInclude(x => x.MenuItems).ThenInclude(x => x.MenuVariant).ThenInclude(x => x.Menu)
            .Where(x => !x.IsDeleted)
            .Where(x => !x.StandaloneReplacementItemGroupID.HasValue)
            .SelectMany(x => x.ReplacementItemVehicleModels
                .Where(s => !s.IsDeleted)
                .Where(s => s.MenuItems.Any()))
            .SelectMany(x => x.MenuItems
                .Where(s => s.Parts.Any(p => !p.IsDeleted && p.StandaloneQuantity.HasValue))
                .Where(s => s.MenuVariant.HasStandaloneItems)
                .Where(s => !s.MenuVariant.IsDeleted)
                .Where(s => !s.MenuVariant.Menu.IsDeleted)
                .Where(s => !s.IsDeleted)
                .Where(s => s.MenuVariant.Menu.VehicleModel != null && s.MenuVariant.Menu.VehicleModel.BrandID.HasValue ? brandIDs.Contains(s.MenuVariant.Menu.VehicleModel.BrandID.Value) : false)
                .Select(s => new RTSCodeExportModel
                {
                    LabourCode = x.ReplacementItem.StandaloneLabourCode,
                    AllowedTime = s.StandaloneAllowedTime,
                    LabourRate = s.MenuVariant.LabourRate,
                    Description = x.ReplacementItem.FriendlyName,
                    BrandID = s.MenuVariant.Menu.VehicleModel!.BrandID
                })
            ).ToListAsync();

        var standaloneGroups = await (await standaloneReplacementItemGroupRepo.GetIQueryable(null, null, false, false)).AsNoTracking()
            .Include(x => x.ReplacementItems).ThenInclude(x=> x.ReplacementItemVehicleModels).ThenInclude(x => x.MenuItems).ThenInclude(x => x.MenuVariant).ThenInclude(x => x.Menu)
            .Where(x => !x.IsDeleted)
            .Select(x=> x.ReplacementItems.First())
            .SelectMany(x => x.ReplacementItemVehicleModels
                .Where(s => !s.IsDeleted)
                .Where(s => s.MenuItems.Any()))
            .SelectMany(x => x.MenuItems
                .Where(s => s.Parts.Any(p => !p.IsDeleted && p.StandaloneQuantity.HasValue))
                .Where(s=> s.MenuVariant.HasStandaloneItems)
                .Where(s=> !s.MenuVariant.IsDeleted)
                .Where(s=> !s.MenuVariant.Menu.IsDeleted)
                .Where(s=> !s.IsDeleted)
                .Where(s => s.MenuVariant.Menu.VehicleModel != null && s.MenuVariant.Menu.VehicleModel.BrandID.HasValue ? brandIDs.Contains(s.MenuVariant.Menu.VehicleModel.BrandID.Value) : false)
                .Select(s => new RTSCodeExportModel
                {
                    LabourCode = x.ReplacementItem.StandaloneReplacementItemGroup!.LabourCode,
                    AllowedTime = s.StandaloneAllowedTime,
                    LabourRate = s.MenuVariant.LabourRate,
                    Description = x.ReplacementItem.StandaloneReplacementItemGroup!.Name,
                    BrandID = s.MenuVariant.Menu.VehicleModel!.BrandID
                })
            ).ToListAsync();

        List<RTSCodeExportModel> items = new();
        items.AddRange(menuItems);
        items.AddRange(standaloneItems);
        items.AddRange(standaloneGroups);
        items = items.DistinctBy(x => new { x.LabourCode, x.AllowedTime, x.LabourRate, x.BrandID }).ToList();

        var laborRates = items.Select(x => new { LaborRate = x.LabourRate, BrandID = x.BrandID })
            .DistinctBy(x => new { x.LaborRate, x.BrandID })
            .ToList();

        var mapping = await labourRateMappingRepo.GetIQueryable(null, null, false, false);
        var missingLaborRates = laborRates
            .Where(x => !mapping.AsNoTracking()
                .Any(y => y.LabourRate == x.LaborRate && y.BrandID == x.BrandID))
            .ToList();

        if (missingLaborRates.Any())
            return BadRequest(new ShiftEntityResponse
            {
                Message = new Message
                {
                    Title = "Some labour rates are missing in Labour Rate Mapping",
                    Body = $"'{string.Join(" , ", missingLaborRates.Select(x => $"{x.LaborRate} - BrandID: {x.BrandID}"))}' is missing"
                }
            });

        var labourRateMappings = (await (await labourRateMappingRepo.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted).AsNoTracking().ToListAsync()).ToDictionary(x => new CompositeKey<long?, decimal>(x.BrandID, x.LabourRate), x => x);

        var byteArray = await reportExporter.ExportRTSCodesToExcel(new RTSCodeExportContext
        {
            Items = items,
            LabourRateMappings = labourRateMappings,
            BrandMappings = brandMapping,
            CountryId = countryId,
            TransferRate = transferRate,
        });

        return File(byteArray, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RTSCodes.xlsx");
    }

    [HttpGet("StockByPartNumber/{partNumber}")]
    [Authorize]
    public async Task<IActionResult> GetStockByPartNumber(string partNumber)
    {
        var stock = await partPriceService.StockByPartNumberAsync(partNumber.Trim());

        if(stock is null)
            return NotFound();

        var countries = await countryProvider.GetSupportedCountriesAsync();
        var countryPrices = GetRetailPricesByCountry(stock, countries.Select(c => c.Id));

        var stockDto = new StockDTO
        {
            PartNumber = stock.PartNumber,
            Price = countryPrices.FirstOrDefault().Price ?? 0,
            CountryPrices = countryPrices
        };

        return Ok(stockDto);
    }

    [HttpGet("StockByPartNumbers")]
    [Authorize]
    public async Task<IActionResult> GetStockByPartNumbers([FromQuery] IEnumerable<string> partNumbers)
    {
        var requestedPartNumbers = (partNumbers ?? Enumerable.Empty<string>())
            .Select(x => x?.Trim() ?? string.Empty)
            .ToList();

        var stocks = await partPriceService.StockByPartNumbersAsync(requestedPartNumbers);

        var countries = await countryProvider.GetSupportedCountriesAsync();
        var cIds = countries.Select(c => c.Id).ToList();

        var stockLookup = new Dictionary<string, List<StockPriceByCountryDTO>>(StringComparer.OrdinalIgnoreCase);
        if (stocks is not null)
        {
            stockLookup = stocks
                .Where(x => !string.IsNullOrWhiteSpace(x.PartNumber))
                .GroupBy(x => x.PartNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => GetRetailPricesByCountry(x.First(), cIds),
                    StringComparer.OrdinalIgnoreCase);
        }

        var result = requestedPartNumbers.Select(partNumber => new StockDTO
        {
            PartNumber = partNumber,
            CountryPrices = stockLookup.GetValueOrDefault(partNumber, []),
            Price = stockLookup.GetValueOrDefault(partNumber, []).FirstOrDefault().Price ?? 0,
        });

        return Ok(result);
    }

    [HttpPost("UpdatePartsPrice")]
    [Authorize]
    public async Task<IActionResult> UpdatePartsPrice()
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(MenuActionTree.Operations.UpdatePartsPrice))
                return Forbid();
        }

        var menuItems = await (await menuRepo.GetIQueryable(null, null, false, false))
            .Where(x=> !x.IsDeleted)
            .SelectMany(x => x.Variants.Where(v => !v.IsDeleted))
            .SelectMany(x => x.Items)
            .Where(x => !x.IsDeleted)
            .Include(x => x.Parts)
                .ThenInclude(x => x.CountryPrices)
            .ToListAsync();

        var partNumbers = menuItems
            .SelectMany(x => x.Parts.Where(p => !p.IsDeleted).Select(p => p.PartNumber))
            .Distinct()
            .ToList();

        var countries = await countryProvider.GetSupportedCountriesAsync();
        var countryIds = countries.Select(c => c.Id).ToList();

        var stocks = (await partPriceService.StockByPartNumbersAsync(partNumbers.Select(x => x.Trim())))
            .Where(x => !string.IsNullOrWhiteSpace(x.PartNumber))
            .GroupBy(x => x.PartNumber, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToDictionary(x => x.PartNumber, x => GetRetailPricesByCountry(x, countryIds), StringComparer.OrdinalIgnoreCase);

        foreach (var item in menuItems)
        {
            foreach (var part in item.Parts.Where(x => !x.IsDeleted))
            {
                if (!stocks.TryGetValue(part.PartNumber, out var countryPrices))
                    continue;

                part.CountryPrices ??= [];
                foreach (var countryId in countryIds)
                {
                    if (part.CountryPrices.Any(x => !x.IsDeleted && x.CountryID == countryId))
                        continue;

                    part.CountryPrices.Add(new MenuItemPartCountryPrice
                    {
                        CountryID = countryId,
                        PartPrice = 0,
                        PartPriceMarginPercentage = 0,
                        PartFinalPrice = 0
                    });
                }

                foreach (var countryPrice in part.CountryPrices.Where(x => !x.IsDeleted))
                {
                    var price = countryPrices.FirstOrDefault(x => x.CountryID == countryPrice.CountryID)?.Price ?? 0;

                    if (part.PartNumber.Trim() == "ZGX-01-AEKS")
                        price = Math.Round(price / 10, 2);
                    else if (part.PartNumber.Trim() == "ZBG8905M" || part.PartNumber.Trim() == "ZBG4035M")
                        price = Math.Round(price / 19, 2);

                    countryPrice.PartPrice = price;
                    countryPrice.PartFinalPrice = Math.Round((price * (1 + countryPrice.PartPriceMarginPercentage.GetValueOrDefault() / 100)), 2);
                }
            }
        }

        await this.menuRepo.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("MenuDetailReportExcel")]
    [Authorize]
    public async Task<IActionResult> MenuDetailReportExcel([FromQuery] DateTimeOffset? oldVersionDateTime,
        [FromQuery] DateTimeOffset? newVersionDateTime, [FromQuery] IEnumerable<string> brands, [FromQuery] long countryId, [FromQuery] decimal transferRate, [FromQuery] string? language = null)
    {
        if (options.EnableMenuActionTreeAuthorization)
        {
            var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
            if (!typeAuth.CanAccess(MenuActionTree.Reports.ExportMenuDetailReport))
                return Forbid();
        }

        bool usePrimaryLabourRate;
        (countryId, transferRate, usePrimaryLabourRate) = await NormalizeCountryAndTransferRateAsync(countryId, transferRate);

        if (options.Languages.Count > 0 && !string.IsNullOrEmpty(language) && !options.Languages.Any(x => x.Culture == language))
            return BadRequest(new ShiftEntityResponse { Message = new Message { Title = "Invalid language", Body = $"Language '{language}' is not configured." } });

        var query = new MenuDetailReportQueryDTO
        {
            OldVersionDateTime = oldVersionDateTime,
            NewVersionDateTime = newVersionDateTime
        };

        var brandIDs = brands.Select(x => ShiftEntityHashIdService.Decode<BrandDTO>(x));

        var labourRateMappings = (await (await labourRateMappingRepo.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted).AsNoTracking().ToListAsync()).ToDictionary(x => new CompositeKey<long?, decimal>(x.BrandID, x.LabourRate), x => x);

        var brandMapping = (await (await brandMappingRepo.GetIQueryable(null, null, false, false))
            .Where(x => !x.IsDeleted).AsNoTracking().ToListAsync())
            .ToDictionary(x => x.BrandID, x => x);

        var oldVersionAsOf = query.OldVersionDateTime?.DateTime ?? null;
        var newVersionAsOf = query.NewVersionDateTime?.DateTime ?? null;

        try
        {
            var oldVersionLines = oldVersionAsOf is not null ? await GenerateLinesAsync(labourRateMappings, brandIDs, brandMapping, countryId, transferRate, oldVersionAsOf, language, usePrimaryLabourRate) : [];
            var newVersionLines = await GenerateLinesAsync(labourRateMappings, brandIDs, brandMapping, countryId, transferRate, newVersionAsOf, language, usePrimaryLabourRate);

            var byteArray = await reportExporter.ExportMenuDetailReportToExcel(new MenuDetailReportExportContext
            {
                NewVersionLines = newVersionLines,
                OldVersionLines = oldVersionLines.DistinctBy(x => x.Code),
                BrandMappings = brandMapping,
                LabourRateMappings = labourRateMappings,
                CountryId = countryId,
                TransferRate = transferRate,
                OldVersionDateTime = oldVersionDateTime,
                NewVersionDateTime = newVersionDateTime,
            });

            return File(byteArray, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Menus.xlsx");
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse { Message = ex.Message });
        }

    }

    private async Task<IEnumerable<MenuLineDTO>> GenerateLinesAsync(
        Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> labourRateMappings,
        IEnumerable<long> brandIDs,
        Dictionary<long?, BrandMapping> brandMapping,
        long countryId,
        decimal transferRate,
        DateTime? asOf = null,
        string? language = null,
        bool usePrimaryLabourRate = false)
    {
        var dbSet = this.menuRepo.db.Set<MenuVariant>();
        IQueryable<MenuVariant> query;

        if (asOf.HasValue)
            query = dbSet.TemporalAsOf(asOf.GetValueOrDefault());
        else
            query = dbSet.AsQueryable();

        var menus = await query.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => !x.Menu.IsDeleted)
            .Where(x => x.Menu.VehicleModel!.BrandID.HasValue ? brandIDs.Contains(x.Menu.VehicleModel!.BrandID.Value) : false)
            .Include(x => x.Items).ThenInclude(x => x.Parts).ThenInclude(x => x.CountryPrices)
            .Include(x => x.Items).ThenInclude(x => x.ReplacementItemVehicleModel!.ReplacementItem).ThenInclude(x => x.ReplacementItemServiceIntervalGroups).ThenInclude(x => x.ServiceIntervalGroup).ThenInclude(x => x.ServiceIntervals)
            .Include(x => x.Items).ThenInclude(x => x.ReplacementItemVehicleModel!.ReplacementItem).ThenInclude(x => x.StandaloneReplacementItemGroup)
            .Include(x => x.PeriodicAvailabilities).ThenInclude(x => x.ServiceInterval)
            .Include(x => x.LabourDetails).ThenInclude(x => x.ServiceIntervalGroup).ThenInclude(x => x.ServiceIntervals)
            .Include(x => x.LabourRates)
            .Include(x => x.Menu).ThenInclude(x => x.VehicleModel)
            .AsSplitQuery()
            .ToListAsync();

        var laborRates = menus.Select(x => new
            {
                LaborRate = x.LabourRate,
                BrandID = x.Menu.VehicleModel!.BrandID
            })
            .Where(x => x.LaborRate > 0)
            .DistinctBy(x => new { x.LaborRate, x.BrandID })
            .ToList();

        var mapping = await labourRateMappingRepo.GetIQueryable(null, null, false, false);
        var missingLaborRates = laborRates
            .Where(x => !mapping.AsNoTracking()
                .Any(y => y.LabourRate == x.LaborRate && y.BrandID == x.BrandID))
            .ToList();

        if (missingLaborRates.Any())
            throw new ShiftEntityException(
                new Message
                {
                    Title = "Some labour rates are missing in Labour Rate Mapping" + (asOf is not null ? "from old version" : ""),
                    Body = $"'{string.Join(" , ", missingLaborRates.Select(x => $"{x.LaborRate} - BrandID: {x.BrandID}"))}' is missing"
                });

        var lines = MenuExportService.GenerateMenuLines(menus, labourRateMappings, brandMapping, countryId, transferRate, language, usePrimaryLabourRate, options.ApplyMenuPrefixPostfixToStandalones);

        return lines;
    }

    private static List<StockPriceByCountryDTO> GetRetailPricesByCountry(MenuPartPrice stock, IEnumerable<long> countryIds)
    {
        var ids = countryIds?.ToList() ?? [];

        // 0-country mode: synthesize a single CountryID=0 row using the first available retail price.
        if (ids.Count == 0)
        {
            var firstPrice = stock.CountryPrices?
                .Select(r => r.Price)
                .FirstOrDefault() ?? 0;

            return [new StockPriceByCountryDTO { CountryID = 0, Price = firstPrice }];
        }

        var result = new List<StockPriceByCountryDTO>();
        foreach (var countryId in ids)
        {
            var price = stock.CountryPrices?.FirstOrDefault(x => x.CountryID == countryId)?.Price ?? 0;
            result.Add(new StockPriceByCountryDTO
            {
                CountryID = countryId,
                Price = price
            });
        }

        return result;
    }
}







