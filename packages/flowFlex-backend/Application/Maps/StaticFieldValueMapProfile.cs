using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// 静态字段值映射配�?    /// </summary>
    public class StaticFieldValueMapProfile : Profile
    {
        public StaticFieldValueMapProfile()
        {
            // 输入DTO到实体的映射
            CreateMap<StaticFieldValueInputDto, StaticFieldValue>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? 0))
                .ForMember(dest => dest.FieldId, opt => opt.MapFrom(src => src.FieldId))
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.SubmitTime, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewTime, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewerId, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewNotes, opt => opt.Ignore())
                .ForMember(dest => dest.Version, opt => opt.Ignore())
                .ForMember(dest => dest.IsLatest, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.IsSubmitted, opt => opt.Ignore())
                .ForMember(dest => dest.Metadata, opt => opt.Ignore());

            // 实体到输出DTO的映射
            CreateMap<StaticFieldValue, StaticFieldValueOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.FieldName, opt => opt.MapFrom(src => src.FieldName))
                .ForMember(dest => dest.FieldId, opt => opt.MapFrom(src => src.FieldId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.FieldValueJson, opt => opt.MapFrom(src => src.FieldValueJson))
                .ForMember(dest => dest.FieldType, opt => opt.MapFrom(src => src.FieldType))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CompletionRate, opt => opt.MapFrom(src => src.CompletionRate))
                .ForMember(dest => dest.SubmitTime, opt => opt.MapFrom(src => src.SubmitTime))
                .ForMember(dest => dest.ReviewTime, opt => opt.MapFrom(src => src.ReviewTime))
                .ForMember(dest => dest.ReviewerId, opt => opt.MapFrom(src => src.ReviewerId))
                .ForMember(dest => dest.ReviewNotes, opt => opt.MapFrom(src => src.ReviewNotes))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version))
                .ForMember(dest => dest.IsLatest, opt => opt.MapFrom(src => src.IsLatest))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source))
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.ValidationStatus, opt => opt.MapFrom(src => src.ValidationStatus))
                .ForMember(dest => dest.ValidationErrors, opt => opt.MapFrom(src => src.ValidationErrors))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => src.IsValid))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy))
                .ForMember(dest => dest.CreateUserId, opt => opt.MapFrom(src => src.CreateUserId))
                .ForMember(dest => dest.ModifyUserId, opt => opt.MapFrom(src => src.ModifyUserId));

            // 批量输入DTO到实体列表的映射
            CreateMap<BatchStaticFieldValueInputDto, List<StaticFieldValue>>()
                .ConvertUsing((src, dest, context) =>
                {
                    List<StaticFieldValue> result = new List<StaticFieldValue>();
                    foreach (StaticFieldValueInputDto fieldValue in src.FieldValues)
                    {
                        StaticFieldValue entity = context.Mapper.Map<StaticFieldValue>(fieldValue);
                        entity.OnboardingId = src.OnboardingId;
                        entity.StageId = src.StageId;
                        entity.Source = src.Source;
                        entity.IpAddress = src.IpAddress;
                        entity.UserAgent = src.UserAgent;
                        result.Add(entity);
                    }
                    return result;
                });
        }
    }
}
