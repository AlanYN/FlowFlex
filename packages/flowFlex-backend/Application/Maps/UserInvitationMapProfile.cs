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
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapStatus(src.Status, src.TokenExpiry)))
                .ForMember(dest => dest.SentDate, opt => opt.MapFrom(src => src.SentDate.DateTime))
                .ForMember(dest => dest.InvitationToken, opt => opt.MapFrom(src => src.InvitationToken))
                .ForMember(dest => dest.TokenExpiry, opt => opt.MapFrom(src => src.TokenExpiry.DateTime))
                .ForMember(dest => dest.LastLoginDate, opt => opt.MapFrom(src => src.LastAccessDate.HasValue ? src.LastAccessDate.Value.DateTime : (DateTime?)null));
        }

        private string MapStatus(string status, DateTimeOffset tokenExpiry)
        {
            if (status == "Used")
                return "Active";

            if (tokenExpiry < DateTimeOffset.UtcNow)
                return "Expired";

            return status; // Pending
        }
    }
}