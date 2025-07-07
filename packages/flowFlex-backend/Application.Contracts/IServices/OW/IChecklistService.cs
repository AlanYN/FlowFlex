using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Models;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Checklist service interface
/// </summary>
public interface IChecklistService : IScopedService
{
    /// <summary>
    /// Create a new checklist
    /// </summary>
    Task<long> CreateAsync(ChecklistInputDto input);

    /// <summary>
    /// Update an existing checklist
    /// </summary>
    Task<bool> UpdateAsync(long id, ChecklistInputDto input);

    /// <summary>
    /// Delete a checklist (with confirmation)
    /// </summary>
    Task<bool> DeleteAsync(long id, bool confirm = false);

    /// <summary>
    /// Get checklist by ID
    /// </summary>
    Task<ChecklistOutputDto> GetByIdAsync(long id);

    /// <summary>
    /// Get list of checklists by team
    /// </summary>
    Task<List<ChecklistOutputDto>> GetListAsync(string team = null);

    /// <summary>
    /// Query checklists (paged)
    /// </summary>
    Task<PagedResult<ChecklistOutputDto>> QueryAsync(ChecklistQueryRequest query);

    /// <summary>
    /// Duplicate a checklist
    /// </summary>
    Task<long> DuplicateAsync(long id, DuplicateChecklistInputDto input);

    /// <summary>
    /// Export checklist to PDF
    /// </summary>
    Task<Stream> ExportToPdfAsync(long id);

    /// <summary>
    /// Create checklist from template
    /// </summary>
    Task<long> CreateFromTemplateAsync(long templateId, ChecklistInputDto input);

    /// <summary>
    /// Calculate completion rate
    /// </summary>
    Task<decimal> CalculateCompletionAsync(long id);

    /// <summary>
    /// Get checklist templates
    /// </summary>
    Task<List<ChecklistOutputDto>> GetTemplatesAsync();

    /// <summary>
    /// Get checklist statistics by team
    /// </summary>
    Task<ChecklistStatisticsDto> GetStatisticsByTeamAsync(string team);

    /// <summary>
    /// Get checklists by stage ID
    /// </summary>
    Task<List<ChecklistOutputDto>> GetByStageIdAsync(long stageId);
}
