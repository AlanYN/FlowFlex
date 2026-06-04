using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    public class ChecklistDeletedEvent : INotification
    {
        public long ChecklistId { get; set; }
        public string ChecklistName { get; set; }

        public ChecklistDeletedEvent(long checklistId, string checklistName = null)
        {
            ChecklistId = checklistId;
            ChecklistName = checklistName;
        }
    }
}
