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
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());

            // 实体到输出DTO的映�?            CreateMap<StaticFieldValue, StaticFieldValueOutputDto>();

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
