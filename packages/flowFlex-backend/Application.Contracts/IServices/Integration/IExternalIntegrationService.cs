using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// External integration service interface for CRM/ERP system integration
    /// </summary>
    public interface IExternalIntegrationService
    {
        /// <summary>
        /// Get workflows available for a specific entity mapping by System ID
        /// </summary>
        /// <param name="systemId">System ID (unique identifier for entity mapping)</param>
        /// <returns>List of available workflows</returns>
        Task<List<WorkflowInfoDto>> GetWorkflowsBySystemIdAsync(string systemId);

        /// <summary>
        /// Create a case/onboarding from external system
        /// </summary>
        /// <param name="request">Create case request</param>
        /// <returns>Created case information</returns>
        Task<CreateCaseFromExternalResponse> CreateCaseAsync(CreateCaseFromExternalRequest request);

        /// <summary>
        /// Get case information by parameters (demo endpoint)
        /// </summary>
        /// <param name="request">Case info request parameters</param>
        /// <returns>Case information response</returns>
        Task<CaseInfoResponse> GetCaseInfoAsync(CaseInfoRequest request);
    }
}

