using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Static field value repository interface
    /// </summary>
    public interface IStaticFieldValueRepository : IBaseRepository<StaticFieldValue>
    {
        /// <summary>
        /// Get static field value list by Onboarding ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Static field value list</returns>
        Task<List<StaticFieldValue>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get static field value list by Onboarding ID and Stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Static field value list</returns>
        Task<List<StaticFieldValue>> GetByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get static field value by Onboarding ID, Stage ID and field name
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Static field value</returns>
        Task<StaticFieldValue?> GetByOnboardingStageAndFieldAsync(long onboardingId, long stageId, string fieldName);

        /// <summary>
        /// Get latest version of static field values
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Latest version static field value list</returns>
        Task<List<StaticFieldValue>> GetLatestByOnboardingAndStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Batch save or update static field values
        /// </summary>
        /// <param name="staticFieldValues">Static field value list</param>
        /// <returns>Whether successful</returns>
        Task<bool> BatchSaveOrUpdateAsync(List<StaticFieldValue> staticFieldValues);

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
        /// Set old versions as not latest
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Whether successful</returns>
        Task<bool> SetOldVersionsAsNotLatestAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get static field value history versions
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>History version list</returns>
        Task<List<StaticFieldValue>> GetFieldHistoryAsync(long onboardingId, long stageId, string fieldName);
    }
} 
