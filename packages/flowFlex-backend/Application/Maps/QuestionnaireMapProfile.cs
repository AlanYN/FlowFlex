using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Common;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Questionnaire AutoMapper profile
    /// </summary>
    public class QuestionnaireMapProfile : Profile
    {
        public QuestionnaireMapProfile()
        {
            // AssignmentDto 映射 - 双向映射
            CreateMap<QuestionnaireAssignmentDto, FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            CreateMap<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto, QuestionnaireAssignmentDto>();

            // Questionnaire entity to output DTO
            CreateMap<Questionnaire, QuestionnaireOutputDto>()
                .ForMember(dest => dest.Assignments, opt => opt.MapFrom(src => src.Assignments));

            // Input DTO to Questionnaire entity
            CreateMap<QuestionnaireInputDto, Questionnaire>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.TotalQuestions, opt => opt.Ignore())
                .ForMember(dest => dest.RequiredQuestions, opt => opt.Ignore())
                .ForMember(dest => dest.Assignments, opt => opt.Ignore()); // Ignore assignments as they're handled separately in service

            // QuestionnaireSection entity to output DTO
            CreateMap<QuestionnaireSection, QuestionnaireSectionDto>();

            // Input DTO to QuestionnaireSection entity
            CreateMap<QuestionnaireSectionInputDto, QuestionnaireSection>()
                .ForMember(dest => dest.QuestionnaireId, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore());
        }
    }
}
