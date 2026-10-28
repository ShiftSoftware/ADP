using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
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
    public ClaimableItemContractRepository(ShiftDbContext db) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(q => q.Include(e => e.Lines));

        // The maps - the lines replaced on write and the server-owned members left alone - are in
        // Mappers/ClaimableItemsMapper.cs.
    })
    {
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
            throw new ShiftEntityException(new Message("Error", "Issuing a contract is not available yet. Save it as a draft."));

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        upserted.IsDraft = true;
        CalculateServerFields(upserted);

        return upserted;
    }

    public override async ValueTask<ClaimableItemContract> DeleteAsync(ClaimableItemContract entity, long? userId, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        EnsureDraft(entity);

        return await base.DeleteAsync(entity, userId, disableDefaultDataLevelAccess, disableGlobalFilters);
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
            line.PriceWithIncrement = decimal.Round(line.Cost * (1 + contract.PriceIncrement), 2);
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
            throw new ShiftEntityException(new Message("Error", "Only a draft contract can be changed or deleted."));
    }
}
