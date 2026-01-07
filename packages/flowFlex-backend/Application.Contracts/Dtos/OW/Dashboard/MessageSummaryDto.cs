using System.Collections.Generic;
using FlowFlex.Application.Contracts.Dtos.OW.Message;

namespace FlowFlex.Application.Contracts.Dtos.OW.Dashboard
{
    /// <summary>
    /// Message summary for dashboard - uses same format as /api/ow/messages/v1
    /// </summary>
    public class MessageSummaryDto
    {
        /// <summary>
        /// Recent messages list (same format as MessageListItemDto)
        /// </summary>
        public List<MessageListItemDto> Messages { get; set; } = new List<MessageListItemDto>();

        /// <summary>
        /// Total unread message count
        /// </summary>
        public int UnreadCount { get; set; }
    }
}
