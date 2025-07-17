using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Workflow version history repository implementation
    /// </summary>
    public class WorkflowVersionRepository : BaseRepository<WorkflowVersion>, IWorkflowVersionRepository, IScopedService
    {
        private readonly IStageVersionRepository _stageVersionRepository;
        private readonly UserContext _userContext;

        public WorkflowVersionRepository(ISqlSugarClient sqlSugarClient, IStageVersionRepository stageVersionRepository, UserContext userContext) : base(sqlSugarClient)
        {
            _stageVersionRepository = stageVersionRepository;
            _userContext = userContext;
        }

        /// <summary>
        /// Get version history by original workflow ID
        /// </summary>
        public async Task<List<WorkflowVersion>> GetVersionHistoryAsync(long originalWorkflowId)
        {
            return await db.Queryable<WorkflowVersion>()
                .Where(x => x.OriginalWorkflowId == originalWorkflowId && x.IsValid == true)
                .OrderBy(x => x.Version, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Create version history record
        /// </summary>
        public async Task<bool> CreateVersionHistoryAsync(Workflow workflow, string changeType, string changeReason = null)
        {
            var latestVersion = await GetLatestVersionAsync(workflow.Id);
            var newVersion = latestVersion + 1;

            var versionHistory = new WorkflowVersion
            {
                OriginalWorkflowId = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                IsDefault = workflow.IsDefault,
                Status = workflow.Status,
                StartDate = workflow.StartDate,
                EndDate = workflow.EndDate,
                Version = newVersion,
                IsActive = workflow.IsActive,
                ConfigJson = null,
                ChangeReason = changeReason ?? $"Workflow {changeType.ToLower()}",
                ChangeType = changeType
            };

            // Initialize entity with proper ID generation
            InitializeWorkflowVersion(versionHistory);

            return await InsertAsync(versionHistory);
        }

        /// <summary>
        /// Get latest version number
        /// </summary>
        public async Task<int> GetLatestVersionAsync(long originalWorkflowId)
        {
            var versions = await db.Queryable<WorkflowVersion>()
                .Where(x => x.OriginalWorkflowId == originalWorkflowId && x.IsValid == true)
                .Select(x => x.Version)
                .ToListAsync();

            return versions.Any() ? versions.Max() : 0;
        }

        /// <summary>
        /// Get history record by version number
        /// </summary>
        public async Task<WorkflowVersion> GetByVersionAsync(long originalWorkflowId, int version)
        {
            return await db.Queryable<WorkflowVersion>()
                .Where(x => x.OriginalWorkflowId == originalWorkflowId && x.Version == version && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Get version details by version ID
        /// </summary>
        public async Task<WorkflowVersion> GetVersionDetailAsync(long versionId)
        {
            return await db.Queryable<WorkflowVersion>()
                .Where(x => x.Id == versionId && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Create version history record with stage snapshots
        /// </summary>
        public async Task<long> CreateVersionHistoryWithStagesAsync(Workflow workflow, List<Stage> stages, string changeType, string changeReason = null)
        {
            var latestVersion = await GetLatestVersionAsync(workflow.Id);
            var newVersion = latestVersion + 1;

            var versionHistory = new WorkflowVersion
            {
                OriginalWorkflowId = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                IsDefault = workflow.IsDefault,
                Status = workflow.Status,
                StartDate = workflow.StartDate,
                EndDate = workflow.EndDate,
                Version = newVersion,
                IsActive = workflow.IsActive,
                ConfigJson = null,
                ChangeReason = changeReason ?? $"Workflow {changeType.ToLower()}",
                ChangeType = changeType
            };

            // Initialize entity with proper ID generation
            InitializeWorkflowVersion(versionHistory);

            // Insert version history record
            await InsertAsync(versionHistory);

            // Create stage snapshots
            if (stages?.Any() == true)
            {
                var stageVersions = stages.Select(stage => new StageVersion
                {
                    WorkflowVersionId = versionHistory.Id,
                    OriginalStageId = stage.Id,
                    Name = stage.Name,
                    Description = stage.Description,
                    DefaultAssignedGroup = stage.DefaultAssignedGroup,
                    DefaultAssignee = stage.DefaultAssignee,
                    EstimatedDuration = stage.EstimatedDuration,
                    OrderIndex = stage.Order,
                    ChecklistId = stage.ChecklistId,
                    QuestionnaireId = stage.QuestionnaireId,
                    Color = stage.Color,

                    WorkflowVersion = stage.WorkflowVersion,
                    IsActive = stage.IsActive
                }).ToList();

                await _stageVersionRepository.BatchInsertAsync(stageVersions);
            }

            return versionHistory.Id;
        }

        /// <summary>
        /// Initialize WorkflowVersion entity with proper ID and audit information
        /// </summary>
        private void InitializeWorkflowVersion(WorkflowVersion entity)
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
