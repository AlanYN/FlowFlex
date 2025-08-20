using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Email template rendering service using embedded resources with simple placeholder replacement.
    /// Placeholders format: {{VariableName}}
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService, IScopedService
    {
        private readonly ILogger<EmailTemplateService> _logger;

        // Templates are embedded under the Application assembly as resources
        // We will resolve by suffix to be resilient to root namespace differences
        private const string ResourceSuffixPrefix = ".Templates.Email.";

        public EmailTemplateService(ILogger<EmailTemplateService> logger)
        {
            _logger = logger;
        }

        public string Render(string templateName, IDictionary<string, object> variables)
        {
            var html = LoadTemplate(templateName);
            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Email template not found: {Template}", templateName);
                return string.Empty;
            }

            if (variables == null || variables.Count == 0)
            {
                return html;
            }

            try
            {
                string result = Regex.Replace(html, "\\{\\{(.*?)\\}\\}", match =>
                {
                    string key = match.Groups[1].Value.Trim();
                    if (variables.TryGetValue(key, out var value) && value != null)
                    {
                        return value.ToString() ?? string.Empty;
                    }
                    return match.Value; // keep original placeholder if not provided
                }, RegexOptions.Singleline);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render email template: {Template}", templateName);
                return html;
            }
        }

        private string LoadTemplate(string templateName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var expectedSuffix = ResourceSuffixPrefix + templateName + ".html";
                string? resourceName = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                {
                    _logger.LogWarning("Email template resource not found by suffix: {Suffix}", expectedSuffix);
                    return string.Empty;
                }

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogWarning("Email template resource stream not found: {Resource}", resourceName);
                    return string.Empty;
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load email template: {Template}", templateName);
                return string.Empty;
            }
        }
    }
}

