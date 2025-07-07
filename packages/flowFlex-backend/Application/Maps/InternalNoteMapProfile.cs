using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.InternalNote;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// 内部备注映射配置
    /// </summary>
    public class InternalNoteMapProfile : Profile
    {
        public InternalNoteMapProfile()
        {
            // 输入DTO到实体的映射
            CreateMap<InternalNoteInputDto, InternalNote>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());

            // 实体到输出DTO的映�?
            CreateMap<InternalNote, InternalNoteOutputDto>();

            // 更新DTO到实体的映射
            CreateMap<InternalNoteUpdateDto, InternalNote>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
} 
