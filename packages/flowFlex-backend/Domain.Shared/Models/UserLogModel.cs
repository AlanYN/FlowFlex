using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Log model
    /// </summary>
    public class UserLogModel
    {
        /// <summary>
        /// Type
        /// </summary>
        public UserEventRecordLogTypeEnum LogTypeEnum { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Remarks
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
        /// <summary>
        /// Login name
        /// </summary>
        public string LoginName { get; set; } = string.Empty;
    }
}
