using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Integration;

public interface IOnboardingEntityResolver : IScopedService
{
    Task<OnboardingEntityInfo?> ResolveAsync(long onboardingId);
}

public record OnboardingEntityInfo(string EntityId, string? EntityType, string? TenantId);
