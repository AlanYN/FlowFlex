using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Query and export operations
    /// Delegates to OnboardingQueryService for actual implementation
    /// </summary>
    public partial class OnboardingService
    {
        /// <summary>
        /// Query onboarding with pagination
        /// </summary>
        public Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request)
            => _queryService.QueryAsync(request);

        /// <summary>
        /// Get all active onboardings by System ID with pagination
        /// </summary>
        public Task<PagedResult<OnboardingOutputDto>> GetActiveBySystemIdAsync(
            string systemId, 
            string? entityId = null, 
            string sortField = "createDate", 
            string sortOrder = "desc", 
            int pageIndex = 1, 
            int pageSize = 20)
            => _queryService.GetActiveBySystemIdAsync(systemId, entityId, sortField, sortOrder, pageIndex, pageSize);

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        public Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query)
            => _queryService.ExportToExcelAsync(query);
    }
}
