namespace FlowFlex.Domain.Shared.Models.Attachment
{
    public class CustomerDocumentPageRequestModel : QueryPageModel
    {
        /// <summary>
        /// Sort order, ascending: A-Z sort; descending: Z-A sort (true: ascending, false: descending)
        /// </summary>
        public bool? IsAsc { get; set; }
        /// <summary>
        /// Sort field
        /// </summary>
        public string OrderBy { get; set; }
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
    }
}
