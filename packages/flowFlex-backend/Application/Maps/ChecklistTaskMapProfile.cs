using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// ChecklistTask 映射配置
    /// </summary>
    public class ChecklistTaskMapProfile : Profile
    {
        public ChecklistTaskMapProfile()
        {
            // Entity -> OutputDto
            CreateMap<ChecklistTask, ChecklistTaskOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.TaskType, opt => opt.MapFrom(src => src.TaskType))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src => src.AssigneeId))
                .ForMember(dest => dest.AssigneeName, opt => opt.MapFrom(src => src.AssigneeName))
                .ForMember(dest => dest.AssignedTeam, opt => opt.MapFrom(src => src.AssignedTeam))
                .ForMember(dest => dest.EstimatedHours, opt => opt.MapFrom(src => src.EstimatedHours))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedDate))
                .ForMember(dest => dest.CompletionNotes, opt => opt.MapFrom(src => src.CompletionNotes))
                .ForMember(dest => dest.AttachmentsJson, opt => opt.MapFrom(src => src.AttachmentsJson))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy));

            // InputDto -> Entity
            CreateMap<ChecklistTaskInputDto, ChecklistTask>()
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.OrderIndex))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.TaskType, opt => opt.MapFrom(src => src.TaskType))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.AssigneeId, opt => opt.MapFrom(src => src.AssigneeId))
                .ForMember(dest => dest.AssigneeName, opt => opt.MapFrom(src => src.AssigneeName))
                .ForMember(dest => dest.AssignedTeam, opt => opt.MapFrom(src => src.AssignedTeam))
                .ForMember(dest => dest.EstimatedHours, opt => opt.MapFrom(src => src.EstimatedHours))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.AttachmentsJson, opt => opt.MapFrom(src => src.AttachmentsJson))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                // 设置默认�?
                .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
                // 忽略基类属性和其他属�?
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ActualHours, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletionNotes, opt => opt.Ignore())
                .ForMember(dest => dest.DependsOnTaskId, opt => opt.Ignore());
        }
    }
}



