using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Models;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Onboardingӳ
    /// </summary>
    public class OnboardingMapProfile : Profile
    {
        public OnboardingMapProfile()
        {
            // DTOʵӳ
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

            // ʵ嵽DTOӳ
            CreateMap<Onboarding, OnboardingOutputDto>()
                .ForMember(dest => dest.WorkflowName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentStageName, opt => opt.Ignore());

            // OnboardingStageProgress  OnboardingStageProgressDto ӳ
            CreateMap<OnboardingStageProgress, OnboardingStageProgressDto>()
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => ParseComponents(src.ComponentsJson)));

            // OnboardingStageProgressDto  OnboardingStageProgress ӳ
            CreateMap<OnboardingStageProgressDto, OnboardingStageProgress>()
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                .ForMember(dest => dest.ComponentsJson, opt => opt.MapFrom(src => SerializeComponents(src.Components)))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));

            // ѯӳ
            CreateMap<OnboardingQueryRequest, Onboarding>()
                .ForAllMembers(opt => opt.Ignore());
        }

        private static List<StageComponent> ParseComponents(string componentsJson)
        {
            if (string.IsNullOrEmpty(componentsJson))
            {
                // Return empty list when JSON is null or empty, not default components
                return new List<StageComponent>();
            }

            try
            {
                var parsedComponents = JsonSerializer.Deserialize<List<StageComponent>>(componentsJson);
                return parsedComponents ?? new List<StageComponent>();
            }
            catch
            {
                // If JSON is invalid, return empty list instead of default components
                return new List<StageComponent>();
            }
        }



        private static string SerializeComponents(List<StageComponent> components)
        {
            if (components == null || !components.Any())
                return null;

            try
            {
                // Ensure all components have proper default values
                foreach (var component in components)
                {
                    component.StaticFields ??= new List<string>();
                    component.ChecklistIds ??= new List<long>();
                    component.QuestionnaireIds ??= new List<long>();
                }

                return JsonSerializer.Serialize(components);
            }
            catch
            {
                return null;
            }
        }
    }
}