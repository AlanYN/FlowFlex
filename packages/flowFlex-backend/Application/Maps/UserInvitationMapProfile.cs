using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// User invitation mapping profile
    /// </summary>
    public class UserInvitationMapProfile : Profile
    {
        public UserInvitationMapProfile()
        {
            CreateMap<UserInvitation, PortalUserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapStatus(src.Status)))
                .ForMember(dest => dest.SentDate, opt => opt.MapFrom(src => src.SentDate))
                .ForMember(dest => dest.InvitationToken, opt => opt.MapFrom(src => src.InvitationToken))
                .ForMember(dest => dest.LastLoginDate, opt => opt.MapFrom(src => src.LastAccessDate));
        }

        private string MapStatus(string status)
        {
            // Map all non-Inactive statuses to Active
            // This includes: Pending, Used, Active
            if (status == "Inactive")
                return "Inactive";

            return "Active";
        }
    }
}