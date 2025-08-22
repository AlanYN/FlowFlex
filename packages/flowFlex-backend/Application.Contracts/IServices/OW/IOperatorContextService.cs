using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Operator context accessor for resolving current operator information
    /// </summary>
    public interface IOperatorContextService
    {
        /// <summary>
        /// Get current operator display name (email preferred, then username, then headers/claims, fallback to System)
        /// </summary>
        string GetOperatorDisplayName();

        /// <summary>
        /// Get current operator id from context, headers, or claims (0 if unknown)
        /// </summary>
        long GetOperatorId();
    }
}

