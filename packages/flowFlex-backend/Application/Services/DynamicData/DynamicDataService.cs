using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

namespace FlowFlex.Application.Services.DynamicData;

/// <summary>
/// Dynamic data service implementation
/// </summary>
public class DynamicDataService : IBusinessDataService, IPropertyService, IScopedService
{
    private readonly IBusinessDataRepository _businessDataRepository;
    private readonly IDataValueRepository _dataValueRepository;
    private readonly IDefineFieldRepository _defineFieldRepository;
    private readonly IFieldGroupRepository _fieldGroupRepository;
    private readonly UserContext _userContext;
    private readonly ILogger<DynamicDataService> _logger;
    
    // Default module ID - not used as query condition
    private const int DefaultModuleId = 0;

    public DynamicDataService(
        IBusinessDataRepository businessDataRepository,
        IDataValueRepository dataValueRepository,
        IDefineFieldRepository defineFieldRepository,
        IFieldGroupRepository fieldGroupRepository,
        UserContext userContext,
        ILogger<DynamicDataService> logger)
    {
        _businessDataRepository = businessDataRepository;
        _dataValueRepository = dataValueRepository;
        _defineFieldRepository = defineFieldRepository;
        _fieldGroupRepository = fieldGroupRepository;
        _userContext = userContext;
        _logger = logger;
    }

    #region IBusinessDataService Implementation

    public async Task<DynamicDataObject?> GetBusinessDataObjectAsync(long businessId)
    {
        return await _businessDataRepository.GetBusinessDataObjectAsync(businessId);
    }

    public async Task<List<DynamicDataObject>> GetBusinessDataObjectListAsync(List<long> businessIds)
    {
        if (businessIds == null || !businessIds.Any())
            return new List<DynamicDataObject>();

        return await _businessDataRepository.GetBusinessDataObjectListAsync(businessIds);
    }

    public async Task<long> CreateBusinessDataAsync(DynamicDataObject data)
    {
        if (data == null)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Data cannot be null");

        data.SetModuleId(DefaultModuleId);
        return await _businessDataRepository.CreateBusinessDataAsync(data);
    }

    public async Task UpdateBusinessDataAsync(DynamicDataObject data)
    {
        if (data == null)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Data cannot be null");

        if (data.BusinessId <= 0)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "BusinessId is required");

