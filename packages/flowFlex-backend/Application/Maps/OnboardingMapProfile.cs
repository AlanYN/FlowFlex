using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Onboarding映射配置
    /// </summary>
    public class OnboardingMapProfile : Profile
    {
        public OnboardingMapProfile()
        {
            // 输入DTO到实体的映射
            CreateMap<OnboardingInputDto, Onboarding>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());

            // 实体到输出DTO的映射
            CreateMap<Onboarding, OnboardingOutputDto>()
                .ForMember(dest => dest.WorkflowName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentStageName, opt => opt.Ignore());

            // OnboardingStageProgress 到 OnboardingStageProgressDto 的映射
            CreateMap<OnboardingStageProgress, OnboardingStageProgressDto>();

            // 查询请求映射
            CreateMap<OnboardingQueryRequest, Onboarding>()
                .ForAllMembers(opt => opt.Ignore());
        }
    }
}