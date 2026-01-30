using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for onboarding CRUD operations
    /// Handles: Create, Update, Delete, GetById
    /// </summary>
    public interface IOnboardingCrudService : IScopedService
    {
        /// <summary>
        /// Create a new onboarding
        /// </summary>
        /// <param name="input">Onboarding input data</param>
        /// <returns>Created onboarding ID</returns>
        Task<long> CreateAsync(OnboardingInputDto input);

        /// <summary>
        /// Update an existing onboarding
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="input">Updated onboarding data</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateAsync(long id, OnboardingInputDto input);

        /// <summary>
        /// Delete an onboarding (soft delete with confirmation)
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <param name="confirm">Confirmation flag</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteAsync(long id, bool confirm = false);

        /// <summary>
        /// Get onboarding by ID with full details
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>Onboarding output DTO or null if not found</returns>
        Task<OnboardingOutputDto?> GetByIdAsync(long id);
    }
}
