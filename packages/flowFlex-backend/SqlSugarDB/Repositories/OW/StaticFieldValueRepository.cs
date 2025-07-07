using SqlSugar;
using System.Linq;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Static field value repository implementation
    /// </summary>
    public class StaticFieldValueRepository : BaseRepository<StaticFieldValue>, IStaticFieldValueRepository, IScopedService
    {
        public StaticFieldValueRepository(ISqlSugarClient db) : base(db)
        {
        }

        public async Task<List<StaticFieldValue>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await db.Queryable<StaticFieldValue>()
                .Where(x => x.OnboardingId == onboardingId && x.IsValid)
                .OrderBy(x => x.StageId)
                .OrderBy(x => x.FieldName)
                .ToListAsync();
        }

        public async Task<List<StaticFieldValue>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await db.Queryable<StaticFieldValue>()
                .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid)
                .OrderBy(x => x.FieldName)
                .ToListAsync();
        }

        public async Task<StaticFieldValue?> GetByOnboardingStageAndFieldAsync(long onboardingId, long stageId, string fieldName)
        {
            return await db.Queryable<StaticFieldValue>()
                .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.FieldName == fieldName && x.IsValid)
                .OrderByDescending(x => x.CreateDate)
                .FirstAsync();
        }

        public async Task<List<StaticFieldValue>> GetLatestByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await db.Queryable<StaticFieldValue>()
                .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsLatest && x.IsValid)
                .OrderBy(x => x.FieldName)
                .ToListAsync();
        }

        public async Task<bool> BatchSaveOrUpdateAsync(List<StaticFieldValue> staticFieldValues)
        {
            try
            {
                await db.Ado.BeginTranAsync();

                foreach (StaticFieldValue fieldValue in staticFieldValues)
                {
                    // Set old versions as not latest
                    await SetOldVersionsAsNotLatestAsync(fieldValue.OnboardingId, fieldValue.StageId);

                    // Check if the same field value already exists
                    StaticFieldValue? existing = await GetByOnboardingStageAndFieldAsync(
                        fieldValue.OnboardingId,
                        fieldValue.StageId,
                        fieldValue.FieldName);

                    if (existing != null)
                    {
                        // Update existing record
                        existing.FieldValueJson = fieldValue.FieldValueJson;
                        existing.DisplayName = fieldValue.DisplayName;
                        existing.FieldType = fieldValue.FieldType;
                        existing.IsRequired = fieldValue.IsRequired;
                        existing.Status = fieldValue.Status;
                        existing.CompletionRate = fieldValue.CompletionRate;
                        existing.ValidationStatus = fieldValue.ValidationStatus;
                        existing.ValidationErrors = fieldValue.ValidationErrors;
                        existing.Version = existing.Version + 1;
                        existing.IsLatest = true;
                        existing.ModifyDate = DateTimeOffset.UtcNow;
                        existing.ModifyBy = fieldValue.ModifyBy ?? fieldValue.CreateBy;
                        existing.ModifyUserId = fieldValue.ModifyUserId > 0 ? fieldValue.ModifyUserId : fieldValue.CreateUserId;

                        await db.Updateable(existing).ExecuteCommandAsync();
                    }
                    else
                    {
                        // Insert new record
                        fieldValue.Version = 1;
                        fieldValue.IsLatest = true;
                        fieldValue.CreateDate = DateTimeOffset.UtcNow;
                        fieldValue.ModifyDate = DateTimeOffset.UtcNow;

                        await db.Insertable(fieldValue).ExecuteCommandAsync();
                    }
                }

                await db.Ado.CommitTranAsync();
                return true;
            }
            catch (Exception)
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }
        }

        public async Task<bool> DeleteByOnboardingIdAsync(long onboardingId)
        {
            return await db.Updateable<StaticFieldValue>()
                .SetColumns(x => new StaticFieldValue { IsValid = false, ModifyDate = DateTimeOffset.UtcNow })
                .Where(x => x.OnboardingId == onboardingId)
                .ExecuteCommandAsync() > 0;
        }

        public async Task<bool> DeleteByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await db.Updateable<StaticFieldValue>()
                .SetColumns(x => new StaticFieldValue { IsValid = false, ModifyDate = DateTimeOffset.UtcNow })
                .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId)
                .ExecuteCommandAsync() > 0;
        }

        public async Task<bool> SetOldVersionsAsNotLatestAsync(long onboardingId, long stageId)
        {
            return await db.Updateable<StaticFieldValue>()
                .SetColumns(x => new StaticFieldValue { IsLatest = false, ModifyDate = DateTimeOffset.UtcNow })
                .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsLatest)
                .ExecuteCommandAsync() >= 0;
        }

        public async Task<List<StaticFieldValue>> GetFieldHistoryAsync(long onboardingId, long stageId, string fieldName)
        {
            return await db.Queryable<StaticFieldValue>()
                .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.FieldName == fieldName && x.IsValid)
                .OrderByDescending(x => x.Version)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }
    }
}
