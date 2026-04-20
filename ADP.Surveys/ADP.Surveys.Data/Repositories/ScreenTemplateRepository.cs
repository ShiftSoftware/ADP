using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

public class ScreenTemplateRepository : ShiftRepository<ShiftDbContext, ScreenTemplate, ScreenTemplateListDTO, ScreenTemplateAdminDTO>
{
    public ScreenTemplateRepository(ShiftDbContext db) : base(db)
    {
    }

    public override async ValueTask<ScreenTemplate> UpsertAsync(
        ScreenTemplate entity, ScreenTemplateAdminDTO dto, ActionTypes actionType, long? userId,
        Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        if (dto.Template is null)
            throw new ShiftEntityException(new("Invalid",
                "Template is required. Paste a ScreenTemplateDto JSON payload in the editor."));

        var keyExists = await db.Set<ScreenTemplate>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Key == dto.Key)
            .Where(x => x.ID != entity.ID)
            .AnyAsync();

        if (keyExists)
            throw new ShiftEntityException(new("Duplicate", $"A screen template with key '{dto.Key}' already exists."));

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }
}
