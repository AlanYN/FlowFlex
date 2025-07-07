using Newtonsoft.Json;
using System;
using Item.Common.Lib.JsonConverts;
using FlowFlex.Domain.Shared.Enums.Item;

namespace FlowFlex.Domain.Shared.Models
{
    public class ActivityDetailModel
    {
        /// <summary>
        /// Primary key ID for deletion
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset? Time { get; set; }

        /// <summary>
        /// Associated ID
        /// </summary>
        public string TargetId { get; set; }

        /// <summary>
        /// Associated type
        /// </summary>
        public RelationalTypeEnum? TargetType { get; set; }
    }
}
