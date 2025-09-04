using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Action-related operation log service interface
    /// </summary>
    public interface IActionLogService
    {
        /// <summary>
        /// Log action trigger mapping association operation
        /// </summary>
        Task<bool> LogActionMappingAssociateAsync(
            long mappingId,
            long actionDefinitionId,
            string actionName,
            string triggerType,
            long triggerSourceId,
            string triggerSourceName,
            string triggerEvent,
            long? workflowId = null,
            long? stageId = null,
            string extendedData = null);

        /// <summary>
        /// Log action trigger mapping disassociation operation
        /// </summary>
        Task<bool> LogActionMappingDisassociateAsync(
            long mappingId,
            long actionDefinitionId,
            string actionName,
            string triggerType,
            long triggerSourceId,
            string triggerSourceName,
            string triggerEvent,
            long? workflowId = null,
            long? stageId = null,
            bool wasEnabled = true,
            string extendedData = null);

        /// <summary>
        /// Log action trigger mapping update operation
        /// </summary>
        Task<bool> LogActionMappingUpdateAsync(
            long mappingId,
            long actionDefinitionId,
            string actionName,
            string triggerType,
            long triggerSourceId,
            string triggerSourceName,
            string changeDescription,
            string beforeData,
            string afterData,
            List<string> changedFields,
            string extendedData = null);

        /// <summary>
        /// Log action definition create operation
        /// </summary>
        Task<bool> LogActionDefinitionCreateAsync(
            long actionDefinitionId,
            string actionName,
            string actionType,
            string extendedData = null);

        /// <summary>
        /// Log action definition update operation
        /// </summary>
        Task<bool> LogActionDefinitionUpdateAsync(
            long actionDefinitionId,
            string actionName,
            string beforeData,
            string afterData,
            List<string> changedFields,
            string extendedData = null);

        /// <summary>
        /// Log action definition delete operation
        /// </summary>
        Task<bool> LogActionDefinitionDeleteAsync(
            long actionDefinitionId,
            string actionName,
            string reason = null,
            string extendedData = null);

        /// <summary>
        /// Get action logs by action definition ID
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetActionDefinitionLogsAsync(
            long actionDefinitionId,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get action mapping logs by mapping ID
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetActionMappingLogsAsync(
            long mappingId,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get action logs by trigger source
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetActionLogsByTriggerSourceAsync(
            string triggerType,
            long triggerSourceId,
            long? onboardingId = null,
            long? stageId = null,
            int pageIndex = 1,
            int pageSize = 20);
    }
}