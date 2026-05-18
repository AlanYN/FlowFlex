using FlowFlex.Application.Contracts.Dtos.OW.PluginPriceList;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using FlowFlex.SqlSugarDB.Context;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    public class PluginPriceListService : IPluginPriceListService, IScopedService
    {
        private readonly IPluginPriceListRepository _repository;
        private readonly ISqlSugarClient _db;
        private readonly IPermissionService _permissionService;
        private readonly UserContext _userContext;
        private readonly ILogger<PluginPriceListService> _logger;

        public PluginPriceListService(
            IPluginPriceListRepository repository,
            ISqlSugarContext sqlSugarContext,
            IPermissionService permissionService,
            UserContext userContext,
            ILogger<PluginPriceListService> logger)
        {
            _repository = repository;
            _db = sqlSugarContext.GetDbClient();
            _permissionService = permissionService;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<PluginPriceListOutputDto> GetAsync(string caseCode)
        {
            var userId = ParseUserId();
            var onboarding = await GetOnboardingByCaseCodeAsync(caseCode);
            if (onboarding == null)
            {
                throw new Exception($"Onboarding not found for CaseCode: {caseCode}");
            }

            var actualCaseCode = onboarding.CaseCode ?? caseCode;

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userId, onboarding.Id, OperationTypeEnum.View);

            if (!permissionResult.CanView)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Access denied - no view permission on this case");
            }

            var permission = permissionResult.CanOperate ? "write" : "read";

            var entity = await _repository.GetByCaseCodeAsync(actualCaseCode);
            if (entity == null)
            {
                return new PluginPriceListOutputDto
                {
                    CaseCode = actualCaseCode,
                    Permission = permission,
                    Data = null
                };
            }

            return new PluginPriceListOutputDto
            {
                Id = entity.Id,
                CaseCode = entity.CaseCode,
                CustomerCode = entity.CustomerCode,
                CustomerName = entity.CustomerName,
                PriceListType = entity.PriceListType,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Data = string.IsNullOrEmpty(entity.Data) ? null : JsonConvert.DeserializeObject(entity.Data),
                Status = entity.Status,
                Permission = permission,
                CreatedBy = entity.CreateBy,
                CreatedAt = entity.CreateDate,
                UpdatedBy = entity.ModifyBy,
                UpdatedAt = entity.ModifyDate
            };
        }

        public async Task<object> SaveAsync(PluginPriceListInputDto input)
        {
            var userId = ParseUserId();
            var onboarding = await GetOnboardingByCaseCodeAsync(input.CaseCode);
            if (onboarding == null)
            {
                throw new Exception($"Onboarding not found for CaseCode: {input.CaseCode}");
            }

            var actualCaseCode = onboarding.CaseCode ?? input.CaseCode;

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userId, onboarding.Id, OperationTypeEnum.Operate);

            if (!permissionResult.CanOperate)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Access denied - write permission required");
            }

            var dataJson = input.Data != null ? JsonConvert.SerializeObject(input.Data) : "{}";

            var existing = await _repository.GetByCaseCodeAsync(actualCaseCode);
            if (existing != null)
            {
                existing.CustomerCode = input.CustomerCode;
                existing.CustomerName = input.CustomerName;
                existing.PriceListType = input.PriceListType ?? existing.PriceListType;
                existing.StartDate = input.StartDate;
                existing.EndDate = input.EndDate;
                existing.Data = dataJson;
                existing.InitUpdateInfo(_userContext);

                await _repository.UpdateAsync(existing);

                return new { id = existing.Id, savedAt = DateTimeOffset.UtcNow };
            }
            else
            {
                var entity = new PluginPriceList
                {
                    CaseCode = actualCaseCode,
                    CustomerCode = input.CustomerCode,
                    CustomerName = input.CustomerName,
                    PriceListType = input.PriceListType ?? "Customer Specific",
                    StartDate = input.StartDate,
                    EndDate = input.EndDate,
                    Data = dataJson,
                    Status = "draft"
                };
                entity.InitCreateInfo(_userContext);

                await _repository.InsertAsync(entity);

                return new { id = entity.Id, savedAt = DateTimeOffset.UtcNow };
            }
        }

        public async Task<object> SubmitAsync(PluginPriceListSubmitDto input)
        {
            var userId = ParseUserId();
            var onboarding = await GetOnboardingByCaseCodeAsync(input.CaseCode);
            if (onboarding == null)
            {
                throw new Exception($"Onboarding not found for CaseCode: {input.CaseCode}");
            }

            var actualCaseCode = onboarding.CaseCode ?? input.CaseCode;

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userId, onboarding.Id, OperationTypeEnum.Operate);

            if (!permissionResult.CanOperate)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Access denied - write permission required");
            }

            var entity = await _repository.GetByCaseCodeAsync(actualCaseCode);
            if (entity == null)
            {
                throw new Exception($"Price list not found for CaseCode: {actualCaseCode}");
            }

            entity.Status = "submitted";
            entity.InitUpdateInfo(_userContext);
            await _repository.UpdateAsync(entity);

            return new { status = "submitted", submittedAt = DateTimeOffset.UtcNow };
        }

        private async Task<Onboarding?> GetOnboardingByCaseCodeAsync(string caseCode)
        {
            // Support both CaseCode (e.g. "C00001") and Onboarding ID (numeric)
            if (long.TryParse(caseCode, out var onboardingId))
            {
                return await _db.Queryable<Onboarding>()
                    .Where(x => (x.Id == onboardingId || x.CaseCode == caseCode) && x.IsValid)
                    .FirstAsync();
            }

            return await _db.Queryable<Onboarding>()
                .Where(x => x.CaseCode == caseCode && x.IsValid)
                .FirstAsync();
        }

        private long ParseUserId()
        {
            if (long.TryParse(_userContext?.UserId, out var id) && id > 0)
                return id;
            throw new UnauthorizedAccessException("Unable to determine user identity.");
        }
    }
}
