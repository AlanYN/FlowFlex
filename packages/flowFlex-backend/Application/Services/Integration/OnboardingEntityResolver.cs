using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Domain.Repository.OW;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.Integration;

public class OnboardingEntityResolver : IOnboardingEntityResolver
{
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<OnboardingEntityResolver> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public OnboardingEntityResolver(
        IOnboardingRepository onboardingRepository,
        IMemoryCache memoryCache,
        ILogger<OnboardingEntityResolver> logger)
    {
        _onboardingRepository = onboardingRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<OnboardingEntityInfo?> ResolveAsync(long onboardingId)
    {
        var cacheKey = $"onboarding:entity:{onboardingId}";

        if (_memoryCache.TryGetValue(cacheKey, out OnboardingEntityInfo? cached))
            return cached;

        try
        {
            var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(onboardingId);
            if (onboarding == null || string.IsNullOrEmpty(onboarding.EntityId))
            {
                _memoryCache.Set(cacheKey, (OnboardingEntityInfo?)null, CacheDuration);
                return null;
            }

            var result = new OnboardingEntityInfo(onboarding.EntityId, onboarding.EntityType, onboarding.TenantId);
            _memoryCache.Set(cacheKey, (OnboardingEntityInfo?)result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve entity info for onboarding {OnboardingId}", onboardingId);
            return null;
        }
    }
}
