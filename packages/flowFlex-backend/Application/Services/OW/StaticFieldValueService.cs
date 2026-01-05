using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using System.Text.Json;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using FlowFlex.Application.Services.OW.Extensions;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Static field value service implementation
    /// </summary>
    public class StaticFieldValueService : IStaticFieldValueService, IScopedService
    {
        private readonly IStaticFieldValueRepository _staticFieldValueRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IdmUserDataClient _idmUserDataClient;

        public StaticFieldValueService(
            IStaticFieldValueRepository staticFieldValueRepository,
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IOperationChangeLogService operationChangeLogService,
            IMapper mapper,
            UserContext userContext,
            IOperatorContextService operatorContextService,
            IdmUserDataClient idmUserDataClient)
        {
            _staticFieldValueRepository = staticFieldValueRepository;
            _stageRepository = stageRepository;
            _onboardingRepository = onboardingRepository;
            _operationChangeLogService = operationChangeLogService;
            _mapper = mapper;
            _userContext = userContext;
            _operatorContextService = operatorContextService;
            _idmUserDataClient = idmUserDataClient;
        }

        /// <summary>
        /// Get current user name from OperatorContextService (FirstName + LastName > UserName > Email)
        /// </summary>
        private string GetCurrentUserName()
        {
            return _operatorContextService.GetOperatorDisplayName();
        }

        /// <summary>
        /// Log static field value change to Change Log
        /// </summary>
        private async Task LogStaticFieldValueChangeAsync(long onboardingId, long stageId, string fieldName, string action, object oldValue = null, object newValue = null, string notes = null)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                var stage = await _stageRepository.GetByIdAsync(stageId);

                var logData = new
                {
                    OnboardingId = onboardingId,
                    LeadId = onboarding?.LeadId,
                    LeadName = onboarding?.CaseName,
                    StageId = stageId,
                    StageName = stage?.Name ?? "Unknown",
                    FieldName = fieldName,
                    Action = action,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Notes = notes,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = GetCurrentUserName(),
                    Source = "static_field_value"
                };

                // Stage completion log functionality removed
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Save or update a static field value
        /// </summary>
        public async Task<long> SaveAsync(StaticFieldValueInputDto input)
        {
            var entity = _mapper.Map<StaticFieldValue>(input);

            // Ensure FieldValueJson is properly formatted as JSON
            entity.FieldValueJson = EnsureValidJson(entity.FieldValueJson);

            // Check if record exists by OnboardingId and FieldName (ignore StageId for updates)
            var existingEntity = await _staticFieldValueRepository.GetByOnboardingAndFieldAsync(
                entity.OnboardingId, entity.FieldName);

            if (existingEntity != null)
            {
                // Update existing record and replace StageId
                entity.Id = existingEntity.Id;
                entity.InitUpdateInfo(_userContext);
                AuditHelper.ApplyModifyAudit(entity, _operatorContextService);
                entity.Version = existingEntity.Version + 1;
                entity.IsLatest = true;

                await _staticFieldValueRepository.UpdateAsync(entity);

                // Process Assignee field data to include user names for logging
                var processedBeforeData = await ProcessAssigneeFieldDataAsync(existingEntity.FieldValueJson, entity.FieldName);
                var processedAfterData = await ProcessAssigneeFieldDataAsync(entity.FieldValueJson, entity.FieldName);

                // Log the update
                await _operationChangeLogService.LogStaticFieldValueChangeAsync(
                    entity.Id,
                    entity.FieldName,
                    entity.OnboardingId,
                    entity.StageId,
                    processedBeforeData,
                    processedAfterData,
                    GetChangedFields(existingEntity, entity),
                    entity.DisplayName
                );

                return entity.Id;
            }
            else
            {
                // Create new record
                entity.InitCreateInfo(_userContext);
                AuditHelper.ApplyCreateAudit(entity, _operatorContextService);
                entity.Version = 1;
                entity.IsLatest = true;

                long result = await _staticFieldValueRepository.InsertReturnBigIdentityAsync(entity);

                // Process Assignee field data to include user names for logging
                var processedAfterData = await ProcessAssigneeFieldDataAsync(entity.FieldValueJson, entity.FieldName);

                // Log the creation
                await _operationChangeLogService.LogStaticFieldValueChangeAsync(
                    result,
                    entity.FieldName,
                    entity.OnboardingId,
                    entity.StageId,
                    null,
                    processedAfterData,
                    new List<string> { "FieldValueJson", "Status", "CompletionRate" },
                    entity.DisplayName
                );

                return result;
            }
        }

        /// <summary>
        /// Batch save static field values
        /// </summary>
        public async Task<bool> BatchSaveAsync(BatchStaticFieldValueInputDto input)
        {
            List<StaticFieldValue> entities = new List<StaticFieldValue>();
            Dictionary<string, StaticFieldValue> oldEntitiesMap = new Dictionary<string, StaticFieldValue>();
            Dictionary<string, string> fieldLabelMap = new Dictionary<string, string>();

            foreach (StaticFieldValueInputDto fieldValue in input.FieldValues)
            {
                StaticFieldValue entity = _mapper.Map<StaticFieldValue>(fieldValue);
                entity.OnboardingId = input.OnboardingId;
                entity.StageId = input.StageId;
                entity.Source = input.Source;
                entity.IpAddress = input.IpAddress;
                entity.UserAgent = input.UserAgent;

                // Ensure FieldValueJson is properly formatted as JSON
                entity.FieldValueJson = EnsureValidJson(entity.FieldValueJson);

                // Initialize create/update information
                entity.InitCreateInfo(_userContext);
                AuditHelper.ApplyCreateAudit(entity, _operatorContextService);

                // Debug logging for audit fields
                Console.WriteLine($"[StaticFieldValueService] After audit - Field: {entity.FieldName}, CreateBy: '{entity.CreateBy}', ModifyBy: '{entity.ModifyBy}', CreateUserId: {entity.CreateUserId}, ModifyUserId: {entity.ModifyUserId}");

                // Store fieldLabel mapping for operation logging
                if (!string.IsNullOrEmpty(fieldValue.FieldLabel))
                {
                    fieldLabelMap[entity.FieldName] = fieldValue.FieldLabel;
                }

                // Get old entity by business key (OnboardingId and FieldName only)
                var businessKey = $"{input.OnboardingId}_{entity.FieldName}";
                var oldEntity = await _staticFieldValueRepository.GetByOnboardingAndFieldAsync(
                    input.OnboardingId,
                    entity.FieldName
                );

                if (oldEntity != null)
                {
                    var oldEntityValue = _mapper.Map<StaticFieldValue>(oldEntity);
                    oldEntitiesMap[businessKey] = oldEntityValue;
                }

                entities.Add(entity);
            }

            var result = await _staticFieldValueRepository.BatchSaveOrUpdateAsync(entities);

            // Log batch update with detailed operation logging
            if (result)
            {
                foreach (var entity in entities)
                {
                    // Find corresponding old entity by business key
                    var businessKey = $"{entity.OnboardingId}_{entity.FieldName}";
                    StaticFieldValue oldEntity = null;
                    if (oldEntitiesMap.ContainsKey(businessKey))
                    {
                        oldEntity = oldEntitiesMap[businessKey];
                    }

                    var changedFields = oldEntity != null ? GetChangedFields(oldEntity, entity) : new List<string> { "FieldValueJson", "Status", "CompletionRate" };

                    // Get fieldLabel from mapping
                    string fieldLabel = null;
                    if (fieldLabelMap.ContainsKey(entity.FieldName))
                    {
                        fieldLabel = fieldLabelMap[entity.FieldName];
                    }
                    else if (!string.IsNullOrEmpty(entity.DisplayName))
                    {
                        fieldLabel = entity.DisplayName;
                    }

                    // Process Assignee field data to include user names for logging
                    var processedBeforeData = await ProcessAssigneeFieldDataAsync(oldEntity?.FieldValueJson, entity.FieldName);
                    var processedAfterData = await ProcessAssigneeFieldDataAsync(entity.FieldValueJson, entity.FieldName);

                    await _operationChangeLogService.LogStaticFieldValueChangeAsync(
                        entity.Id,
                        entity.FieldName,
                        entity.OnboardingId,
                        entity.StageId,
                        processedBeforeData,
                        processedAfterData,
                        changedFields,
                        fieldLabel
                    );
                }
            }

            return result;
        }

        public async Task<StaticFieldValueOutputDto?> GetByIdAsync(long id)
        {
            StaticFieldValue? entity = await _staticFieldValueRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<StaticFieldValueOutputDto>(entity);
        }

        public async Task<List<StaticFieldValueOutputDto>> GetByOnboardingIdAsync(long onboardingId)
        {
            List<StaticFieldValue> entities = await _staticFieldValueRepository.GetByOnboardingIdAsync(onboardingId);
            return _mapper.Map<List<StaticFieldValueOutputDto>>(entities);
        }

        public async Task<List<StaticFieldValueOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            List<StaticFieldValue> entities = await _staticFieldValueRepository.GetByOnboardingAndStageAsync(onboardingId, stageId);
            return _mapper.Map<List<StaticFieldValueOutputDto>>(entities);
        }

        public async Task<StaticFieldValueOutputDto?> GetByOnboardingStageAndFieldAsync(long onboardingId, long stageId, string fieldName)
        {
            StaticFieldValue? entity = await _staticFieldValueRepository.GetByOnboardingStageAndFieldAsync(onboardingId, stageId, fieldName);
            return entity == null ? null : _mapper.Map<StaticFieldValueOutputDto>(entity);
        }

        public async Task<List<StaticFieldValueOutputDto>> GetLatestByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            List<StaticFieldValue> entities = await _staticFieldValueRepository.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);
            return _mapper.Map<List<StaticFieldValueOutputDto>>(entities);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            StaticFieldValue? entity = await _staticFieldValueRepository.GetByIdAsync(id);
            if (entity == null) return false;

            return await _staticFieldValueRepository.DeleteAsync(entity);
        }

        public async Task<bool> DeleteByOnboardingIdAsync(long onboardingId)
        {
            return await _staticFieldValueRepository.DeleteByOnboardingIdAsync(onboardingId);
        }

        public async Task<bool> DeleteByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            return await _staticFieldValueRepository.DeleteByOnboardingAndStageAsync(onboardingId, stageId);
        }

        public async Task<Dictionary<string, string>> ValidateFieldValuesAsync(long onboardingId, long stageId)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            List<StaticFieldValue> fieldValues = await _staticFieldValueRepository.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);

            // Get Stage configuration to validate required fields
            Stage? stage = await _stageRepository.GetByIdAsync(stageId);
            if (stage == null)
            {
                errors["stage"] = "Stage not found";
                return errors;
            }

            // Parse static field configuration from Components
            List<StaticFieldConfig> staticFields = new List<StaticFieldConfig>();
            if (!string.IsNullOrEmpty(stage.ComponentsJson))
            {
                try
                {
                    var components = JsonSerializer.Deserialize<List<FlowFlex.Domain.Shared.Models.StageComponent>>(stage.ComponentsJson);
                    var fieldsComponent = components?.FirstOrDefault(c => c.Key == "fields");
                    if (fieldsComponent?.StaticFields != null)
                    {
                        staticFields = fieldsComponent.StaticFields;
                    }
                }
                catch
                {
                    // If parsing fails, ignore the error
                }
            }

            foreach (StaticFieldValue fieldValue in fieldValues)
            {
                // Required field validation
                if (fieldValue.IsRequired && string.IsNullOrWhiteSpace(fieldValue.FieldValueJson))
                {
                    errors[fieldValue.FieldName] = $"{fieldValue.DisplayName ?? fieldValue.FieldName} is a required field";
                    continue;
                }

                // Field type validation
                string? validationError = ValidateFieldValue(fieldValue.FieldValueJson, fieldValue.FieldType);
                if (!string.IsNullOrEmpty(validationError))
                {
                    errors[fieldValue.FieldName] = validationError;
                }
            }

            return errors;
        }

        public async Task<bool> SubmitFieldValuesAsync(long onboardingId, long stageId)
        {
            // First validate field values
            Dictionary<string, string> validationErrors = await ValidateFieldValuesAsync(onboardingId, stageId);
            if (validationErrors.Any())
            {
                return false;
            }

            // Update status to submitted
            List<StaticFieldValue> fieldValues = await _staticFieldValueRepository.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);
            foreach (StaticFieldValue fieldValue in fieldValues)
            {
                fieldValue.Status = "Submitted";
                fieldValue.SubmitTime = DateTimeOffset.UtcNow;
                fieldValue.ValidationStatus = "Valid";
                fieldValue.CompletionRate = 100;
                fieldValue.ModifyDate = DateTimeOffset.UtcNow;
            }

            return await _staticFieldValueRepository.BatchSaveOrUpdateAsync(fieldValues);
        }

        public async Task<List<StaticFieldValueOutputDto>> GetFieldHistoryAsync(long onboardingId, long stageId, string fieldName)
        {
            List<StaticFieldValue> entities = await _staticFieldValueRepository.GetFieldHistoryAsync(onboardingId, stageId, fieldName);
            return _mapper.Map<List<StaticFieldValueOutputDto>>(entities);
        }

        public async Task<bool> CopyFieldValuesAsync(long sourceOnboardingId, List<long> targetOnboardingIds, long stageId, List<string>? fieldNames = null)
        {
            // Get source data
            List<StaticFieldValue> sourceFieldValues = await _staticFieldValueRepository.GetLatestByOnboardingAndStageAsync(sourceOnboardingId, stageId);

            if (fieldNames != null && fieldNames.Any())
            {
                sourceFieldValues = sourceFieldValues.Where(f => fieldNames.Contains(f.FieldName)).ToList();
            }

            if (!sourceFieldValues.Any()) return true;

            // Copy to target Onboarding
            foreach (long targetOnboardingId in targetOnboardingIds)
            {
                List<StaticFieldValue> targetFieldValues = new List<StaticFieldValue>();

                foreach (StaticFieldValue sourceFieldValue in sourceFieldValues)
                {
                    StaticFieldValue targetFieldValue = new StaticFieldValue
                    {
                        OnboardingId = targetOnboardingId,
                        StageId = sourceFieldValue.StageId,
                        FieldName = sourceFieldValue.FieldName,
                        DisplayName = sourceFieldValue.DisplayName,
                        FieldValueJson = EnsureValidJson(sourceFieldValue.FieldValueJson),
                        FieldType = sourceFieldValue.FieldType,
                        IsRequired = sourceFieldValue.IsRequired,
                        Status = "Draft",
                        CompletionRate = sourceFieldValue.CompletionRate,
                        Source = "copied",
                        ValidationStatus = "Pending",
                        Version = 1,
                        IsLatest = true,
                        CreateDate = DateTimeOffset.UtcNow,
                        ModifyDate = DateTimeOffset.UtcNow,
                        CreateBy = GetCurrentUserName(),
                        ModifyBy = GetCurrentUserName()
                    };

                    targetFieldValues.Add(targetFieldValue);
                }

                await _staticFieldValueRepository.BatchSaveOrUpdateAsync(targetFieldValues);
            }

            return true;
        }

        private string? ValidateFieldValue(string? value, string fieldType)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            switch (fieldType.ToLower())
            {
                case "email":
                    if (!IsValidEmail(value))
                        return "Please enter a valid email address";
                    break;
                case "phone":
                    if (!IsValidPhone(value))
                        return "Please enter a valid phone number";
                    break;
                case "number":
                    if (!decimal.TryParse(value, out _))
                        return "Please enter a valid number";
                    break;
                case "date":
                    if (!DateTimeOffset.TryParse(value, out _))
                        return "Please enter a valid date";
                    break;
                case "boolean":
                    if (!bool.TryParse(value, out _))
                        return "Please enter a valid boolean value";
                    break;
            }

            return null;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Simple phone number validation, can be adjusted as needed
            return phone.All(c => char.IsDigit(c) || c == '-' || c == '(' || c == ')' || c == ' ' || c == '+');
        }

        /// <summary>
        /// Get changed fields between old and new entities
        /// </summary>
        private List<string> GetChangedFields(StaticFieldValue oldEntity, StaticFieldValue newEntity)
        {
            var changedFields = new List<string>();

            if (oldEntity.FieldValueJson != newEntity.FieldValueJson)
                changedFields.Add("FieldValueJson");

            if (oldEntity.Status != newEntity.Status)
                changedFields.Add("Status");

            if (oldEntity.CompletionRate != newEntity.CompletionRate)
                changedFields.Add("CompletionRate");

            if (oldEntity.IsRequired != newEntity.IsRequired)
                changedFields.Add("IsRequired");

            if (oldEntity.ValidationStatus != newEntity.ValidationStatus)
                changedFields.Add("ValidationStatus");

            return changedFields;
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


        /// <summary>
        /// Get user information from IDM by user IDs
        /// </summary>
        /// <param name="userIds">List of user IDs</param>
        /// <returns>Dictionary of user ID to user display name</returns>
        private async Task<Dictionary<string, string>> GetUserInfoByIdsAsync(List<string> userIds)
        {
            var userMap = new Dictionary<string, string>();

            if (userIds == null || !userIds.Any())
            {
                return userMap;
            }

            try
            {
                Console.WriteLine($"Calling IDM API for user IDs: {string.Join(", ", userIds)}");

                // Use IdmUserDataClient to get team users with proper authentication
                var teamUsers = await _idmUserDataClient.GetAllTeamUsersAsync("1401", 10000, 1);

                if (teamUsers != null && teamUsers.Any())
                {
                    Console.WriteLine($"Found {teamUsers.Count} users from IDM API");

                    foreach (var user in teamUsers)
                    {
                        if (userIds.Contains(user.Id))
                        {
                            // Use UserName as display name (this is what's available from team users API)
                            var displayName = !string.IsNullOrEmpty(user.UserName) ? user.UserName : user.Id;

                            userMap[user.Id] = displayName;
                            Console.WriteLine($"Mapped user {user.Id} to '{displayName}'");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No team users returned from IDM API");
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing without user names
                Console.WriteLine($"Exception calling IDM API: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            // For any user IDs that weren't found, use the ID as fallback
            foreach (var userId in userIds)
            {
                if (!userMap.ContainsKey(userId))
                {
                    userMap[userId] = userId;
                }
            }

            return userMap;
        }

        /// <summary>
        /// Process Assignee field data to replace user IDs with user names
        /// </summary>
        /// <param name="fieldData">Field data JSON string</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Processed field data with user names</returns>
        private async Task<string> ProcessAssigneeFieldDataAsync(string fieldData, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldData) ||
                (!string.Equals(fieldName, "Assignee", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(fieldName, "ASSIGNEE", StringComparison.OrdinalIgnoreCase)))
            {
                return fieldData;
            }

            try
            {
                List<string> userIds = null;

                // First try to parse as direct array (for newer format)
                if (fieldData.Trim().StartsWith("["))
                {
                    userIds = JsonSerializer.Deserialize<List<string>>(fieldData);
                }
                else
                {
                    // Try to parse as object with value property (for older format)
                    var fieldDataObj = JsonSerializer.Deserialize<JsonElement>(fieldData);

                    if (fieldDataObj.TryGetProperty("value", out var valueElement))
                    {
                        var valueString = valueElement.GetString();
                        if (!string.IsNullOrEmpty(valueString))
                        {
                            // Try to parse as JSON array of user IDs
                            userIds = JsonSerializer.Deserialize<List<string>>(valueString);
                        }
                    }
                }

                if (userIds != null && userIds.Any())
                {
                    // Get user information
                    var userMap = await GetUserInfoByIdsAsync(userIds);

                    // Create a list of user display names
                    var userNames = userIds.Select(id => userMap.ContainsKey(id) ? userMap[id] : id).ToList();

                    // Create enhanced field data with both IDs and names
                    var enhancedData = new
                    {
                        value = fieldData.Trim().StartsWith("[") ? fieldData : JsonSerializer.Serialize(userIds), // Keep original IDs for system processing
                        userIds = userIds,
                        userNames = userNames,
                        fieldName = fieldName,
                        displayValue = string.Join(", ", userNames) // Human-readable display
                    };

                    return JsonSerializer.Serialize(enhancedData);
                }
            }
            catch (Exception ex)
            {
                // If processing fails, return original data
                // Add some basic logging for debugging
                Console.WriteLine($"Failed to process Assignee field data: {ex.Message}");
                Console.WriteLine($"Field data: {fieldData}");
            }

            return fieldData;
        }
    }
}
