using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Text.Json;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Questionnaire repository implementation
    /// </summary>
    public class QuestionnaireRepository : BaseRepository<Questionnaire>, IQuestionnaireRepository, IScopedService
    {
        public QuestionnaireRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
        {
        }

        /// <summary>
        /// Get questionnaire list by category
        /// </summary>
        public async Task<List<Questionnaire>> GetByCategoryAsync(string category)
        {
            var query = db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && x.IsActive == true);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            return await query
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get template questionnaires
        /// </summary>
        public async Task<List<Questionnaire>> GetTemplatesAsync()
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.IsTemplate == true && x.IsValid == true && x.IsActive == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get questionnaire instances by template
        /// </summary>
        public async Task<List<Questionnaire>> GetByTemplateIdAsync(long templateId)
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.TemplateId == templateId && x.IsValid == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Check if name exists
        /// </summary>
        public async Task<bool> IsNameExistsAsync(string name, string category = null, long? excludeId = null)
        {
            var query = db.Queryable<Questionnaire>()
                .Where(x => x.Name == name && x.IsValid == true);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get questionnaires with pagination and filters
        /// </summary>
        public async Task<(List<Questionnaire> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string name = null,
            long? workflowId = null,
            long? stageId = null,
            bool? isTemplate = null,
            bool? isActive = null,
            string sortField = "CreateDate",
            string sortDirection = "desc")
        {
            // Build query condition list
            var whereExpressions = new List<Expression<Func<Questionnaire, bool>>>();

            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid == true);

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereExpressions.Add(x => x.Name.ToLower().Contains(name.ToLower()));
            }

            if (workflowId.HasValue)
            {
                whereExpressions.Add(x => x.WorkflowId == workflowId.Value);
            }

            if (stageId.HasValue)
            {
                whereExpressions.Add(x => x.StageId == stageId.Value);
            }

            if (isTemplate.HasValue)
            {
                whereExpressions.Add(x => x.IsTemplate == isTemplate.Value);
            }

            if (isActive.HasValue)
            {
                whereExpressions.Add(x => x.IsActive == isActive.Value);
            }

            // Determine sort field and direction
            Expression<Func<Questionnaire, object>> orderByExpression = sortField?.ToLower() switch
            {
                "name" => x => x.Name,
                "workflowid" => x => x.WorkflowId,
                "stageid" => x => x.StageId,
                _ => x => x.CreateDate
            };

            bool isAsc = sortDirection?.ToLower() == "asc";

            // Use BaseRepository's safe pagination method
            var (items, totalCount) = await GetPageListAsync(
                whereExpressions,
                pageIndex,
                pageSize,
                orderByExpression: orderByExpression,
                isAsc: isAsc
            );

            return (items, totalCount);
        }

        /// <summary>
        /// Get questionnaire statistics by category
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatisticsByCategoryAsync(string category)
        {
            var query = db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            var totalCount = await query.CountAsync();
            var activeCount = await query.Where(x => x.IsActive == true).CountAsync();
            var templateCount = await query.Where(x => x.IsTemplate == true).CountAsync();
            var publishedCount = await query.Where(x => x.Status == "Published").CountAsync();
            var draftCount = await query.Where(x => x.Status == "Draft").CountAsync();

            return new Dictionary<string, object>
            {
                { "TotalCount", totalCount },
                { "ActiveCount", activeCount },
                { "TemplateCount", templateCount },
                { "PublishedCount", publishedCount },
                { "DraftCount", draftCount }
            };
        }

        /// <summary>
        /// Update questionnaire statistics
        /// </summary>
        public async Task<bool> UpdateStatisticsAsync(long id, int totalQuestions, int requiredQuestions)
        {
            var result = await db.Updateable<Questionnaire>()
                .SetColumns(x => new Questionnaire
                {
                    TotalQuestions = totalQuestions,
                    RequiredQuestions = requiredQuestions,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == id && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get published questionnaires
        /// </summary>
        public async Task<List<Questionnaire>> GetPublishedAsync()
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.Status == "Published" && x.IsValid == true && x.IsActive == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Validate structure JSON
        /// </summary>
        public async Task<bool> ValidateStructureAsync(string structureJson)
        {
            if (string.IsNullOrWhiteSpace(structureJson))
            {
                return true; // Empty structure is valid
            }

            try
            {
                // Basic JSON validation
                JsonDocument.Parse(structureJson);

                // Additional structure validation can be implemented here
                // Future enhancement: validate required fields, question types, etc.

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Get questionnaires by multiple stage IDs
        /// </summary>
        public async Task<List<Questionnaire>> GetByStageIdsAsync(List<long> stageIds)
        {
            if (stageIds == null || !stageIds.Any())
            {
                return new List<Questionnaire>();
            }

            return await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && x.IsActive == true && stageIds.Contains(x.StageId ?? 0))
                .OrderBy(x => x.StageId, SqlSugar.OrderByType.Asc)
                .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// Check if workflow and stage association already exists
        /// </summary>
        public async Task<bool> IsWorkflowStageAssociationExistsAsync(long? workflowId, long? stageId, long? excludeId = null)
        {
            // If both workflowId and stageId are empty, no need to check
            if (!workflowId.HasValue && !stageId.HasValue)
            {
                return false;
            }

            var query = db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true);

            // Check WorkflowId and StageId combination
            if (workflowId.HasValue && stageId.HasValue)
            {
                // Both have values, check complete match
                query = query.Where(x => x.WorkflowId == workflowId.Value && x.StageId == stageId.Value);
            }
            else if (workflowId.HasValue)
            {
                // Only WorkflowId, check if questionnaire already exists in same Workflow
                query = query.Where(x => x.WorkflowId == workflowId.Value);
            }
            else if (stageId.HasValue)
            {
                // Only StageId, check if questionnaire already exists in same Stage
                query = query.Where(x => x.StageId == stageId.Value);
            }

            // Exclude current record (for update scenario)
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get existing questionnaire with same workflow and stage association
        /// </summary>
        public async Task<Questionnaire> GetByWorkflowStageAssociationAsync(long? workflowId, long? stageId, long? excludeId = null)
        {
            // If both workflowId and stageId are empty, return null
            if (!workflowId.HasValue && !stageId.HasValue)
            {
                return null;
            }

            var query = db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true);

            // Check WorkflowId and StageId combination
            if (workflowId.HasValue && stageId.HasValue)
            {
                // Both have values, check complete match
                query = query.Where(x => x.WorkflowId == workflowId.Value && x.StageId == stageId.Value);
            }
            else if (workflowId.HasValue)
            {
                // Only WorkflowId, check if questionnaire already exists in same Workflow
                query = query.Where(x => x.WorkflowId == workflowId.Value);
            }
            else if (stageId.HasValue)
            {
                // Only StageId, check if questionnaire already exists in same Stage
                query = query.Where(x => x.StageId == stageId.Value);
            }

            // Exclude current record (for update scenario)
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.FirstAsync();
        }

        /// <summary>
        /// Get questionnaires by workflow ID
        /// </summary>
        public async Task<List<Questionnaire>> GetByWorkflowIdAsync(long workflowId)
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get questionnaires by stage ID
        /// </summary>
        public async Task<List<Questionnaire>> GetByStageIdAsync(long stageId)
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.StageId == stageId && x.IsValid == true)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get questionnaires by status
        /// </summary>
        public async Task<List<Questionnaire>> GetByStatusAsync(string status)
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.Status == status && x.IsValid == true)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Update questionnaire statistics
        /// </summary>
        public async Task<bool> UpdateQuestionnaireStatsAsync(long questionnaireId, int totalQuestions, int requiredQuestions)
        {
            var result = await db.Updateable<Questionnaire>()
                .SetColumns(x => new Questionnaire
                {
                    TotalQuestions = totalQuestions,
                    RequiredQuestions = requiredQuestions,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == questionnaireId && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get questionnaires by names
        /// </summary>
        public async Task<List<Questionnaire>> GetByNamesAsync(List<string> names)
        {
            if (names == null || !names.Any())
            {
                return new List<Questionnaire>();
            }

            return await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && names.Contains(x.Name))
                .OrderBy(x => x.Name)
                .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
                .ToListAsync();
        }
    }
}
