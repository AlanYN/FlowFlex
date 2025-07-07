using System;
using FlowFlex.Domain.Shared.Enums.Item;

namespace FlowFlex.Domain.Shared.Models
{
    public class RelationalModel
    {
        /// <summary>
        /// Primary key ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Associated source ID
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// Associated type
        /// </summary>
        public RelationalTypeEnum TargetType { get; set; }

        public RelationalTypeEnum SourceType { get; set; }

        /// <summary>
        /// Associated ID
        /// </summary>
        public string TargetId { get; set; }

        /// <summary>
        /// CreatedDate
        /// </summary>
        public DateTimeOffset CreatedDate { get; set; }
    }

    public class TaskRelationalModel : RelationalModel
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
    }
}
