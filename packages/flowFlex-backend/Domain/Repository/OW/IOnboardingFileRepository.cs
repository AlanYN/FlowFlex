using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Onboarding file repository interface
    /// </summary>
    public interface IOnboardingFileRepository : IBaseRepository<OnboardingFile>
    {
        /// <summary>
        /// Get file list by OnboardingId
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>File list</returns>
        Task<List<OnboardingFile>> GetFilesByOnboardingAsync(long onboardingId, long? stageId = null);

        /// <summary>
        /// Get file list by StageId
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>File list</returns>
        Task<List<OnboardingFile>> GetFilesByStageAsync(long stageId);

        /// <summary>
        /// Get file list by category
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="category">File category</param>
        /// <returns>File list</returns>
        Task<List<OnboardingFile>> GetFilesByCategoryAsync(long onboardingId, string category);

        /// <summary>
        /// Get required file list
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Required file list</returns>
        Task<List<OnboardingFile>> GetRequiredFilesAsync(long onboardingId, long? stageId = null);

        /// <summary>
        /// Check if file exists
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="fileName">File name</param>
        /// <returns>Whether exists</returns>
        Task<bool> FileExistsAsync(long onboardingId, string fileName);

        /// <summary>
        /// Get file statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <returns>Statistics information</returns>
        Task<(int count, long totalSize)> GetFileStatisticsAsync(long onboardingId, long? stageId = null);

        /// <summary>
        /// Get OnboardingFile by AttachmentId
        /// </summary>
        /// <param name="attachmentId">Attachment ID</param>
        /// <returns>OnboardingFile entity</returns>
        Task<OnboardingFile?> GetByAttachmentIdAsync(long attachmentId);
    }
}
