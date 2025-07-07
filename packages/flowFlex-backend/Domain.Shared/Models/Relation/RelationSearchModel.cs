namespace FlowFlex.Domain.Shared.Models.Relation
{
    /// <summary>
    /// Relation search, applicable for Company Contract Deal mutual association queries
    /// </summary>
    public class RelationSearchModel : QueryPageModel
    {
        public long RelationId { get; set; }
    }
}
