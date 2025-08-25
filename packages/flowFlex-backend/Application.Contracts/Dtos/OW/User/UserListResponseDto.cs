using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// User List Response DTO
    /// </summary>
    public class UserListResponseDto
    {
        /// <summary>
        /// User list
        /// </summary>
        public List<UserDto> Users { get; set; } = new List<UserDto>();

        /// <summary>
        /// Total count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page index
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total pages
        /// </summary>
        public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 0;

        /// <summary>
        /// Has previous page
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// Has next page
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;
    }
}