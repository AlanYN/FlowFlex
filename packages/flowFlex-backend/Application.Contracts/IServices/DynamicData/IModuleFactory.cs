namespace FlowFlex.Application.Contracts.IServices.DynamicData;

/// <summary>
/// Module factory interface - creates module providers
/// </summary>
public interface IModuleFactory
{
    /// <summary>
    /// Get module provider by module ID
    /// </summary>
    IModuleProvider GetProvider(int moduleId);

    /// <summary>
    /// Check if module provider exists
    /// </summary>
    bool HasProvider(int moduleId);
}
