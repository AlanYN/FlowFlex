using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Core operation log service interface - base for all specialized log services
    /// </summary>
    /// <typeparam name="TLogDto">Log DTO type</typeparam>
    public interface IOperationLogService<TLogDto> : IScopedService
        where TLogDto : class
    {
        /// <summary>
        /// Log a basic operation
        /// </summary>
        Task<bool> LogOperationAsync(
            OperationTypeEnum operationType,
            BusinessModuleEnum businessModule,
            long businessId,
            long? onboardingId,
            long? stageId,
            string operationTitle,
            string operationDescription,
            string operationSource = null,
            string beforeData = null,
            string afterData = null,
            string changedFields = null,
            string extendedData = null);

        /// <summary>
        /// Get operation logs with pagination
        /// </summary>
        Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsAsync(
            long? onboardingId = null,
            long? stageId = null,
            OperationTypeEnum? operationType = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Get operation statistics
        /// </summary>
        Task<Dictionary<string, int>> GetOperationStatisticsAsync(
            long? onboardingId = null,
            long? stageId = null);
    }

    /// <summary>
    /// Base operation log service interface for common operations
    /// </summary>
    public interface IBaseOperationLogService : IOperationLogService<OperationChangeLogOutputDto>
    {
        // Common operations that all services should support
    }
}