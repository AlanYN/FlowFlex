using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.WhatsNew;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;

namespace FlowFlex.Application.Maps;

/// <summary>
/// AutoMapper profile for What's New entities and DTOs
/// </summary>
public class WhatsNewMapProfile : Profile
{
    public WhatsNewMapProfile()
    {
        // Entity → user-facing DTOs
        CreateMap<WhatsNew, WhatsNewPanelItemDto>();
        CreateMap<WhatsNew, WhatsNewDetailDto>();

        // Projection → admin DTO
        CreateMap<WhatsNewAdminItemProjection, WhatsNewAdminItemDto>();

        // Request → Entity (Id and audit fields ignored; handled by service/base class)
        CreateMap<CreateWhatsNewRequest, WhatsNew>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PublishTime, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduledTime, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.IsValid, opt => opt.Ignore())
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());

        CreateMap<UpdateWhatsNewRequest, WhatsNew>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PublishTime, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduledTime, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.IsValid, opt => opt.Ignore())
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());
    }
}
