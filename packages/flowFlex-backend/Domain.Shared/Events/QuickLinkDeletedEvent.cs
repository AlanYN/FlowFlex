using MediatR;

namespace FlowFlex.Domain.Shared.Events
{
    public class QuickLinkDeletedEvent : INotification
    {
        public long QuickLinkId { get; set; }
        public string QuickLinkName { get; set; }

        public QuickLinkDeletedEvent(long quickLinkId, string quickLinkName = null)
        {
            QuickLinkId = quickLinkId;
            QuickLinkName = quickLinkName;
        }
    }
}
