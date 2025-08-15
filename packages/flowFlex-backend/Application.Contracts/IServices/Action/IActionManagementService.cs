using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// Service for managing action definitions and trigger mappings
    /// </summary>
    public interface IActionManagementService : IScopedService
    {
        #region Action Definition Management

        /// <summary>
        /// Get action definition by ID
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <returns>Action definition DTO</returns>
        Task<ActionDefinitionDto> GetActionDefinitionAsync(long id);

        /// <summary>
        /// Get action definitions
        /// </summary>
        /// <param name="search"></param>
        /// <param name="actionType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="isAssignmentStage"></param>
        /// <param name="isAssignmentChecklist"></param>
        /// <param name="isAssignmentQuestionnaire"></param>
        /// <param name="isAssignmentWorkflow"></param>
        /// <returns></returns>
        Task<PageModelDto<ActionDefinitionDto>> GetPagedActionDefinitionsAsync(string? search,
            ActionTypeEnum? actionType,
            int pageIndex,
            int pageSize,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null);

        /// <summary>
        /// Get enabled action definitions
        /// </summary>
        /// <returns>List of enabled action definition DTOs</returns>
        Task<List<ActionDefinitionDto>> GetEnabledActionDefinitionsAsync();

        /// <summary>
        /// Create new action definition
        /// </summary>
        /// <param name="dto">Create action definition DTO</param>
        /// <returns>Created action definition DTO</returns>
        Task<ActionDefinitionDto> CreateActionDefinitionAsync(CreateActionDefinitionDto dto);

        /// <summary>
        /// Update action definition
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <param name="dto">Update action definition DTO</param>
        /// <returns>Updated action definition DTO</returns>
        Task<ActionDefinitionDto> UpdateActionDefinitionAsync(long id, UpdateActionDefinitionDto dto);

        /// <summary>
        /// Delete action definition
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteActionDefinitionAsync(long id);

        /// <summary>
        /// Enable or disable action definition
        /// </summary>
        /// <param name="id">Action definition ID</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateActionDefinitionStatusAsync(long id, bool isEnabled);

        /// <summary>
        /// Export action definition
        /// </summary>
        /// <param name="search"></param>
        /// <param name="actionType"></param>
        /// <param name="isAssignmentStage"></param>
        /// <param name="isAssignmentChecklist"></param>
        /// <param name="isAssignmentQuestionnaire"></param>
        /// <param name="isAssignmentWorkflow"></param>
        /// <returns></returns>
        Task<Stream> ExportAsync(string? search,
            ActionTypeEnum? actionType,
            bool? isAssignmentStage = null,
            bool? isAssignmentChecklist = null,
            bool? isAssignmentQuestionnaire = null,
            bool? isAssignmentWorkflow = null);

        #endregion

        #region Action Trigger Mapping Management

        /// <summary>
        /// Get action trigger mapping by ID
        /// </summary>
        /// <param name="id">Mapping ID</param>
        /// <returns>Action trigger mapping DTO</returns>
        Task<ActionTriggerMappingDto> GetActionTriggerMappingAsync(long id);

        /// <summary>
        /// Get all action trigger mappings
        /// </summary>
        /// <returns>List of action trigger mapping DTOs</returns>
        Task<List<ActionTriggerMappingDto>> GetAllActionTriggerMappingsAsync();

        /// <summary>
        /// Get action trigger mappings by action definition ID
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <returns>List of action trigger mapping DTOs</returns>
        Task<List<ActionTriggerMappingInfo>> GetActionTriggerMappingsByActionIdAsync(long actionDefinitionId);

        /// <summary>
        /// Get action trigger mappings by trigger type
        /// </summary>
        /// <param name="triggerType">Trigger type</param>
        /// <returns>List of action trigger mapping DTOs</returns>
        Task<List<ActionTriggerMappingDto>> GetActionTriggerMappingsByTriggerTypeAsync(string triggerType);

        /// <summary>
        /// Get action trigger mappings by trigger source id
        /// </summary>
        /// <param name="triggerSourceId"></param>
        /// <returns></returns>
        Task<List<ActionTriggerMappingDto>> GetActionTriggerMappingsByTriggerSourceIdAsync(long triggerSourceId);

        /// <summary>
        /// Create new action trigger mapping
        /// </summary>
        /// <param name="dto">Create action trigger mapping DTO</param>
        /// <returns>Created action trigger mapping DTO</returns>
        Task<ActionTriggerMappingDto> CreateActionTriggerMappingAsync(CreateActionTriggerMappingDto dto);

        /// <summary>
        /// Delete action trigger mapping
        /// </summary>
        /// <param name="id">Mapping ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteActionTriggerMappingAsync(long id);

        /// <summary>
        /// Enable or disable action trigger mapping
        /// </summary>
        /// <param name="id">Mapping ID</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateActionTriggerMappingStatusAsync(long id, bool isEnabled);

        /// <summary>
        /// Batch enable or disable action trigger mappings
        /// </summary>
        /// <param name="mappingIds">Mapping ID list</param>
        /// <param name="isEnabled">Enable or disable</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchUpdateActionTriggerMappingStatusAsync(List<long> mappingIds, bool isEnabled);

        #endregion
    }
}