using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire
{
    /// <summary>
    /// 批量查询Stage问卷响应DTO
    /// </summary>
    public class BatchStageQuestionnaireResponse
    {
        /// <summary>
        /// Stage ID到问卷列表的映射
        /// </summary>
        public Dictionary<long, List<QuestionnaireOutputDto>> StageQuestionnaires { get; set; } = new Dictionary<long, List<QuestionnaireOutputDto>>();
    }
}