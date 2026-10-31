using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;
using System.Net;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Repositories;

public class ClaimableItemContractRepository
    : ShiftRepository<ShiftDbContext, ClaimableItemContract, ClaimableItemContractListDTO, ClaimableItemContractDTO>
{
    private readonly ClaimableItemContractMenuService menuService;

    public ClaimableItemContractRepository(ShiftDbContext db, ClaimableItemContractMenuService menuService) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(q => q.Include(e => e.Lines));

        // The maps - the lines replaced on write and the server-owned members left alone - are in
        // Mappers/ClaimableItemsMapper.cs.
    })
    {
        this.menuService = menuService;
    }

    private ClaimableItemsDataOptions Options => menuService.Options;

    public override async ValueTask<ClaimableItemContract> UpsertAsync(
        ClaimableItemContract entity,
        ClaimableItemContractDTO dto,
        ActionTypes actionType,
        long? userId,
        Guid? idempotencyKey,
        bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters)
    {
        if (actionType == ActionTypes.Update)
        {
            EnsureDraft(entity);

            // Delete-then-insert, as on the warranty claim lines. Safe because only a draft's lines are
            // ever replaced, and nothing refers to a draft's lines.
            db.Set<ClaimableItemContractLine>().RemoveRange(entity.Lines.ToList());
        }

        // Saving with the draft flag off issues the contract in the same step.
        var issue = dto.IsDraft == false;

        if (issue)
            EnsureCanIssue();

        EnsureAllowedPriceIncrement(dto.PriceIncrement);

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        var today = menuService.Today;

        upserted.IsDraft = true;
        await ApplyMenuAsync(upserted, actionType == ActionTypes.Update ? upserted.ID : null, today);
        CalculateServerFields(upserted);

        if (issue)
            Issue(upserted, userId, today);

        return upserted;
    }

    /// <summary>
    /// Issues a saved draft. The menu is read again, so a line taken by another contract since the draft was
    /// saved is refused; a price that moved since then is refused too, so nobody issues a price they never saw.
    /// The caller saves the changes.
    /// </summary>
    public async Task<ClaimableItemContract> IssueAsync(long id, long? userId, bool disableDefaultDataLevelAccess = false)
    {
        EnsureCanIssue();

        var contract = await FindContractAsync(id, disableDefaultDataLevelAccess);
        EnsureDraft(contract);
        EnsureAllowedPriceIncrement(contract.PriceIncrement);

        var savedCosts = contract.Lines.Select(x => (Line: x, x.Cost)).ToList();
        var today = menuService.Today;

        await ApplyMenuAsync(contract, contract.ID, today);

        var changed = savedCosts.FirstOrDefault(x => x.Line.Cost != x.Cost);
        if (changed.Line is not null)
            throw Error($"The price of {Name(changed.Line)} changed from {changed.Cost:N2} to {changed.Line.Cost:N2} since the draft was saved. " +
                "Open the contract, check the prices and save it before issuing.");

        CalculateServerFields(contract);
        Issue(contract, userId, today);

        return contract;
    }

    /// <summary>
    /// Cancels an issued contract and every line still live on it, which frees their periodic menus. The totals
    /// keep the lines, as history. The caller saves the changes.
    /// </summary>
    public async Task<ClaimableItemContract> CancelAsync(long id, string? reason, long? userId, bool disableDefaultDataLevelAccess = false)
    {
        EnsureCanCancel();
        reason = RequireReason(reason);

        var contract = await FindContractAsync(id, disableDefaultDataLevelAccess);
        EnsureCancellable(contract);

        var now = menuService.UtcNow;

        foreach (var line in contract.Lines.Where(x => !x.IsCancelled))
            CancelLine(line, reason, userId, now);

        CancelContract(contract, reason, userId, now);

        return contract;
    }

    /// <summary>
    /// Cancels one line of an issued contract, which frees its periodic menu. The contract stays issued unless
    /// that was its last live line, in which case it is cancelled with the same reason. The caller saves the
    /// changes.
    /// </summary>
    public async Task<ClaimableItemContract> CancelLineAsync(long id, long lineId, string? reason, long? userId, bool disableDefaultDataLevelAccess = false)
    {
        EnsureCanCancel();
        reason = RequireReason(reason);

        var contract = await FindContractAsync(id, disableDefaultDataLevelAccess);
        EnsureCancellable(contract);

        var line = contract.Lines.FirstOrDefault(x => x.ID == lineId && !x.IsDeleted)
            ?? throw new ShiftEntityException(new Message("Not Found", "The contract has no such service."), (int)HttpStatusCode.NotFound);

        if (line.IsCancelled)
            throw Error($"The {Name(line)} service is already cancelled.");

        var now = menuService.UtcNow;
        CancelLine(line, reason, userId, now);

        if (contract.Lines.All(x => x.IsCancelled))
            CancelContract(contract, reason, userId, now);

        return contract;
    }

    /// <summary>
    /// Prints an issued contract (a draft is refused). The template is the module's embedded one unless
    /// <see cref="ClaimableItemsReportOverrides.ClaimableItemContractFrxPath"/> replaces it; the company name, logo
    /// and contact details come from the host's <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/>. Cancelled
    /// lines are listed and marked, and left out of the total. Amounts are in the menu's currency.
    /// </summary>
    /// <remarks>
    /// Reached through the framework's print token, which the controller only issues to a user who may print, so
    /// data-level access is not applied again here (as on the certificate print).
    /// </remarks>
    public override async Task<Stream> PrintAsync(string id)
    {
        var companyInfoProvider = PrintingServices.GetRequiredCompanyInfoProvider(this.db);
        var dateFormatter = PrintingServices.GetPrintoutDateFormatter(this.db);
        var hashIdService = PrintingServices.GetRequiredService<IHashIdService>(this.db,
            "Printing requires ShiftSoftware.ShiftEntity.Core.IHashIdService (registered by AddShiftEntityWeb).");

        var contract = await FindContractAsync(hashIdService.Decode<ClaimableItemContractDTO>(id), disableDefaultDataLevelAccess: true);

        if (contract.IsDraft)
            throw Error("Only an issued contract can be printed.");

        var language = string.IsNullOrWhiteSpace(contract.MenuLanguage) ? ClaimableItemContractMenuService.DefaultLanguage : contract.MenuLanguage;
        var today = menuService.Today;

        var distributor = await companyInfoProvider.GetDistributorAsync(language);
        var dealer = contract.CompanyID is { } companyId ? (await companyInfoProvider.GetDealerAsync(language, companyId)).Name : null;
        var branch = contract.CompanyBranchID is { } branchId ? (await companyInfoProvider.GetBranchAsync(language, branchId)).Name : null;

        var status = contract.IsCancelled ? "Cancelled" : contract.EndDate.Date < today ? "Expired" : "Issued";

        var lines = contract.Lines
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.LineType == ClaimableItemContractLineType.Periodic ? 0 : 1)
            .ThenBy(x => x.LineType == ClaimableItemContractLineType.Periodic ? x.ServiceIntervalValueInMeter ?? 0 : 0)
            .ThenBy(x => x.Sort)
            .ToList();

        var liveLines = lines.Where(x => !x.IsCancelled).ToList();
        var no = 1;

        return await new FastReportBuilder()
            .AddFastReportFile(ClaimableItemsReports.ClaimableItemContract(PrintingServices.GetService<ClaimableItemsReportOverrides>(this.db)))
            .AddDataObject("Header", new
            {
                ContractNo = contract.ID.ToString(),
                IssuedDate = dateFormatter.GetFormattedDateTime(AsUtc(contract.IssuedDate)),
                PrintedDate = dateFormatter.GetFormattedDateTime(AsUtc(menuService.UtcNow)!.Value),
                Status = status,
                contract.CustomerName,
                contract.CustomerPhone,
                contract.VIN,
                Model = contract.ModelDescription,
                contract.Katashiki,
                contract.Variant,
                Mileage = $"{contract.Mileage:N0} km",
                ContractType = Describe(contract.ContractType),
                StartDate = dateFormatter.GetFormattedDate(contract.StartDate),
                EndDate = dateFormatter.GetFormattedDate(contract.EndDate),
                Dealer = dealer,
                Branch = branch,
            })
            .AddDataList("Lines", "DataLines", lines.Select(x => new
            {
                No = no++,
                x.MenuCode,
                x.Description,
                Type = Describe(x.LineType),
                ExpireDate = dateFormatter.GetFormattedDate(x.ExpireDate),
                Status = x.IsCancelled ? "Cancelled" : x.ExpireDate?.Date < today ? "Expired" : "Active",
                Price = x.PriceWithIncrement.ToString("N2"),
            }).ToList<object>())
            .AddDataObject("Footer", new
            {
                TotalLabel = liveLines.Count == lines.Count ? "Total" : "Total (excluding cancelled)",
                Total = liveLines.Sum(x => x.PriceWithIncrement).ToString("N2"),
            })
            .GetPDFStream(report =>
            {
                (report.FindObject("TextTitle") as FastReport.TextObject)!.Text = distributor.Name;
                (report.FindObject("PictureLogo") as FastReport.PictureObject)!.ImageLocation = distributor.Logo;
                (report.FindObject("TextFooterAddress") as FastReport.TextObject)!.Text = distributor.Address;
                (report.FindObject("TextFooterContactDetails") as FastReport.TextObject)!.Text = $"{distributor.Phone}\r\n{distributor.Website}";

                if (contract.IsCancelled)
                    (report.FindObject("TextDocumentTitle") as FastReport.TextObject)!.Text = "Service Contract - CANCELLED";

                // The contract number and the VIN, so a scan finds the contract and shows which vehicle it belongs to.
                (report.FindObject("BarcodeContract") as FastReport.Barcode.BarcodeObject)!.Text = $"{contract.ID}|{contract.VIN}";
            });
    }

    /// <summary>
    /// The rules that only matter once a contract is sold: enough services, a start that is not in the past, and
    /// every periodic service expiring within the contract. Then each line gets its expiry: the periodic ones
    /// roll from the start date, the others run to the end date.
    /// </summary>
    private void Issue(ClaimableItemContract contract, long? userId, DateTime today)
    {
        var lines = contract.Lines.Where(x => !x.IsCancelled).ToList();

        if (lines.Count < Options.ContractMinimumLines)
            throw Error($"A contract needs at least {Options.ContractMinimumLines} services to be issued; this one has {lines.Count}.");

        if (contract.StartDate.Date < today)
            throw Error($"The contract starts on {contract.StartDate:yyyy-MM-dd}, which is in the past. Move the start date to {today:yyyy-MM-dd} or later.");

        var periodic = ClaimableItemContractCalculations.RollPeriodicExpiry(
            contract.StartDate,
            lines.Where(x => x.LineType == ClaimableItemContractLineType.Periodic),
            x => x.ServiceIntervalValueInMeter,
            x => x.Duration,
            x => x.DurationUnit);

        foreach (var (line, expireDate) in periodic)
        {
            if (expireDate is null)
                throw Error($"Enter a duration for the {Name(line)} service.");

            if (expireDate > contract.EndDate)
                throw Error($"The {line.ServiceIntervalValueInMeter:N0} km service would expire on {expireDate:yyyy-MM-dd}, after the contract ends on " +
                    $"{contract.EndDate:yyyy-MM-dd}. Shorten the service durations or lengthen the contract.");

            line.ExpireDate = expireDate;
        }

        foreach (var line in lines.Where(x => x.LineType != ClaimableItemContractLineType.Periodic))
            line.ExpireDate = contract.EndDate;

        contract.IsDraft = false;
        contract.IssuedDate = menuService.UtcNow;
        contract.IssuedByUserID = userId;
    }

    /// <summary>
    /// Whether the current user may issue contracts. The default checks
    /// <see cref="ClaimableItemsDataOptions.IssueContractAction"/> when the host sets it, and allows otherwise.
    /// </summary>
    protected virtual bool CanIssue() => CanAccess(Options.IssueContractAction, nameof(ClaimableItemsDataOptions.IssueContractAction));

    /// <summary>
    /// Whether the current user may cancel contracts and their lines. The default checks
    /// <see cref="ClaimableItemsDataOptions.CancelContractAction"/> when the host sets it, and allows otherwise.
    /// </summary>
    protected virtual bool CanCancel() => CanAccess(Options.CancelContractAction, nameof(ClaimableItemsDataOptions.CancelContractAction));

    private bool CanAccess(BooleanAction? action, string optionName)
    {
        if (action is null)
            return true;

        var typeAuthService = PrintingServices.GetRequiredService<ITypeAuthService>(
            this.db,
            $"ClaimableItemsApiOptions.{optionName} is set but no ShiftSoftware.TypeAuth.Core.ITypeAuthService is registered. " +
            "Register TypeAuth (services.AddTypeAuth(...)) or leave the option null.");

        return typeAuthService.CanAccess(action);
    }

    private void EnsureCanIssue()
    {
        if (!CanIssue())
            throw new ShiftEntityException(new Message("Forbidden", "You are not allowed to issue service contracts."), (int)HttpStatusCode.Forbidden);
    }

    private void EnsureCanCancel()
    {
        if (!CanCancel())
            throw new ShiftEntityException(new Message("Forbidden", "You are not allowed to cancel service contracts."), (int)HttpStatusCode.Forbidden);
    }

    private void EnsureAllowedPriceIncrement(decimal priceIncrement)
    {
        if (!Options.ContractPriceIncrements.Contains(priceIncrement))
            throw Error($"A price increment of {Percent(priceIncrement)} is not allowed. Choose one of " +
                $"{string.Join(", ", Options.ContractPriceIncrements.Select(Percent))}.");
    }

    private async Task<ClaimableItemContract> FindContractAsync(long id, bool disableDefaultDataLevelAccess) =>
        await FindAsync(id, null, disableDefaultDataLevelAccess, disableGlobalFilters: false)
            ?? throw new ShiftEntityException(new Message("Not Found", "The contract was not found."), (int)HttpStatusCode.NotFound);

    private static void EnsureCancellable(ClaimableItemContract contract)
    {
        if (contract.IsCancelled)
            throw Error("The contract is already cancelled.");

        if (contract.IsDraft)
            throw Error("A draft contract is not cancelled; delete it instead.");
    }

    private static string RequireReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? throw Error("Enter the reason for the cancellation.") : reason.Trim();

    private static void CancelContract(ClaimableItemContract contract, string reason, long? userId, DateTime now)
    {
        contract.IsCancelled = true;
        contract.CancelledDate = now;
        contract.CancelledByUserID = userId;
        contract.CancelReason = reason;
    }

    private static void CancelLine(ClaimableItemContractLine line, string reason, long? userId, DateTime now)
    {
        line.IsCancelled = true;
        line.CancelledDate = now;
        line.CancelledByUserID = userId;
        line.CancelReason = reason;
    }

    private static string Name(ClaimableItemContractLine line) => line.MenuCode ?? line.Description ?? line.MenuLineKey;

    // Issued and cancelled dates are stored as UTC without a kind.
    private static DateTimeOffset? AsUtc(DateTime? utc) =>
        utc is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)) : null;

    private static string Describe(Enum value) =>
        value.GetType().GetField(value.ToString())?
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .OfType<System.ComponentModel.DescriptionAttribute>()
            .FirstOrDefault()?.Description
        ?? value.ToString();

    private static string Percent(decimal fraction) => (fraction * 100).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%";

    public override async ValueTask<ClaimableItemContract> DeleteAsync(ClaimableItemContract entity, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        EnsureDraft(entity);

        return await base.DeleteAsync(entity, userId, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    /// <summary>
    /// Reads the vehicle's menu again and takes everything about the vehicle and the lines from it: the client
    /// only chooses which menu lines, by key. Menu lines are not signed, so nothing else it sends is trusted.
    /// </summary>
    private async Task ApplyMenuAsync(ClaimableItemContract contract, long? excludeContractId, DateTime today)
    {
        contract.VIN = ClaimableItemContractMenuService.NormalizeVin(contract.VIN);

        var lookup = await menuService.LookupAsync(
            contract.VIN, contract.Mileage, contract.CountryID, contract.TransferRate, contract.MenuLanguage, excludeContractId, today);

        if (lookup.Status != ClaimableItemContractLookupStatus.Found)
            throw Error(lookup.Status switch
            {
                ClaimableItemContractLookupStatus.NotOfficial => "This VIN is not one of the official vehicles. Contracts for other vehicles are not supported yet.",
                ClaimableItemContractLookupStatus.CountryRequired => "Select the country whose menu prices the contract uses.",
                ClaimableItemContractLookupStatus.MenuNotFound => "This vehicle has no contract menu.",
                _ => "The vehicle's service menu could not be read. Try again later.",
            });

        contract.Katashiki = lookup.Katashiki;
        contract.BasicModelCode = lookup.BasicModelCode;
        contract.Variant = lookup.Variant;
        contract.ModelCode = lookup.ModelCode;
        contract.ModelDescription = lookup.ModelDescription;
        contract.BrandID = lookup.Brand is null ? null : long.Parse(lookup.Brand.Value);
        contract.CountryID = lookup.Country is null ? null : long.Parse(lookup.Country.Value);
        contract.TransferRate = lookup.TransferRate;
        contract.MenuLanguage = lookup.MenuLanguage;

        var menu = lookup.Lines
            .GroupBy(x => x.MenuLineKey)
            .ToDictionary(x => x.Key, x => x.First());

        foreach (var line in contract.Lines)
        {
            if (!menu.TryGetValue(line.MenuLineKey, out var menuLine))
                throw Error($"The menu {line.MenuCode ?? line.MenuLineKey} is not offered for this vehicle any more. Remove it and save again.");

            var name = menuLine.MenuCode ?? menuLine.Description ?? menuLine.MenuLineKey;

            switch (menuLine.UnavailableReason)
            {
                case ClaimableItemContractMenuLineUnavailableReason.UnpricedParts:
                    throw Error($"The menu {name} has parts without a price, so it cannot be sold. Remove it and save again.");
                case ClaimableItemContractMenuLineUnavailableReason.AtOrBelowMileage:
                    throw Error($"The {name} service is due at {menuLine.ServiceIntervalValueInMeter:N0} km, which the vehicle has already reached ({contract.Mileage:N0} km). Remove it and save again.");
                case ClaimableItemContractMenuLineUnavailableReason.OnAnotherContract:
                    throw Error($"The menu {name} is already on another contract for this VIN that is neither cancelled nor expired.");
            }

            line.MenuCode = menuLine.MenuCode;
            line.LabourCode = menuLine.LabourCode;
            line.VariantID = menuLine.VariantID;
            line.VariantName = menuLine.VariantName;
            line.LineType = menuLine.LineType;
            line.ServiceIntervalCode = menuLine.ServiceIntervalCode;
            line.ServiceIntervalValueInMeter = menuLine.ServiceIntervalValueInMeter;
            line.Description = menuLine.Description;
            line.LabourTotalPrice = menuLine.LabourTotalPrice;
            line.PartsTotalPrice = menuLine.PartsTotalPrice;
            line.DiscountAmount = menuLine.DiscountAmount;
            line.Cost = menuLine.Price;
        }

        // The validator checked this against the line types the client sent; the menu's are the real ones.
        var repeated = contract.Lines
            .Where(x => x.LineType == ClaimableItemContractLineType.Periodic)
            .GroupBy(x => x.MenuLineKey)
            .FirstOrDefault(x => x.Count() > 1);

        if (repeated is not null)
            throw Error($"The menu {repeated.First().MenuCode ?? repeated.Key} can be added to a contract only once.");
    }

    /// <summary>
    /// The members a client may not set: the end date, each line's customer price, and the totals.
    /// Cancelled lines stay in the totals, as history.
    /// </summary>
    public static void CalculateServerFields(ClaimableItemContract contract)
    {
        contract.EndDate = ClaimableItemContractDurations.AddTo(contract.StartDate, contract.Duration, contract.DurationUnit);

        var sort = 0;
        foreach (var line in contract.Lines)
        {
            line.PriceWithIncrement = ClaimableItemContractCalculations.PriceWithIncrement(line.Cost, contract.PriceIncrement);
            line.Sort = sort++;

            if (line.LineType != ClaimableItemContractLineType.Periodic)
            {
                line.Duration = null;
                line.DurationUnit = null;
            }
        }

        contract.Total = contract.Lines.Sum(x => x.Cost);
        contract.TotalWithIncrement = contract.Lines.Sum(x => x.PriceWithIncrement);
    }

    private static void EnsureDraft(ClaimableItemContract entity)
    {
        if (!entity.IsDraft || entity.IsCancelled)
            throw Error("Only a draft contract can be changed or deleted.");
    }

    private static ShiftEntityException Error(string message) => new(new Message("Error", message));
}
