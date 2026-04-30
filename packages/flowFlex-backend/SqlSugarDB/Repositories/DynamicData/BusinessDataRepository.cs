using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.DynamicData;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Repositories.DynamicData;

/// <summary>
/// Business data repository implementation
/// </summary>
public class BusinessDataRepository : BaseRepository<BusinessData>, IBusinessDataRepository, IScopedService
{
    private readonly IDataValueRepository _dataValueRepository;
    private readonly IDefineFieldRepository _defineFieldRepository;
    private readonly UserContext _userContext;
    private readonly ILogger<BusinessDataRepository> _logger;
    
    // Default module ID - not used as query condition
    private const int DefaultModuleId = 0;

    public BusinessDataRepository(
        ISqlSugarClient sqlSugarClient,
        IDataValueRepository dataValueRepository,
        IDefineFieldRepository defineFieldRepository,
        UserContext userContext,
        ILogger<BusinessDataRepository> logger) : base(sqlSugarClient)
    {
        _dataValueRepository = dataValueRepository;
        _defineFieldRepository = defineFieldRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<DynamicDataObject?> GetBusinessDataObjectAsync(long businessId)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        var businessData = await db.Queryable<BusinessData>()
            .Where(x => x.Id == businessId && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .FirstAsync();

        if (businessData == null)
            return null;

        var dataValues = await _dataValueRepository.GetByBusinessIdAsync(businessId);
        var fieldDefinitions = await _defineFieldRepository.GetAllAsync();

        return BuildDynamicDataObject(businessData, dataValues, fieldDefinitions);
    }

    public async Task<List<DynamicDataObject>> GetBusinessDataObjectListAsync(List<long> businessIds)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        var businessDataList = await db.Queryable<BusinessData>()
            .Where(x => businessIds.Contains(x.Id) && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .ToListAsync();

        if (!businessDataList.Any())
            return new List<DynamicDataObject>();

        var dataValues = await _dataValueRepository.GetByBusinessIdsAsync(businessIds);
        var fieldDefinitions = await _defineFieldRepository.GetAllAsync();

        var result = new List<DynamicDataObject>();
        foreach (var businessData in businessDataList)
        {
            var values = dataValues.Where(v => v.BusinessId == businessData.Id).ToList();
            result.Add(BuildDynamicDataObject(businessData, values, fieldDefinitions));
        }

        return result;
    }

    public async Task<long> CreateBusinessDataAsync(DynamicDataObject data)
    {
        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);
        
        var businessData = new BusinessData
        {
            ModuleId = DefaultModuleId,
            InternalData = data.InternalData,
            TenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext),
            AppCode = TenantContextHelper.GetAppCodeOrDefault(_userContext),
            CreateDate = DateTimeOffset.UtcNow,
            CreateBy = _userContext.UserName ?? "SYSTEM",
            CreateUserId = userId,
            ModifyDate = DateTimeOffset.UtcNow,
            ModifyBy = _userContext.UserName ?? "SYSTEM",
            ModifyUserId = userId
        };

        var businessId = await db.Insertable(businessData).ExecuteReturnSnowflakeIdAsync();

        // Get field definitions for data type mapping
        var fieldDefinitions = await _defineFieldRepository.GetAllAsync();
        var fieldDict = fieldDefinitions.ToDictionary(f => f.FieldName, f => f, StringComparer.OrdinalIgnoreCase);

        // Create data values
        var dataValues = new List<DataValue>();
        foreach (var item in data)
        {
            if (string.IsNullOrEmpty(item.FieldName))
                continue;

            var dataValue = CreateDataValue(businessId, item, fieldDict);
            if (dataValue != null)
                dataValues.Add(dataValue);
        }

        if (dataValues.Any())
            await _dataValueRepository.BatchInsertAsync(dataValues);

