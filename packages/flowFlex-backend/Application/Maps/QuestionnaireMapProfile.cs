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
                .ForMember(dest => dest.Assignments, opt => opt.Ignore()) // Assignments handled separately in service
                .ForMember(dest => dest.StructureJson, opt => opt.MapFrom(src => src.Structure != null ? src.Structure.ToString(Newtonsoft.Json.Formatting.None) : null))
                .ForMember(dest => dest.TagsJson, opt => opt.MapFrom(src => src.Tags != null ? src.Tags.ToString(Newtonsoft.Json.Formatting.None) : null));

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
                // Assignments removed from entity - handled through Stage Components
                .ForMember(dest => dest.Structure, opt => opt.MapFrom(src => ParseJToken(NormalizeJson(src.StructureJson))))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => ParseJToken(NormalizeJson(src.TagsJson))));

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

        private static string NormalizeJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var current = raw.Trim();
            // Unwrap up to 3 layers until looks like JSON object/array
            for (int i = 0; i < 3; i++)
            {
                if (current.StartsWith("[") || current.StartsWith("{"))
                {
                    return current;
                }
                var startsWithQuote = (current.StartsWith("\"") && current.EndsWith("\"")) ||
                                      (current.StartsWith("\'") && current.EndsWith("\'"));
                if (!startsWithQuote) break;
                try
                {
                    var inner = System.Text.Json.JsonSerializer.Deserialize<string>(current);
                    if (string.IsNullOrWhiteSpace(inner)) break;
                    current = inner.Trim();
                }
                catch
                {
                    break;
                }
            }
            return current;
        }

        private static Newtonsoft.Json.Linq.JToken ParseJToken(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return Newtonsoft.Json.Linq.JToken.Parse(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
