using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for onboarding query and export operations
    /// Extracted from OnboardingService to reduce complexity
    /// </summary>
    public interface IOnboardingQueryService : IScopedService
    {
        /// <summary>
        /// Query onboarding with pagination and filtering
        /// </summary>
        /// <param name="request">Query request with filters and pagination</param>
        /// <returns>Paged result of onboarding records</returns>
        Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request);

        /// <summary>
        /// Get all active onboardings by System ID with pagination
        /// </summary>
        /// <param name="systemId">External system identifier</param>
        /// <param name="entityId">External entity ID for filtering (optional)</param>
        /// <param name="sortField">Sort field: createDate, modifyDate, leadName, caseCode, status (default: createDate)</param>
        /// <param name="sortOrder">Sort order: asc, desc (default: desc)</param>
        /// <param name="pageIndex">Page index (from 1, default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <returns>Paged result of active onboarding records</returns>
        Task<PagedResult<OnboardingOutputDto>> GetActiveBySystemIdAsync(
            string systemId, 
            string? entityId = null, 
            string sortField = "createDate", 
            string sortOrder = "desc", 
            int pageIndex = 1, 
            int pageSize = 20);

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        /// <param name="query">Query request for filtering export data</param>
        /// <returns>Excel file stream</returns>
        Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query);

        /// <summary>
        /// Get onboarding progress information
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>Progress information including stages and completion status</returns>
        Task<OnboardingProgressDto> GetProgressAsync(long id);

        /// <summary>
        /// Populate onboarding output DTOs with related data
        /// </summary>
        /// <param name="results">List of OnboardingOutputDto to populate</param>
        /// <param name="entities">List of Onboarding entities with source data</param>
        Task PopulateOnboardingOutputDtoAsync(
            List<OnboardingOutputDto> results, 
            List<Domain.Entities.OW.Onboarding> entities);
    }
}
