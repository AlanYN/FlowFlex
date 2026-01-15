namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Static field configuration for stage component
    /// </summary>
    public class StaticFieldConfig
    {
        /// <summary>
        /// Static field ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Whether the field is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Display order of the field
        /// </summary>
        public int Order { get; set; }
    }
}
