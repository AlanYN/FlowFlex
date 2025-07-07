
using System;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models
{
    public class NotificationModuleModel
    {
        public string Id { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Checked
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// NotificationModuleType
        /// </summary>
        public NotificationModuleTypeEnum NotificationModuleType { get; set; }

        public DateTimeOffset CreateDate { get; set; }
    }
}
