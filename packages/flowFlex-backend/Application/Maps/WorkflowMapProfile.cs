using System.Text.Json;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Workflow AutoMapper Profile
    /// </summary>
    public class WorkflowMapProfile : Profile
    {
        public WorkflowMapProfile()
        {
            // Workflow entity to output DTO mapping
            CreateMap<Workflow, WorkflowOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => src.IsValid))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
                .ForMember(dest => dest.ViewPermissionMode, opt => opt.MapFrom(src => src.ViewPermissionMode))
                .ForMember(dest => dest.ViewTeams, opt => opt.MapFrom(src => DeserializeTeamList(src.ViewTeams)))
                .ForMember(dest => dest.OperateTeams, opt => opt.MapFrom(src => DeserializeTeamList(src.OperateTeams)))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy))
                .ForMember(dest => dest.CreateUserId, opt => opt.MapFrom(src => src.CreateUserId))
                .ForMember(dest => dest.ModifyUserId, opt => opt.MapFrom(src => src.ModifyUserId))
                // 为了优化性能，工作流列表接口不返回Stage数据
                // Stage数据通过单独的接口获取: /api/ow/workflows/{id}/stages
                .ForMember(dest => dest.Stages, opt => opt.Ignore());

            // Input DTO to Workflow entity mapping
            CreateMap<WorkflowInputDto, Workflow>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.ConfigJson, opt => opt.MapFrom(src => src.ConfigJson))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
                .ForMember(dest => dest.ViewPermissionMode, opt => opt.MapFrom(src => src.ViewPermissionMode))
                .ForMember(dest => dest.ViewTeams, opt => opt.MapFrom(src => SerializeTeamList(src.ViewTeams)))
                .ForMember(dest => dest.OperateTeams, opt => opt.MapFrom(src => SerializeTeamList(src.OperateTeams)))
                // Ignore fields that will be set by extension methods
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.Version, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Stages, opt => opt.Ignore());
        }

        /// <summary>
        /// Helper method to deserialize JSON string to List of strings
        /// Handles both direct JSON arrays and double-encoded JSON strings
        /// </summary>
        private static List<string> DeserializeTeamList(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<string>();
            }

            try
            {
                var trimmed = jsonString.Trim();
                
                // Check if it's double-encoded (starts with a quote)
                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                {
                    // Remove outer quotes and unescape
                    var unescaped = JsonSerializer.Deserialize<string>(trimmed);
                    if (!string.IsNullOrEmpty(unescaped))
                    {
                        trimmed = unescaped;
                    }
                }
                
                // Now deserialize the actual array
                if (trimmed.StartsWith("["))
                {
                    return JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
                }
                
                return new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Helper method to serialize List of strings to JSON string
        /// </summary>
        private static string SerializeTeamList(List<string> teamList)
        {
            if (teamList == null || teamList.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(teamList);
        }
    }
}
