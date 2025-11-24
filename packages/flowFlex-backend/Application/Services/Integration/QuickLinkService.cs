using System.Text.RegularExpressions;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Quick link service implementation
    /// </summary>
    public class QuickLinkService : IQuickLinkService, IScopedService
    {
        private readonly IQuickLinkRepository _quickLinkRepository;
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<QuickLinkService> _logger;

        public QuickLinkService(
            IQuickLinkRepository quickLinkRepository,
            IIntegrationRepository integrationRepository,
            IMapper mapper,
            UserContext userContext,
            ILogger<QuickLinkService> logger)
        {
            _quickLinkRepository = quickLinkRepository;
            _integrationRepository = integrationRepository;
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

            _logger.LogInformation($"Created quick link: {input.LinkName} (ID: {id})");

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

            _logger.LogInformation($"Updated quick link: {input.LinkName} (ID: {id})");

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

            _logger.LogInformation($"Deleted quick link: {entity.LinkName} (ID: {id})");

            return result;
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

            return dtos.OrderBy(d => d.SortOrder).ToList();
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
                var value = string.Empty;

                // Try to get value from data context
                if (dataContext.TryGetValue(param.SourceField, out var contextValue))
                {
                    value = contextValue;
                }
                else if (!string.IsNullOrEmpty(param.DefaultValue))
                {
                    value = param.DefaultValue;
                }

                // Replace placeholder in URL
                var placeholder = $"{{{param.Name}}}";
                url = url.Replace(placeholder, Uri.EscapeDataString(value));
            }

            _logger.LogInformation($"Generated URL for quick link {entity.LinkName}: {url}");

            return url;
        }
    }
}

