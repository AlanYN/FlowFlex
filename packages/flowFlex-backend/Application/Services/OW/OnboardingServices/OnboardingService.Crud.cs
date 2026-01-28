using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - CRUD operations (delegates to OnboardingCrudService)
    /// </summary>
    public partial class OnboardingService
    {
        /// <summary>
        /// Create a new onboarding
        /// </summary>
        public Task<long> CreateAsync(OnboardingInputDto input)
            => _crudService.CreateAsync(input);

        /// <summary>
        /// Update an existing onboarding
        /// </summary>
        public Task<bool> UpdateAsync(long id, OnboardingInputDto input)
            => _crudService.UpdateAsync(id, input);

        /// <summary>
        /// Delete an onboarding (soft delete with confirmation)
        /// </summary>
        public Task<bool> DeleteAsync(long id, bool confirm = false)
            => _crudService.DeleteAsync(id, confirm);

        /// <summary>
        /// Get onboarding by ID with full details
        /// </summary>
        public Task<OnboardingOutputDto?> GetByIdAsync(long id)
            => _crudService.GetByIdAsync(id);
    }
}
