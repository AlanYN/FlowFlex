using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Repository.DynamicData;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.DynamicData;

/// <summary>
/// Default module provider implementation
/// </summary>
public class DefaultModuleProvider : IModuleProvider, IScopedService
{
    private readonly IBusinessDataRepository _businessDataRepository;
    private readonly IDefineFieldRepository _defineFieldRepository;
    private readonly IPropertyService _propertyService;
    private readonly ILogger<DefaultModuleProvider> _logger;

    protected int _moduleId = 0;

    public virtual int ModuleId => _moduleId;

    public DefaultModuleProvider(
        IBusinessDataRepository businessDataRepository,
        IDefineFieldRepository defineFieldRepository,
        IPropertyService propertyService,
        ILogger<DefaultModuleProvider> logger)
    {
        _businessDataRepository = businessDataRepository;
        _defineFieldRepository = defineFieldRepository;
        _propertyService = propertyService;
        _logger = logger;
    }

    public virtual void SetModuleId(int moduleId)
    {
        _moduleId = moduleId;
    }

    public virtual async Task<DynamicDataObject?> GetBusinessDataAsync(long id)
    {
        return await _businessDataRepository.GetBusinessDataObjectAsync(id);
    }

    public virtual async Task<List<DynamicDataObject>> GetBusinessDataListAsync(List<long> ids)
    {
        if (ids == null || !ids.Any())
            return new List<DynamicDataObject>();

        return await _businessDataRepository.GetBusinessDataObjectListAsync(ids);
    }

    public virtual async Task<long> CreateBusinessDataAsync(DynamicDataObject dynamicDataObject)
    {
        // Execute before create hook
        dynamicDataObject = await BeforeCreateAsync(dynamicDataObject);

        var businessId = await _businessDataRepository.CreateBusinessDataAsync(dynamicDataObject);

        // Execute after create hook
        await AfterCreateAsync(businessId, dynamicDataObject);

        return businessId;
    }

    public virtual async Task UpdateBusinessDataAsync(DynamicDataObject data)
    {
        // Execute before update hook
        data = await BeforeUpdateAsync(data);

        await _businessDataRepository.UpdateBusinessDataAsync(data);

        // Execute after update hook
        await AfterUpdateAsync(data);
    }

    public virtual async Task DeleteBusinessDataAsync(long businessId)
    {
        // Execute before delete hook
        await BeforeDeleteAsync(businessId);

        await _businessDataRepository.DeleteBusinessDataAsync(businessId);

        // Execute after delete hook
        await AfterDeleteAsync(businessId);
    }

    public virtual async Task<List<DefineFieldDto>> GetPropertyListAsync()
    {
        return await _propertyService.GetPropertyListAsync();
    }

    public virtual async Task<long> AddPropertyAsync(DefineFieldDto defineFieldDto)
    {
        return await _propertyService.AddPropertyAsync(defineFieldDto);
    }

    public virtual async Task UpdatePropertyAsync(DefineFieldDto defineFieldDto)
    {
        await _propertyService.UpdatePropertyAsync(defineFieldDto);
    }

    public virtual async Task DeletePropertyAsync(long propertyId)
    {
        await _propertyService.DeletePropertyAsync(propertyId);
    }

    #region Hooks - Override in derived classes for custom logic

    public virtual Task<DynamicDataObject> BeforeCreateAsync(DynamicDataObject data)
    {
        return Task.FromResult(data);
    }

    public virtual Task AfterCreateAsync(long businessId, DynamicDataObject data)
    {
        return Task.CompletedTask;
    }

    public virtual Task<DynamicDataObject> BeforeUpdateAsync(DynamicDataObject data)
    {
        return Task.FromResult(data);
    }

    public virtual Task AfterUpdateAsync(DynamicDataObject data)
    {
        return Task.CompletedTask;
    }

    public virtual Task BeforeDeleteAsync(long businessId)
    {
        return Task.CompletedTask;
    }

    public virtual Task AfterDeleteAsync(long businessId)
    {
        return Task.CompletedTask;
    }

    #endregion
}