        data.SetModuleId(DefaultModuleId);
        await _businessDataRepository.UpdateBusinessDataAsync(data);
    }

    public async Task UpdateBusinessDataAsync(long businessId, Dictionary<string, object?> fields)
    {
        if (fields == null || !fields.Any())
            return;

        await _businessDataRepository.UpdateBusinessDataFieldsAsync(businessId, fields);
    }

    public async Task DeleteBusinessDataAsync(long businessId)
    {
        await _businessDataRepository.DeleteBusinessDataAsync(businessId);
    }

    public async Task BatchDeleteBusinessDataAsync(List<long> businessIds)
    {
        if (businessIds == null || !businessIds.Any())
            return;

        await _businessDataRepository.BatchDeleteBusinessDataAsync(businessIds);
    }

    public DynamicDataObject ObjectToBusinessData(object obj)
    {
        var data = new DynamicDataObject(DefaultModuleId);

        if (obj == null)
            return data;

        var jObject = obj is JObject jo ? jo : JObject.FromObject(obj);

        foreach (var property in jObject.Properties())
        {
            data.Add(new FieldDataItem
            {
                FieldName = property.Name,
                Value = property.Value?.ToObject<object>()
            });
        }

        return data;
    }

    #endregion

    #region IPropertyService Implementation

    public async Task<List<DefineFieldDto>> GetPropertyListAsync()
    {
        var fields = await _defineFieldRepository.GetAllAsync();
        return fields.Select(MapToDefineFieldDto).ToList();
    }

    public async Task<PagedResult<DefineFieldDto>> GetPropertyPagedListAsync(PropertyQueryRequest request)
    {
        var pagedResult = await _defineFieldRepository.GetPagedListAsync(request);
        return new PagedResult<DefineFieldDto>
        {
            Items = pagedResult.Items.Select(MapToDefineFieldDto).ToList(),
            TotalCount = pagedResult.TotalCount,
            PageIndex = pagedResult.PageIndex,
            PageSize = pagedResult.PageSize
        };
    }

    public async Task<Stream> ExportToExcelAsync(PropertyQueryRequest request)
    {
        // Set large page size for export
        request.PageSize = 10000;
        request.PageIndex = 1;

        var pagedResult = await _defineFieldRepository.GetPagedListAsync(request);

        // Transform to export format
        var exportData = pagedResult.Items.Select(item => new PropertyExportDto
        {
            Id = item.Id.ToString(),
            FieldName = item.FieldName,
            DisplayName = item.DisplayName,
            Description = item.Description ?? string.Empty,
            DataType = GetDataTypeName(item.DataType),
            IsRequired = item.IsRequired ? "Yes" : "No",
            IsSystemDefine = item.IsSystemDefine ? "Yes" : "No",
            CreateBy = item.CreateBy,
            CreateDate = item.CreateDate.ToString("MM/dd/yyyy HH:mm:ss"),
            ModifyBy = item.ModifyBy,
            ModifyDate = item.ModifyDate.ToString("MM/dd/yyyy HH:mm:ss")
        }).ToList();

        return GenerateExcelWithEPPlus(exportData);
    }

    private static string GetDataTypeName(DataType dataType)
    {
        return dataType switch
        {
            DataType.Phone => "Phone",
            DataType.Email => "Email",
            DataType.DropDown => "DropDown",
            DataType.Bool => "Bool",
            DataType.DateTime => "DateTime",
            DataType.SingleLineText => "SingleLineText",
            DataType.MultilineText => "MultilineText",
            DataType.Number => "Number",
            DataType.StringList => "StringList",
            DataType.File => "File",
            DataType.FileList => "FileList",
            DataType.ID => "ID",
            DataType.People => "People",
            DataType.Connection => "Connection",
            DataType.Image => "Image",
            DataType.TimeLine => "TimeLine",
            _ => dataType.ToString()
        };
    }

    private static Stream GenerateExcelWithEPPlus(List<PropertyExportDto> data)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Dynamic Fields Export");

        // Set headers
        var headers = new[]
        {
            "Field ID", "Field Name", "Display Name", "Description", "Data Type",
            "Is Required", "Is System Define", "Created By", "Create Date", "Modified By", "Modify Date"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Set data
        for (int row = 0; row < data.Count; row++)
        {
            var item = data[row];
            worksheet.Cells[row + 2, 1].Value = item.Id;
            worksheet.Cells[row + 2, 2].Value = item.FieldName;
            worksheet.Cells[row + 2, 3].Value = item.DisplayName;
            worksheet.Cells[row + 2, 4].Value = item.Description;
            worksheet.Cells[row + 2, 5].Value = item.DataType;
            worksheet.Cells[row + 2, 6].Value = item.IsRequired;
            worksheet.Cells[row + 2, 7].Value = item.IsSystemDefine;
            worksheet.Cells[row + 2, 8].Value = item.CreateBy;
            worksheet.Cells[row + 2, 9].Value = item.CreateDate;
            worksheet.Cells[row + 2, 10].Value = item.ModifyBy;
            worksheet.Cells[row + 2, 11].Value = item.ModifyDate;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public async Task<DefineFieldDto?> GetPropertyByIdAsync(long propertyId)
    {
        var field = await _defineFieldRepository.GetByIdAsync(propertyId);
        return field == null ? null : MapToDefineFieldDto(field);
    }

    public async Task<DefineFieldDto?> GetPropertyByNameAsync(string propertyName)
    {
        var field = await _defineFieldRepository.GetByFieldNameAsync(propertyName);
        return field == null ? null : MapToDefineFieldDto(field);
    }

    public async Task<long> AddPropertyAsync(DefineFieldDto defineFieldDto)
    {
        if (defineFieldDto == null)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Property cannot be null");

        // Check field name uniqueness
        if (await _defineFieldRepository.ExistsFieldNameAsync(defineFieldDto.FieldName))
            throw new CRMException(ErrorCodeEnum.BusinessError, 
                $"Field name '{defineFieldDto.FieldName}' already exists");

        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);

        var entity = MapToDefineField(defineFieldDto);
        entity.ModuleId = DefaultModuleId;
        entity.TenantId = _userContext.TenantId ?? "default";
        entity.AppCode = _userContext.AppCode ?? "default";
        entity.CreateDate = DateTimeOffset.UtcNow;
        entity.CreateBy = _userContext.UserName ?? "SYSTEM";
        entity.CreateUserId = userId;
        entity.ModifyDate = DateTimeOffset.UtcNow;
        entity.ModifyBy = _userContext.UserName ?? "SYSTEM";
        entity.ModifyUserId = userId;

        return await _defineFieldRepository.InsertReturnSnowflakeIdAsync(entity);
    }

    public async Task UpdatePropertyAsync(DefineFieldDto defineFieldDto)
    {
        if (defineFieldDto == null)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Property cannot be null");

        var existing = await _defineFieldRepository.GetByIdAsync(defineFieldDto.Id);
        if (existing == null)
            throw new CRMException(ErrorCodeEnum.NotFound, "Property not found");

        // System defined property cannot be modified
        if (existing.IsSystemDefine)
            throw new CRMException(ErrorCodeEnum.BusinessError, "System defined property cannot be modified");

        // Check field name uniqueness (excluding current)
        if (await _defineFieldRepository.ExistsFieldNameAsync(defineFieldDto.FieldName, defineFieldDto.Id))
            throw new CRMException(ErrorCodeEnum.BusinessError, 
                $"Field name '{defineFieldDto.FieldName}' already exists");

        // Update fields
        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);
        
        existing.DisplayName = defineFieldDto.DisplayName;
        existing.FieldName = defineFieldDto.FieldName;
        existing.Description = defineFieldDto.Description;
        existing.DataType = defineFieldDto.DataType;
        existing.SourceType = defineFieldDto.SourceType;
        existing.SourceName = defineFieldDto.SourceName;
        existing.RefFieldId = defineFieldDto.RefFieldId;
        existing.IsDisplayField = defineFieldDto.IsDisplayField;
        existing.IsMustUse = defineFieldDto.IsMustUse;
        existing.IsRequired = defineFieldDto.IsRequired;
        existing.IsTableMustShow = defineFieldDto.IsTableMustShow;
        existing.IsHidden = defineFieldDto.IsHidden;
        existing.IsComputed = defineFieldDto.IsComputed;
        existing.AllowEdit = defineFieldDto.AllowEdit;
        existing.AllowEditItem = defineFieldDto.AllowEditItem;
        existing.Sort = defineFieldDto.Sort;
        
        // Build AdditionalInfo with configuration fields
        existing.AdditionalInfo = defineFieldDto.AdditionalInfo ?? new JObject();
        if (defineFieldDto.Format != null)
        {
            existing.AdditionalInfo["format"] = JToken.FromObject(defineFieldDto.Format);
        }
        if (defineFieldDto.FieldValidate != null)
        {
            existing.AdditionalInfo["fieldValidate"] = JToken.FromObject(defineFieldDto.FieldValidate);
        }
        if (defineFieldDto.DropdownItems != null && defineFieldDto.DropdownItems.Any())
        {
            existing.AdditionalInfo["dropdownItems"] = JToken.FromObject(defineFieldDto.DropdownItems);
        }
        
        existing.ModifyDate = DateTimeOffset.UtcNow;
        existing.ModifyBy = _userContext.UserName ?? "SYSTEM";
        existing.ModifyUserId = userId;

        await _defineFieldRepository.UpdateAsync(existing);
    }

    public async Task DeletePropertyAsync(long propertyId)
    {
        var existing = await _defineFieldRepository.GetByIdAsync(propertyId);
        if (existing == null)
            throw new CRMException(ErrorCodeEnum.NotFound, "Property not found");

        if (existing.IsSystemDefine)
            throw new CRMException(ErrorCodeEnum.BusinessError, "System defined property cannot be deleted");

        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);

        existing.IsValid = false;
        existing.ModifyDate = DateTimeOffset.UtcNow;
        existing.ModifyBy = _userContext.UserName ?? "SYSTEM";
        existing.ModifyUserId = userId;

        await _defineFieldRepository.UpdateAsync(existing);
    }

    public async Task MovePropertyToGroupAsync(long[] propertyIds, long groupId)
    {
        if (propertyIds == null || !propertyIds.Any())
            return;

        var group = await _fieldGroupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new CRMException(ErrorCodeEnum.NotFound, "Group not found");

        var existingFields = group.Fields?.ToList() ?? new List<long>();
        foreach (var propertyId in propertyIds)
        {
            if (!existingFields.Contains(propertyId))
                existingFields.Add(propertyId);
        }

        await _fieldGroupRepository.UpdateGroupFieldsAsync(groupId, existingFields.ToArray());
    }

    public async Task UpdatePropertySortAsync(Dictionary<long, int> propertySorts)
    {
        if (propertySorts == null || !propertySorts.Any())
            return;

        await _defineFieldRepository.BatchUpdateSortAsync(propertySorts);
    }

    public async Task<bool> InitializeDefaultPropertiesAsync()
    {
        try
        {
            // Try multiple paths to find static-field.json
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "static-field.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "static-field.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static-field.json")
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
                _logger.LogWarning($"Static field JSON file not found");
                return false;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning($"static-field.json file is empty");
                return false;
            }

            var staticFields = System.Text.Json.JsonSerializer.Deserialize<StaticFieldJson>(jsonContent, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (staticFields?.FormFields == null || !staticFields.FormFields.Any())
            {
                _logger.LogWarning("No fields found in static-field.json");
                return false;
            }

            _logger.LogInformation($"Found {staticFields.FormFields.Count} fields in static-field.json");

            // Check if system fields already initialized
            var existingFields = await _defineFieldRepository.GetAllAsync();
            if (existingFields.Any(f => f.IsSystemDefine))
            {
                _logger.LogInformation("Default properties already initialized");
                return true;
            }

            // Create default field group by category
            var categories = staticFields.FormFields.Select(f => f.Category).Distinct().ToList();
            var categoryGroupMap = new Dictionary<string, long>();

            long.TryParse(_userContext.UserId, out var userId);
            var sortOrder = 0;

            foreach (var category in categories)
            {
                var group = new FieldGroup
                {
                    ModuleId = DefaultModuleId,
                    GroupName = category,
                    Sort = sortOrder++,
                    IsSystemDefine = true,
                    IsDefault = category == "Basic Info",
                    Fields = Array.Empty<long>(),
                    TenantId = _userContext.TenantId ?? "default",
                    AppCode = _userContext.AppCode ?? "default",
                    CreateDate = DateTimeOffset.UtcNow,
                    CreateBy = _userContext.UserName ?? "SYSTEM",
                    CreateUserId = userId,
                    ModifyDate = DateTimeOffset.UtcNow,
                    ModifyBy = _userContext.UserName ?? "SYSTEM",
                    ModifyUserId = userId
                };

                var groupId = await _fieldGroupRepository.InsertReturnSnowflakeIdAsync(group);
                categoryGroupMap[category] = groupId;
            }

            // Create default fields
            var fieldSortOrder = 0;
            var groupFieldsMap = new Dictionary<long, List<long>>();

            foreach (var field in staticFields.FormFields)
            {
                // Check if field already exists
                var existingField = await _defineFieldRepository.GetByFieldNameAsync(field.VIfKey);
                if (existingField != null)
                    continue;

                var dataType = InferDataType(field.VIfKey, field.FormProp);

                var entity = new DefineField
                {
                    ModuleId = DefaultModuleId,
                    DisplayName = field.Label,
                    FieldName = field.VIfKey,
                    Description = field.Label,
                    DataType = dataType,
                    IsSystemDefine = true,
                    IsStatic = true,
                    IsDisplayField = field.Category == "Basic Info",
                    IsMustUse = false,
                    IsRequired = false,
                    IsTableMustShow = false,
                    IsHidden = false,
                    IsComputed = false,
                    AllowEdit = true,
                    AllowEditItem = true,
                    Sort = fieldSortOrder++,
                    TenantId = _userContext.TenantId ?? "default",
                    AppCode = _userContext.AppCode ?? "default",
                    CreateDate = DateTimeOffset.UtcNow,
                    CreateBy = _userContext.UserName ?? "SYSTEM",
                    CreateUserId = userId,
                    ModifyDate = DateTimeOffset.UtcNow,
                    ModifyBy = _userContext.UserName ?? "SYSTEM",
                    ModifyUserId = userId
                };

                var fieldId = await _defineFieldRepository.InsertReturnSnowflakeIdAsync(entity);

                // Add field to group
                if (categoryGroupMap.TryGetValue(field.Category, out var groupId))
                {
                    if (!groupFieldsMap.ContainsKey(groupId))
                        groupFieldsMap[groupId] = new List<long>();
                    groupFieldsMap[groupId].Add(fieldId);
                }
            }

            // Update group fields
            foreach (var kvp in groupFieldsMap)
            {
                await _fieldGroupRepository.UpdateGroupFieldsAsync(kvp.Key, kvp.Value.ToArray());
            }

            _logger.LogInformation($"Initialized {staticFields.FormFields.Count} default properties");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default properties");
            throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to initialize default properties: {ex.Message}");
        }
    }

    public async Task<List<DefineFieldDto>> GetPropertiesByIdsAsync(IEnumerable<long> ids)
    {
        if (ids == null || !ids.Any())
            return new List<DefineFieldDto>();

        var fields = await _defineFieldRepository.GetByIdsAsync(ids);
        return fields.Select(MapToDefineFieldDto).ToList();
    }

    /// <summary>
    /// Infer data type from field name
    /// </summary>
    private static DataType InferDataType(string fieldName, string formProp)
    {
        var lowerName = fieldName.ToLowerInvariant();
        var lowerProp = formProp.ToLowerInvariant();

        if (lowerName.Contains("email") || lowerProp.Contains("email"))
            return DataType.Email;
        if (lowerName.Contains("phone") || lowerProp.Contains("phone"))
            return DataType.Phone;
        if (lowerName.Contains("date") || lowerProp.Contains("date"))
            return DataType.DateTime;
        if (lowerName.Contains("amount") || lowerName.Contains("limit") || lowerName.Contains("score") ||
            lowerName.Contains("balance") || lowerName.Contains("allowance"))
            return DataType.Number;
        if (lowerName.Contains("note") || lowerName.Contains("notes") || lowerName.Contains("description"))
            return DataType.MultilineText;

        return DataType.SingleLineText;
    }

    #endregion

    #region JSON Models

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

    #endregion

    #region Mapping Methods

    private static DefineFieldDto MapToDefineFieldDto(DefineField entity)
    {
        var dto = new DefineFieldDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            DisplayName = entity.DisplayName,
            FieldName = entity.FieldName,
            Description = entity.Description,
            DataType = entity.DataType,
            SourceType = entity.SourceType,
            SourceName = entity.SourceName,
            RefFieldId = entity.RefFieldId,
            IsSystemDefine = entity.IsSystemDefine,
            IsStatic = entity.IsStatic,
            IsDisplayField = entity.IsDisplayField,
            IsMustUse = entity.IsMustUse,
            IsRequired = entity.IsRequired,
            IsTableMustShow = entity.IsTableMustShow,
            IsHidden = entity.IsHidden,
            IsComputed = entity.IsComputed,
            AllowEdit = entity.AllowEdit,
            AllowEditItem = entity.AllowEditItem,
            Sort = entity.Sort,
            CreateDate = entity.CreateDate.ToUniversalTime(),
            ModifyDate = entity.ModifyDate.ToUniversalTime(),
            CreateBy = entity.CreateBy,
            ModifyBy = entity.ModifyBy,
            AdditionalInfo = entity.AdditionalInfo
        };

        // Extract configuration from AdditionalInfo if present
        if (entity.AdditionalInfo != null)
        {
            // Extract Format configuration
            if (entity.AdditionalInfo.TryGetValue("format", out var formatToken) && formatToken.Type != JTokenType.Null)
            {
                dto.Format = formatToken.ToObject<FieldTypeFormatDto>();
            }

            // Extract FieldValidate configuration
            if (entity.AdditionalInfo.TryGetValue("fieldValidate", out var validateToken) && validateToken.Type != JTokenType.Null)
            {
                dto.FieldValidate = validateToken.ToObject<FieldValidateDto>();
            }

            // Extract DropdownItems configuration
            if (entity.AdditionalInfo.TryGetValue("dropdownItems", out var dropdownToken) && dropdownToken.Type != JTokenType.Null)
            {
                dto.DropdownItems = dropdownToken.ToObject<List<DropdownItemDto>>();
            }
        }

        return dto;
    }

    private static DefineField MapToDefineField(DefineFieldDto dto)
    {
        var entity = new DefineField
        {
            Id = dto.Id,
            ModuleId = dto.ModuleId,
            DisplayName = dto.DisplayName,
            FieldName = dto.FieldName,
            Description = dto.Description,
            DataType = dto.DataType,
            SourceType = dto.SourceType,
            SourceName = dto.SourceName,
            RefFieldId = dto.RefFieldId,
            IsSystemDefine = dto.IsSystemDefine,
            IsStatic = dto.IsStatic,
            IsDisplayField = dto.IsDisplayField,
            IsMustUse = dto.IsMustUse,
            IsRequired = dto.IsRequired,
            IsTableMustShow = dto.IsTableMustShow,
            IsHidden = dto.IsHidden,
            IsComputed = dto.IsComputed,
            AllowEdit = dto.AllowEdit,
            AllowEditItem = dto.AllowEditItem,
            Sort = dto.Sort,
            AdditionalInfo = dto.AdditionalInfo ?? new JObject()
        };

        // Store configuration in AdditionalInfo
        if (dto.Format != null)
        {
            entity.AdditionalInfo["format"] = JToken.FromObject(dto.Format);
        }

        if (dto.FieldValidate != null)
        {
            entity.AdditionalInfo["fieldValidate"] = JToken.FromObject(dto.FieldValidate);
        }

        if (dto.DropdownItems != null && dto.DropdownItems.Any())
        {
            entity.AdditionalInfo["dropdownItems"] = JToken.FromObject(dto.DropdownItems);
        }

        return entity;
    }

    #endregion
}
