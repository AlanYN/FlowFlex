using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.SqlSugarDB;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Repository.OW;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Onboarding file repository implementation
    /// </summary>
    public class OnboardingFileRepository : BaseRepository<OnboardingFile>, IOnboardingFileRepository, IScopedService
    {
        public OnboardingFileRepository(ISqlSugarClient db) : base(db)
        {
        }

        /// <summary>
        /// Get file list by OnboardingId
        /// </summary>
        public async Task<List<OnboardingFile>> GetFilesByOnboardingAsync(long onboardingId, long? stageId = null)
        {
            ISugarQueryable<OnboardingFile> query = db.Queryable<OnboardingFile>()
                .Where(f => f.OnboardingId == onboardingId && f.IsValid == true);

            if (stageId.HasValue)
            {
                query = query.Where(f => f.StageId == stageId.Value);
            }

            return await query
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get file list by StageId
        /// </summary>
        public async Task<List<OnboardingFile>> GetFilesByStageAsync(long stageId)
        {
            return await db.Queryable<OnboardingFile>()
                .Where(f => f.StageId == stageId && f.IsValid == true)
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get file list by category
        /// </summary>
        public async Task<List<OnboardingFile>> GetFilesByCategoryAsync(long onboardingId, string category)
        {
            return await db.Queryable<OnboardingFile>()
                .Where(f => f.OnboardingId == onboardingId && f.Category == category && f.IsValid == true)
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get required files list
        /// </summary>
        public async Task<List<OnboardingFile>> GetRequiredFilesAsync(long onboardingId, long? stageId = null)
        {
            ISugarQueryable<OnboardingFile> query = db.Queryable<OnboardingFile>()
                .Where(f => f.OnboardingId == onboardingId && f.IsRequired == true && f.IsValid == true);

            if (stageId.HasValue)
            {
                query = query.Where(f => f.StageId == stageId.Value);
            }

            return await query
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Check if file exists
        /// </summary>
        public async Task<bool> FileExistsAsync(long onboardingId, string fileName)
        {
            return await db.Queryable<OnboardingFile>()
                .Where(f => f.OnboardingId == onboardingId && f.OriginalFileName == fileName && f.IsValid == true)
                .AnyAsync();
        }

        /// <summary>
        /// Get file statistics
        /// </summary>
        public async Task<(int count, long totalSize)> GetFileStatisticsAsync(long onboardingId, long? stageId = null)
        {
            ISugarQueryable<OnboardingFile> query = db.Queryable<OnboardingFile>()
                .Where(f => f.OnboardingId == onboardingId && f.IsValid == true);

            if (stageId.HasValue)
            {
                query = query.Where(f => f.StageId == stageId.Value);
            }

            var files = await query.ToListAsync();
            var count = files.Count;
            var totalSize = files.Sum(f => f.FileSize);

            return (count, totalSize);
        }

        /// <summary>
        /// Get OnboardingFile by AttachmentId
        /// </summary>
        public async Task<OnboardingFile?> GetByAttachmentIdAsync(long attachmentId)
        {
            return await db.Queryable<OnboardingFile>()
                .Where(f => f.AttachmentId == attachmentId && f.IsValid == true)
                .FirstAsync();
        }
    }
}
