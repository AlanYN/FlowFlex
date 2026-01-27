using System;
using System.Linq;
using System.Text.RegularExpressions;
using FlowFlex.Domain.Shared.Const;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Validator for rule expressions to prevent injection attacks
    /// </summary>
    public static class ExpressionValidator
    {
        /// <summary>
        /// Validate a field path for use in rule expressions
        /// </summary>
        /// <param name="fieldPath">The field path to validate</param>
        /// <returns>Validation result with sanitized path if valid</returns>
        public static ExpressionValidationResult ValidateFieldPath(string fieldPath)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
            {
                return ExpressionValidationResult.Invalid("Field path cannot be empty");
            }

            // Check length
            if (fieldPath.Length > StageConditionConstants.MaxFieldPathLength)
            {
                return ExpressionValidationResult.Invalid(
                    $"Field path exceeds maximum length of {StageConditionConstants.MaxFieldPathLength} characters");
            }

            // Check for disallowed characters (potential injection)
            foreach (var disallowedChar in StageConditionConstants.DisallowedFieldPathChars)
            {
                if (fieldPath.Contains(disallowedChar))
                {
                    return ExpressionValidationResult.Invalid(
                        $"Field path contains disallowed character: '{disallowedChar}'");
                }
            }

            // Check for dangerous patterns
            if (ContainsDangerousPattern(fieldPath))
            {
                return ExpressionValidationResult.Invalid(
                    "Field path contains potentially dangerous pattern");
            }

            // Validate field path starts with allowed prefix
            var hasValidPrefix = StageConditionConstants.AllowedFieldPathPrefixes
                .Any(prefix => fieldPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (!hasValidPrefix)
            {
                return ExpressionValidationResult.Invalid(
                    $"Field path must start with one of: {string.Join(", ", StageConditionConstants.AllowedFieldPathPrefixes)}");
            }

            // Validate bracket syntax if present
            if (!ValidateBracketSyntax(fieldPath))
            {
                return ExpressionValidationResult.Invalid(
                    "Field path has invalid bracket syntax");
            }

            return ExpressionValidationResult.Valid(fieldPath);
        }

        /// <summary>
        /// Validate and sanitize a value for use in rule expressions
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>Validation result with sanitized value if valid</returns>
        public static ExpressionValidationResult ValidateValue(object value)
        {
            if (value == null)
            {
                return ExpressionValidationResult.Valid("null");
            }

            var stringValue = value.ToString();

            // Check length
            if (stringValue.Length > StageConditionConstants.MaxFieldValueLength)
            {
                return ExpressionValidationResult.Invalid(
                    $"Value exceeds maximum length of {StageConditionConstants.MaxFieldValueLength} characters");
            }

            // Check for dangerous patterns in string values
            if (value is string && ContainsDangerousPattern(stringValue))
            {
                return ExpressionValidationResult.Invalid(
                    "Value contains potentially dangerous pattern");
            }

            // Sanitize the value
            var sanitized = SanitizeValue(value);
            return ExpressionValidationResult.Valid(sanitized);
        }

        /// <summary>
        /// Sanitize a value for safe use in expressions
        /// </summary>
        public static string SanitizeValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is bool boolValue)
            {
                return boolValue.ToString().ToLower();
            }

            if (value is string strValue)
            {
                // Escape special characters for string literals
                var escaped = strValue
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
                return $"\"{escaped}\"";
            }

            // For numeric types, return as-is
            if (IsNumericType(value))
            {
                return value.ToString();
            }

            // For other types, treat as string
            var strVal = value.ToString();
            var escapedVal = strVal
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
            return $"\"{escapedVal}\"";
        }

        /// <summary>
        /// Check if a string contains dangerous patterns that could be used for injection
        /// </summary>
        private static bool ContainsDangerousPattern(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Patterns that could indicate code injection attempts
            var dangerousPatterns = new[]
            {
                @"System\.",           // System namespace access
                @"Process\.",          // Process manipulation
                @"File\.",             // File system access
                @"Directory\.",        // Directory access
                @"Environment\.",      // Environment access
                @"Assembly\.",         // Assembly manipulation
                @"Reflection\.",       // Reflection access
                @"typeof\s*\(",        // Type inspection
                @"GetType\s*\(",       // Type inspection
                @"Invoke\s*\(",        // Method invocation
                @"CreateInstance",     // Object creation
                @"Activator\.",        // Object activation
                @"AppDomain\.",        // AppDomain manipulation
                @"Thread\.",           // Thread manipulation
                @"Task\.Run",          // Task execution
                @"new\s+\w+\s*\(",     // Object instantiation
                @"=>",                 // Lambda expressions
                @"delegate",           // Delegate creation
                @"unsafe",             // Unsafe code
                @"fixed\s*\(",         // Fixed statement
                @"stackalloc",         // Stack allocation
                @"__",                 // Double underscore (often internal)
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validate bracket syntax in field path
        /// </summary>
        private static bool ValidateBracketSyntax(string fieldPath)
        {
            int bracketDepth = 0;
            bool inQuotes = false;

            for (int i = 0; i < fieldPath.Length; i++)
            {
                char c = fieldPath[i];

                if (c == '"' && (i == 0 || fieldPath[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (c == '[')
                    {
                        bracketDepth++;
                        // Check for valid bracket content start
                        if (i + 1 < fieldPath.Length && fieldPath[i + 1] != '"')
                        {
                            // Allow numeric index without quotes
                            if (!char.IsDigit(fieldPath[i + 1]))
                            {
                                return false;
                            }
                        }
                    }
                    else if (c == ']')
                    {
                        bracketDepth--;
                        if (bracketDepth < 0)
                        {
                            return false;
                        }
                    }
                }
            }

            return bracketDepth == 0 && !inQuotes;
        }

        /// <summary>
        /// Check if a value is a numeric type
        /// </summary>
        private static bool IsNumericType(object value)
        {
            return value is byte || value is sbyte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal;
        }
    }

    /// <summary>
    /// Result of expression validation
    /// </summary>
    public class ExpressionValidationResult
    {
        public bool IsValid { get; private set; }
        public string SanitizedValue { get; private set; }
        public string ErrorMessage { get; private set; }

        private ExpressionValidationResult() { }

        public static ExpressionValidationResult Valid(string sanitizedValue)
        {
            return new ExpressionValidationResult
            {
                IsValid = true,
                SanitizedValue = sanitizedValue
            };
        }

        public static ExpressionValidationResult Invalid(string errorMessage)
        {
            return new ExpressionValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
