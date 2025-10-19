namespace FlowFlex.Domain.Shared.Enums.Permission
{
    /// <summary>
    /// Permission entity type enumeration
    /// Function: Define the types of entities that can be accessed
    /// </summary>
    public enum PermissionEntityTypeEnum
    {
        /// <summary>
        /// Workflow entity
        /// </summary>
        Workflow = 1,

        /// <summary>
        /// Stage entity
        /// </summary>
        Stage = 2,

        /// <summary>
        /// Case entity (Onboarding)
        /// </summary>
        Case = 3
    }
}

