using System;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    /// <summary>
    /// Base class for data messages in the CRM system
    /// </summary>
    public class DataMessageBase
    {
        /// <summary>
        /// Unique identifier for the data message
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Indicates whether the data message is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Name of the user who created the data message
        /// </summary>
        public virtual string CreateBy { get; set; }

        /// <summary>
        /// Name of the user who last modified the data message
        /// </summary>
        public virtual string ModifyBy { get; set; }

        /// <summary>
        /// User ID of the creator
        /// </summary>
        public virtual long CreateUserId { get; set; }

        /// <summary>
        /// User ID of the last modifier
        /// </summary>
        public virtual long ModifyUserId { get; set; }

        /// <summary>
        /// Date and time when the data message was created
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Date and time when the data message was last modified
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }
    }
}
