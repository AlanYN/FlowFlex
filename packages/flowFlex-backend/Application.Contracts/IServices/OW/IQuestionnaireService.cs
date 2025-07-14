using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW;

/// <summary>
/// Questionnaire service interface
/// </summary>
public interface IQuestionnaireService : IScopedService
{
    /// <summary>
    /// Create a new questionnaire
    /// </summary>
    Task<long> CreateAsync(QuestionnaireInputDto input);

    /// <summary>
    /// Update an existing questionnaire
    /// </summary>
    Task<bool> UpdateAsync(long id, QuestionnaireInputDto input);

    /// <summary>
    /// Delete a questionnaire (with confirmation)
    /// </summary>
    Task<bool> DeleteAsync(long id, bool confirm = false);

    /// <summary>
    /// Get questionnaire by ID
    /// </summary>
    Task<QuestionnaireOutputDto> GetByIdAsync(long id);

    /// <summary>
    /// Get list of questionnaires by category
    /// </summary>
    Task<List<QuestionnaireOutputDto>> GetListAsync(string category = null);

    /// <summary>
    /// Get questionnaires by stage ID
    /// </summary>
    Task<List<QuestionnaireOutputDto>> GetByStageIdAsync(long stageId);

    /// <summary>
    /// Get questionnaires by multiple IDs (batch query)
    /// </summary>
    Task<List<QuestionnaireOutputDto>> GetByIdsAsync(List<long> ids);

    /// <summary>
    /// Query questionnaires (paged)
    /// </summary>
    Task<PagedResult<QuestionnaireOutputDto>> QueryAsync(QuestionnaireQueryRequest query);

    /// <summary>
    /// Duplicate a questionnaire
    /// </summary>
    Task<long> DuplicateAsync(long id, DuplicateQuestionnaireInputDto input);

    /// <summary>
    /// Preview questionnaire
    /// </summary>
    Task<QuestionnaireOutputDto> PreviewAsync(long id);

    /// <summary>
    /// Publish questionnaire
    /// </summary>
    Task<bool> PublishAsync(long id);

    /// <summary>
    /// Archive questionnaire
    /// </summary>
    Task<bool> ArchiveAsync(long id);

    /// <summary>
    /// Get questionnaire templates
    /// </summary>
    Task<List<QuestionnaireOutputDto>> GetTemplatesAsync();

    /// <summary>
    /// Create questionnaire from template
    /// </summary>
    Task<long> CreateFromTemplateAsync(long templateId, QuestionnaireInputDto input);

    /// <summary>
    /// Validate questionnaire structure
    /// </summary>
    Task<bool> ValidateStructureAsync(long id);

    /// <summary>
    /// Update questionnaire statistics
    /// </summary>
    Task<bool> UpdateStatisticsAsync(long id);

    /// <summary>
    /// Batch get questionnaires by stage IDs
    /// </summary>
    Task<BatchStageQuestionnaireResponse> GetByStageIdsBatchAsync(BatchStageQuestionnaireRequest request);
}
