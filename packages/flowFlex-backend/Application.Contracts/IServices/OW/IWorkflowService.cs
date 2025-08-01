using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Domain.Shared.Models;

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
        Task<Stream> ExportDetailedToExcelAsync(long workflowId);
        Task<Stream> ExportMultipleDetailedToExcelAsync(WorkflowExportSearch search);

        /// <summary>
        /// Create workflow from version with stages
        /// </summary>
        /// <param name="input">Create from version input</param>
        /// <returns>New workflow ID</returns>
        Task<long> CreateFromVersionAsync(CreateWorkflowFromVersionInputDto input);

        /// <summary>
        /// Manually create a new workflow version
        /// </summary>
        /// <param name="id">Workflow ID</param>
        /// <param name="changeReason">Optional reason for creating the version</param>
        /// <returns>Success result</returns>
        Task<bool> CreateNewVersionAsync(long id, string changeReason = null);

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
