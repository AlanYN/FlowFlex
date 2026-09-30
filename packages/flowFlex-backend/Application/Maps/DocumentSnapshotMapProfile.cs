using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;
using FlowFlex.Domain.Entities.OW;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Document snapshot AutoMapper profile
    /// </summary>
    public class DocumentSnapshotMapProfile : Profile
    {
        public DocumentSnapshotMapProfile()
        {
            // DocumentSnapshot entity → output DTO
            // DataJson is stored as a raw JSON string in the entity.
            // Map it to object (JToken) so ASP.NET Core serialises it inline
            // as a JSON object rather than a quoted-and-escaped string.
            // This means the frontend receives it as a plain object and needs
            // no JSON.parse() at all.
            CreateMap<DocumentSnapshot, DocumentSnapshotOutputDto>()
                .ForMember(dest => dest.DataJson, opt => opt.MapFrom(src =>
                    !string.IsNullOrWhiteSpace(src.DataJson)
                        ? TryParseJson(src.DataJson)
                        : null));

            // Input DTO → DocumentSnapshot entity (audit fields are ignored; set by service/infrastructure)
            CreateMap<DocumentSnapshotInputDto, DocumentSnapshot>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UnitId, opt => opt.Ignore())
                .ForMember(dest => dest.Revision, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.AppCode, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore());

            // DocumentOperationLog entity → output DTO
            CreateMap<DocumentOperationLog, DocumentOperationLogOutputDto>();
        }

        /// <summary>
        /// Parse a JSON string into a JToken. Returns null if the string is not
        /// valid JSON, so the field is omitted from the response rather than
        /// causing a serialisation error.
        /// </summary>
        private static object TryParseJson(string json)
        {
            try { return JToken.Parse(json); }
            catch (JsonException) { return null; }
        }
    }
}
