using Newtonsoft.Json;
using System;
using Item.Common.Lib.JsonConverts;

namespace FlowFlex.Domain.Shared.Models.Relation
{
    public class RelationNotesDetailModel
    {
        /// <summary>
        /// Task primary key ID
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Binding relationship primary key ID for unbinding operation
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long NoteId { get; set; }

        /// <summary>
        /// Create time
        /// </summary>
        public DateTimeOffset? CreateDate { get; set; }
    }
}
