using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    public class TriggerGraphMapProfile : Profile
    {
        public TriggerGraphMapProfile()
        {
            CreateMap<WorkflowTriggerGraph, TriggerGraphDto>()
                .ForMember(d => d.Connections, o => o.Ignore()); // populated manually in service

            CreateMap<WorkflowTriggerConnection, TriggerConnectionDto>();

            CreateMap<TriggerConnectionDto, WorkflowTriggerConnection>()
                .ForMember(d => d.Id, o => o.Ignore())              // id assigned by snowflake
                .ForMember(d => d.TenantId, o => o.Ignore())
                .ForMember(d => d.AppCode, o => o.Ignore())
                .ForMember(d => d.IsValid, o => o.Ignore())
                .ForMember(d => d.CreateDate, o => o.Ignore())
                .ForMember(d => d.ModifyDate, o => o.Ignore())
                .ForMember(d => d.CreateBy, o => o.Ignore())
                .ForMember(d => d.ModifyBy, o => o.Ignore())
                .ForMember(d => d.CreateUserId, o => o.Ignore())
                .ForMember(d => d.ModifyUserId, o => o.Ignore());
        }
    }
}
