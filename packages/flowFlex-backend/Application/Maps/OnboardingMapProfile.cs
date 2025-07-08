using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Onboardingӳ������
    /// </summary>
    public class OnboardingMapProfile : Profile
    {
        public OnboardingMapProfile()
        {
            // ����DTO��ʵ���ӳ��
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

            // ʵ�嵽���DTO��ӳ��
            CreateMap<Onboarding, OnboardingOutputDto>()
                .ForMember(dest => dest.WorkflowName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentStageName, opt => opt.Ignore());

            // OnboardingStageProgress �� OnboardingStageProgressDto ��ӳ��
            CreateMap<OnboardingStageProgress, OnboardingStageProgressDto>();

            // ��ѯ����ӳ��
            CreateMap<OnboardingQueryRequest, Onboarding>()
                .ForAllMembers(opt => opt.Ignore());
        }
    }
}