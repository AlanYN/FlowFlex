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
        /// Get operation logs by business module and business ID
        /// </summary>
        /// <param name="businessModule">Business module</param>
        /// <param name="businessId">Business ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Operation log paged list</returns>
        Task<PagedResult<OperationChangeLogOutputDto>> GetLogsByBusinessAsync(string businessModule, long businessId, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Get operation statistics information
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Operation statistics information</returns>
        Task<Dictionary<string, int>> GetOperationStatisticsAsync(long? onboardingId = null, long? stageId = null);
    }
}
