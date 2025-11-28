using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Repository.Action;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Domain.Shared.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// Integration service implementation
    /// </summary>
    public class IntegrationService : IIntegrationService, IScopedService
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IEntityMappingRepository _entityMappingRepository;
        private readonly IInboundFieldMappingRepository _fieldMappingRepository;
        private readonly IQuickLinkRepository _quickLinkRepository;
        private readonly IActionDefinitionRepository _actionDefinitionRepository;
        private readonly IActionTriggerMappingRepository _actionTriggerMappingRepository;
        private readonly ISqlSugarClient _sqlSugarClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<IntegrationService> _logger;

        // AES encryption key (should be moved to configuration in production)
        private const string ENCRYPTION_KEY = "FlowFlex2024IntegrationKey123456"; // 32 bytes for AES-256

        public IntegrationService(
            IIntegrationRepository integrationRepository,
            IEntityMappingRepository entityMappingRepository,
            IInboundFieldMappingRepository fieldMappingRepository,
            IQuickLinkRepository quickLinkRepository,
            IActionDefinitionRepository actionDefinitionRepository,
            IActionTriggerMappingRepository actionTriggerMappingRepository,
            ISqlSugarClient sqlSugarClient,
            IHttpClientFactory httpClientFactory,
            IMapper mapper,
            UserContext userContext,
            ILogger<IntegrationService> logger)
        {
            _integrationRepository = integrationRepository;
            _entityMappingRepository = entityMappingRepository;
            _fieldMappingRepository = fieldMappingRepository;
            _quickLinkRepository = quickLinkRepository;
            _actionDefinitionRepository = actionDefinitionRepository;
            _actionTriggerMappingRepository = actionTriggerMappingRepository;
            _sqlSugarClient = sqlSugarClient;
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<long> CreateAsync(IntegrationInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate name uniqueness
            if (await _integrationRepository.ExistsNameAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Integration name '{input.Name}' already exists");
            }

            var entity = _mapper.Map<Domain.Entities.Integration.Integration>(input);

            // Encrypt credentials
            entity.EncryptedCredentials = EncryptCredentials(input.Credentials);

            // Initialize create information
            entity.InitCreateInfo(_userContext);
            entity.TenantId = _userContext.TenantId;

            var id = await _integrationRepository.InsertReturnSnowflakeIdAsync(entity);

            _logger.LogInformation($"Created integration: {input.Name} (ID: {id})");

            return id;
        }

        public async Task<bool> UpdateAsync(long id, IntegrationInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Validate name uniqueness only if name has changed (excluding current entity)
            if (entity.Name != input.Name && await _integrationRepository.ExistsNameAsync(input.Name, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Integration name '{input.Name}' already exists");
            }

            // Update properties
            entity.Type = input.Type;
            entity.Name = input.Name;
            entity.Description = input.Description;
            entity.SystemName = input.SystemName;
            entity.EndpointUrl = input.EndpointUrl;
            entity.AuthMethod = input.AuthMethod;
            entity.Status = input.Status;

            // Encrypt credentials
            entity.EncryptedCredentials = EncryptCredentials(input.Credentials);

            // Update modify information
            entity.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(entity);

            _logger.LogInformation($"Updated integration: {input.Name} (ID: {id})");

            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Soft delete by setting IsValid to false
            entity.IsValid = false;
            entity.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(entity);

            _logger.LogInformation($"Deleted integration: {entity.Name} (ID: {id})");

            return result;
        }

        public async Task<IntegrationOutputDto> GetByIdAsync(long id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var dto = _mapper.Map<IntegrationOutputDto>(entity);
            await PopulateConfiguredEntityTypeNamesAsync(dto);

            // Decrypt credentials
            if (!string.IsNullOrEmpty(entity.EncryptedCredentials) && entity.EncryptedCredentials != "{}")
            {
                try
                {
                    var decryptedJson = DecryptString(entity.EncryptedCredentials, ENCRYPTION_KEY);
                    _logger.LogDebug($"Decrypted JSON for integration {id}: {decryptedJson}");

                    if (!string.IsNullOrEmpty(decryptedJson) && decryptedJson != "{}")
                    {
                        dto.Credentials = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedJson) ?? new Dictionary<string, string>();
                        _logger.LogDebug($"Successfully decrypted credentials for integration {id}, found {dto.Credentials.Count} credential keys: {string.Join(", ", dto.Credentials.Keys)}");
                    }
                    else
                    {
                        _logger.LogDebug($"Decrypted JSON is empty or '{{}}' for integration {id}");
                        dto.Credentials = new Dictionary<string, string>();
                    }
                }
                catch (Exception ex)
                {
                    var preview = entity.EncryptedCredentials != null && entity.EncryptedCredentials.Length > 50
                        ? entity.EncryptedCredentials.Substring(0, 50)
                        : entity.EncryptedCredentials ?? "";
                    _logger.LogWarning(ex, $"Failed to decrypt credentials for integration {id}. EncryptedCredentials: {preview}...");
                    dto.Credentials = new Dictionary<string, string>();
                }
            }
            else
            {
                _logger.LogDebug($"Integration {id} has no encrypted credentials (empty or '{{}}')");
                dto.Credentials = new Dictionary<string, string>();
            }

            return dto;
        }

        public async Task<IntegrationOutputDto> GetWithDetailsAsync(long id)
        {
            var entity = await _integrationRepository.GetWithDetailsAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var dto = _mapper.Map<IntegrationOutputDto>(entity);
            await PopulateConfiguredEntityTypeNamesAsync(dto);

            // Decrypt credentials for details view
            if (!string.IsNullOrEmpty(entity.EncryptedCredentials) && entity.EncryptedCredentials != "{}")
            {
                try
                {
                    var decryptedJson = DecryptString(entity.EncryptedCredentials, ENCRYPTION_KEY);
                    _logger.LogDebug($"Decrypted JSON for integration {id}: {decryptedJson}");

                    if (!string.IsNullOrEmpty(decryptedJson) && decryptedJson != "{}")
                    {
                        dto.Credentials = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedJson) ?? new Dictionary<string, string>();
                        _logger.LogDebug($"Successfully decrypted credentials for integration {id}, found {dto.Credentials.Count} credential keys: {string.Join(", ", dto.Credentials.Keys)}");
                    }
                    else
                    {
                        _logger.LogDebug($"Decrypted JSON is empty or '{{}}' for integration {id}");
                        dto.Credentials = new Dictionary<string, string>();
                    }
                }
                catch (Exception ex)
                {
                    var preview = entity.EncryptedCredentials != null && entity.EncryptedCredentials.Length > 50
                        ? entity.EncryptedCredentials.Substring(0, 50)
                        : entity.EncryptedCredentials ?? "";
                    _logger.LogWarning(ex, $"Failed to decrypt credentials for integration {id}. EncryptedCredentials: {preview}...");
                    dto.Credentials = new Dictionary<string, string>();
                }
            }
            else
            {
                _logger.LogDebug($"Integration {id} has no encrypted credentials (empty or '{{}}')");
                dto.Credentials = new Dictionary<string, string>();
            }

            // Populate connection configuration
            dto.Connection = new ConnectionConfigDto
            {
                Status = entity.Status,
                LastSyncDate = entity.LastSyncDate,
                ErrorMessage = entity.ErrorMessage
            };

            // Populate entity mappings
            if (entity.EntityMappings != null && entity.EntityMappings.Any())
            {
                dto.EntityMappings = _mapper.Map<List<EntityMappingOutputDto>>(entity.EntityMappings);
                foreach (var mappingDto in dto.EntityMappings)
                {
                    var mapping = entity.EntityMappings.FirstOrDefault(em => em.Id == mappingDto.Id);
                    if (mapping != null)
                    {
                        mappingDto.WorkflowIds = JsonConvert.DeserializeObject<List<long>>(mapping.WorkflowIds) ?? new List<long>();
                    }
                }
            }

            // Populate inbound attachments from new field
            if (!string.IsNullOrEmpty(entity.InboundAttachments))
            {
                try
                {
                    dto.InboundAttachments = JsonConvert.DeserializeObject<List<InboundAttachmentItemDto>>(entity.InboundAttachments) ?? new List<InboundAttachmentItemDto>();
                }
                catch
                {
                    dto.InboundAttachments = new List<InboundAttachmentItemDto>();
                }
            }

            // Populate outbound attachments from new field
            if (!string.IsNullOrEmpty(entity.OutboundAttachments))
            {
                try
                {
                    dto.OutboundAttachments = JsonConvert.DeserializeObject<List<OutboundAttachmentItemDto>>(entity.OutboundAttachments) ?? new List<OutboundAttachmentItemDto>();
                }
                catch
                {
                    dto.OutboundAttachments = new List<OutboundAttachmentItemDto>();
                }
            }

            // Populate quick links
            if (entity.QuickLinks != null && entity.QuickLinks.Any())
            {
                dto.QuickLinks = _mapper.Map<List<QuickLinkOutputDto>>(entity.QuickLinks);
                foreach (var quickLinkDto in dto.QuickLinks)
                {
                    var quickLink = entity.QuickLinks.FirstOrDefault(ql => ql.Id == quickLinkDto.Id);
                    if (quickLink != null)
                    {
                        quickLinkDto.UrlParameters = JsonConvert.DeserializeObject<List<UrlParameterDto>>(quickLink.UrlParameters) ?? new List<UrlParameterDto>();
                    }
                }
            }

            // Get field mappings through Action trigger mappings
            // Find Actions that are triggered by this Integration (TriggerType = 'Integration', TriggerSourceId = IntegrationId)
            try
            {
                var integrationTriggerMappings = await _actionTriggerMappingRepository.GetByTriggerTypeAsync("Integration");
                var relatedMappings = integrationTriggerMappings
                    .Where(tm => tm.TriggerSourceId == id && tm.IsValid)
                    .ToList();

                if (relatedMappings.Any())
                {
                    var actionIds = relatedMappings.Select(tm => tm.ActionDefinitionId).Distinct().ToList();
                    var inboundMappings = new List<ActionFieldMappingDto>();
                    var outboundMappings = new List<ActionFieldMappingDto>();

                    foreach (var actionId in actionIds)
                    {
                        var action = await _actionDefinitionRepository.GetByIdAsync(actionId);
                        if (action == null || !action.IsValid) continue;

                        var fieldMappings = await _fieldMappingRepository.GetByActionIdAsync(actionId);
                        
                        foreach (var fm in fieldMappings)
                        {
                            var mappingDto = new ActionFieldMappingDto
                            {
                                Id = fm.Id,
                                ActionId = fm.ActionId,
                                ActionCode = action.ActionCode,
                                ActionName = action.ActionName,
                                ExternalFieldName = fm.ExternalFieldName,
                                WfeFieldId = fm.WfeFieldId,
                                WfeFieldName = fm.WfeFieldId,
                                FieldType = fm.FieldType.ToString(),
                                SyncDirection = fm.SyncDirection.ToString(),
                                IsRequired = fm.IsRequired,
                                DefaultValue = fm.DefaultValue,
                                SortOrder = fm.SortOrder
                            };

                            // Inbound: ViewOnly or Editable
                            if (fm.SyncDirection == SyncDirection.ViewOnly || fm.SyncDirection == SyncDirection.Editable)
                            {
                                inboundMappings.Add(mappingDto);
                            }

                            // Outbound: OutboundOnly or Editable
                            if (fm.SyncDirection == SyncDirection.OutboundOnly || fm.SyncDirection == SyncDirection.Editable)
                            {
                                outboundMappings.Add(mappingDto);
                            }
                        }
                    }

                    dto.InboundFieldMappings = inboundMappings.OrderBy(m => m.SortOrder).ToList();
                    dto.OutboundFieldMappings = outboundMappings.OrderBy(m => m.SortOrder).ToList();
                }
                else
                {
                    dto.InboundFieldMappings = new List<ActionFieldMappingDto>();
                    dto.OutboundFieldMappings = new List<ActionFieldMappingDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get field mappings through action mapping for integration {id}");
                dto.InboundFieldMappings = new List<ActionFieldMappingDto>();
                dto.OutboundFieldMappings = new List<ActionFieldMappingDto>();
            }

            return dto;
        }

        public async Task<List<IntegrationOutputDto>> GetAllAsync(
            string? name = null,
            string? type = null,
            string? status = null)
        {
            var items = await _integrationRepository.GetAllAsync(name, type, status);

            var dtos = _mapper.Map<List<IntegrationOutputDto>>(items);

            // Populate ConfiguredEntityTypeNames and LastDaysSeconds for each integration
            foreach (var dto in dtos)
            {
                await PopulateConfiguredEntityTypeNamesAsync(dto);
                dto.LastDaysSeconds = GenerateLastDaysSeconds();
            }

            return dtos;
        }

        public async Task<bool> TestConnectionAsync(long id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            try
            {
                // Decrypt credentials
                var credentials = DecryptCredentials(entity.EncryptedCredentials);

                _logger.LogInformation($"Testing connection for integration: {entity.Name} (ID: {id}), AuthMethod: {entity.AuthMethod}, EndpointUrl: {entity.EndpointUrl}");

                // Perform actual HTTP connection test
                var (success, errorMessage) = await PerformConnectionTestAsync(entity.EndpointUrl, entity.AuthMethod, credentials);

                if (success)
                {
                    // Update status to Connected if test succeeds
                    entity.Status = global::Domain.Shared.Enums.IntegrationStatus.Connected;
                    entity.ErrorMessage = null;
                    entity.LastSyncDate = DateTimeOffset.UtcNow;
                    _logger.LogInformation($"Connection test succeeded for integration: {entity.Name} (ID: {id})");
                }
                else
                {
                    // Update status to Error if test fails
                    entity.Status = global::Domain.Shared.Enums.IntegrationStatus.Error;
                    entity.ErrorMessage = errorMessage;
                    _logger.LogWarning($"Connection test failed for integration: {entity.Name} (ID: {id}), Error: {errorMessage}");
                }

                entity.InitModifyInfo(_userContext);
                await _integrationRepository.UpdateAsync(entity);

                if (!success)
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, $"Connection test failed: {errorMessage}");
                }

                return true;
            }
            catch (CRMException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Connection test failed for integration: {entity.Name} (ID: {id})");

                // Update status to Error if test fails
                entity.Status = global::Domain.Shared.Enums.IntegrationStatus.Error;
                entity.ErrorMessage = ex.Message;
                entity.InitModifyInfo(_userContext);
                await _integrationRepository.UpdateAsync(entity);

                throw new CRMException(ErrorCodeEnum.BusinessError, $"Connection test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Perform actual HTTP connection test based on authentication method
        /// </summary>
        private async Task<(bool Success, string? ErrorMessage)> PerformConnectionTestAsync(
            string endpointUrl,
            AuthenticationMethod authMethod,
            Dictionary<string, string>? credentials)
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                return (false, "Endpoint URL is required");
            }

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                HttpRequestMessage request;

                switch (authMethod)
                {
                    case AuthenticationMethod.ApiKey:
                        // API Key - Send GET request with API key in header
                        request = new HttpRequestMessage(HttpMethod.Get, endpointUrl);
                        if (credentials != null && credentials.TryGetValue("apiKey", out var apiKey))
                        {
                            request.Headers.Add("X-API-Key", apiKey);
                            request.Headers.Add("Authorization", $"ApiKey {apiKey}");
                        }
                        break;

                    case AuthenticationMethod.BasicAuth:
                        // Basic Auth - For OAuth token endpoint, use POST with form data
                        if (endpointUrl.Contains("/oauth2/token") || endpointUrl.Contains("/connect/token"))
                        {
                            request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
                            var formData = new Dictionary<string, string>
                            {
                                { "grant_type", "password" }
                            };
                            if (credentials != null)
                            {
                                if (credentials.TryGetValue("username", out var username))
                                    formData["username"] = username;
                                if (credentials.TryGetValue("password", out var password))
                                    formData["password"] = password;
                            }
                            request.Content = new FormUrlEncodedContent(formData);
                        }
                        else
                        {
                            // Regular Basic Auth - GET request with Authorization header
                            request = new HttpRequestMessage(HttpMethod.Get, endpointUrl);
                            if (credentials != null &&
                                credentials.TryGetValue("username", out var username) &&
                                credentials.TryGetValue("password", out var password))
                            {
                                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                            }
                        }
                        break;

                    case AuthenticationMethod.OAuth2:
                        // OAuth 2.0 - POST to token endpoint with client credentials
                        request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
                        var oauthFormData = new Dictionary<string, string>
                        {
                            { "grant_type", "client_credentials" }
                        };
                        if (credentials != null)
                        {
                            if (credentials.TryGetValue("clientId", out var clientId))
                                oauthFormData["client_id"] = clientId;
                            if (credentials.TryGetValue("clientSecret", out var clientSecret))
                                oauthFormData["client_secret"] = clientSecret;
                        }
                        request.Content = new FormUrlEncodedContent(oauthFormData);
                        break;

                    case AuthenticationMethod.BearerToken:
                        // Bearer Token - GET request with Authorization header
                        request = new HttpRequestMessage(HttpMethod.Get, endpointUrl);
                        if (credentials != null && credentials.TryGetValue("token", out var token))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }
                        break;

                    default:
                        return (false, $"Unsupported authentication method: {authMethod}");
                }

                // Add common headers
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("User-Agent", "FlowFlex-Integration/1.0");

                _logger.LogDebug($"Sending {request.Method} request to {endpointUrl} with AuthMethod: {authMethod}");

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug($"Response status: {response.StatusCode}, Content length: {responseContent.Length}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    // Some APIs return 401/403 which might still indicate the endpoint is reachable
                    var statusCode = (int)response.StatusCode;
                    var errorDetail = responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent;
                    return (false, $"HTTP {statusCode}: {response.ReasonPhrase}. Response: {errorDetail}");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, $"HTTP request failed: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "Connection timeout (30 seconds)");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<bool> UpdateStatusAsync(long id, global::Domain.Shared.Enums.IntegrationStatus status)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            entity.Status = status;
            entity.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(entity);

            _logger.LogInformation($"Updated integration status: {entity.Name} (ID: {id}) to {status}");

            return result;
        }

        #region Private Methods

        /// <summary>
        /// Generate last 30 days random seconds statistics
        /// </summary>
        private Dictionary<string, string> GenerateLastDaysSeconds()
        {
            var result = new Dictionary<string, string>();
            var random = new Random();
            var today = DateTime.Today;

            // Generate data for the last 30 days
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dateKey = date.ToString("yyyy-MM-dd");
                var randomValue = random.Next(0, 1001).ToString(); // 0-1000
                result[dateKey] = randomValue;
            }

            return result;
        }

        private string EncryptCredentials(Dictionary<string, string> credentials)
        {
            var json = JsonConvert.SerializeObject(credentials);
            var encrypted = EncryptString(json, ENCRYPTION_KEY);
            return encrypted;
        }

        private Dictionary<string, string> DecryptCredentials(string encryptedCredentials)
        {
            var json = DecryptString(encryptedCredentials, ENCRYPTION_KEY);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }

        private string EncryptString(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // Use zero IV for simplicity (should use random IV in production)

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private string DecryptString(string cipherText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // Use zero IV for simplicity (should use random IV in production)

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        #endregion

        /// <summary>
        /// Get integrations by type
        /// </summary>
        public async Task<List<IntegrationOutputDto>> GetByTypeAsync(string type)
        {
            var entities = await _integrationRepository.GetByTypeAsync(type);
            return _mapper.Map<List<IntegrationOutputDto>>(entities);
        }

        /// <summary>
        /// Get all active (connected) integrations
        /// </summary>
        public async Task<List<IntegrationOutputDto>> GetActiveIntegrationsAsync()
        {
            var entities = await _integrationRepository.GetActiveIntegrationsAsync();
            return _mapper.Map<List<IntegrationOutputDto>>(entities);
        }

        /// <summary>
        /// Get inbound field mappings by action ID (read-only view)
        /// </summary>
        public async Task<List<InboundFieldMappingDto>> GetInboundFieldMappingsByActionAsync(
            long actionId,
            string? externalFieldName = null,
            string? wfeFieldName = null)
        {
            var actionName = await GetActionNameAsync(actionId);
            var result = new List<InboundFieldMappingDto>();

            // Get field mappings by action ID
            var fieldMappings = await _fieldMappingRepository.GetByActionIdAsync(actionId);

            // Filter for inbound fields (ViewOnly or Editable)
            var inboundFields = fieldMappings.Where(fm =>
                fm.SyncDirection == SyncDirection.ViewOnly ||
                fm.SyncDirection == SyncDirection.Editable);

            foreach (var fieldMapping in inboundFields)
            {
                // Apply filters if provided
                if (!string.IsNullOrEmpty(externalFieldName) &&
                    !fieldMapping.ExternalFieldName.Contains(externalFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(wfeFieldName) &&
                    !fieldMapping.WfeFieldId.Contains(wfeFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new InboundFieldMappingDto
                {
                    ActionId = actionId,
                    ActionName = actionName,
                    ExternalFieldName = fieldMapping.ExternalFieldName,
                    WfeFieldId = fieldMapping.WfeFieldId,
                    WfeFieldName = fieldMapping.WfeFieldId // Use WfeFieldId as display name for now
                });
            }

            return result.OrderBy(r => r.ExternalFieldName).ToList();
        }

        /// <summary>
        /// Get outbound shared fields by action ID (read-only view)
        /// Uses FieldMapping table with OutboundOnly or Editable sync direction
        /// </summary>
        public async Task<List<OutboundSharedFieldDto>> GetOutboundSharedFieldsByActionAsync(
            long actionId,
            string? fieldName = null)
        {
            var actionName = await GetActionNameAsync(actionId);
            var result = new List<OutboundSharedFieldDto>();

            // Get field mappings by action ID
            var fieldMappings = await _fieldMappingRepository.GetByActionIdAsync(actionId);

            // Filter for outbound fields (OutboundOnly or Editable)
            var outboundFields = fieldMappings.Where(fm =>
                fm.SyncDirection == SyncDirection.OutboundOnly ||
                fm.SyncDirection == SyncDirection.Editable);

            foreach (var fieldMapping in outboundFields)
            {
                // Apply filter if provided
                if (!string.IsNullOrEmpty(fieldName) &&
                    !fieldMapping.WfeFieldId.Contains(fieldName, StringComparison.OrdinalIgnoreCase) &&
                    !fieldMapping.ExternalFieldName.Contains(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new OutboundSharedFieldDto
                {
                    ActionId = actionId,
                    ActionName = actionName,
                    FieldDisplayName = fieldMapping.WfeFieldId, // Use WfeFieldId as display name
                    FieldApiName = fieldMapping.WfeFieldId
                });
            }

            return result.OrderBy(r => r.FieldApiName).ToList();
        }

        /// <summary>
        /// Populate ConfiguredEntityTypeNames and ConfiguredEntityTypes for IntegrationOutputDto
        /// </summary>
        private async Task PopulateConfiguredEntityTypeNamesAsync(IntegrationOutputDto dto)
        {
            try
            {
                var entityMappings = await _entityMappingRepository.GetByIntegrationIdAsync(dto.Id);
                dto.ConfiguredEntityTypeNames = entityMappings
                    .Where(em => em.IsActive && em.IsValid)
                    .Select(em => em.ExternalEntityType)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                // Update ConfiguredEntityTypes to match the count of ConfiguredEntityTypeNames
                dto.ConfiguredEntityTypes = dto.ConfiguredEntityTypeNames.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to populate ConfiguredEntityTypeNames for integration {dto.Id}");
                dto.ConfiguredEntityTypeNames = new List<string>();
                dto.ConfiguredEntityTypes = 0;
            }
        }

        /// <summary>
        /// Get action name by action ID
        /// </summary>
        private async Task<string> GetActionNameAsync(long actionId)
        {
            try
            {
                // Try to get action definition by ID
                var actionDefinition = await _actionDefinitionRepository.GetByIdAsync(actionId);
                if (actionDefinition != null && !string.IsNullOrEmpty(actionDefinition.ActionName))
                {
                    return actionDefinition.ActionName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get action name for action ID {ActionId}", actionId);
            }

            // Fallback: return formatted action ID
            return $"Action {actionId}";
        }

        /// <summary>
        /// Get inbound attachments configuration
        /// </summary>
        public async Task<InboundAttachmentsOutputDto> GetInboundAttachmentsAsync(long integrationId)
        {
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var result = new InboundAttachmentsOutputDto
            {
                IntegrationId = integrationId,
                Items = new List<InboundAttachmentItemDto>()
            };

            // Parse inbound attachments from JSON
            if (!string.IsNullOrEmpty(integration.InboundAttachments))
            {
                try
                {
                    result.Items = JsonConvert.DeserializeObject<List<InboundAttachmentItemDto>>(integration.InboundAttachments) ?? new List<InboundAttachmentItemDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse InboundAttachments for integration {IntegrationId}", integrationId);
                    result.Items = new List<InboundAttachmentItemDto>();
                }
            }

            return result;
        }

        /// <summary>
        /// Save inbound attachments configuration
        /// </summary>
        public async Task<bool> SaveInboundAttachmentsAsync(long integrationId, InboundAttachmentsInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Ensure all items have an ID (using Snowflake ID)
            var items = input.Items ?? new List<InboundAttachmentItemDto>();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Id))
                {
                    item.Id = SnowFlakeSingle.Instance.NextId().ToString();
                }
            }

            // Serialize inbound attachments to JSON
            integration.InboundAttachments = JsonConvert.SerializeObject(items);
            integration.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(integration);

            _logger.LogInformation("Updated inbound attachments for integration {IntegrationId}: {Items}",
                integrationId, integration.InboundAttachments);

            return result;
        }

        /// <summary>
        /// Get outbound attachments configuration
        /// </summary>
        public async Task<OutboundAttachmentsOutputDto> GetOutboundAttachmentsAsync(long integrationId)
        {
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            var result = new OutboundAttachmentsOutputDto
            {
                IntegrationId = integrationId,
                Items = new List<OutboundAttachmentItemDto>()
            };

            // Parse outbound attachments from JSON
            if (!string.IsNullOrEmpty(integration.OutboundAttachments))
            {
                try
                {
                    result.Items = JsonConvert.DeserializeObject<List<OutboundAttachmentItemDto>>(integration.OutboundAttachments) ?? new List<OutboundAttachmentItemDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse OutboundAttachments for integration {IntegrationId}", integrationId);
                    result.Items = new List<OutboundAttachmentItemDto>();
                }
            }

            return result;
        }

        /// <summary>
        /// Save outbound attachments configuration
        /// </summary>
        public async Task<bool> SaveOutboundAttachmentsAsync(long integrationId, OutboundAttachmentsInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, "Integration not found");
            }

            // Ensure all items have an ID (using Snowflake ID)
            var items = input.Items ?? new List<OutboundAttachmentItemDto>();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Id))
                {
                    item.Id = SnowFlakeSingle.Instance.NextId().ToString();
                }
            }

            // Serialize outbound attachments to JSON
            integration.OutboundAttachments = JsonConvert.SerializeObject(items);
            integration.InitModifyInfo(_userContext);

            var result = await _integrationRepository.UpdateAsync(integration);

            _logger.LogInformation("Updated outbound attachments for integration {IntegrationId}: {Items}",
                integrationId, integration.OutboundAttachments);

            return result;
        }
    }
}
