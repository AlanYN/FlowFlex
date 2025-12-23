using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.ChangeLog
{
    /// <summary>
    /// Onboarding operation log service interface
    /// </summary>
    public interface IOnboardingLogService : IBaseOperationLogService
    {
        // Onboarding lifecycle operations
        Task<bool> LogOnboardingCreateAsync(long onboardingId, string onboardingName, string afterData = null, string extendedData = null);
        Task<bool> LogOnboardingUpdateAsync(long onboardingId, string onboardingName, string beforeData, string afterData, List<string> changedFields, string extendedData = null);
        Task<bool> LogOnboardingDeleteAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);

        // Onboarding status operations
        Task<bool> LogOnboardingStartAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);
        Task<bool> LogOnboardingPauseAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);
        Task<bool> LogOnboardingResumeAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);
        Task<bool> LogOnboardingAbortAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);
        Task<bool> LogOnboardingReactivateAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);
        Task<bool> LogOnboardingForceCompleteAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null);

        // Onboarding-specific queries
        Task<PagedResult<OperationChangeLogOutputDto>> GetOnboardingLogsAsync(long onboardingId, int pageIndex = 1, int pageSize = 20);
        Task<Dictionary<string, int>> GetOnboardingOperationStatisticsAsync(long onboardingId);
    }
}


