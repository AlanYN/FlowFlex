using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// User List Request DTO
    /// </summary>
    public class UserListRequestDto : QueryPageModel
    {
        /// <summary>
        /// Search text (search in username and email)
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// Email filter
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Username filter
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Team filter
        /// </summary>
        public string Team { get; set; }

        /// <summary>
        /// Status filter
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Email verification status filter
        /// </summary>
        public bool? EmailVerified { get; set; }

        /// <summary>
        /// Sort field (default: CreateDate)
        /// </summary>
        public string SortField { get; set; } = "CreateDate";

        /// <summary>
        /// Sort direction (asc/desc, default: desc)
        /// </summary>
        public string SortDirection { get; set; } = "desc";
    }
}