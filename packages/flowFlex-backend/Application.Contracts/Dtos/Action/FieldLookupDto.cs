using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Mapping configuration model containing field mappings and lookup settings
    /// </summary>
    public class MappingConfigModel
    {
        /// <summary>
        /// List of field mapping items
        /// </summary>
        public List<FieldMappingItem>? FieldMappings { get; set; }

        /// <summary>
        /// Global lookup configuration
        /// </summary>
        public LookupGlobalConfig? LookupConfig { get; set; }
    }

    /// <summary>
    /// Individual field mapping item with optional lookup configuration
    /// </summary>
    public class FieldMappingItem
    {
        /// <summary>
        /// WFE internal field identifier
        /// </summary>
        public string WfeField { get; set; } = string.Empty;

        /// <summary>
        /// External API field identifier
        /// </summary>
        public string ApiField { get; set; } = string.Empty;

        /// <summary>
        /// Optional lookup configuration for this field
        /// </summary>
        public LookupConfig? Lookup { get; set; }

        /// <summary>
        /// Optional default value to inject into contextData when the field resolves to empty/null
        /// after lookup and matching. When JToken is C# null, no default is configured.
        /// When JToken is JTokenType.Null, the configured default is JSON null (rendered as literal "null").
        /// </summary>
        public JToken? DefaultValue { get; set; }
    }

    /// <summary>
    /// Lookup configuration for a single field
    /// </summary>
    public class LookupConfig
    {
        /// <summary>
        /// API endpoint to fetch lookup options
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// JSONPath to extract display value from each option item
        /// </summary>
        public string DisplayPath { get; set; } = string.Empty;

        /// <summary>
        /// JSONPath to extract actual value from each option item
        /// </summary>
        public string ValuePath { get; set; } = string.Empty;

        /// <summary>
        /// Optional JSONPath to locate the options array in the response
        /// </summary>
        public string? ResponsePath { get; set; }

        /// <summary>
        /// Optional custom headers for the lookup request
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Optional override integration ID (uses default if null)
        /// </summary>
        public long? IntegrationId { get; set; }
    }

    /// <summary>
    /// Global lookup configuration applied to all lookup fields
    /// </summary>
    public class LookupGlobalConfig
    {
        /// <summary>
        /// Request timeout in seconds (default: 10)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Maximum number of options to return per field (default: 200)
        /// </summary>
        public int MaxOptionsPerField { get; set; } = 200;
    }

    /// <summary>
    /// Single option item with display text and value
    /// </summary>
    public class OptionItem
    {
        /// <summary>
        /// Display text shown to the user
        /// </summary>
        public string Display { get; set; } = string.Empty;

        /// <summary>
        /// Actual value stored when option is selected
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of a field lookup operation
    /// </summary>
    public class FieldLookupResult
    {
        /// <summary>
        /// The API field identifier this result belongs to
        /// </summary>
        public string ApiField { get; set; } = string.Empty;

        /// <summary>
        /// Lookup status: "success" or "lookup_failed"
        /// </summary>
        public string Status { get; set; } = "success";

        /// <summary>
        /// List of resolved options
        /// </summary>
        public List<OptionItem> Options { get; set; } = new();

        /// <summary>
        /// Total count of options before truncation
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Error message when status is "lookup_failed"
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Create a successful lookup result
        /// </summary>
        public static FieldLookupResult Success(string apiField, List<OptionItem> options, int totalCount)
            => new() { ApiField = apiField, Status = "success", Options = options, TotalCount = totalCount };

        /// <summary>
        /// Create a failed lookup result
        /// </summary>
        public static FieldLookupResult Failed(string apiField, string? error)
            => new() { ApiField = apiField, Status = "lookup_failed", Error = error };
    }

    /// <summary>
    /// Request DTO for previewing lookup options
    /// </summary>
    public class LookupPreviewRequest
    {
        /// <summary>
        /// Integration ID to use for authentication
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// API endpoint to fetch lookup options
        /// </summary>
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// JSONPath to extract display value from each option item
        /// </summary>
        [Required]
        public string DisplayPath { get; set; } = string.Empty;

        /// <summary>
        /// JSONPath to extract actual value from each option item
        /// </summary>
        [Required]
        public string ValuePath { get; set; } = string.Empty;

        /// <summary>
        /// Optional JSONPath to locate the options array in the response
        /// </summary>
        public string? ResponsePath { get; set; }

        /// <summary>
        /// Optional custom headers for the lookup request
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }
    }

    /// <summary>
    /// Response DTO for lookup preview
    /// </summary>
    public class LookupPreviewResponse
    {
        /// <summary>
        /// Whether the lookup was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Preview options (limited to first 10)
        /// </summary>
        public List<OptionItem> Options { get; set; } = new();

        /// <summary>
        /// Total count of available options
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Error message when lookup fails
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Unified HTTP response from integration API calls
    /// </summary>
    public class IntegrationHttpResponse
    {
        /// <summary>
        /// Whether the request was successful (2xx status code)
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response body content
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Error message when request fails
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Request duration in milliseconds
        /// </summary>
        public long DurationMs { get; set; }
    }
}
