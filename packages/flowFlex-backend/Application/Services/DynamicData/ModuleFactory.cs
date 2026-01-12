using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace FlowFlex.Application.Services.DynamicData;

/// <summary>
/// Module factory implementation
/// </summary>
public class ModuleFactory : IModuleFactory, IScopedService
{
    private readonly IServiceProvider _serviceProvider;

    public ModuleFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IModuleProvider GetProvider(int moduleId)
    {
        // Try to get keyed service first
        var provider = _serviceProvider.GetKeyedService<IModuleProvider>(moduleId);
        
        if (provider != null)
            return provider;

        // Fallback to default provider
        var defaultProvider = _serviceProvider.GetKeyedService<IModuleProvider>(0);
        if (defaultProvider != null)
            return defaultProvider;

        // Create default provider if no keyed service found
        return _serviceProvider.GetRequiredService<DefaultModuleProvider>();
    }

    public bool HasProvider(int moduleId)
    {
        var provider = _serviceProvider.GetKeyedService<IModuleProvider>(moduleId);
        return provider != null;
    }
}
