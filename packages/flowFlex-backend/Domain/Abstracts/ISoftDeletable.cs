namespace FlowFlex.Domain.Abstracts
{
    /// <summary>
    /// Interface for entities that support soft delete
    /// </summary>
    public interface ISoftDeletable
    {
        /// <summary>
        /// Whether the entity is valid (not soft deleted)
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        /// Modification date for soft delete tracking
        /// </summary>
        DateTimeOffset ModifyDate { get; set; }
    }
}