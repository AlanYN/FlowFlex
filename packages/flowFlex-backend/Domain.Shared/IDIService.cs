namespace FlowFlex.Domain.Shared
{
    /// <summary>
    /// Represents that a new service instance is created for each request, and different instances are used for each request
    /// </summary>
    public interface ITransientService
    {
    }

    /// <summary>
    /// Represents that a new service instance is created for each request or operation, and the same instance is used throughout the entire request or operation
    /// </summary>
    public interface IScopedService
    {
    }

    /// <summary>
    /// Represents that only one service instance is created throughout the entire application lifecycle, and all requests use the same instance
    /// </summary>
    public interface ISingletonService
    {
    }
}
