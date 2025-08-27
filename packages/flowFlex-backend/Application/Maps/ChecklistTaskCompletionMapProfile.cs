using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// 检查清单任务完成记录映射配置
    /// </summary>
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
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.FilesJson, opt => opt.MapFrom(src => src.FilesJson)); // 显式映射FilesJson字段

            // 实体到输出DTO的映射
            CreateMap<ChecklistTaskCompletion, ChecklistTaskCompletionOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.LeadId, opt => opt.MapFrom(src => src.LeadId))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.TaskId, opt => opt.MapFrom(src => src.TaskId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
                .ForMember(dest => dest.CompletedTime, opt => opt.MapFrom(src => src.CompletedTime))
                .ForMember(dest => dest.CompletionNotes, opt => opt.MapFrom(src => src.CompletionNotes))
                .ForMember(dest => dest.FilesJson, opt => opt.MapFrom(src => src.FilesJson))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy))
                .ForMember(dest => dest.FilesCount, opt => opt.Ignore()) // Will be filled by service
                .ForMember(dest => dest.NotesCount, opt => opt.Ignore()); // Will be filled by service
        }
    }
}
