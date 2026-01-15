using SqlSugar;
using System.Linq;
using System.Text.Json;
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

        public async Task<StaticFieldValue?> GetByOnboardingAndFieldAsync(long onboardingId, string fieldName)
        {
            return await db.Queryable<StaticFieldValue>()
                .Where(x => x.OnboardingId == onboardingId && x.FieldName == fieldName && x.IsValid)
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
                        // Update existing record using raw SQL to handle JSONB properly
                        var sql = @"
                            UPDATE ff_static_field_values
                            SET
                                field_value_json = @FieldValueJson::jsonb,
                                display_name = @DisplayName,
                                field_id = @FieldId,
                                field_type = @FieldType,
                                is_required = @IsRequired,
                                status = @Status,
                                completion_rate = @CompletionRate,
                                validation_status = @ValidationStatus,
                                validation_errors = @ValidationErrors,
                                version = @Version,
                                is_latest = @IsLatest,
                                modify_date = @ModifyDate,
                                modify_by = @ModifyBy,
                                modify_user_id = @ModifyUserId,
                                metadata = COALESCE(@Metadata::jsonb, metadata)
                            WHERE id = @Id";

                        await db.Ado.ExecuteCommandAsync(sql, new
                        {
                            Id = existing.Id,
                            FieldValueJson = EnsureValidJson(fieldValue.FieldValueJson),
                            DisplayName = fieldValue.DisplayName,
                            FieldId = fieldValue.FieldId,
                            FieldType = fieldValue.FieldType,
                            IsRequired = fieldValue.IsRequired,
                            Status = fieldValue.Status,
                            CompletionRate = fieldValue.CompletionRate,
                            ValidationStatus = fieldValue.ValidationStatus,
                            ValidationErrors = fieldValue.ValidationErrors,
                            Version = existing.Version + 1,
                            IsLatest = true,
                            ModifyDate = DateTimeOffset.UtcNow,
                            ModifyBy = fieldValue.ModifyBy ?? fieldValue.CreateBy,
                            ModifyUserId = fieldValue.ModifyUserId > 0 ? fieldValue.ModifyUserId : fieldValue.CreateUserId,
                            Metadata = EnsureValidJson(fieldValue.Metadata) ?? EnsureValidJson(existing.Metadata)
                        });
                    }
                    else
                    {
                        // Generate primary key ID for new record
                        if (fieldValue.Id == 0)
                        {
                            fieldValue.InitNewId();
                        }

                        // Insert new record using raw SQL to handle JSONB properly
                        var insertSql = @"
                            INSERT INTO ff_static_field_values (
                                id, onboarding_id, stage_id, field_name, field_id, display_name, field_value_json,
                                field_type, is_required, status, completion_rate, validation_status,
                                validation_errors, version, is_latest, is_submitted, source,
                                ip_address, user_agent, metadata, create_date, modify_date,
                                create_by, modify_by, create_user_id, modify_user_id, is_valid,
                                tenant_id, app_code
                            ) VALUES (
                                @Id, @OnboardingId, @StageId, @FieldName, @FieldId, @DisplayName, @FieldValueJson::jsonb,
                                @FieldType, @IsRequired, @Status, @CompletionRate, @ValidationStatus,
                                @ValidationErrors, @Version, @IsLatest, @IsSubmitted, @Source,
                                @IpAddress, @UserAgent, @Metadata::jsonb, @CreateDate, @ModifyDate,
                                @CreateBy, @ModifyBy, @CreateUserId, @ModifyUserId, @IsValid,
                                @TenantId, @AppCode
                            )";

                        await db.Ado.ExecuteCommandAsync(insertSql, new
                        {
                            Id = fieldValue.Id,
                            OnboardingId = fieldValue.OnboardingId,
                            StageId = fieldValue.StageId,
                            FieldName = fieldValue.FieldName,
                            FieldId = fieldValue.FieldId,
                            DisplayName = fieldValue.DisplayName,
                            FieldValueJson = EnsureValidJson(fieldValue.FieldValueJson),
                            FieldType = fieldValue.FieldType,
                            IsRequired = fieldValue.IsRequired,
                            Status = fieldValue.Status,
                            CompletionRate = fieldValue.CompletionRate,
                            ValidationStatus = fieldValue.ValidationStatus,
                            ValidationErrors = fieldValue.ValidationErrors,
                            Version = 1,
                            IsLatest = true,
                            IsSubmitted = fieldValue.IsSubmitted,
                            Source = fieldValue.Source,
                            IpAddress = fieldValue.IpAddress,
                            UserAgent = fieldValue.UserAgent,
                            Metadata = EnsureValidJson(fieldValue.Metadata),
                            CreateDate = DateTimeOffset.UtcNow,
                            ModifyDate = DateTimeOffset.UtcNow,
                            CreateBy = fieldValue.CreateBy,
                            ModifyBy = fieldValue.ModifyBy,
                            CreateUserId = fieldValue.CreateUserId,
                            ModifyUserId = fieldValue.ModifyUserId,
                            IsValid = fieldValue.IsValid,
                            TenantId = fieldValue.TenantId,
                            AppCode = fieldValue.AppCode
                        });
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

        /// <summary>
        /// Ensure the input string is valid JSON format
        /// If not valid JSON, convert to JSON string
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Valid JSON string</returns>
        private static string EnsureValidJson(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "null";
            }

            // Try to parse as JSON first
            try
            {
                JsonDocument.Parse(input);
                return input; // Already valid JSON
            }
            catch (JsonException)
            {
                // Not valid JSON, serialize as JSON string
                return JsonSerializer.Serialize(input);
            }
        }
    }
}
