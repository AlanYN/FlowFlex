namespace FlowFlex.Application.Contracts.Dtos.Action
{
    public class ActionQuestionnaireAnswerDto
    {
        public string StageId { get; set; }
        public string QuestionnaireId { get; set; }
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public object Answer { get; set; }
    }
}
