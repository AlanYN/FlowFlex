using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Client;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Services.Action.Executors
{
    /// <summary>
    /// Python action executor - executes Python scripts via Judge0
    /// </summary>
    public class PythonActionExecutor : IActionExecutor
    {
        private readonly IdeClient _ideClient;
        private readonly ILogger<PythonActionExecutor> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private const int Python3LanguageId = 71;

        public PythonActionExecutor(IdeClient ideClient, ILogger<PythonActionExecutor> logger)
        {
            _ideClient = ideClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public ActionTypeEnum ActionType => ActionTypeEnum.Python;

        public async Task<object> ExecuteAsync(string config, object triggerContext)
        {
            try
            {
                var configData = ParseConfig(config);

                if (configData == null)
                {
                    return CreateErrorResult("Invalid configuration format");
                }

                if (string.IsNullOrWhiteSpace(configData.SourceCode))
                {
                    return CreateErrorResult("Script content is required");
                }

                var parameters = ParseMainFunctionParameters(configData.SourceCode);

                var parameterValues = ProcessTriggerContextData(parameters, triggerContext);

                configData.SourceCode = CreateRunnerScript(configData.SourceCode, parameterValues);

                var result = await ExecutePythonScriptAsync(configData);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute Python action");
                return CreateErrorResult($"Execution failed: {ex.Message}");
            }
        }

        private PythonActionConfigDto? ParseConfig(string config)
        {
            try
            {
                return JsonSerializer.Deserialize<PythonActionConfigDto>(config, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Python action configuration");
                return null;
            }
        }

        private async Task<object> ExecutePythonScriptAsync(PythonActionConfigDto config)
        {
            string? base64Stdin = null;
            if (!string.IsNullOrEmpty(config.Stdin))
            {
                var stdinBytes = System.Text.Encoding.UTF8.GetBytes(config.Stdin);
                base64Stdin = Convert.ToBase64String(stdinBytes);
            }

            var request = new Judge0SubmissionRequestDto
            {
                SourceCode = config.SourceCode,
                LanguageId = Python3LanguageId,
                Stdin = base64Stdin,
                CompilerOptions = "",
                CommandLineArguments = config.CommandLineArguments ?? "",
                RedirectStderrToStdout = true
            };

            var submission = await _ideClient.SubmitCodeAsync(request);
            var token = submission.Token;

            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(30))
            {
                var judge0Result = await _ideClient.GetSubmissionResultAsync(token);

                if (judge0Result.Status?.Id == 3)
                {
                    return CreateSuccessResult(judge0Result);
                }
                else if (judge0Result.Status?.Id >= 4)
                {
                    return CreateErrorResult(judge0Result);
                }

                await Task.Delay(1000);
            }

            var timeoutResult = await _ideClient.GetSubmissionResultAsync(token);
            return CreateErrorResult(timeoutResult, "Execution timed out");
        }

        private object CreateSuccessResult(Judge0SubmissionResultDto judge0Result)
        {
            return new
            {
                success = true,
                message = "Python script executed successfully",
                stdout = judge0Result.Stdout,
                stderr = judge0Result.Stderr,
                executionTime = judge0Result.Time,
                memoryUsage = judge0Result.Memory,
                status = judge0Result.Status?.Description,
                token = judge0Result.Token,
                timestamp = DateTimeOffset.UtcNow
            };
        }

        private object CreateErrorResult(Judge0SubmissionResultDto? judge0Result = null, string? customMessage = null)
        {
            var message = customMessage ?? judge0Result?.Status?.Description ?? "Execution failed";

            return new
            {
                success = false,
                message,
                stdout = judge0Result?.Stdout,
                stderr = judge0Result?.Stderr,
                executionTime = judge0Result?.Time,
                memoryUsage = judge0Result?.Memory,
                status = judge0Result?.Status?.Description,
                token = judge0Result?.Token,
                errorDetails = judge0Result?.Message ?? message,
                timestamp = DateTimeOffset.UtcNow
            };
        }

        private object CreateErrorResult(string message)
        {
            return new
            {
                success = false,
                message,
                errorDetails = message,
                timestamp = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// Parse main function parameters from Python source code
        /// </summary>
        /// <param name="sourceCode">Python source code</param>
        /// <returns>List of main function parameter names</returns>
        private List<string> ParseMainFunctionParameters(string sourceCode)
        {
            var parameters = new List<string>();

            var mainPattern = @"def\s+main\s*\(([^)]*)\)(?:\s*->\s*[^:]*)?\s*:";
            var match = Regex.Match(sourceCode, mainPattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                var fallbackPattern = @"def\s+main\s*\(([^)]*)\)";
                match = Regex.Match(sourceCode, fallbackPattern, RegexOptions.IgnoreCase);
            }

            if (match.Success)
            {
                var paramString = match.Groups[1].Value.Trim();
                _logger.LogDebug($"Captured parameters: {paramString}");

                if (!string.IsNullOrEmpty(paramString))
                {
                    var paramList = paramString.Split(',');

                    foreach (var param in paramList)
                    {
                        var cleanParam = param.Trim();

                        if (cleanParam.Contains('='))
                        {
                            cleanParam = cleanParam.Substring(0, cleanParam.IndexOf('=')).Trim();
                        }

                        if (cleanParam.Contains(':') && !cleanParam.Contains('='))
                        {
                            cleanParam = cleanParam.Substring(0, cleanParam.IndexOf(':')).Trim();
                        }

                        if (!string.IsNullOrEmpty(cleanParam))
                        {
                            parameters.Add(cleanParam);
                            _logger.LogDebug($"Added parameter: {cleanParam}");
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning($"Failed to match main function in source code: {sourceCode}");
            }

            return parameters;
        }

        /// <summary>
        /// Process trigger context data based on parameter names
        /// </summary>
        /// <param name="parameters">List of parameter names from main function</param>
        /// <param name="triggerContext">Trigger context data</param>
        /// <returns>List of parameter values to be passed to main function</returns>
        private List<string> ProcessTriggerContextData(List<string> parameters, object triggerContext)
        {
            var parameterValues = new List<string>();
            var missingParameters = new List<string>();

            foreach (var paramName in parameters)
            {
                if (paramName.Equals("workflowContext", StringComparison.OrdinalIgnoreCase))
                {
                    var jsonString = JsonSerializer.Serialize(triggerContext, _jsonOptions);
                    parameterValues.Add(jsonString);
                }
                else
                {
                    var value = ExtractPropertyValue(triggerContext, paramName);
                    if (value != null)
                    {
                        var jsonString = JsonSerializer.Serialize(value, _jsonOptions);
                        parameterValues.Add(jsonString);
                    }
                    else
                    {
                        missingParameters.Add(paramName);
                        parameterValues.Add("null");
                    }
                }
            }

            if (missingParameters.Count > 0)
            {
                var missingParamsStr = string.Join(", ", missingParameters);

                throw new InvalidOperationException(
                    $"The following parameters could not be found in triggerContext: {missingParamsStr}. ");
            }

            return parameterValues;
        }

        /// <summary>
        /// Extract property value from triggerContext object by property name
        /// </summary>
        /// <param name="triggerContext">Trigger context object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <returns>Property value or null if not found</returns>
        private object? ExtractPropertyValue(object triggerContext, string propertyName)
        {
            if (triggerContext == null)
                return null;

            if (triggerContext is JToken jToken)
            {
                try
                {
                    var token = jToken[propertyName];
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        return token.ToObject<object>();
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Failed to extract property '{propertyName}' from JToken: {ex.Message}");
                    return null;
                }
            }

            var property = triggerContext.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(triggerContext);
            }

            property = triggerContext.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                return property.GetValue(triggerContext);
            }

            if (triggerContext is System.Collections.IDictionary dict)
            {
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    if (entry.Key?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return entry.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Create runner script template with user's source code embedded
        /// </summary>
        /// <param name="userSourceCode">User's source code</param>
        /// <param name="parameterValues">Parameter values to be passed to main function</param>
        /// <returns>Complete runner script with user code embedded</returns>
        private string CreateRunnerScript(string userSourceCode, List<string> parameterValues)
        {
            var scriptBuilder = new System.Text.StringBuilder();

            scriptBuilder.AppendLine("import json");
            scriptBuilder.AppendLine();

            scriptBuilder.AppendLine("# declare main function");
            scriptBuilder.AppendLine(userSourceCode);
            scriptBuilder.AppendLine();

            scriptBuilder.AppendLine("# parse parameters");
            for (int i = 0; i < parameterValues.Count; i++)
            {
                scriptBuilder.AppendLine($"param_{i} = {parameterValues[i]}");
            }
            scriptBuilder.AppendLine();

            scriptBuilder.AppendLine("# execute main function");
            var parameterNames = string.Join(", ", Enumerable.Range(0, parameterValues.Count).Select(i => $"param_{i}"));
            scriptBuilder.AppendLine($"output_obj = main({parameterNames})");
            scriptBuilder.AppendLine("print(output_obj)");

            return scriptBuilder.ToString();
        }
    }
}