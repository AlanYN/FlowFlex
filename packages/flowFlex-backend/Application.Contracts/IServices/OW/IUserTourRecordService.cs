namespace FlowFlex.Application.Contracts.IServices.OW
{
    public interface IUserTourRecordService
    {
        /// <summary>
        /// Returns true if the current user has already seen the specified tour.
        /// </summary>
        Task<bool> HasSeenAsync(string tourKey);

        /// <summary>
        /// Marks the specified tour as seen for the current user.
        /// Idempotent — safe to call multiple times.
        /// </summary>
        Task MarkSeenAsync(string tourKey);
    }
}
