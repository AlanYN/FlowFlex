using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Stage operation log service interface
    /// </summary>
    public interface IStageLogService : IBaseOperationLogService
    {
        // Stage lifecycle operations
        Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string extendedData = null);
        Task<bool> LogStageUpdateAsync(long stageId, string stageName, string beforeData, string afterData, List<string> changedFields, long? workflowId = null, string extendedData = null);
        Task<bool> LogStageDeleteAsync(long stageId, string stageName, long? workflowId = null, string reason = null, string extendedData = null);
        Task<bool> LogStageOrderChangeAsync(long stageId, string stageName, int oldOrder, int newOrder, long? workflowId = null, string extendedData = null);

        // Stage runtime operations
        Task<bool> LogStageCompleteAsync(long stageId, string stageName, long onboardingId, string completionNotes = null, string extendedData = null);
        Task<bool> LogStageReopenAsync(long stageId, string stageName, long onboardingId, string reason = null, string extendedData = null);

        // Action execution logging
        Task<bool> LogStageActionExecutionAsync(long stageId, string stageName, long? onboardingId, string actionName, string actionType, string executionResult, string executionDetails = null, string extendedData = null);

        // Portal permission logging
        Task<bool> LogStagePortalPermissionChangeAsync(long stageId, string stageName, bool beforeVisibleInPortal, bool afterVisibleInPortal, string beforePermission, string afterPermission, long? workflowId = null, string extendedData = null);

        // Component change logging
        Task<bool> LogStageComponentsChangeAsync(long stageId, string stageName, string beforeComponentsJson, string afterComponentsJson, long? workflowId = null, string extendedData = null);

        // Stage-specific queries
        Task<PagedResult<OperationChangeLogOutputDto>> GetStageLogsAsync(long stageId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20);
        Task<PagedResult<OperationChangeLogOutputDto>> GetStageComponentLogsAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);
        Task<PagedResult<OperationChangeLogOutputDto>> GetStageComponentLogsOptimizedAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);
        Task<Dictionary<string, int>> GetStageOperationStatisticsAsync(long stageId, long? onboardingId = null);
    }
}