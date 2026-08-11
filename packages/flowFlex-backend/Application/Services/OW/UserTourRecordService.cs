using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Manages per-user, per-tour "seen" records so guided tours only show once per account.
    /// </summary>
    public class UserTourRecordService : IUserTourRecordService, IScopedService
    {
        private readonly IUserTourRecordRepository _repo;
        private readonly IUserContextService _userContext;

        public UserTourRecordService(
            IUserTourRecordRepository repo,
            IUserContextService userContext)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        /// <inheritdoc />
        public async Task<bool> HasSeenAsync(string tourKey)
        {
            if (string.IsNullOrWhiteSpace(tourKey))
                return false;

            var userId = _userContext.GetCurrentUserId();
            if (userId <= 0) return false;

            return await _repo.HasSeenAsync(userId, tourKey.Trim());
        }

        /// <inheritdoc />
        public async Task MarkSeenAsync(string tourKey)
        {
            if (string.IsNullOrWhiteSpace(tourKey))
                return;

            var userId = _userContext.GetCurrentUserId();
            if (userId <= 0) return;

            await _repo.MarkSeenAsync(userId, tourKey.Trim());
        }
    }
}
