using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stage Condition service interface
    /// </summary>
    public interface IStageConditionService : IScopedService
    {
        /// <summary>
        /// Create a new stage condition
        /// </summary>
        Task<long> CreateAsync(StageConditionInputDto input);

        /// <summary>
        /// Update an existing stage condition
        /// </summary>
        Task<bool> UpdateAsync(long id, StageConditionInputDto input);

        /// <summary>
        /// Delete a stage condition
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get stage condition by ID
        /// </summary>
        Task<StageConditionOutputDto?> GetByIdAsync(long id);

        /// <summary>
        /// Get stage condition by Stage ID
        /// </summary>
        Task<StageConditionOutputDto?> GetByStageIdAsync(long stageId);

        /// <summary>
        /// Get all conditions for a workflow
        /// </summary>
        Task<List<StageConditionOutputDto>> GetByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Validate a stage condition configuration
        /// </summary>
        Task<ConditionValidationResult> ValidateAsync(long id);

        /// <summary>
        /// Validate RulesJson format
        /// </summary>
        Task<ConditionValidationResult> ValidateRulesJsonAsync(string rulesJson);
    }
}
