using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.Models;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Workflow service interface
    /// </summary>
    public interface IWorkflowService
    {
        Task<long> CreateAsync(WorkflowInputDto input);
        Task<bool> UpdateAsync(long id, WorkflowInputDto input);
        Task<bool> DeleteAsync(long id, bool confirm = false);
        Task<WorkflowOutputDto> GetByIdAsync(long id);
        Task<List<WorkflowOutputDto>> GetListAsync();
        Task<List<WorkflowOutputDto>> GetAllAsync();
        Task<PagedResult<WorkflowOutputDto>> QueryAsync(WorkflowQueryRequest query);
        Task<bool> ActivateAsync(long id);
        Task<bool> DeactivateAsync(long id);
        Task<bool> SetDefaultAsync(long id);
        Task<bool> RemoveDefaultAsync(long id);
        Task<long> DuplicateAsync(long id, DuplicateWorkflowInputDto input);
        Task<List<WorkflowVersionDto>> GetVersionHistoryAsync(long id);
        Task<Stream> ExportDetailedToExcelAsync(long workflowId);
        Task<Stream> ExportMultipleDetailedToExcelAsync(WorkflowExportSearch search);

        /// <summary>
        /// Get stages by workflow version id
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>Stage list</returns>
        Task<List<StageOutputDto>> GetStagesByVersionIdAsync(long workflowId, long versionId);

        /// <summary>
        /// Get workflow version detail with stages
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>Version detail with stages</returns>
        Task<WorkflowVersionDetailDto> GetVersionDetailAsync(long workflowId, long versionId);

        /// <summary>
        /// Create workflow from version with stages
        /// </summary>
        /// <param name="input">Create from version input</param>
        /// <returns>New workflow ID</returns>
        Task<long> CreateFromVersionAsync(CreateWorkflowFromVersionInputDto input);

        /// <summary>
        /// Process expired workflows, set them to inactive
        /// </summary>
        Task<int> ProcessExpiredWorkflowsAsync();

        /// <summary>
        /// Get workflows that are about to expire (advance reminder in days)
        /// </summary>
        Task<List<WorkflowOutputDto>> GetExpiringWorkflowsAsync(int daysAhead = 7);
    }

    /// <summary>
    /// File export result
    /// </summary>
    public class FileExportResult
    {
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
