using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.DynamicData;

/// <summary>
/// Property group service implementation
/// </summary>
public class PropertyGroupService : IPropertyGroupService, IScopedService
{
    private readonly IFieldGroupRepository _fieldGroupRepository;
    private readonly IDefineFieldRepository _defineFieldRepository;
    private readonly UserContext _userContext;
    private readonly ILogger<PropertyGroupService> _logger;
    
    // Default module ID - not used as query condition
    private const int DefaultModuleId = 0;

    public PropertyGroupService(
        IFieldGroupRepository fieldGroupRepository,
        IDefineFieldRepository defineFieldRepository,
        UserContext userContext,
        ILogger<PropertyGroupService> logger)
    {
        _fieldGroupRepository = fieldGroupRepository;
        _defineFieldRepository = defineFieldRepository;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<List<PropertyGroupDto>> GetGroupListAsync()
    {
        var groups = await _fieldGroupRepository.GetAllAsync();
        return groups.Select(MapToPropertyGroupDto).ToList();
    }

    public async Task<PropertyGroupDto?> GetGroupByIdAsync(long groupId)
    {
        var group = await _fieldGroupRepository.GetByIdAsync(groupId);
        return group == null ? null : MapToPropertyGroupDto(group);
    }

    public async Task<long> AddGroupAsync(PropertyGroupDto groupDto)
    {
        if (groupDto == null)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Group cannot be null");

        // Check group name uniqueness
        if (await _fieldGroupRepository.ExistsGroupNameAsync(groupDto.GroupName))
            throw new CRMException(ErrorCodeEnum.BusinessError, 
                $"Group name '{groupDto.GroupName}' already exists");

        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);

        var entity = new FieldGroup
        {
            ModuleId = DefaultModuleId,
            GroupName = groupDto.GroupName,
            Sort = groupDto.Sort,
            IsSystemDefine = groupDto.IsSystemDefine,
            IsDefault = groupDto.IsDefault,
            Fields = groupDto.FieldIds?.ToArray(),
            TenantId = _userContext.TenantId ?? "default",
            AppCode = _userContext.AppCode ?? "default",
            CreateDate = DateTimeOffset.UtcNow,
            CreateBy = _userContext.UserName ?? "SYSTEM",
            CreateUserId = userId,
            ModifyDate = DateTimeOffset.UtcNow,
            ModifyBy = _userContext.UserName ?? "SYSTEM",
            ModifyUserId = userId
        };

        return await _fieldGroupRepository.InsertReturnSnowflakeIdAsync(entity);
    }

    public async Task UpdateGroupAsync(PropertyGroupDto groupDto)
    {
        if (groupDto == null)
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Group cannot be null");

        var existing = await _fieldGroupRepository.GetByIdAsync(groupDto.Id);
        if (existing == null)
            throw new CRMException(ErrorCodeEnum.NotFound, "Group not found");

        // Check group name uniqueness (excluding current)
        if (await _fieldGroupRepository.ExistsGroupNameAsync(groupDto.GroupName, groupDto.Id))
            throw new CRMException(ErrorCodeEnum.BusinessError, 
                $"Group name '{groupDto.GroupName}' already exists");

        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);

        existing.GroupName = groupDto.GroupName;
        existing.Sort = groupDto.Sort;
        existing.IsDefault = groupDto.IsDefault;
        existing.Fields = groupDto.FieldIds?.ToArray();
        existing.ModifyDate = DateTimeOffset.UtcNow;
        existing.ModifyBy = _userContext.UserName ?? "SYSTEM";
        existing.ModifyUserId = userId;

        await _fieldGroupRepository.UpdateAsync(existing);
    }

    public async Task DeleteGroupAsync(long groupId)
    {
        var existing = await _fieldGroupRepository.GetByIdAsync(groupId);
        if (existing == null)
            throw new CRMException(ErrorCodeEnum.NotFound, "Group not found");

        if (existing.IsSystemDefine)
            throw new CRMException(ErrorCodeEnum.BusinessError, "System defined group cannot be deleted");

        if (existing.IsDefault)
            throw new CRMException(ErrorCodeEnum.BusinessError, "Default group cannot be deleted");

        // Parse UserId to long, default to 0 if parsing fails
        long.TryParse(_userContext.UserId, out var userId);

        existing.IsValid = false;
        existing.ModifyDate = DateTimeOffset.UtcNow;
        existing.ModifyBy = _userContext.UserName ?? "SYSTEM";
        existing.ModifyUserId = userId;

        await _fieldGroupRepository.UpdateAsync(existing);
    }

    public async Task UpdateGroupSortAsync(Dictionary<long, int> groupSorts)
    {
        if (groupSorts == null || !groupSorts.Any())
            return;

        await _fieldGroupRepository.BatchUpdateSortAsync(groupSorts);
    }

    public async Task<List<PropertyGroupDto>> GetGroupsWithFieldsAsync()
    {
        var groups = await _fieldGroupRepository.GetAllAsync();
        var allFields = await _defineFieldRepository.GetAllAsync();

        var result = new List<PropertyGroupDto>();
        foreach (var group in groups)
        {
            var dto = MapToPropertyGroupDto(group);
            if (group.Fields != null && group.Fields.Any())
            {
                dto.Fields = allFields
                    .Where(f => group.Fields.Contains(f.Id))
                    .Select(f => new DefineFieldDto
                    {
                        Id = f.Id,
                        ModuleId = f.ModuleId,
                        GroupId = group.Id,
                        DisplayName = f.DisplayName,
                        FieldName = f.FieldName,
                        Description = f.Description,
                        DataType = f.DataType,
                        IsSystemDefine = f.IsSystemDefine,
                        IsRequired = f.IsRequired,
                        IsHidden = f.IsHidden,
                        AllowEdit = f.AllowEdit,
                        Sort = f.Sort
                    })
                    .OrderBy(f => f.Sort)
                    .ToList();
            }
            result.Add(dto);
        }

        return result.OrderBy(g => g.Sort).ToList();
    }

    private static PropertyGroupDto MapToPropertyGroupDto(FieldGroup entity)
    {
        return new PropertyGroupDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            GroupName = entity.GroupName,
            Sort = entity.Sort,
            IsSystemDefine = entity.IsSystemDefine,
            IsDefault = entity.IsDefault,
            FieldIds = entity.Fields?.ToList() ?? new List<long>()
        };
    }
}
