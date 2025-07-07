using System;
using FlowFlex.Domain.Shared.Enums.Item;
using FlowFlex.Domain.Shared.Enums.Unis;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Join table query view model
    /// </summary>
    public class RelationshipMappingViewModel
    {
        /// <summary>
        /// Primary key ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Source identifier
        /// </summary>
        public long SourceId { get; set; }

        /// <summary>
        /// Source type
        /// </summary>
        public RelationalTypeEnum SourceType { get; set; }

        /// <summary>
        /// Target identifier
        /// </summary>
        public long TargetId { get; set; }

        /// <summary>
        /// Target type
        /// </summary>
        public RelationalTypeEnum TargetType { get; set; }

        /// <summary>
        /// Source code
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// Target code
        /// </summary>
        public string TargetCode { get; set; }

        /// <summary>
        /// Source name
        /// </summary>
        public string SourceName { get; set; }
        /// <summary>
        /// Target name
        /// </summary>
        public string TargetName { get; set; }



        public bool IsValid { get; set; }

        /// <summary>
        /// Creator of the record
        /// </summary>
        public virtual string CreateBy { get; set; }

        /// <summary>
        /// Last modifier of the record
        /// </summary>
        public virtual string ModifyBy { get; set; }

        /// <summary>
        /// ID of the creator
        /// </summary>
        public virtual long CreateUserId { get; set; }

        /// <summary>
        /// ID of the last modifier
        /// </summary>
        public virtual long ModifyUserId { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Last modification time
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

    }

    /// <summary>
    /// Join table query dictionary model
    /// </summary>
    public class RelationshipMappingDicModel
    {
        /// <summary>
        /// Source identifier
        /// </summary>
        public long SourceId { get; set; }

        /// <summary>
        /// Source code
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// Source name
        /// </summary>
        public string SourceName { get; set; }
    }
}
