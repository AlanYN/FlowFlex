using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Manages per-user, per-tour "seen" records so guided tours only show once per account.
    /// </summary>
    public class UserTourRecordService : IUserTourRecordService, IScopedService
    {
        private readonly IUserTourRecordRepository _repo;
        private readonly IOperatorContextService _operatorContext;
        private readonly ILogger<UserTourRecordService> _logger;

        public UserTourRecordService(
            IUserTourRecordRepository repo,
            IOperatorContextService operatorContext,
            ILogger<UserTourRecordService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _operatorContext = operatorContext ?? throw new ArgumentNullException(nameof(operatorContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<bool> HasSeenAsync(string tourKey)
        {
            if (string.IsNullOrWhiteSpace(tourKey))
                return false;

            // Resolve the current user the same way the rest of the codebase does
            // (UserContext -> X-User-Id header -> claims). IUserContextService only
            // reads NameIdentifier/sub claims, which IDM/IAM auth flows never set,
            // so it would silently return 0 and disable seen tracking.
            var userId = _operatorContext.GetOperatorId();
            if (userId <= 0)
            {
                _logger.LogWarning("HasSeenAsync: unable to resolve current user id (tourKey={TourKey})", tourKey);
                return false;
            }

            return await _repo.HasSeenAsync(userId, tourKey.Trim());
        }

        /// <inheritdoc />
        public async Task MarkSeenAsync(string tourKey)
        {
            if (string.IsNullOrWhiteSpace(tourKey))
                return;

            var userId = _operatorContext.GetOperatorId();
            if (userId <= 0)
            {
                _logger.LogWarning("MarkSeenAsync: unable to resolve current user id (tourKey={TourKey})", tourKey);
                return;
            }

            await _repo.MarkSeenAsync(userId, tourKey.Trim());
        }
    }
}
