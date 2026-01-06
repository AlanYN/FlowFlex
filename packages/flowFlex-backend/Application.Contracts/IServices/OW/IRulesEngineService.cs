using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// RulesEngine evaluation service interface
    /// </summary>
    public interface IRulesEngineService : IScopedService
    {
        /// <summary>
        /// Evaluate condition for a completed stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Condition evaluation result</returns>
        Task<ConditionEvaluationResult> EvaluateConditionAsync(long onboardingId, long stageId);

        /// <summary>
        /// Evaluate condition with transaction lock for concurrency control
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Condition evaluation result</returns>
        Task<ConditionEvaluationResult> EvaluateConditionWithLockAsync(long onboardingId, long stageId);

        /// <summary>
        /// Build input data object for RulesEngine evaluation
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Input data object</returns>
        Task<object> BuildInputDataAsync(long onboardingId, long stageId);
    }
}
