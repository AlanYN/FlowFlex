using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// InternalNote repository interface
    /// </summary>
    public interface IInternalNoteRepository : IBaseRepository<InternalNote>
    {
        /// <summary>
        /// Get notes by onboarding ID
        /// </summary>
        Task<List<InternalNote>> GetByOnboardingIdAsync(long onboardingId);

        /// <summary>
        /// Get notes by onboarding and stage
        /// </summary>
        Task<List<InternalNote>> GetByOnboardingAndStageAsync(long onboardingId, long? stageId);

        /// <summary>
        /// Get unresolved notes
        /// </summary>
        Task<List<InternalNote>> GetUnresolvedNotesAsync(long onboardingId);

        /// <summary>
        /// Get notes by priority
        /// </summary>
        Task<List<InternalNote>> GetByPriorityAsync(string priority);

        /// <summary>
        /// Get notes by type
        /// </summary>
        Task<List<InternalNote>> GetByTypeAsync(string noteType);

        /// <summary>
        /// Mark note as resolved
        /// </summary>
        Task<bool> MarkAsResolvedAsync(long id, string resolvedBy);

        /// <summary>
        /// Mark note as unresolved
        /// </summary>
        Task<bool> MarkAsUnresolvedAsync(long id);

        /// <summary>
        /// Get notes with pagination
        /// </summary>
        Task<(List<InternalNote> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            long? onboardingId = null,
            long? stageId = null,
            string noteType = null,
            string priority = null,
            bool? isResolved = null,
            string sortField = "CreateDate",
            string sortDirection = "desc");

        /// <summary>
        /// Get notes by note type
        /// </summary>
        /// <param name="noteType">Note type</param>
        /// <param name="days">Recent days</param>
        /// <returns>Note list</returns>
        Task<List<InternalNote>> GetByNoteTypeAsync(string noteType, int days = 30);

        /// <summary>
        /// Get notes by visibility
        /// </summary>
        /// <param name="visibility">Visibility</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Note list</returns>
        Task<List<InternalNote>> GetByVisibilityAsync(string visibility, long onboardingId);

        /// <summary>
        /// Search notes by tags
        /// </summary>
        /// <param name="tags">Tag list</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <returns>Note list</returns>
        Task<List<InternalNote>> SearchByTagsAsync(List<string> tags, long? onboardingId = null);

        /// <summary>
        /// Search notes by keyword
        /// </summary>
        /// <param name="keyword">Keyword</param>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <returns>Note list</returns>
        Task<List<InternalNote>> SearchByKeywordAsync(string keyword, long? onboardingId = null);

        /// <summary>
        /// Batch resolve notes
        /// </summary>
        /// <param name="noteIds">Note ID list</param>
        /// <param name="resolvedById">Resolver ID</param>
        /// <param name="resolutionNotes">Resolution notes</param>
        /// <returns>Update result</returns>
        Task<bool> BatchResolveAsync(List<long> noteIds, long resolvedById, string resolutionNotes = "");

        /// <summary>
        /// Get note statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID (optional)</param>
        /// <param name="days">Statistics days</param>
        /// <returns>Statistics information</returns>
        Task<Dictionary<string, object>> GetNoteStatisticsAsync(long? onboardingId = null, int days = 30);

        /// <summary>
        /// Get notes that mention specified user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="days">Recent days</param>
        /// <returns>Note list</returns>
        Task<List<InternalNote>> GetMentionedNotesAsync(long userId, int days = 30);
    }
}
