using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// AI Prompt History Repository Implementation
    /// </summary>
    public class AIPromptHistoryRepository : BaseRepository<AIPromptHistory>, IAIPromptHistoryRepository
    {
        private readonly ILogger<AIPromptHistoryRepository> _logger;

        public AIPromptHistoryRepository(ISqlSugarClient db, ILogger<AIPromptHistoryRepository> logger) : base(db)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get prompt history by entity
        /// </summary>
        public async Task<List<AIPromptHistory>> GetByEntityAsync(string entityType, long entityId, string promptType = null, int limit = 50)
        {
            var query = db.Queryable<AIPromptHistory>()
                .Where(x => x.IsValid && x.EntityType == entityType && x.EntityId == entityId);

            if (!string.IsNullOrEmpty(promptType))
            {
                query = query.Where(x => x.PromptType == promptType);
            }

            return await query
                .OrderByDescending(x => x.CreateDate)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get prompt history by onboarding
        /// </summary>
        public async Task<List<AIPromptHistory>> GetByOnboardingAsync(long onboardingId, string promptType = null, int limit = 50)
        {
            var query = db.Queryable<AIPromptHistory>()
                .Where(x => x.IsValid && x.OnboardingId == onboardingId);

            if (!string.IsNullOrEmpty(promptType))
            {
                query = query.Where(x => x.PromptType == promptType);
            }

            return await query
                .OrderByDescending(x => x.CreateDate)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get prompt history by user
        /// </summary>
        public async Task<List<AIPromptHistory>> GetByUserAsync(long userId, string promptType = null, int limit = 50)
        {
            var query = db.Queryable<AIPromptHistory>()
                .Where(x => x.IsValid && x.UserId == userId);

            if (!string.IsNullOrEmpty(promptType))
            {
                query = query.Where(x => x.PromptType == promptType);
            }

            return await query
                .OrderByDescending(x => x.CreateDate)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get recent prompt history
        /// </summary>
        public async Task<List<AIPromptHistory>> GetRecentAsync(string promptType = null, int limit = 100)
        {
            var query = db.Queryable<AIPromptHistory>()
                .Where(x => x.IsValid);

            if (!string.IsNullOrEmpty(promptType))
            {
                query = query.Where(x => x.PromptType == promptType);
            }

            return await query
                .OrderByDescending(x => x.CreateDate)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Clean up old prompt history records
        /// </summary>
        public async Task<int> CleanupOldRecordsAsync(int daysToKeep = 30)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);

            return await db.Updateable<AIPromptHistory>()
                .SetColumns(x => x.IsValid == false)
                .SetColumns(x => x.ModifyDate == DateTimeOffset.UtcNow)
                .Where(x => x.IsValid && x.CreateDate < cutoffDate)
                .ExecuteCommandAsync();
        }
    }
}