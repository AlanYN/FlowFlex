using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Dynamic field service implementation
    /// </summary>
    public class DynamicFieldService : IDynamicFieldService, IScopedService
    {
        private readonly IDynamicFieldRepository _dynamicFieldRepository;
        private readonly UserContext _userContext;
        private readonly ILogger<DynamicFieldService> _logger;

        public DynamicFieldService(
            IDynamicFieldRepository dynamicFieldRepository,
            UserContext userContext,
            ILogger<DynamicFieldService> logger)
        {
            _dynamicFieldRepository = dynamicFieldRepository;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(DynamicFieldInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate field ID uniqueness
            if (await _dynamicFieldRepository.ExistsFieldIdAsync(input.FieldId))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Field with ID '{input.FieldId}' already exists");
            }

            var entity = new DynamicField
            {
                FieldId = input.FieldId,
                FieldLabel = input.FieldLabel,
                FormProp = input.FormProp,
                Category = input.Category,
                FieldType = input.FieldType,
                SortOrder = input.SortOrder,
                IsRequired = input.IsRequired,
                IsSystem = false // User-created fields are not system fields
            };
            entity.InitCreateInfo(_userContext);

            var id = await _dynamicFieldRepository.InsertReturnSnowflakeIdAsync(entity);

            _logger.LogInformation($"Created dynamic field: {input.FieldId} (ID: {id})");

            return id;
        }

        public async Task<bool> UpdateAsync(long id, DynamicFieldInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _dynamicFieldRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Dynamic field not found");
            }

            // System fields cannot be modified
            if (entity.IsSystem)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    "System fields cannot be modified");
            }

            // Validate field ID uniqueness (excluding current entity)
            if (await _dynamicFieldRepository.ExistsFieldIdAsync(input.FieldId, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Field with ID '{input.FieldId}' already exists");
            }

            entity.FieldId = input.FieldId;
            entity.FieldLabel = input.FieldLabel;
            entity.FormProp = input.FormProp;
            entity.Category = input.Category;
            entity.FieldType = input.FieldType;
            entity.SortOrder = input.SortOrder;
            entity.IsRequired = input.IsRequired;
            entity.InitModifyInfo(_userContext);

            var result = await _dynamicFieldRepository.UpdateAsync(entity);

            _logger.LogInformation($"Updated dynamic field: {input.FieldId} (ID: {id})");

            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _dynamicFieldRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Dynamic field not found");
            }

            // System fields cannot be deleted
            if (entity.IsSystem)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    "System fields cannot be deleted");
            }

            // Soft delete
            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _dynamicFieldRepository.UpdateAsync(entity);

            _logger.LogInformation($"Deleted dynamic field: {entity.FieldId} (ID: {id})");

            return result;
        }

        public async Task<DynamicFieldOutputDto> GetByIdAsync(long id)
        {
            var entity = await _dynamicFieldRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Dynamic field not found");
            }

            return MapToOutputDto(entity);
        }

        public async Task<DynamicFieldOutputDto?> GetByFieldIdAsync(string fieldId)
        {
            var entity = await _dynamicFieldRepository.GetByFieldIdAsync(fieldId);
            if (entity == null)
            {
                return null;
            }

            return MapToOutputDto(entity);
        }

        public async Task<List<DynamicFieldOutputDto>> GetAllAsync()
        {
            var entities = await _dynamicFieldRepository.GetAllOrderedAsync();
            
            // Auto-initialize if no fields exist for this tenant
            if (!entities.Any())
            {
                _logger.LogInformation($"No dynamic fields found for tenant {_userContext.TenantId}, auto-initializing default fields...");
                try
                {
                    var initialized = await InitializeDefaultFieldsAsync();
                    if (initialized)
                    {
                        // Re-query after initialization
                        entities = await _dynamicFieldRepository.GetAllOrderedAsync();
                        _logger.LogInformation($"Auto-initialized {entities.Count} default fields for tenant {_userContext.TenantId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to auto-initialize default fields for tenant {_userContext.TenantId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during auto-initialization for tenant {_userContext.TenantId}");
                    // Don't throw exception, just return empty list
                }
            }
            
            // Ensure sorting by SortOrder
            return entities
                .OrderBy(x => x.SortOrder)
                .Select(MapToOutputDto)
                .ToList();
        }

        public async Task<List<DynamicFieldOutputDto>> GetByCategoryAsync(string category)
        {
            var entities = await _dynamicFieldRepository.GetByCategoryAsync(category);
            // Ensure sorting by SortOrder
            return entities
                .OrderBy(x => x.SortOrder)
                .Select(MapToOutputDto)
                .ToList();
        }

        public async Task<bool> InitializeDefaultFieldsAsync()
        {
            try
            {
                // Try multiple paths to find static-field.json
                // Priority: Local Data directory > Base directory > Common package (fallback)
                var possiblePaths = new[]
                {
                    // Local Data directory (highest priority - recommended location)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "static-field.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "static-field.json"),
                    // Base directory fallback
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static-field.json"),
                    // Common package paths (fallback for development)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "packages", "flowFlex-common", "src", "app", "components", "actionTools", "static-field.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "packages", "flowFlex-common", "src", "app", "components", "actionTools", "static-field.json")
                };

                string? jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var normalizedPath = Path.GetFullPath(path);
                        if (File.Exists(normalizedPath))
                        {
                            jsonPath = normalizedPath;
                            _logger.LogInformation($"Found static-field.json at: {jsonPath}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Error checking path {path}: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
                {
                    _logger.LogWarning($"Static field JSON file not found. Tried paths: {string.Join(", ", possiblePaths.Select(p => Path.GetFullPath(p)))}");
                    _logger.LogWarning($"Current BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                    _logger.LogWarning($"Current Directory: {Directory.GetCurrentDirectory()}");
                    return false;
                }

                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning($"static-field.json file is empty at: {jsonPath}");
                    return false;
                }

                _logger.LogInformation($"Reading static-field.json from: {jsonPath}, content length: {jsonContent.Length}");

                var staticFields = System.Text.Json.JsonSerializer.Deserialize<StaticFieldJson>(jsonContent, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (staticFields == null)
                {
                    _logger.LogWarning("Failed to deserialize static-field.json");
                    return false;
                }

                if (staticFields.FormFields == null || !staticFields.FormFields.Any())
                {
                    _logger.LogWarning($"No fields found in static-field.json. FormFields is null or empty.");
                    return false;
                }

                _logger.LogInformation($"Found {staticFields.FormFields.Count} fields in static-field.json");

                // Check if fields already initialized for this tenant
                var existingFields = await _dynamicFieldRepository.GetAllOrderedAsync();
                if (existingFields.Any(f => f.IsSystem))
                {
                    _logger.LogInformation("Default fields already initialized for this tenant");
                    return true;
                }

                // Create default fields
                var sortOrder = 0;
                foreach (var field in staticFields.FormFields)
                {
                    // Check if field already exists
                    var existingField = await _dynamicFieldRepository.GetByFieldIdAsync(field.VIfKey);
                    if (existingField != null)
                    {
                        continue; // Skip if already exists
                    }

                    var entity = new DynamicField
                    {
                        FieldId = field.VIfKey,
                        FieldLabel = field.Label,
                        FormProp = field.FormProp,
                        Category = field.Category,
                        FieldType = 0, // Default to Text
                        SortOrder = sortOrder++,
                        IsRequired = false,
                        IsSystem = true // Mark as system field
                    };
                    entity.InitCreateInfo(_userContext);

                    await _dynamicFieldRepository.InsertReturnSnowflakeIdAsync(entity);
                }

                _logger.LogInformation($"Initialized {staticFields.FormFields.Count} default fields for tenant {_userContext.TenantId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default fields");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to initialize default fields: {ex.Message}");
            }
        }

        private static DynamicFieldOutputDto MapToOutputDto(DynamicField entity)
        {
            return new DynamicFieldOutputDto
            {
                Id = entity.Id,
                FieldId = entity.FieldId,
                FieldLabel = entity.FieldLabel,
                FormProp = entity.FormProp,
                Category = entity.Category,
                FieldType = entity.FieldType,
                SortOrder = entity.SortOrder,
                IsRequired = entity.IsRequired,
                IsSystem = entity.IsSystem,
                CreateDate = entity.CreateDate,
                ModifyDate = entity.ModifyDate
            };
        }

        // JSON deserialization model for static-field.json
        private class StaticFieldJson
        {
            [System.Text.Json.Serialization.JsonPropertyName("formFields")]
            public List<StaticFieldItem> FormFields { get; set; } = new();
        }

        private class StaticFieldItem
        {
            [System.Text.Json.Serialization.JsonPropertyName("label")]
            public string Label { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("vIfKey")]
            public string VIfKey { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("formProp")]
            public string FormProp { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("category")]
            public string Category { get; set; } = string.Empty;
        }
    }
}

