using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.IServices
{
    /// <summary>
    /// Email template rendering service contract
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Render an email template with the given variables.
        /// </summary>
        /// <param name="templateName">Template name without path, e.g. "welcome_en"</param>
        /// <param name="variables">Template variables</param>
        /// <returns>Rendered HTML string</returns>
        string Render(string templateName, IDictionary<string, object> variables);
    }
}