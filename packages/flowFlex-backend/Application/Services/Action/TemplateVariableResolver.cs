using System.Text.RegularExpressions;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Services.Action
{
    public interface ITemplateVariableResolver : IScopedService
    {
        string Replace(string input, object context);
        object? ResolvePath(object context, string path);
    }

    public class TemplateVariableResolver : ITemplateVariableResolver
    {
        private static readonly Regex PlaceholderPattern = new(@"\{\{\s*([\w.]+)\s*\}\}", RegexOptions.Compiled);
        private readonly ILogger<TemplateVariableResolver> _logger;

        public TemplateVariableResolver(ILogger<TemplateVariableResolver> logger)
        {
            _logger = logger;
        }

        public string Replace(string input, object context)
        {
            if (string.IsNullOrEmpty(input) || context == null)
                return input;

            var jToken = ConvertToJToken(context);
            if (jToken == null)
                return input;

            return PlaceholderPattern.Replace(input, match =>
            {
                var path = match.Groups[1].Value;
                var resolved = ResolvePathFromToken(jToken, path);
                if (resolved == null)
                {
                    _logger.LogWarning("Template variable not found: {Path}", path);
                    return string.Empty;
                }
                return resolved.Type == JTokenType.Object || resolved.Type == JTokenType.Array
                    ? resolved.ToString(Formatting.None)
                    : resolved.ToString();
            });
        }

        public object? ResolvePath(object context, string path)
        {
            if (context == null || string.IsNullOrEmpty(path))
                return null;

            var jToken = ConvertToJToken(context);
            if (jToken == null)
                return null;

            var resolved = ResolvePathFromToken(jToken, path);
            return resolved?.ToObject<object>();
        }

        private JToken? ResolvePathFromToken(JToken root, string path)
        {
            var segments = path.Split('.');
            var current = root;

            foreach (var segment in segments)
            {
                if (current == null || current.Type == JTokenType.Null)
                    return null;

                var child = current[segment];
                if (child != null)
                {
                    current = child;
                    continue;
                }

                // JToken indexer with string key on JObject is case-sensitive;
                // try SelectToken for more flexible access
                child = current.SelectToken(segment);
                if (child == null)
                    return null;

                current = child;
            }

            return current == root ? null : current;
        }

        private JToken? ConvertToJToken(object context)
        {
            try
            {
                if (context is JToken jt)
                    return jt;

                var json = JsonConvert.SerializeObject(context);
                return JToken.Parse(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert context to JToken");
                return null;
            }
        }
    }
}
