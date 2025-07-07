using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Static Field Value Service Interface
    /// </summary>
    public interface IStaticFieldValueService
    {
        /// <summary>
        /// Save static field value
        /// </summary>
        /// <param name="input">Static field value input DTO</param>
        /// <returns>Saved ID</returns>
        Task<long> SaveAsync(StaticFieldValueInputDto input);

        /// <summary>
        /// Batch save static field values
        /// </summary>
        /// <param name="input">Batch save input DTO</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchSaveAsync(BatchStaticFieldValueInputDto input);

        /// <summary>
        /// Get static field value by ID
        /// </summary>
        /// <param name="id">Static field value ID</param>
        /// <returns>Static field value output DTO</returns>
        Task<StaticFieldValueOutputDto?> GetByIdAsync(long id);

        /// <summary>
        /// Get static field value list by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Static field value list</returns>
        Task<List<StaticFieldValueOutputDto>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get static field value list by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Static field value list</returns>
        Task<List<StaticFieldValueOutputDto>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get static field value by Onboarding ID, Stage ID and field name
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Static field value</returns>
        Task<StaticFieldValueOutputDto?> GetByOnboardingStageAndFieldAsync(long onboardingId, long stageId, string fieldName);

        /// <summary>
        /// Get latest version static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Latest version static field value list</returns>
        Task<List<StaticFieldValueOutputDto>> GetLatestByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Delete static field value
        /// </summary>
        /// <param name="id">Static field value ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Delete static field values by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Delete static field values by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Validate static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Validation result dictionary</returns>
        Task<Dictionary<string, string>> ValidateFieldValuesAsync(long onboardingId, long stageId);

        /// <summary>
        /// Submit static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> SubmitFieldValuesAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get static field value history versions
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>History version list</returns>
        Task<List<StaticFieldValueOutputDto>> GetFieldHistoryAsync(long onboardingId, long stageId, string fieldName);

        /// <summary>
        /// Copy static field values to other Onboarding
        /// </summary>
        /// <param name="sourceOnboardingId">Source Onboarding ID</param>
        /// <param name="targetOnboardingIds">Target Onboarding ID list</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldNames">Field name list (optional)</param>
        /// <returns>Whether successful</returns>
        Task<bool> CopyFieldValuesAsync(long sourceOnboardingId, List<long> targetOnboardingIds, long stageId, List<string>? fieldNames = null);
    }
}
