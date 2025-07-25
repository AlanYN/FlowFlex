using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Client;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;

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
            var request = new Judge0SubmissionRequestDto
            {
                SourceCode = config.SourceCode,
                LanguageId = Python3LanguageId,
                Stdin = config.Stdin,
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
                    _logger.LogInformation("Code execution completed successfully. Token: {Token}", token);
                    return CreateSuccessResult(judge0Result);
                }
                else if (judge0Result.Status?.Id >= 4)
                {
                    _logger.LogWarning("Code execution failed. Token: {Token}, Status: {Status}", token, judge0Result.Status?.Description);
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
    }
}