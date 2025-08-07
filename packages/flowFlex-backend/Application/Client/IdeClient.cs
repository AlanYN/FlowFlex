using System.Text;
using System.Text.Json;
using FlowFlex.Application.Contracts.Dtos.Action;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Client
{
    /// <summary>
    /// Judge0 online IDE client
    /// </summary>
    public class IdeClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<IdeClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public IdeClient(HttpClient client, ILogger<IdeClient> logger)
        {
            _client = client;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        /// <summary>
        /// Submit code to Judge0 for execution
        /// </summary>
        /// <param name="request">Submission request</param>
        /// <returns>Submission response with token</returns>
        public async Task<Judge0SubmissionResponseDto> SubmitCodeAsync(Judge0SubmissionRequestDto request)
        {
            try
            {
                request.SourceCode = EncodeToBase64(request.SourceCode);
                if (!string.IsNullOrEmpty(request.Stdin))
                {
                    request.Stdin = EncodeToBase64(request.Stdin);
                }
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("/submissions", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Judge0SubmissionResponseDto>(responseContent, _jsonOptions);

                _logger.LogInformation("Code submitted successfully. Token: {Token}", result?.Token);
                return result ?? new Judge0SubmissionResponseDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit code to Judge0");
                throw;
            }
        }

        /// <summary>
        /// Get submission result by token
        /// </summary>
        /// <param name="token">Submission token</param>
        /// <returns>Submission result</returns>
        public async Task<Judge0SubmissionResultDto> GetSubmissionResultAsync(string token)
        {
            try
            {
                var response = await _client.GetAsync($"/submissions/{token}");
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Judge0SubmissionResultDto>(responseContent, _jsonOptions);

                if (result != null && result.Stdout != null)
                {
                    result.Stdout = DecodeFromBase64(result.Stdout);
                }

                return result ?? new Judge0SubmissionResultDto { Token = token };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get submission result for token: {Token}", token);
                throw;
            }
        }

        /// <summary>
        /// Helper method to encode string to base64
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Base64 encoded string</returns>
        public static string EncodeToBase64(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Helper method to decode base64 string
        /// </summary>
        /// <param name="base64Input">Base64 encoded string</param>
        /// <returns>Decoded string</returns>
        public static string DecodeFromBase64(string base64Input)
        {
            if (string.IsNullOrEmpty(base64Input))
                return string.Empty;

            var bytes = Convert.FromBase64String(base64Input);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
