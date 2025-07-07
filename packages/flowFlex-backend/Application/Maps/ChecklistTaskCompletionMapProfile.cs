using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// 检查清单任务完成记录映射配�?    /// </summary>
    public class ChecklistTaskCompletionMapProfile : Profile
    {
        public ChecklistTaskCompletionMapProfile()
        {
            // 输入DTO到实体的映射
            CreateMap<ChecklistTaskCompletionInputDto, ChecklistTaskCompletion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());

            // 实体到输出DTO的映�?            CreateMap<ChecklistTaskCompletion, ChecklistTaskCompletionOutputDto>();
        }
    }
} 
