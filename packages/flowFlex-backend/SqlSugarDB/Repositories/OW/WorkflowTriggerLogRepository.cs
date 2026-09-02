using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    public class WorkflowTriggerLogRepository
        : BaseRepository<WorkflowTriggerLog>, IWorkflowTriggerLogRepository, IScopedService
    {
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<WorkflowTriggerLogRepository> _logger;

        public WorkflowTriggerLogRepository(
            ISqlSugarClient db,
            IHttpContextAccessor http,
            ILogger<WorkflowTriggerLogRepository> logger) : base(db)
        {
            _http = http;
            _logger = logger;
        }

        private string TenantId =>
            _http.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";

        private string AppCode =>
            _http.HttpContext?.Request.Headers["X-App-Code"].FirstOrDefault() ?? "default";

        public async Task<List<WorkflowTriggerLog>> GetBySourceOnboardingIdAsync(long sourceOnboardingId)
            => await db.Queryable<WorkflowTriggerLog>()
                .Where(x => x.SourceOnboardingId == sourceOnboardingId
                         && x.IsValid == true
                         && x.TenantId == TenantId
                         && x.AppCode == AppCode)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();

        public async Task<List<WorkflowTriggerLog>> GetByTargetOnboardingIdAsync(long targetOnboardingId)
            => await db.Queryable<WorkflowTriggerLog>()
                .Where(x => x.TargetOnboardingId == targetOnboardingId
                         && x.IsValid   == true
                         && x.TenantId  == TenantId
                         && x.AppCode   == AppCode)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();

        public async Task<(List<TriggerLogOutputDto> items, int total)> GetPagedByWorkflowAsync(
            long sourceWorkflowId, int pageIndex, int pageSize, string? status = null)
        {
            var tenantId = TenantId;
            var appCode  = AppCode;

            var query = db.Queryable<WorkflowTriggerLog>()
                .Where(x => x.SourceWorkflowId == sourceWorkflowId
                         && x.IsValid  == true
                         && x.TenantId == tenantId
                         && x.AppCode  == appCode);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status == status);

            var total = await query.CountAsync();

            // Join source and target onboarding to get case name / code
            var logs = await query
                .OrderByDescending(x => x.CreateDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (logs.Count == 0)
                return (new List<TriggerLogOutputDto>(), total);

            // Batch-load onboarding display info for source + target cases
            var sourceIds = logs.Select(l => l.SourceOnboardingId).Distinct().ToList();
            var targetIds = logs
                .Where(l => l.TargetOnboardingId.HasValue)
                .Select(l => l.TargetOnboardingId!.Value)
                .Distinct()
                .ToList();
            var allIds = sourceIds.Concat(targetIds).Distinct().ToList();

            var caseMap = await db.Queryable<Onboarding>()
                .Where(o => allIds.Contains(o.Id) && o.IsValid == true)
                .Select(o => new { o.Id, o.CaseName, o.CaseCode })
                .ToListAsync();
            var caseDict = caseMap.ToDictionary(c => c.Id, c => (c.CaseName, c.CaseCode));

            var dtos = logs.Select(l =>
            {
                caseDict.TryGetValue(l.SourceOnboardingId, out var src);
                string? tgtName = null, tgtCode = null;
                if (l.TargetOnboardingId.HasValue)
                {
                    caseDict.TryGetValue(l.TargetOnboardingId.Value, out var tgt);
                    tgtName = tgt.CaseName;
                    tgtCode = tgt.CaseCode;
                }

                return new TriggerLogOutputDto
                {
                    Id                 = l.Id.ToString(),
                    ConnectionId       = l.ConnectionId.ToString(),
                    SourceWorkflowId   = l.SourceWorkflowId.ToString(),
                    SourceOnboardingId = l.SourceOnboardingId.ToString(),
                    SourceCaseName     = src.CaseName ?? string.Empty,
                    SourceCaseCode     = src.CaseCode ?? string.Empty,
                    TargetWorkflowId   = l.TargetWorkflowId.ToString(),
                    TargetOnboardingId = l.TargetOnboardingId?.ToString(),
                    TargetCaseName     = tgtName,
                    TargetCaseCode     = tgtCode,
                    Status             = l.Status ?? string.Empty,
                    Reason             = l.Reason ?? string.Empty,
                    CompletionType     = l.CompletionType ?? string.Empty,
                    ConditionsSnapshot = l.ConditionsSnapshot ?? string.Empty,
                    MappingsSnapshot   = l.MappingsSnapshot ?? string.Empty,
                    CreateDate         = l.CreateDate,
                    CreateBy           = l.CreateBy ?? string.Empty,
                    TenantId           = l.TenantId ?? string.Empty,
                    AppCode            = l.AppCode  ?? string.Empty,
                };
            }).ToList();

            return (dtos, total);
        }

        public async Task<bool> HasAlreadyTriggeredAsync(long connectionId, long sourceOnboardingId)
        {
            var count = await db.Queryable<WorkflowTriggerLog>()
                .Where(x => x.ConnectionId      == connectionId
                         && x.SourceOnboardingId == sourceOnboardingId
                         && x.Status            == "Triggered"
                         && x.IsValid           == true
                         && x.TenantId          == TenantId
                         && x.AppCode           == AppCode)
                .CountAsync();
            return count > 0;
        }
    }
}
