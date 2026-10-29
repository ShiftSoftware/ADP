using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Services;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

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

        // Issuing freezes the lines and works out their expiry; until it exists a contract stays a draft.
        if (dto.IsDraft == false)
            throw Error("Issuing a contract is not available yet. Save it as a draft.");

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        upserted.IsDraft = true;
        await ApplyMenuAsync(upserted, actionType == ActionTypes.Update ? upserted.ID : null, DateTime.Today);
        CalculateServerFields(upserted);

        return upserted;
    }

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
