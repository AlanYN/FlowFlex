using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer
{
    /// <summary>
    /// 批量查询Stage问卷答案请求DTO
    /// </summary>
    public class BatchStageAnswerRequest
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID列表
        /// </summary>
        
        public List<long> StageIds { get; set; } = new List<long>();
    }
}