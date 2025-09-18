namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// System action parameter DTO
    /// </summary>
    public class SystemActionParameterDto
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parameter type (string, number, boolean, array, object)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Whether this parameter is required
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Parameter description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Default value (optional)
        /// </summary>
        public object? DefaultValue { get; set; }
    }
}