using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer
{
    /// <summary>
    /// 批量查询Stage问卷答案响应DTO
    /// </summary>
    public class BatchStageAnswerResponse
    {
        /// <summary>
        /// Stage ID到问卷答案的映射
        /// </summary>
        public Dictionary<long, object> StageAnswers { get; set; } = new Dictionary<long, object>();
    }
}