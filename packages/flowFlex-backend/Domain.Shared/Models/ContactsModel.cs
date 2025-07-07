namespace FlowFlex.Domain.Shared.Models
{
    public class ContactsModel : QueryPageModel
    {
        /// <summary>
        /// Customer ID
        /// </summary>
        public long CustomerId { get; set; }
        /// <summary>
        /// Sort order, ascending: A-Z sort; descending: Z-A sort (true: ascending, false: descending)
        /// </summary>
        public bool? OrderBySort { get; set; }
        /// <summary>
        /// Sort field
        /// </summary>
        public string? OrderByName { get; set; }
    }
}
