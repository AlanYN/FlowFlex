using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Domain.Shared.Models
{
    public class UserTeamModel
    {
        public long TeamId { get; set; }
        public List<UserTeamModel> SubTeam { get; set; }


        //Get all team ID collection by looping through subTeam
        public List<long> GetAllTeamIds()
        {
            var teamIds = new List<long>();
            if (TeamId == 0)
                return teamIds;
            teamIds.Add(TeamId);
            if (SubTeam != null && SubTeam.Any())
            {
                teamIds.AddRange(SubTeam.SelectMany(a => a.GetAllTeamIds()));
            }
            return teamIds;
        }
    }
}
