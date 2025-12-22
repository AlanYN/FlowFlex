using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// External integration service interface for CRM/ERP system integration
    /// </summary>
    public interface IExternalIntegrationService
    {
        /// <summary>
        /// Get entity type mappings by Integration System Name
        /// </summary>
        /// <param name="systemName">Integration System Name</param>
        /// <returns>Entity type mappings response</returns>
        Task<EntityTypeMappingResponse> GetEntityTypeMappingsBySystemNameAsync(string systemName);

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

        /// <summary>
        /// Get attachments by case ID (onboarding ID)
        /// </summary>
        /// <param name="caseId">Case ID (onboarding ID)</param>
        /// <returns>Attachments list response</returns>
        Task<GetAttachmentsFromExternalResponse> GetAttachmentsByCaseIdAsync(string caseId);

        /// <summary>
        /// Get outbound attachments by System ID
        /// </summary>
        /// <param name="systemId">System ID (unique identifier for entity mapping)</param>
        /// <returns>Attachments list response</returns>
        Task<GetAttachmentsFromExternalResponse> GetOutboundAttachmentsBySystemIdAsync(string systemId);

        /// <summary>
        /// Get inbound attachments by System ID
        /// Retrieves attachment list from all onboardings associated with the System ID
        /// </summary>
        /// <param name="systemId">System ID (unique identifier for entity mapping)</param>
        /// <returns>Attachments list response</returns>
        Task<GetAttachmentsFromExternalResponse> GetInboundAttachmentsBySystemIdAsync(string systemId);

        /// <summary>
        /// Fetch inbound attachments from external system by System ID
        /// 1. Get IntegrationId from EntityMapping by SystemId
        /// 2. Get InboundAttachment configuration from Integration
        /// 3. Execute HTTP Action to fetch attachments from external system
        /// 4. Parse and return the attachment list
        /// </summary>
        /// <param name="systemId">System ID (unique identifier for entity mapping)</param>
        /// <returns>Attachments list response from external system</returns>
        Task<GetAttachmentsFromExternalResponse> FetchInboundAttachmentsFromExternalAsync(string systemId);
    }
}

