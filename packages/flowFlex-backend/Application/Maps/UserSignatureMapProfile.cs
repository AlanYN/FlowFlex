using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.UserSignature;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// UserSignature AutoMapper Profile
    /// </summary>
    public class UserSignatureMapProfile : Profile
    {
        public UserSignatureMapProfile()
        {
            // UserSignature entity -> ProfileSignatureOutputDto
            CreateMap<UserSignature, ProfileSignatureOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ImageBase64, opt => opt.MapFrom(src => src.ImageData))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreateDate));
        }
    }
}
