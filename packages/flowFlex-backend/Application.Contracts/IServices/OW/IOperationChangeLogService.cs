using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;

using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Operation Change Log Service Interface
    /// </summary>
    public interface IOperationChangeLogService : IScopedService
    {
        /// <summary>
        /// Log Checklist task completion operation
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="taskName">Task name</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="completionNotes">Completion notes</param>
        /// <param name="actualHours">Actual completion hours</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistTaskCompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string completionNotes = null, int actualHours = 0);

        /// <summary>
        /// Log Checklist task uncomplete operation
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="taskName">Task name</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="reason">Cancellation reason</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistTaskUncompleteAsync(long taskId, string taskName, long onboardingId, long? stageId, string reason = null);

        /// <summary>
        /// Log questionnaire answer submission operation
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="isUpdate">Is update operation</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogQuestionnaireAnswerSubmitAsync(long answerId, long onboardingId, long stageId, long? questionnaireId, string beforeData = null, string afterData = null, bool isUpdate = false);

        /// <summary>
        /// Log static field value change operation
        /// </summary>
        /// <param name="fieldValueId">Field value ID</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="fieldLabel">Field label (for display)</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogStaticFieldValueChangeAsync(long fieldValueId, string fieldName, long onboardingId, long stageId, string beforeData, string afterData, List<string> changedFields, string fieldLabel = null);

        /// <summary>
        /// Log file upload operation
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="fileName">File name</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fileSize">File size</param>
        /// <param name="contentType">File type</param>
        /// <param name="category">File category</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogFileUploadAsync(long fileId, string fileName, long onboardingId, long? stageId, long fileSize, string contentType, string category);

        /// <summary>
        /// Log file deletion operation
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="fileName">File name</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="reason">Deletion reason</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogFileDeleteAsync(long fileId, string fileName, long onboardingId, long? stageId, string reason = null);

        /// <summary>
        /// Log file update operation
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="fileName">File name</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogFileUpdateAsync(long fileId, string fileName, long onboardingId, long? stageId, string beforeData, string afterData, List<string> changedFields);

        /// <summary>
        /// Generic log recording method
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessId">Business ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="operationTitle">Operation title</param>
        /// <param name="operationDescription">Operation description</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="extendedData">Extended data</param>
        /// <param name="operationStatus">Operation status</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            long? onboardingId,
            long? stageId,
            string operationTitle,
            string operationDescription,
            string beforeData = null,
            string afterData = null,
            List<string> changedFields = null,
            string extendedData = null,
            OperationStatusEnum operationStatus = OperationStatusEnum.Success,
            string errorMessage = null);

        /// <summary>
        /// Get operation log list
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="operationType">Operation type (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeActionExecutions">Whether to include action executions</param>
        /// <returns>Operation log list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsAsync(long? onboardingId = null, long? stageId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);

        /// <summary>
        /// Get operation log list for task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="operationType">Operation type (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeActionExecutions">Whether to include action executions</param>
        /// <returns>Operation log list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByTaskAsync(long taskId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);

        /// <summary>
        /// Get operation log list for question
        /// </summary>
        /// <param name="questionId">Question ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="operationType">Operation type (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeActionExecutions">Whether to include action executions</param>
        /// <returns>Operation log list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByQuestionAsync(long questionId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);

        /// <summary>
        /// Get operation log list for stage components (all related tasks and questions)
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="operationType">Operation type (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeActionExecutions">Whether to include action executions</param>
        /// <returns>Operation log list including all tasks and questions from stage components</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByStageComponentsAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);

        /// <summary>
        /// Get operation log list for stage components (OPTIMIZED VERSION with database-level pagination)
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="operationType">Operation type (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeActionExecutions">Whether to include action executions</param>
        /// <returns>Operation log list including all tasks and questions from stage components (optimized)</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsByStageComponentsOptimizedAsync(long stageId, long? onboardingId = null, OperationTypeEnum? operationType = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);

        /// <summary>
        /// Get operation logs by business module and business ID
        /// </summary>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessId">Business ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Operation log paged list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessAsync(string businessModule, long businessId, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Get operation logs by business ID (without specifying business module)
        /// </summary>
        /// <param name="businessId">Business ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Operation log paged list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdAsync(long businessId, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Get operation logs by business ID with optional business type and related data
        /// </summary>
        /// <param name="businessId">Business ID</param>
        /// <param name="businessType">Business type (optional)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Operation log paged list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdWithTypeAsync(long businessId, BusinessTypeEnum? businessType = null, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Get operation logs by multiple business IDs (batch query)
        /// </summary>
        /// <param name="businessIds">List of business IDs</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Operation log paged list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessIdsAsync(List<long> businessIds, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Get operation statistics information
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Operation statistics information</returns>
        Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null);

        #region Workflow Operations (Independent of Onboarding)

        /// <summary>
        /// Log workflow create operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="workflowDescription">Workflow description</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowCreateAsync(long workflowId, string workflowName, string workflowDescription = null, string extendedData = null);

        /// <summary>
        /// Log workflow update operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowUpdateAsync(long workflowId, string workflowName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);

        /// <summary>
        /// Log workflow delete operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="reason">Deletion reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowDeleteAsync(long workflowId, string workflowName, string reason = null, string extendedData = null);

        /// <summary>
        /// Log workflow publish operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="version">Published version</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowPublishAsync(long workflowId, string workflowName, string version = null, string extendedData = null);

        /// <summary>
        /// Log workflow unpublish operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="reason">Unpublish reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowUnpublishAsync(long workflowId, string workflowName, string reason = null, string extendedData = null);

        /// <summary>
        /// Log workflow activate operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowActivateAsync(long workflowId, string workflowName, string extendedData = null);

        /// <summary>
        /// Log workflow deactivate operation
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="workflowName">Workflow name</param>
        /// <param name="reason">Deactivation reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogWorkflowDeactivateAsync(long workflowId, string workflowName, string reason = null, string extendedData = null);

        #endregion

        #region Stage Operations (Independent of Onboarding)

        /// <summary>
        /// Log stage create operation
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="stageName">Stage name</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogStageCreateAsync(long stageId, string stageName, long? workflowId = null, string extendedData = null);

        /// <summary>
        /// Log stage update operation
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="stageName">Stage name</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogStageUpdateAsync(long stageId, string stageName, string beforeData, string afterData, List<string> changedFields, long? workflowId = null, string extendedData = null);

        /// <summary>
        /// Log stage delete operation
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="stageName">Stage name</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="reason">Deletion reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogStageDeleteAsync(long stageId, string stageName, long? workflowId = null, string reason = null, string extendedData = null);

        /// <summary>
        /// Log stage order change operation
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="stageName">Stage name</param>
        /// <param name="oldOrder">Old order</param>
        /// <param name="newOrder">New order</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogStageOrderChangeAsync(long stageId, string stageName, int oldOrder, int newOrder, long? workflowId = null, string extendedData = null);

        #endregion

        #region Checklist Operations (Independent of Onboarding)

        /// <summary>
        /// Log checklist create operation
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="checklistName">Checklist name</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistCreateAsync(long checklistId, string checklistName, string extendedData = null);

        /// <summary>
        /// Log checklist update operation
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="checklistName">Checklist name</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistUpdateAsync(long checklistId, string checklistName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);

        /// <summary>
        /// Log checklist delete operation
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="checklistName">Checklist name</param>
        /// <param name="reason">Deletion reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistDeleteAsync(long checklistId, string checklistName, string reason = null, string extendedData = null);

        /// <summary>
        /// Log checklist task create operation
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="taskName">Task name</param>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistTaskCreateAsync(long taskId, string taskName, long checklistId, string extendedData = null);

        /// <summary>
        /// Log checklist task update operation
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="taskName">Task name</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistTaskUpdateAsync(long taskId, string taskName, string beforeData, string afterData, List<string> changedFields, long checklistId, string extendedData = null);

        /// <summary>
        /// Log checklist task delete operation
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="taskName">Task name</param>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="reason">Deletion reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogChecklistTaskDeleteAsync(long taskId, string taskName, long checklistId, string reason = null, string extendedData = null);

        #endregion

        #region Questionnaire Operations (Independent of Onboarding)

        /// <summary>
        /// Log questionnaire create operation
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="questionnaireName">Questionnaire name</param>
        /// <param name="afterData">After data (questionnaire JSON)</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogQuestionnaireCreateAsync(long questionnaireId, string questionnaireName, string afterData = null, string extendedData = null);

        /// <summary>
        /// Log questionnaire update operation
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="questionnaireName">Questionnaire name</param>
        /// <param name="beforeData">Before change data</param>
        /// <param name="afterData">After change data</param>
        /// <param name="changedFields">Changed field list</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogQuestionnaireUpdateAsync(long questionnaireId, string questionnaireName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);

        /// <summary>
        /// Log questionnaire delete operation
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="questionnaireName">Questionnaire name</param>
        /// <param name="reason">Deletion reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogQuestionnaireDeleteAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null);

        /// <summary>
        /// Log questionnaire publish operation
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="questionnaireName">Questionnaire name</param>
        /// <param name="version">Published version</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogQuestionnairePublishAsync(long questionnaireId, string questionnaireName, string version = null, string extendedData = null);

        /// <summary>
        /// Log questionnaire unpublish operation
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="questionnaireName">Questionnaire name</param>
        /// <param name="reason">Unpublish reason</param>
        /// <param name="extendedData">Extended data</param>
        /// <returns>Whether successful</returns>
        Task<bool> LogQuestionnaireUnpublishAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null);

        #endregion
    }
}
