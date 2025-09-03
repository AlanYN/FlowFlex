using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Domain.Entities.OW;
using System.Text.Json;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// 操作变更日志映射配置
    /// </summary>
    public class OperationChangeLogMapProfile : Profile
    {
        public OperationChangeLogMapProfile()
        {
            // 实体到输出DTO的映射
            CreateMap<OperationChangeLog, OperationChangeLogOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.OperationType, opt => opt.MapFrom(src => src.OperationType))
                .ForMember(dest => dest.BusinessModule, opt => opt.MapFrom(src => src.BusinessModule))
                .ForMember(dest => dest.BusinessId, opt => opt.MapFrom(src => src.BusinessId))
                .ForMember(dest => dest.OnboardingId, opt => opt.MapFrom(src => src.OnboardingId))
                .ForMember(dest => dest.StageId, opt => opt.MapFrom(src => src.StageId))
                .ForMember(dest => dest.OperationStatus, opt => opt.MapFrom(src => src.OperationStatus))
                .ForMember(dest => dest.OperationDescription, opt => opt.MapFrom(src => src.OperationDescription))
                .ForMember(dest => dest.OperationTitle, opt => opt.MapFrom(src => src.OperationTitle))
                .ForMember(dest => dest.OperationSource, opt => opt.MapFrom(src => src.OperationSource))
                .ForMember(dest => dest.BeforeData, opt => opt.MapFrom(src => NormalizeJsonData(src.BeforeData)))
                .ForMember(dest => dest.AfterData, opt => opt.MapFrom(src => NormalizeJsonData(src.AfterData)))
                .ForMember(dest => dest.ChangedFields, opt => opt.MapFrom(src => ParseChangedFields(src.ChangedFields)))
                .ForMember(dest => dest.OperatorId, opt => opt.MapFrom(src => src.OperatorId))
                .ForMember(dest => dest.OperatorName, opt => opt.MapFrom(src => src.OperatorName))
                .ForMember(dest => dest.OperationTime, opt => opt.MapFrom(src => src.OperationTime))
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.ExtendedData, opt => opt.MapFrom(src => NormalizeJsonData(src.ExtendedData)))
                .ForMember(dest => dest.ErrorMessage, opt => opt.MapFrom(src => src.ErrorMessage))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));
        }

        /// <summary>
        /// 解析变更字段JSON字符串为字符串列表
        /// </summary>
        private static List<string> ParseChangedFields(string changedFieldsJson)
        {
            if (string.IsNullOrEmpty(changedFieldsJson))
                return new List<string>();

            try
            {
                var fields = JsonSerializer.Deserialize<string[]>(changedFieldsJson);
                return fields?.ToList() ?? new List<string>();
            }
            catch
            {
                // 如果解析失败，返回空列表
                return new List<string>();
            }
        }

        /// <summary>
        /// 规范化JSON数据，处理双重编码问题
        /// </summary>
        private static string NormalizeJsonData(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
                return jsonData;

            try
            {
                // 检查是否被双重编码（字符串包含转义的JSON）
                if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
                {
                    // 尝试反序列化外层字符串
                    var unescapedJson = JsonSerializer.Deserialize<string>(jsonData);
                    
                    // 验证内层字符串是否为有效JSON
                    if (!string.IsNullOrEmpty(unescapedJson) && 
                        (unescapedJson.TrimStart().StartsWith("{") || unescapedJson.TrimStart().StartsWith("[")))
                    {
                        // 返回解码后的JSON字符串
                        return unescapedJson;
                    }
                }

                // 如果不是双重编码或解析失败，返回原字符串
                return jsonData;
            }
            catch
            {
                // 如果处理失败，返回原字符串
                return jsonData;
            }
        }
    }
}
