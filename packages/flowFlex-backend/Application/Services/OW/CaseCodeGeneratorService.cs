using FlowFlex.Application.Contracts.IServices.OW;
using Item.Redis;
using Microsoft.Extensions.Configuration;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Case code generator service implementation
    /// Generates unique case codes with fixed prefix "C" and auto-increment number
    /// Format: C00001, C00002, ..., C99999, C100000, C100001, ...
    /// </summary>
    public class CaseCodeGeneratorService : ICaseCodeGeneratorService
    {
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;

        // Configuration constants
        private const string CodePrefix = "C";          // Fixed prefix
        private const int InitialNumberLength = 5;      // Initial number length (00001-99999)
        private const char PaddingChar = '0';

        public CaseCodeGeneratorService(IRedisService redisService, IConfiguration configuration)
        {
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Generate case code with format: C00001, C00002, ..., C99999, C100000, ...
        /// </summary>
        public async Task<string> GenerateCaseCodeAsync(string leadName)
        {
            var counterKey = GetCounterKey();
            var uniqueId = await GenerateUniqueIdAsync(counterKey);
            
            // Determine padding length based on number size
            // For numbers 1-99999: use 5 digits (C00001)
            // For numbers >= 100000: use actual length (C100000)
            var numberLength = Math.Max(InitialNumberLength, uniqueId.ToString().Length);
            
            return $"{CodePrefix}{uniqueId.ToString().PadLeft(numberLength, PaddingChar)}";
        }

        /// <summary>
        /// Generate unique ID using Redis counter
        /// </summary>
        private async Task<long> GenerateUniqueIdAsync(string counterKey)
        {
            return await _redisService.StringIncrementAsync(counterKey);
        }

        /// <summary>
        /// Get Redis counter key (single global counter for all cases)
        /// </summary>
        private string GetCounterKey()
        {
            var sysPrefix = string.IsNullOrEmpty(_configuration["Redis:KeyPrefix"]) 
                ? "" 
                : $"{_configuration["Redis:KeyPrefix"]}:";
            return $"{sysPrefix}ow:case:global:count";
        }
    }
}

