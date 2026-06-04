using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    public class QuestionnaireDeletedEvent : INotification
    {
        public long QuestionnaireId { get; set; }
        public string QuestionnaireName { get; set; }

        public QuestionnaireDeletedEvent(long questionnaireId, string questionnaireName = null)
        {
            QuestionnaireId = questionnaireId;
            QuestionnaireName = questionnaireName;
        }
    }
}
