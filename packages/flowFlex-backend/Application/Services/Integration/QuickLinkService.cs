using System.Text.RegularExpressions;
using System.Text.Json;
using AutoMapper;
using Domain.Shared.Enums;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Quick link service implementation
    /// </summary>
    public class QuickLinkService : IQuickLinkService, IScopedService
    {
        private readonly IQuickLinkRepository _quickLinkRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IStageRepository _stageRepository;
        private readonly ISqlSugarClient _db;
        private readonly IDistributedCache _cache;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<QuickLinkService> _logger;

        public QuickLinkService(
            IQuickLinkRepository quickLinkRepository,
            IIntegrationRepository integrationRepository,
            IStageRepository stageRepository,
            ISqlSugarClient db,
            IDistributedCache cache,
            IMapper mapper,
            UserContext userContext,
            ILogger<QuickLinkService> logger)
        {
            _quickLinkRepository = quickLinkRepository;
            _integrationRepository = integrationRepository;
            _stageRepository = stageRepository;
            _db = db;
            _cache = cache;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(QuickLinkInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate integration exists
            var integration = await _integrationRepository.GetByIdAsync(input.IntegrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Validate quick link label uniqueness
            if (await _quickLinkRepository.ExistsLabelAsync(input.IntegrationId, input.LinkName))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Quick link with name '{input.LinkName}' already exists");
            }

            var entity = _mapper.Map<QuickLink>(input);
            entity.UrlParameters = JsonConvert.SerializeObject(input.UrlParameters);
            entity.InitCreateInfo(_userContext);

            var id = await _quickLinkRepository.InsertReturnSnowflakeIdAsync(entity);

            _logger.LogInformation("Created quick link: {LinkName} (ID: {Id})", input.LinkName, id);

            return id;
        }

        public async Task<bool> UpdateAsync(long id, QuickLinkInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _quickLinkRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Quick link not found");
            }

            // Validate quick link label uniqueness (excluding current entity)
            if (await _quickLinkRepository.ExistsLabelAsync(input.IntegrationId, input.LinkName, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Quick link with name '{input.LinkName}' already exists");
            }

            entity.LinkName = input.LinkName;
            entity.Description = input.Description;
            entity.TargetUrl = input.TargetUrl;
            entity.UrlParameters = JsonConvert.SerializeObject(input.UrlParameters);
            entity.DisplayIcon = input.DisplayIcon;
            entity.RedirectType = input.RedirectType;
            entity.IsActive = input.IsActive;
            entity.SortOrder = input.SortOrder;
            entity.InitModifyInfo(_userContext);

            var result = await _quickLinkRepository.UpdateAsync(entity);

            _logger.LogInformation("Updated quick link: {LinkName} (ID: {Id})", input.LinkName, id);

            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _quickLinkRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Quick link not found");
            }

            // Soft delete
            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _quickLinkRepository.UpdateAsync(entity);

            // Cascade cleanup: remove from Stage ComponentsJson
            try
            {
                await CleanupStageComponentsAsync(id, entity.LinkName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup stage components for deleted quick link {Id}", id);
            }

            _logger.LogInformation("Deleted quick link: {LinkName} (ID: {Id})", entity.LinkName, id);

            return result;
        }

        private async Task CleanupStageComponentsAsync(long quickLinkId, string quickLinkName)
        {
            var idStr = quickLinkId.ToString();
            var allStages = await _stageRepository.GetListAsync(s => !string.IsNullOrEmpty(s.ComponentsJson) && s.ComponentsJson.Contains(idStr));

            if (!allStages.Any()) return;

            foreach (var stage in allStages)
            {
                try
                {
                    var components = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(stage.ComponentsJson);
                    if (components == null) continue;

                    var modified = false;
                    var updatedComponents = new List<System.Text.Json.JsonElement>();

                    foreach (var component in components)
                    {
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(component.GetRawText());
                        if (dict == null) { updatedComponents.Add(component); continue; }

                        if (dict.TryGetValue("QuickLinkIds", out var idsElement) && idsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var ids = idsElement.EnumerateArray().Select(e => e.GetInt64()).ToList();
                            if (ids.Contains(quickLinkId))
                            {
                                ids.Remove(quickLinkId);
                                dict["QuickLinkIds"] = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(System.Text.Json.JsonSerializer.Serialize(ids));

                                if (dict.TryGetValue("QuickLinkNames", out var namesElement) && namesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var names = namesElement.EnumerateArray().Select(e => e.GetString()).Where(n => n != quickLinkName).ToList();
                                    dict["QuickLinkNames"] = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(System.Text.Json.JsonSerializer.Serialize(names));
                                }

                                modified = true;
                            }
                        }

                        updatedComponents.Add(System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(System.Text.Json.JsonSerializer.Serialize(dict)));
                    }

                    if (modified)
                    {
                        stage.ComponentsJson = System.Text.Json.JsonSerializer.Serialize(updatedComponents);
                        await _stageRepository.UpdateAsync(stage);
                        await _cache.RemoveAsync($"ow:stage:workflow:{stage.WorkflowId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup ComponentsJson for stage {StageId}", stage.Id);
                }
            }
        }

        public async Task<QuickLinkOutputDto> GetByIdAsync(long id)
        {
            var entity = await _quickLinkRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Quick link not found");
            }

            var dto = _mapper.Map<QuickLinkOutputDto>(entity);
            dto.UrlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(entity.UrlParameters)
                ?? new List<UrlParameterDto>();

            return dto;
        }

        public async Task<List<QuickLinkOutputDto>> GetByIntegrationIdAsync(long integrationId)
        {
            var entities = await _quickLinkRepository.GetByIntegrationIdAsync(integrationId);
            var dtos = _mapper.Map<List<QuickLinkOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.UrlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(entity.UrlParameters)
                        ?? new List<UrlParameterDto>();
                }
            }

            return dtos;
        }

        public async Task<List<QuickLinkOutputDto>> GetAllAsync()
        {
            var entities = await _quickLinkRepository.GetAllAsync();
            var dtos = _mapper.Map<List<QuickLinkOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.UrlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(entity.UrlParameters)
                        ?? new List<UrlParameterDto>();
                }
            }

            return dtos;
        }

        public async Task<List<QuickLinkOutputDto>> GetByIdsAsync(List<long> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<QuickLinkOutputDto>();
            }

            var entities = await _quickLinkRepository.GetListAsync(x => ids.Contains(x.Id));
            var dtos = _mapper.Map<List<QuickLinkOutputDto>>(entities);

            foreach (var dto in dtos)
            {
                var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.UrlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(entity.UrlParameters)
                        ?? new List<UrlParameterDto>();
                }
            }

            return dtos;
        }

        public async Task<string> GenerateUrlAsync(long quickLinkId, Dictionary<string, string> dataContext)
        {
            var entity = await _quickLinkRepository.GetByIdAsync(quickLinkId);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Quick link not found");
            }

            var urlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(entity.UrlParameters)
                ?? new List<UrlParameterDto>();

            var url = entity.TargetUrl;

            // Replace placeholders with actual values
            foreach (var param in urlParameters)
            {
                var value = GetParameterValue(param, dataContext);

                // Replace placeholder in URL
                var placeholder = $"{{{param.Name}}}";
                url = url.Replace(placeholder, Uri.EscapeDataString(value));
            }

            _logger.LogInformation("Generated URL for quick link {LinkName}: {Url}", entity.LinkName, url);

            return url;
        }

        /// <summary>
        /// Get parameter value based on ValueSource type
        /// 根据值来源类型获取参数值
        /// </summary>
        private string GetParameterValue(UrlParameterDto param, Dictionary<string, string> dataContext)
        {
            var valueDetail = param.ValueDetail ?? string.Empty;

            return param.ValueSource switch
            {
                UrlParameterValueSource.PageParameter =>
                    dataContext.TryGetValue(valueDetail, out var contextValue) ? contextValue : string.Empty,

                UrlParameterValueSource.LoginUserInfo =>
                    GetUserInfoValue(valueDetail),

                UrlParameterValueSource.FixedValue =>
                    valueDetail,

                UrlParameterValueSource.SystemVariable =>
                    GetSystemVariableValue(valueDetail),

                _ => string.Empty
            };
        }

        /// <summary>
        /// Get value from current user context
        /// 从当前用户上下文获取值
        /// </summary>
        private string GetUserInfoValue(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName) || _userContext == null)
                return string.Empty;

            return fieldName.ToLower() switch
            {
                "userid" => _userContext.UserId ?? string.Empty,
                "username" => _userContext.UserName ?? string.Empty,
                "email" => _userContext.Email ?? string.Empty,
                "firstname" => _userContext.FirstName ?? string.Empty,
                "companyid" => _userContext.CompanyId ?? string.Empty,
                "tenantid" => _userContext.TenantId ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Get system variable value
        /// 获取系统变量值
        /// </summary>
        private string GetSystemVariableValue(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
                return string.Empty;

            return variableName.ToLower() switch
            {
                "currentdate" => DateTime.Now.ToString("yyyy-MM-dd"),
                "currenttime" => DateTime.Now.ToString("HH:mm:ss"),
                "currentdatetime" => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "currenttimestamp" => DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                "currentyear" => DateTime.Now.Year.ToString(),
                "currentmonth" => DateTime.Now.Month.ToString(),
                _ => string.Empty
            };
        }
    }
}

