using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// QuestionnaireAnswer 映射配置
    /// </summary>
    public class QuestionnaireAnswerMapProfile : Profile
    {
        public QuestionnaireAnswerMapProfile()
        {
            // Entity -> OutputDto
            CreateMap<QuestionnaireAnswer, QuestionnaireAnswerOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.AnswerJson, opt => opt.MapFrom(src => src.Answer != null ? src.Answer.ToString(Newtonsoft.Json.Formatting.None) : null))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CompletionRate, opt => opt.MapFrom(src => src.CompletionRate))
                .ForMember(dest => dest.CurrentSectionIndex, opt => opt.MapFrom(src => src.CurrentSectionIndex))
                .ForMember(dest => dest.SubmitTime, opt => opt.MapFrom(src => src.SubmitTime))
                .ForMember(dest => dest.ReviewTime, opt => opt.MapFrom(src => src.ReviewTime))
                .ForMember(dest => dest.ReviewerId, opt => opt.MapFrom(src => src.ReviewerId))
                .ForMember(dest => dest.ReviewNotes, opt => opt.MapFrom(src => src.ReviewNotes))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version))
                .ForMember(dest => dest.IsLatest, opt => opt.MapFrom(src => src.IsLatest))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.DeviceInfo, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy));

            // InputDto -> Entity
            CreateMap<QuestionnaireAnswerInputDto, QuestionnaireAnswer>()
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.AnswerJson) ? null : Newtonsoft.Json.Linq.JToken.Parse(src.AnswerJson)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? "Draft"))
                .ForMember(dest => dest.CompletionRate, opt => opt.MapFrom(src => src.CompletionRate ?? 0))
                .ForMember(dest => dest.CurrentSectionIndex, opt => opt.MapFrom(src => src.CurrentSectionIndex ?? 0))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                // 设置默认?
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.IsLatest, opt => opt.MapFrom(src => true))
                // 忽略基类属性和其他属?
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.SubmitTime, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewTime, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewerId, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewNotes, opt => opt.Ignore())
                .ForMember(dest => dest.IpAddress, opt => opt.Ignore())
                .ForMember(dest => dest.Source, opt => opt.Ignore());
        }
    }
}

