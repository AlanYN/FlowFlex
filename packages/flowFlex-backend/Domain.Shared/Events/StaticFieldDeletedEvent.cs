using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    public class StaticFieldDeletedEvent : INotification
    {
        public long FieldId { get; set; }

        public StaticFieldDeletedEvent(long fieldId)
        {
            FieldId = fieldId;
        }
    }
}
