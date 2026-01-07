using FlowFlex.Domain.Entities.Shared;

namespace FlowFlex.Domain.Repository.Shared;

/// <summary>
/// Phone number prefix repository interface
/// </summary>
public interface IPhoneNumberPrefixRepository
{
    /// <summary>
    /// Get all phone number prefixes
    /// </summary>
    Task<List<PhoneNumberPrefix>> GetAllAsync(CancellationToken cancellationToken = default);
}
