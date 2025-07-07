
using System;

namespace FlowFlex.Domain.Shared.Models
{
    public class NotificationUserModel
    {
        public long NotificationModuelId { get; set; }

        public long UserId { get; set; }

        public virtual DateTimeOffset CreateDate { get; set; }
    }
}
