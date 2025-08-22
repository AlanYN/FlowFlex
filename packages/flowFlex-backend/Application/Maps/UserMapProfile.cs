using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// User AutoMapper Profile
    /// </summary>
    public class UserMapProfile : Profile
    {
        public UserMapProfile()
        {
            // User entity to UserDto
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => src.EmailVerified))
                .ForMember(dest => dest.LastLoginDate, opt => opt.MapFrom(src => src.LastLoginDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.Team, opt => opt.MapFrom(src => src.Team));

            // UserDto to User entity (for updates)
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => src.EmailVerified))
                .ForMember(dest => dest.LastLoginDate, opt => opt.MapFrom(src => src.LastLoginDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Team, opt => opt.MapFrom(src => src.Team))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationCode, opt => opt.Ignore())
                .ForMember(dest => dest.VerificationCodeExpiry, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginIp, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.AppCode, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());
        }
    }
}