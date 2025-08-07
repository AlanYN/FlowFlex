using Item.Redis;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.CodeGenerator;

public abstract class CodeGeneratorAbstract
{
    protected abstract string CodePrefix { get; set; }

    protected abstract string CodeSeparator { get; }

    protected abstract int CodeMaxNum { get; }

    protected abstract int MinCodeLength { get; }

    private int PrefixPartCount => CodePrefix.Length + CodeSeparator.Length;

    private int UniqueIdLength => CodeMaxNum - PrefixPartCount;

    private static readonly char PaddingChar = '0';

    private readonly IRedisService _redisService;

    private readonly IConfiguration _configuration;

    protected CodeGeneratorAbstract(IRedisService redisService,
        IConfiguration configuration)
    {
        _redisService = redisService;
        _configuration = configuration;
    }

    protected virtual async Task<long> GenerateUniqueIdAsync(string counterKey)
    {
        return await _redisService.StringIncrementAsync(counterKey);
    }

    protected virtual async Task<string> GenerateCodeAsync(
        string? codePrefix = null,
        int? codeLength = null)
    {
        var counterKey = GetCounterKey();
        var uniqueId = await GenerateUniqueIdAsync(counterKey);
        var uniqueIdLength = uniqueId.ToString().Length < (MinCodeLength - PrefixPartCount) ? (MinCodeLength - PrefixPartCount) : UniqueIdLength;
        return $"{CodePrefix}{CodeSeparator}{uniqueId.ToString().PadLeft(uniqueIdLength, PaddingChar)}";
    }

    private string GetCounterKey()
    {
        var sysPrefix = string.IsNullOrEmpty(_configuration["Redis:KeyPrefix"]) ? "" : $"{_configuration["Redis:KeyPrefix"]}:";
        return $"{sysPrefix}{CodePrefix}:count";
    }
}
