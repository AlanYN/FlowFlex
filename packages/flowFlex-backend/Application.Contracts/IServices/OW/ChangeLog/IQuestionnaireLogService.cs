using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Questionnaire operation log service interface
    /// </summary>
    public interface IQuestionnaireLogService : IBaseOperationLogService
    {
        // Questionnaire lifecycle operations
        Task<bool> LogQuestionnaireCreateAsync(long questionnaireId, string questionnaireName, string extendedData = null);
        Task<bool> LogQuestionnaireUpdateAsync(long questionnaireId, string questionnaireName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);
        Task<bool> LogQuestionnaireDeleteAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null);
        Task<bool> LogQuestionnairePublishAsync(long questionnaireId, string questionnaireName, string version = null, string extendedData = null);
        Task<bool> LogQuestionnaireUnpublishAsync(long questionnaireId, string questionnaireName, string reason = null, string extendedData = null);

        // Questionnaire answer operations
        Task<bool> LogQuestionnaireAnswerSubmitAsync(long answerId, long onboardingId, long stageId, long? questionnaireId, string beforeData = null, string afterData = null, bool isUpdate = false);

        // Static field operations
        Task<bool> LogStaticFieldValueChangeAsync(long fieldValueId, string fieldName, long onboardingId, long stageId, string beforeData, string afterData, List<string> changedFields, string fieldType = null);

        // File operations
        Task<bool> LogFileUploadAsync(long fileId, string fileName, long onboardingId, long? stageId, long fileSize, string contentType, string category);
        Task<bool> LogFileDeleteAsync(long fileId, string fileName, long onboardingId, long? stageId, string reason = null);
        Task<bool> LogFileUpdateAsync(long fileId, string fileName, long onboardingId, long? stageId, string beforeData, string afterData, List<string> changedFields);

        // Questionnaire-specific queries
        Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionnaireLogsAsync(long questionnaireId, int pageIndex = 1, int pageSize = 20);
        Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionLogsAsync(long questionId, long? onboardingId = null, int pageIndex = 1, int pageSize = 20, bool includeActionExecutions = true);
        Task<PagedResult<OperationChangeLogOutputDto>> GetQuestionnaireAnswerLogsAsync(long answerId, long onboardingId, int pageIndex = 1, int pageSize = 20);
        Task<Dictionary<string, int>> GetQuestionnaireOperationStatisticsAsync(long questionnaireId);
    }
}