        return businessId;
    }

    public async Task UpdateBusinessDataAsync(DynamicDataObject data)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        var businessData = await db.Queryable<BusinessData>()
            .Where(x => x.Id == data.BusinessId && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .FirstAsync();

        if (businessData == null)
            return;

        // Update business data
        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext?.UserId, out var userId);
        
        businessData.InternalData = data.InternalData;
        businessData.ModifyDate = DateTimeOffset.UtcNow;
        businessData.ModifyBy = _userContext?.UserName ?? "SYSTEM";
        businessData.ModifyUserId = userId;

        await db.Updateable(businessData).ExecuteCommandAsync();

        // Get existing data values
        var existingValues = await _dataValueRepository.GetByBusinessIdAsync(data.BusinessId);
        var existingDict = existingValues.ToDictionary(v => v.FieldName, v => v, StringComparer.OrdinalIgnoreCase);

        // Get field definitions
        var fieldDefinitions = await _defineFieldRepository.GetAllAsync();
        var fieldDict = fieldDefinitions.ToDictionary(f => f.FieldName, f => f, StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<DataValue>();
        var toUpdate = new List<DataValue>();

        foreach (var item in data)
        {
            if (string.IsNullOrEmpty(item.FieldName))
                continue;

            if (existingDict.TryGetValue(item.FieldName, out var existing))
            {
                // Update existing
                SetDataValueField(existing, item, fieldDict);
                toUpdate.Add(existing);
            }
            else
            {
                // Insert new
                var dataValue = CreateDataValue(data.BusinessId, item, fieldDict);
                if (dataValue != null)
                    toInsert.Add(dataValue);
            }
        }

        if (toInsert.Any())
            await _dataValueRepository.BatchInsertAsync(toInsert);

        if (toUpdate.Any())
            await _dataValueRepository.BatchUpdateAsync(toUpdate);
    }

    public async Task UpdateBusinessDataFieldsAsync(long businessId, Dictionary<string, object?> fields)
    {
        var data = new DynamicDataObject(DefaultModuleId) { BusinessId = businessId };
        foreach (var field in fields)
        {
            data.Add(new FieldDataItem { FieldName = field.Key, Value = field.Value });
        }

        await UpdateBusinessDataAsync(data);
    }

    public async Task DeleteBusinessDataAsync(long businessId)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        
        var businessData = await db.Queryable<BusinessData>()
            .Where(x => x.Id == businessId && x.IsValid)
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .FirstAsync();

        if (businessData == null)
            return;

        // Soft delete business data
        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext?.UserId, out var userId);
        
        businessData.IsValid = false;
        businessData.ModifyDate = DateTimeOffset.UtcNow;
        businessData.ModifyBy = _userContext?.UserName ?? "SYSTEM";
        businessData.ModifyUserId = userId;

        await db.Updateable(businessData).ExecuteCommandAsync();

        // Soft delete data values
        await _dataValueRepository.DeleteByBusinessIdAsync(businessId);
    }

    public async Task BatchDeleteBusinessDataAsync(List<long> businessIds)
    {
        var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
        var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
        var modifyBy = _userContext?.UserName ?? "SYSTEM";
        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext?.UserId, out var modifyUserId);
        
        await db.Updateable<BusinessData>()
            .SetColumns(x => x.IsValid == false)
            .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
            .SetColumns(x => x.ModifyBy == modifyBy)
            .SetColumns(x => x.ModifyUserId == modifyUserId)
            .Where(x => businessIds.Contains(x.Id))
            .Where(x => x.TenantId == tenantId && x.AppCode == appCode)
            .ExecuteCommandAsync();

        await _dataValueRepository.DeleteByBusinessIdsAsync(businessIds);
    }

    #region Private Methods

    private DynamicDataObject BuildDynamicDataObject(
        BusinessData businessData, 
        List<DataValue> dataValues, 
        List<DefineField> fieldDefinitions)
    {
        var result = new DynamicDataObject(businessData.ModuleId)
        {
            BusinessId = businessData.Id,
            InternalData = businessData.InternalData,
            CreateDate = businessData.CreateDate,
            ModifyDate = businessData.ModifyDate,
            CreateBy = businessData.CreateBy,
            ModifyBy = businessData.ModifyBy,
            CreateUserId = businessData.CreateUserId,
            ModifyUserId = businessData.ModifyUserId
        };

        var fieldDict = fieldDefinitions.ToDictionary(f => f.Id, f => f);

        foreach (var dataValue in dataValues)
        {
            fieldDict.TryGetValue(dataValue.FieldId, out var fieldDef);

            var item = new FieldDataItem
            {
                BusinessId = dataValue.BusinessId,
                FieldId = dataValue.FieldId,
                FieldName = dataValue.FieldName,
                DisplayName = fieldDef?.FieldName ?? dataValue.FieldName,
                DataType = dataValue.DataType,
                Description = fieldDef?.Description,
                Sort = fieldDef?.Sort ?? 0,
                IsHidden = fieldDef?.IsHidden ?? false,
                IsDisplayField = fieldDef?.IsDisplayField ?? false,
                Value = GetValueFromDataValue(dataValue)
            };

            result.Add(item);
        }

        return result;
    }

    private object? GetValueFromDataValue(DataValue dataValue)
    {
        return dataValue.DataType switch
        {
            DataType.Phone or DataType.Email => dataValue.Varchar100Value,
            DataType.SingleLineText => dataValue.Varchar500Value,
            DataType.MultilineText => dataValue.TextValue,
            DataType.Number => dataValue.DoubleValue,
            DataType.Bool => dataValue.BoolValue,
            DataType.DateTime => dataValue.DateTimeValue,
            DataType.DropDown or DataType.File or DataType.People or 
            DataType.Connection or DataType.Image => dataValue.LongValue,
            DataType.StringList or DataType.FileList or DataType.TimeLine => dataValue.StringListValue,
            _ => dataValue.TextValue
        };
    }

    private DataValue? CreateDataValue(
        long businessId, 
        FieldDataItem item, 
        Dictionary<string, DefineField> fieldDict)
    {
        var dataType = item.DataType;
        long fieldId = item.FieldId;

        if (fieldDict.TryGetValue(item.FieldName, out var fieldDef))
        {
            dataType = fieldDef.DataType;
            fieldId = fieldDef.Id;
        }

        var dataValue = new DataValue
        {
            ModuleId = DefaultModuleId,
            BusinessId = businessId,
            FieldId = fieldId,
            FieldName = item.FieldName,
            DataType = dataType,
            TenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext),
            AppCode = TenantContextHelper.GetAppCodeOrDefault(_userContext)
        };

        SetDataValueField(dataValue, item, fieldDict);

        return dataValue;
    }

    private void SetDataValueField(
        DataValue dataValue, 
        FieldDataItem item, 
        Dictionary<string, DefineField> fieldDict)
    {
        var dataType = item.DataType;
        if (fieldDict.TryGetValue(item.FieldName, out var fieldDef))
        {
            dataType = fieldDef.DataType;
            dataValue.FieldId = fieldDef.Id;
        }

        dataValue.DataType = dataType;

        // Clear all value fields first
        dataValue.LongValue = null;
        dataValue.IntValue = null;
        dataValue.DoubleValue = null;
        dataValue.TextValue = null;
        dataValue.Varchar100Value = null;
        dataValue.Varchar500Value = null;
        dataValue.VarcharValue = null;
        dataValue.BoolValue = null;
        dataValue.DateTimeValue = null;
        dataValue.StringListValue = null;

        if (item.Value == null)
            return;

        switch (dataType)
        {
            case DataType.Phone:
            case DataType.Email:
                dataValue.Varchar100Value = item.Value?.ToString();
                break;

            case DataType.SingleLineText:
                dataValue.Varchar500Value = item.Value?.ToString();
                break;

            case DataType.MultilineText:
                dataValue.TextValue = item.Value?.ToString();
                break;

            case DataType.Number:
                if (double.TryParse(item.Value?.ToString(), out var doubleVal))
                    dataValue.DoubleValue = doubleVal;
                break;

            case DataType.Bool:
                if (bool.TryParse(item.Value?.ToString(), out var boolVal))
                    dataValue.BoolValue = boolVal;
                break;

            case DataType.DateTime:
                if (item.Value is DateTimeOffset dto)
                    dataValue.DateTimeValue = dto;
                else if (DateTimeOffset.TryParse(item.Value?.ToString(), out var parsedDto))
                    dataValue.DateTimeValue = parsedDto;
                break;

            case DataType.DropDown:
            case DataType.File:
            case DataType.People:
            case DataType.Connection:
            case DataType.Image:
                if (long.TryParse(item.Value?.ToString(), out var longVal))
                    dataValue.LongValue = longVal;
                break;

            case DataType.StringList:
            case DataType.FileList:
            case DataType.TimeLine:
                if (item.Value is JToken jToken)
                    dataValue.StringListValue = jToken;
                else
                    dataValue.StringListValue = JToken.FromObject(item.Value);
                break;

            default:
                dataValue.TextValue = item.Value?.ToString();
                break;
        }
    }

    #endregion
}
