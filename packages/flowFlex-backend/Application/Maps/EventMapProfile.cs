using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Event;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Event mapping profile
    /// </summary>
    public class EventMapProfile : Profile
    {
        public EventMapProfile()
        {
            CreateMap<Event, EventOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType))
                .ForMember(dest => dest.EventVersion, opt => opt.MapFrom(src => src.EventVersion))
                .ForMember(dest => dest.EventTimestamp, opt => opt.MapFrom(src => src.EventTimestamp))
                .ForMember(dest => dest.AggregateId, opt => opt.MapFrom(src => src.AggregateId))
                .ForMember(dest => dest.AggregateType, opt => opt.MapFrom(src => src.AggregateType))
                .ForMember(dest => dest.EventSource, opt => opt.MapFrom(src => src.EventSource))
                .ForMember(dest => dest.EventData, opt => opt.MapFrom(src => src.EventData))
                .ForMember(dest => dest.EventMetadata, opt => opt.MapFrom(src => src.EventMetadata))
                .ForMember(dest => dest.EventDescription, opt => opt.MapFrom(src => src.EventDescription))
                .ForMember(dest => dest.EventStatus, opt => opt.MapFrom(src => src.EventStatus))
                .ForMember(dest => dest.ProcessCount, opt => opt.MapFrom(src => src.ProcessCount))
                .ForMember(dest => dest.LastProcessedAt, opt => opt.MapFrom(src => src.LastProcessedAt))
                .ForMember(dest => dest.ErrorMessage, opt => opt.MapFrom(src => src.ErrorMessage))
                .ForMember(dest => dest.RelatedEntityId, opt => opt.MapFrom(src => src.RelatedEntityId))
                .ForMember(dest => dest.RelatedEntityType, opt => opt.MapFrom(src => src.RelatedEntityType))
                .ForMember(dest => dest.EventTags, opt => opt.MapFrom(src => src.EventTags))
                .ForMember(dest => dest.RequiresRetry, opt => opt.MapFrom(src => src.RequiresRetry))
                .ForMember(dest => dest.NextRetryAt, opt => opt.MapFrom(src => src.NextRetryAt))
                .ForMember(dest => dest.MaxRetryCount, opt => opt.MapFrom(src => src.MaxRetryCount))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy));
        }
    }
} 