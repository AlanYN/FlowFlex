namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// System action template DTO
    /// </summary>
    public class SystemActionTemplateDto
    {
        /// <summary>
        /// Action name
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Configuration template JSON
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// Parameter definitions
        /// </summary>
        public List<SystemActionParameterDto> Parameters { get; set; } = new List<SystemActionParameterDto>();
    }
}