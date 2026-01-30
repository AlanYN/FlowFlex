using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.MessageCenter
{
    /// <summary>
    /// Email template rendering service using embedded resources with Handlebars-like syntax support.
    /// Supports: {{variable}}, {{{unescaped}}}, {{#if var}}...{{else}}...{{/if}}
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

            try
            {
                var data = variables ?? new Dictionary<string, object>();
                
                // Step 1: Process {{#if variable}}...{{else}}...{{/if}} blocks
                html = ProcessIfBlocks(html, data);
                
                // Step 2: Replace {{{variable}}} (unescaped, triple braces)
                html = Regex.Replace(html, @"\{\{\{(\w+)\}\}\}", match =>
                {
                    string key = match.Groups[1].Value;
                    if (data.TryGetValue(key, out var value) && value != null)
                    {
                        return value.ToString() ?? string.Empty;
                    }
                    return string.Empty;
                });
                
                // Step 3: Replace {{variable}} (escaped, double braces)
                html = Regex.Replace(html, @"\{\{(\w+)\}\}", match =>
                {
                    string key = match.Groups[1].Value;
                    if (data.TryGetValue(key, out var value) && value != null)
                    {
                        return System.Net.WebUtility.HtmlEncode(value.ToString() ?? string.Empty);
                    }
                    return string.Empty;
                });

                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render email template: {Template}", templateName);
                return html;
            }
        }

        /// <summary>
        /// Process {{#if variable}}...{{else}}...{{/if}} blocks
        /// </summary>
        private string ProcessIfBlocks(string html, IDictionary<string, object> data)
        {
            // Pattern to match {{#if variable}}...{{else}}...{{/if}} or {{#if variable}}...{{/if}}
            // Using non-greedy matching and allowing nested content
            var ifPattern = new Regex(
                @"\{\{#if\s+(\w+)\}\}(.*?)(?:\{\{else\}\}(.*?))?\{\{/if\}\}",
                RegexOptions.Singleline);

            // Process from innermost to outermost by iterating until no more matches
            string result = html;
            int maxIterations = 10; // Prevent infinite loops
            int iteration = 0;

            while (ifPattern.IsMatch(result) && iteration < maxIterations)
            {
                result = ifPattern.Replace(result, match =>
                {
                    string variableName = match.Groups[1].Value;
                    string ifContent = match.Groups[2].Value;
                    string elseContent = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;

                    // Check if variable exists and is truthy
                    bool isTruthy = false;
                    if (data.TryGetValue(variableName, out var value) && value != null)
                    {
                        if (value is bool boolValue)
                        {
                            isTruthy = boolValue;
                        }
                        else if (value is string strValue)
                        {
                            isTruthy = !string.IsNullOrWhiteSpace(strValue);
                        }
                        else
                        {
                            isTruthy = true;
                        }
                    }

                    return isTruthy ? ifContent : elseContent;
                });
                iteration++;
            }

            return result;
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

