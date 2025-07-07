namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum TaskTypeEnum
    {
        Unknown = 0,
        /// <summary>
        /// Due tasks - deadline until 24:00 today
        /// </summary>
        Due = 1,
        /// <summary>
        /// Future tasks - not including today
        /// </summary>
        Future = 2,
        All = 3,
    }
}
