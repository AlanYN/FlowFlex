using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Stage version snapshot repository implementation
    /// </summary>
    public class StageVersionRepository : BaseRepository<StageVersion>, IStageVersionRepository, IScopedService
    {
        private readonly UserContext _userContext;

        public StageVersionRepository(ISqlSugarClient sqlSugarClient, UserContext userContext) : base(sqlSugarClient)
        {
            _userContext = userContext;
        }

        /// <summary>
        /// Get stage snapshot list by workflow version ID
        /// </summary>
        public async Task<List<StageVersion>> GetByWorkflowVersionIdAsync(long workflowVersionId)
        {
            return await db.Queryable<StageVersion>()
                .Where(x => x.WorkflowVersionId == workflowVersionId && x.IsValid == true)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();
        }

        /// <summary>
        /// Batch create stage version snapshots
        /// </summary>
        public async Task<bool> BatchInsertAsync(List<StageVersion> stageVersions)
        {
            if (stageVersions == null || !stageVersions.Any())
                return true;

            try
            {
                // Initialize each StageVersion with proper ID generation
                foreach (var stageVersion in stageVersions)
                {
                    InitializeStageVersion(stageVersion);
                }

                await db.Insertable(stageVersions).ExecuteCommandAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get stage snapshot by original stage ID and workflow version ID
        /// </summary>
        public async Task<StageVersion> GetByOriginalStageIdAsync(long originalStageId, long workflowVersionId)
        {
            return await db.Queryable<StageVersion>()
                .Where(x => x.OriginalStageId == originalStageId && x.WorkflowVersionId == workflowVersionId && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Initialize StageVersion entity with proper ID and audit information
        /// </summary>
        private void InitializeStageVersion(StageVersion entity)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Generate timestamp-based ID
            entity.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            // Set timestamps
            entity.CreateDate = now;
            entity.ModifyDate = now;
            
            // Set user information
            entity.CreateBy = _userContext?.UserName ?? "SYSTEM";
            entity.ModifyBy = _userContext?.UserName ?? "SYSTEM";
            entity.CreateUserId = ParseToLong(_userContext?.UserId);
            entity.ModifyUserId = ParseToLong(_userContext?.UserId);
            
            // Set default values
            entity.IsValid = true;
            entity.TenantId = _userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Parse string to long, return 0 if null or invalid
        /// </summary>
        private static long ParseToLong(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
                
            if (long.TryParse(value, out long result))
                return result;
                
            return 0;
        }
    }
}
