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

                        var updateParams = new List<SugarParameter>
                        {
                            new SugarParameter("@Id", existing.Id),
                            new SugarParameter("@FieldValueJson", EnsureValidJson(fieldValue.FieldValueJson)),
                            new SugarParameter("@DisplayName", fieldValue.DisplayName),
                            new SugarParameter("@FieldId", fieldValue.FieldId.HasValue ? fieldValue.FieldId.Value : DBNull.Value, System.Data.DbType.Int64),
                            new SugarParameter("@FieldType", fieldValue.FieldType),
                            new SugarParameter("@IsRequired", fieldValue.IsRequired),
                            new SugarParameter("@Status", fieldValue.Status),
                            new SugarParameter("@CompletionRate", fieldValue.CompletionRate),
                            new SugarParameter("@ValidationStatus", fieldValue.ValidationStatus),
                            new SugarParameter("@ValidationErrors", fieldValue.ValidationErrors),
                            new SugarParameter("@Version", existing.Version + 1),
                            new SugarParameter("@IsLatest", true),
                            new SugarParameter("@ModifyDate", DateTimeOffset.UtcNow),
                            new SugarParameter("@ModifyBy", fieldValue.ModifyBy ?? fieldValue.CreateBy),
                            new SugarParameter("@ModifyUserId", fieldValue.ModifyUserId > 0 ? fieldValue.ModifyUserId : fieldValue.CreateUserId),
                            new SugarParameter("@Metadata", EnsureValidJson(fieldValue.Metadata) ?? EnsureValidJson(existing.Metadata))
                        };

                        await db.Ado.ExecuteCommandAsync(sql, updateParams);
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

                        var insertParams = new List<SugarParameter>
                        {
                            new SugarParameter("@Id", fieldValue.Id),
                            new SugarParameter("@OnboardingId", fieldValue.OnboardingId),
                            new SugarParameter("@StageId", fieldValue.StageId),
                            new SugarParameter("@FieldName", fieldValue.FieldName),
                            new SugarParameter("@FieldId", fieldValue.FieldId.HasValue ? fieldValue.FieldId.Value : DBNull.Value, System.Data.DbType.Int64),
                            new SugarParameter("@DisplayName", fieldValue.DisplayName),
                            new SugarParameter("@FieldValueJson", EnsureValidJson(fieldValue.FieldValueJson)),
                            new SugarParameter("@FieldType", fieldValue.FieldType),
                            new SugarParameter("@IsRequired", fieldValue.IsRequired),
                            new SugarParameter("@Status", fieldValue.Status),
                            new SugarParameter("@CompletionRate", fieldValue.CompletionRate),
                            new SugarParameter("@ValidationStatus", fieldValue.ValidationStatus),
                            new SugarParameter("@ValidationErrors", fieldValue.ValidationErrors),
                            new SugarParameter("@Version", 1),
                            new SugarParameter("@IsLatest", true),
                            new SugarParameter("@IsSubmitted", fieldValue.IsSubmitted),
                            new SugarParameter("@Source", fieldValue.Source),
                            new SugarParameter("@IpAddress", fieldValue.IpAddress),
                            new SugarParameter("@UserAgent", fieldValue.UserAgent),
                            new SugarParameter("@Metadata", EnsureValidJson(fieldValue.Metadata)),
                            new SugarParameter("@CreateDate", DateTimeOffset.UtcNow),
                            new SugarParameter("@ModifyDate", DateTimeOffset.UtcNow),
                            new SugarParameter("@CreateBy", fieldValue.CreateBy),
                            new SugarParameter("@ModifyBy", fieldValue.ModifyBy),
                            new SugarParameter("@CreateUserId", fieldValue.CreateUserId),
                            new SugarParameter("@ModifyUserId", fieldValue.ModifyUserId),
                            new SugarParameter("@IsValid", fieldValue.IsValid),
                            new SugarParameter("@TenantId", fieldValue.TenantId),
                            new SugarParameter("@AppCode", fieldValue.AppCode)
                        };

                        await db.Ado.ExecuteCommandAsync(insertSql, insertParams);
                    }
                }

                await db.Ado.CommitTranAsync();
                return true;
            }
            catch (Exception ex)
            {
                await db.Ado.RollbackTranAsync();
                // Log is not available in repository, re-throw with context
                throw new InvalidOperationException($"BatchSaveOrUpdate failed: {ex.Message}", ex);
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
