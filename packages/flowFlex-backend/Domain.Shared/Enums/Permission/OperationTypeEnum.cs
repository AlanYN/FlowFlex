namespace FlowFlex.Domain.Shared.Enums.Permission
{
    /// <summary>
    /// Operation type enumeration
    /// Function: Define operation types that users can perform
    /// </summary>
    public enum OperationTypeEnum
    {
        /// <summary>
        /// Create operation
        /// </summary>
        Create = 1,

        /// <summary>
        /// View operation (read-only)
        /// </summary>
        View = 2,

        /// <summary>
        /// Operate permission (edit, complete, etc.)
        /// </summary>
        Operate = 3,

        /// <summary>
        /// Delete operation
        /// </summary>
        Delete = 4,

        /// <summary>
        /// Assign/Reassign operation
        /// </summary>
        Assign = 5
    }
}

