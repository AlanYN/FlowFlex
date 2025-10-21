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
                .ForMember(dest => dest.AppCode, opt => opt.Ignore()) // 忽略AppCode，防止更新时被修改
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                // Permission fields mapping
                .ForMember(dest => dest.ViewPermissionSubjectType, opt => opt.MapFrom(src => src.ViewPermissionSubjectType))
                .ForMember(dest => dest.OperatePermissionSubjectType, opt => opt.MapFrom(src => src.OperatePermissionSubjectType))
                .ForMember(dest => dest.ViewPermissionMode, opt => opt.MapFrom(src => src.ViewPermissionMode))
                .ForMember(dest => dest.ViewTeams, opt => opt.MapFrom(src => SerializeSubjectList(src.ViewTeams)))
                .ForMember(dest => dest.ViewUsers, opt => opt.MapFrom(src => SerializeSubjectList(src.ViewUsers)))
                .ForMember(dest => dest.OperateTeams, opt => opt.MapFrom(src => SerializeSubjectList(src.OperateTeams)))
                .ForMember(dest => dest.OperateUsers, opt => opt.MapFrom(src => SerializeSubjectList(src.OperateUsers)));

            // Entity to DTO mapping
            CreateMap<Onboarding, OnboardingOutputDto>()
                .ForMember(dest => dest.WorkflowName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentStageName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentStageEndTime, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentStageEstimatedDays, opt => opt.Ignore())
                // Permission fields mapping
                .ForMember(dest => dest.ViewPermissionSubjectType, opt => opt.MapFrom(src => src.ViewPermissionSubjectType))
                .ForMember(dest => dest.OperatePermissionSubjectType, opt => opt.MapFrom(src => src.OperatePermissionSubjectType))
                .ForMember(dest => dest.ViewPermissionMode, opt => opt.MapFrom(src => src.ViewPermissionMode))
                .ForMember(dest => dest.ViewTeams, opt => opt.MapFrom(src => DeserializeSubjectList(src.ViewTeams)))
                .ForMember(dest => dest.ViewUsers, opt => opt.MapFrom(src => DeserializeSubjectList(src.ViewUsers)))
                .ForMember(dest => dest.OperateTeams, opt => opt.MapFrom(src => DeserializeSubjectList(src.OperateTeams)))
                .ForMember(dest => dest.OperateUsers, opt => opt.MapFrom(src => DeserializeSubjectList(src.OperateUsers)));

            // OnboardingStageProgress to OnboardingStageProgressDto mapping
            CreateMap<OnboardingStageProgress, OnboardingStageProgressDto>()
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => ParseComponents(src.ComponentsJson)))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                // EstimatedDays priority: CustomEstimatedDays > EstimatedDays (from Stage)
                .ForMember(dest => dest.EstimatedDays, opt => opt.MapFrom(src => src.CustomEstimatedDays ?? src.EstimatedDays))
                // Keep CustomEstimatedDays for transparency
                .ForMember(dest => dest.CustomEstimatedDays, opt => opt.MapFrom(src => src.CustomEstimatedDays))
                .ForMember(dest => dest.CustomEndTime, opt => opt.MapFrom(src => src.CustomEndTime))
                // AI summary fields
                .ForMember(dest => dest.AiSummary, opt => opt.MapFrom(src => src.AiSummary))
                .ForMember(dest => dest.AiSummaryGeneratedAt, opt => opt.MapFrom(src => src.AiSummaryGeneratedAt))
                .ForMember(dest => dest.AiSummaryConfidence, opt => opt.MapFrom(src => src.AiSummaryConfidence))
                .ForMember(dest => dest.AiSummaryModel, opt => opt.MapFrom(src => src.AiSummaryModel))
                .ForMember(dest => dest.AiSummaryData, opt => opt.MapFrom(src => src.AiSummaryData))
                // Save fields
                .ForMember(dest => dest.IsSaved, opt => opt.MapFrom(src => src.IsSaved))
                .ForMember(dest => dest.SaveTime, opt => opt.MapFrom(src => src.SaveTime))
                .ForMember(dest => dest.SavedById, opt => opt.MapFrom(src => src.SavedById))
                .ForMember(dest => dest.SavedBy, opt => opt.MapFrom(src => src.SavedBy));

            // OnboardingStageProgressDto  OnboardingStageProgress ӳ
            CreateMap<OnboardingStageProgressDto, OnboardingStageProgress>()
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                .ForMember(dest => dest.ComponentsJson, opt => opt.MapFrom(src => SerializeComponents(src.Components)))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components))
                // Save fields
                .ForMember(dest => dest.IsSaved, opt => opt.MapFrom(src => src.IsSaved))
                .ForMember(dest => dest.SaveTime, opt => opt.MapFrom(src => src.SaveTime))
                .ForMember(dest => dest.SavedById, opt => opt.MapFrom(src => src.SavedById))
                .ForMember(dest => dest.SavedBy, opt => opt.MapFrom(src => src.SavedBy));

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
                var normalized = NormalizeJson(componentsJson);
                var parsedComponents = JsonSerializer.Deserialize<List<StageComponent>>(normalized);
                return parsedComponents ?? new List<StageComponent>();
            }
            catch
            {
                // If JSON is invalid, return empty list instead of default components
                return new List<StageComponent>();
            }
        }

        private static string NormalizeJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var current = raw.Trim();
            for (int i = 0; i < 3; i++)
            {
                if (current.StartsWith("[") || current.StartsWith("{")) return current;
                var quoted = (current.StartsWith("\"") && current.EndsWith("\"")) || (current.StartsWith("\'") && current.EndsWith("\'"));
                if (!quoted) break;
                try
                {
                    var inner = JsonSerializer.Deserialize<string>(current);
                    if (string.IsNullOrWhiteSpace(inner)) break;
                    current = inner.Trim();
                }
                catch { break; }
            }
            return current;
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

        /// <summary>
        /// Helper method to deserialize JSON string to List of strings
        /// Handles both direct JSON arrays and double-encoded JSON strings
        /// </summary>
        private static List<string> DeserializeSubjectList(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<string>();
            }

            try
            {
                var trimmed = jsonString.Trim();

                // Check if it's double-encoded (starts with a quote)
                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                {
                    // Remove outer quotes and unescape
                    var unescaped = JsonSerializer.Deserialize<string>(trimmed);
                    if (!string.IsNullOrEmpty(unescaped))
                    {
                        trimmed = unescaped;
                    }
                }

                // Now deserialize the actual array
                if (trimmed.StartsWith("["))
                {
                    return JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
                }

                return new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Helper method to serialize List of strings to JSON string
        /// </summary>
        private static string SerializeSubjectList(List<string> subjectList)
        {
            if (subjectList == null || subjectList.Count == 0)
            {
                return null;
            }
            return JsonSerializer.Serialize(subjectList);
        }
    }
}