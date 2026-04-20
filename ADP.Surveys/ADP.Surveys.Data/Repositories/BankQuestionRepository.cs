using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

public class BankQuestionRepository : ShiftRepository<ShiftDbContext, BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>
{
    public BankQuestionRepository(ShiftDbContext db) : base(db)
    {
    }

    public override async ValueTask<BankQuestion> UpsertAsync(
        BankQuestion entity, BankQuestionAdminDTO dto, ActionTypes actionType, long? userId,
        Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        // Admin DTO's Question is nullable so the JSON-editor form can surface parse
        // errors inline; this is the hard server-side backstop.
        if (dto.Question is null)
            throw new ShiftEntityException(new("Invalid",
                "Question is required. Paste a QuestionDto JSON payload in the editor."));

        // Uniqueness on human-readable Key (among non-deleted entries).
        var keyExists = await db.Set<BankQuestion>()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Key == dto.Key)
            .Where(x => x.ID != entity.ID)
            .AnyAsync();

        if (keyExists)
            throw new ShiftEntityException(new("Duplicate", $"A bank question with key '{dto.Key}' already exists."));

        // Decision #9 lock enforcement. Once any published survey has referenced this
        // entry (Locked == true), the type + validation are frozen — only presentation
        // and Key (for typo correction) may change.
        if (actionType == ActionTypes.Update && entity.Locked)
        {
            if (entity.BankEntryID != dto.BankEntryID)
                throw new ShiftEntityException(new("Locked",
                    "BankEntryID is immutable. It is the stable BI join anchor per Decision #11."));

            // Compare existing QuestionJson vs incoming Question to detect disallowed changes.
            // Conservative approach for this pass: simply block any type change; deeper
            // presentation-vs-semantic diffing can be added once we have a real builder UX.
            if (dto.Question is not null)
            {
                var incomingType = dto.Question.GetType();
                var existingDto = System.Text.Json.JsonSerializer.Deserialize<Shared.DTOs.Questions.QuestionDto>(
                    entity.QuestionJson, Shared.Json.SurveySchemaSerializer.Options);
                if (existingDto is not null && existingDto.GetType() != incomingType)
                    throw new ShiftEntityException(new("Locked",
                        $"Cannot change the question type of a locked bank entry (was {existingDto.GetType().Name}, got {incomingType.Name})."));
            }
        }

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }
}
