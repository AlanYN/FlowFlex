using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.User
{
    /// <summary>
    /// User Tree Node DTO for hierarchical display
    /// </summary>
    public class UserTreeNodeDto
    {
        /// <summary>
        /// Node ID (user ID for user nodes, team name for team nodes)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Node name (team name or user name)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Node type (team or user)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public List<UserTreeNodeDto> Children { get; set; } = new List<UserTreeNodeDto>();

        /// <summary>
        /// User details (only for user type nodes)
        /// </summary>
        public UserDto UserDetails { get; set; }

        /// <summary>
        /// Team member count (only for team type nodes)
        /// </summary>
        public int MemberCount { get; set; }
    }
}