using FlowFlex.Application.Contracts.IServices.OW;
using Infrastructure.CodeGenerator;
using Item.Redis;
using Microsoft.Extensions.Configuration;
using static Infrastructure.CodeGenerator.AutoCodeExtension;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Case code generator service implementation
    /// Generates unique case codes based on lead names following the pattern:
    /// - Extract prefix from lead name (similar to customer code generation)
    /// - Append sequential number with padding
    /// - Example: "TP-link1" -> "TPLINK0001", "C.H.Robinson" -> "CHRRES0001"
    /// </summary>
    public class CaseCodeGeneratorService : ICaseCodeGeneratorService
    {
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;

        // Configuration constants
        private const int CodeMaxNum = 10;              // Total code length
        private const int PrefixPartCount = 2;          // Number of prefix parts (words to take)
        private const int OnePrefixCount = 3;           // Characters per prefix part
        private const string CodeDefaultPrefix = "C";   // Default prefix when name processing results in empty
        private const string Replacement = " ";
        private const char PaddingChar = '0';
        private static readonly char[] Customize = ['.'];

        private int CodePrefixLength => PrefixPartCount * OnePrefixCount;
        private int UniqueIdLength => CodeMaxNum - CodePrefixLength;

        public CaseCodeGeneratorService(IRedisService redisService, IConfiguration configuration)
        {
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Generate case code from lead name
        /// </summary>
        public async Task<string> GenerateCaseCodeAsync(string leadName)
        {
            if (string.IsNullOrWhiteSpace(leadName))
            {
                leadName = CodeDefaultPrefix;
            }

            var caseCodePrefix = GeneratePrefix(leadName);
            var counterKey = GetCounterKey(caseCodePrefix);
            var uniqueId = await GenerateUniqueIdAsync(counterKey);
            var uniqueIdLength = UniqueIdLength;

            return $"{caseCodePrefix}{uniqueId.ToString().PadLeft(uniqueIdLength, PaddingChar)}";
        }

        /// <summary>
        /// Generate unique ID using Redis counter
        /// </summary>
        private async Task<long> GenerateUniqueIdAsync(string counterKey)
        {
            return await _redisService.StringIncrementAsync(counterKey);
        }

        /// <summary>
        /// Get Redis counter key for the prefix
        /// </summary>
        private string GetCounterKey(string caseCodePrefix)
        {
            var sysPrefix = string.IsNullOrEmpty(_configuration["Redis:KeyPrefix"]) 
                ? "" 
                : $"{_configuration["Redis:KeyPrefix"]}:";
            return $"{sysPrefix}ow:case:{caseCodePrefix}:count";
        }

        /// <summary>
        /// Generate prefix from lead name
        /// </summary>
        private string GeneratePrefix(string leadName)
        {
            // Process the string: remove special chars, merge spaces, remove customize chars
            var formattedString = leadName
                .ReplaceSpecialCharacter(Replacement, Customize)
                .ReplaceRepeatWhiteSpace(Replacement)
                .RemoveChars(Customize);

            if (string.IsNullOrWhiteSpace(formattedString))
            {
                formattedString = CodeDefaultPrefix;
            }

            var separatorArray = formattedString.Split(Replacement);

            var result = GenerateCore(separatorArray.ToList());

            return string.Concat(result).ToUpperInvariant();
        }

        /// <summary>
        /// Core generation logic - extract prefix parts from word array
        /// </summary>
        private string[] GenerateCore(List<string> originalArray)
        {
            // If single word longer than OnePrefixCount, split it into groups
            if (originalArray.Count == 1 && originalArray[0].Length > OnePrefixCount)
            {
                originalArray = SplitStringSingle(originalArray[0]);
            }

            var targetArray = AppendArray(originalArray, PrefixPartCount);

            var newArray = new string[targetArray.Length];

            for (var i = 0; i < targetArray.Length; i++)
            {
                if (i >= PrefixPartCount) break;

                var currentLength = targetArray[i].Length;

                if (currentLength >= OnePrefixCount)
                {
                    // Take first OnePrefixCount characters
                    newArray[i] = targetArray[i].Substring(0, OnePrefixCount);
                }
                else
                {
                    // Not enough characters, borrow from next words
                    var missingChars = OnePrefixCount - currentLength;
                    for (var j = i + 1; j < targetArray.Length && missingChars > 0; j++)
                    {
                        var nextString = targetArray[j];
                        var takeChars = Math.Min(missingChars, nextString.Length);
                        targetArray[i] += nextString.Substring(0, takeChars);
                        missingChars -= takeChars;

                        targetArray[j] = nextString.Substring(takeChars);
                    }

                    // If still not enough, pad with '0'
                    if (targetArray[i].Length < OnePrefixCount)
                    {
                        targetArray[i] = targetArray[i].PadRight(OnePrefixCount, PaddingChar);
                    }

                    newArray[i] = targetArray[i];
                }
            }

            return newArray;
        }

        /// <summary>
        /// Append empty strings to reach target array count
        /// </summary>
        private static string[] AppendArray(List<string> originalArray, int targetArrayCount)
        {
            var count = targetArrayCount - originalArray.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    originalArray.Add(string.Empty);
                }
            }
            return originalArray.ToArray();
        }

        /// <summary>
        /// Split single string into groups of OnePrefixCount characters
        /// </summary>
        private List<string> SplitStringSingle(string input)
        {
            var groups = Enumerable.Range(0, input.Length / OnePrefixCount)
                .Select(i => input.Substring(i * OnePrefixCount, OnePrefixCount))
                .Concat(new[] { input.Substring(input.Length / OnePrefixCount * OnePrefixCount) });

            return groups.ToList();
        }
    }
}

