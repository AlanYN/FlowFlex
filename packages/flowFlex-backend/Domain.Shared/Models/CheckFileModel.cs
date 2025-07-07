
namespace FlowFlex.Domain.Shared.Models
{
    public class CheckFileModel
    {
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
        /// <summary>
        /// User selected type
        /// </summary>
        public string NetTerm { get; set; }
        /// <summary>
        /// Whether to validate file upload
        /// </summary>
        public bool? PaperWorkExempted { get; set; }
        /// <summary>
        /// File type
        /// </summary>
        public string FileType { get; set; }
    }
}
