using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.DynamicData;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Trigger Graph service — CRUD for ff_workflow_trigger_graph + ff_workflow_trigger_connection
    /// and query interfaces for OW-725.
    ///
    /// Pattern: mirrors StageConditionService —
    ///   ISqlSugarClient for explicit tenant-filtered queries,
    ///   TenantContextHelper for isolation,
    ///   IPermissionService for access control.
    /// </summary>
    public class TriggerGraphService : ITriggerGraphService, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IWorkflowTriggerGraphRepository _graphRepo;
        private readonly IWorkflowTriggerConnectionRepository _connRepo;
        private readonly IChecklistTaskRepository _checklistTaskRepo;
        private readonly IPermissionService _permissionService;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<TriggerGraphService> _logger;

        public TriggerGraphService(
            ISqlSugarClient db,
            IWorkflowTriggerGraphRepository graphRepo,
            IWorkflowTriggerConnectionRepository connRepo,
            IChecklistTaskRepository checklistTaskRepo,
            IPermissionService permissionService,
            IMapper mapper,
            UserContext userContext,
            ILogger<TriggerGraphService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _graphRepo = graphRepo ?? throw new ArgumentNullException(nameof(graphRepo));
            _connRepo = connRepo ?? throw new ArgumentNullException(nameof(connRepo));
            _checklistTaskRepo = checklistTaskRepo ?? throw new ArgumentNullException(nameof(checklistTaskRepo));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ─── OW-723: Graph CRUD ──────────────────────────────────────────

        /// <inheritdoc />
        public async Task<TriggerGraphDto> GetByWorkflowIdAsync(long workflowId)
        {
            _logger.LogInformation("[TriggerGraphService] GetByWorkflowIdAsync WorkflowId={WorkflowId}", workflowId);

            await ValidateWorkflowPermissionAsync(workflowId, OperationTypeEnum.View);

            var graph = await _graphRepo.GetByWorkflowIdAsync(workflowId);
            if (graph == null)
            {
                return new TriggerGraphDto
                {
                    Id = 0,
                    WorkflowId = workflowId,   // pass through so frontend knows current workflow
                    Name = string.Empty,
                    CanvasLayout = "{}",
                    CanvasWorkflowIds = "[]",
                    Connections = new List<TriggerConnectionDto>()
                };
            }

            var connections = await _connRepo.GetByGraphIdAsync(graph.Id);
            var dto = _mapper.Map<TriggerGraphDto>(graph);
            dto.WorkflowId = workflowId;   // always reflect the requested workflowId, not the stored 0
            dto.Connections = _mapper.Map<List<TriggerConnectionDto>>(connections);
            return dto;
        }

        /// <inheritdoc />
        public async Task<TriggerGraphDto> SaveAsync(SaveTriggerGraphInput input)
        {
            _logger.LogInformation("[TriggerGraphService] SaveAsync WorkflowId={WorkflowId}", input.WorkflowId);

            await ValidateWorkflowPermissionAsync(input.WorkflowId, OperationTypeEnum.Operate);

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var appCode  = TenantContextHelper.GetAppCodeOrDefault(_userContext);

            var graph = await _graphRepo.GetByWorkflowIdAsync(input.WorkflowId);
            bool isNew = graph == null;

            if (isNew)
            {
                graph = new WorkflowTriggerGraph
                {
                    WorkflowId        = 0,   // 0 = global graph shared by all workflows in this tenant
                    Name              = input.Name,
                    CanvasLayout      = input.CanvasLayout,
                    CanvasWorkflowIds = input.CanvasWorkflowIds,
                    TenantId          = tenantId,
                    AppCode           = appCode,
                };
                await _graphRepo.InsertAsync(graph);
            }
            else
            {
                graph.Name              = input.Name;
                graph.CanvasLayout      = input.CanvasLayout;
                graph.CanvasWorkflowIds = input.CanvasWorkflowIds;
                await _graphRepo.UpdateAsync(graph);
            }

            // Full-replace: soft-delete old connections then insert new ones
            await _connRepo.DeleteByGraphIdAsync(graph.Id);

            var newConnections = new List<WorkflowTriggerConnection>();
            int order = 0;
            foreach (var connDto in input.Connections)
            {
                var conn = _mapper.Map<WorkflowTriggerConnection>(connDto);
                conn.GraphId        = graph.Id;
                conn.ExecutionOrder = order++;
                conn.TenantId       = tenantId;
                conn.AppCode        = appCode;
                newConnections.Add(conn);
            }

            if (newConnections.Count > 0)
                await _connRepo.InsertRangeAsync(newConnections);

            _logger.LogInformation(
                "[TriggerGraphService] Saved graph {GraphId} with {Count} connections",
                graph.Id, newConnections.Count);

            var dto = _mapper.Map<TriggerGraphDto>(graph);
            dto.Connections = _mapper.Map<List<TriggerConnectionDto>>(newConnections);
            return dto;
        }

        // ─── OW-725: Query interfaces ────────────────────────────────────

        /// <inheritdoc />
        public async Task<List<WorkflowOutputDto>> GetAllWorkflowsAsync()
        {
            _logger.LogInformation("[TriggerGraphService] GetAllWorkflowsAsync");

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var appCode  = TenantContextHelper.GetAppCodeOrDefault(_userContext);

            // Use _db directly (same pattern as StageConditionService) to ensure tenant filtering
            var workflows = await _db.Queryable<Workflow>()
                .Where(w => w.IsValid == true
                         && w.TenantId == tenantId
                         && w.AppCode  == appCode)
                .OrderBy(w => w.Name)
                .ToListAsync();

            return _mapper.Map<List<WorkflowOutputDto>>(workflows);
        }

        /// <inheritdoc />
        public async Task<WorkflowNodeInfoDto> GetWorkflowNodeInfoAsync(long workflowId)
        {
            _logger.LogInformation("[TriggerGraphService] GetWorkflowNodeInfoAsync WorkflowId={WorkflowId}", workflowId);

            await ValidateWorkflowPermissionAsync(workflowId, OperationTypeEnum.View);

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var appCode  = TenantContextHelper.GetAppCodeOrDefault(_userContext);

            var workflow = await _db.Queryable<Workflow>()
                .Where(w => w.Id == workflowId
                         && w.IsValid   == true
                         && w.TenantId  == tenantId
                         && w.AppCode   == appCode)
                .FirstAsync();

            if (workflow == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Workflow {workflowId} not found");

            var stages = await _db.Queryable<Stage>()
                .Where(s => s.WorkflowId == workflowId
                         && s.IsValid   == true
                         && s.TenantId  == tenantId
                         && s.AppCode   == appCode)
                .OrderBy(s => s.Order)
                .ToListAsync();

            // ── collect component IDs for batch loading ──
            var allQuestionnaireIds = new List<long>();
            var allChecklistIds     = new List<long>();
            var allFieldIds         = new List<long>();

            foreach (var stage in stages)
            {
                foreach (var comp in ParseComponents(stage.ComponentsJson))
                {
                    if (comp.Key == "questionnaires")
                        allQuestionnaireIds.AddRange(comp.QuestionnaireIds ?? new List<long>());
                    else if (comp.Key == "checklist")
                        allChecklistIds.AddRange(comp.ChecklistIds ?? new List<long>());
                    else if (comp.Key == "fields")
                        allFieldIds.AddRange((comp.StaticFields ?? new List<StaticFieldConfig>())
                            .Select(sf => long.TryParse(sf.Id, out var fid) ? fid : 0L)
                            .Where(id => id != 0));
                }
            }

            var distinctQIds = allQuestionnaireIds.Distinct().ToList();
            var distinctCIds = allChecklistIds.Distinct().ToList();
            var distinctFIds = allFieldIds.Distinct().ToList();

            // ── batch-load with tenant isolation ──
            var questionnaires = distinctQIds.Count > 0
                ? await _db.Queryable<Questionnaire>()
                    .Where(q => distinctQIds.Contains(q.Id)
                             && q.IsValid  == true
                             && q.TenantId == tenantId
                             && q.AppCode  == appCode)
                    .ToListAsync()
                : new List<Questionnaire>();

            var checklists = distinctCIds.Count > 0
                ? await _db.Queryable<Checklist>()
                    .Where(c => distinctCIds.Contains(c.Id)
                             && c.IsValid  == true
                             && c.TenantId == tenantId
                             && c.AppCode  == appCode)
                    .ToListAsync()
                : new List<Checklist>();

            // ChecklistTask does not have its own tenant column — it belongs to a Checklist
            // (already tenant-scoped via its parent) so use the existing repository method
            var checklistTasks = distinctCIds.Count > 0
                ? await _checklistTaskRepo.GetByChecklistIdsAsync(distinctCIds)
                : new List<ChecklistTask>();

            var defineFields = distinctFIds.Count > 0
                ? await _db.Queryable<DefineField>()
                    .Where(f => distinctFIds.Contains(f.Id)
                             && f.IsValid  == true
                             && f.TenantId == tenantId
                             && f.AppCode  == appCode)
                    .ToListAsync()
                : new List<DefineField>();

            // ── build lookup dictionaries ──
            var questionnairesById  = questionnaires.ToDictionary(q => q.Id);
            var checklistsById      = checklists.ToDictionary(c => c.Id);
            var tasksByChecklistId  = checklistTasks
                .GroupBy(t => t.ChecklistId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var fieldsById          = defineFields.ToDictionary(f => f.Id);

            // ── assemble output ──
            var stageNodes = stages.Select(stage =>
            {
                var stageNode = new StageNodeInfoDto
                {
                    Id    = stage.Id,
                    Name  = stage.Name ?? string.Empty,
                    Order = stage.Order,
                };

                foreach (var comp in ParseComponents(stage.ComponentsJson))
                {
                    switch (comp.Key)
                    {
                        case "fields":
                            stageNode.Fields = (comp.StaticFields ?? new List<StaticFieldConfig>())
                                .Select(sf =>
                                {
                                    long.TryParse(sf.Id, out var fid);
                                    fieldsById.TryGetValue(fid, out var df);
                                    return new FieldOptionDto
                                    {
                                        Id        = sf.Id,
                                        Name      = df?.FieldName ?? sf.Id,
                                        FieldType = df?.DataType.ToString() ?? string.Empty
                                    };
                                })
                                .ToList();
                            break;

                        case "questionnaires":
                            stageNode.Questionnaires.AddRange(
                                (comp.QuestionnaireIds ?? new List<long>())
                                    .Where(qId => questionnairesById.ContainsKey(qId))
                                    .Select(qId =>
                                    {
                                        var q = questionnairesById[qId];
                                        return new QuestionnaireNodeDto
                                        {
                                            Id        = q.Id,
                                            Name      = q.Name ?? string.Empty,
                                            Questions = ExtractQuestions(q.Structure?.ToString())
                                        };
                                    })
                            );
                            break;

                        case "checklist":
                            stageNode.Checklists = (comp.ChecklistIds ?? new List<long>())
                                .Select(cId =>
                                {
                                    checklistsById.TryGetValue(cId, out var cl);
                                    tasksByChecklistId.TryGetValue(cId, out var tasks);
                                    return new ChecklistNodeDto
                                    {
                                        Id    = cId,
                                        Name  = cl?.Name ?? string.Empty,
                                        Tasks = (tasks ?? new List<ChecklistTask>())
                                            .OrderBy(t => t.Order)
                                            .Select(t => new TaskOptionDto
                                            {
                                                Id       = t.Id,
                                                Name     = t.Name ?? string.Empty,
                                                TaskType = t.TaskType ?? string.Empty
                                            })
                                            .ToList()
                                    };
                                })
                                .ToList();
                            break;
                    }
                }

                return stageNode;
            }).ToList();

            return new WorkflowNodeInfoDto
            {
                Id        = workflow.Id,
                Name      = workflow.Name ?? string.Empty,
                Status    = workflow.Status ?? string.Empty,
                IsDefault = workflow.IsDefault,
                Stages    = stageNodes
            };
        }

        // ─── Private helpers ─────────────────────────────────────────────

        /// <summary>
        /// Validate workflow access — mirrors StageConditionService.ValidateWorkflowPermissionAsync
        /// </summary>
        private async Task ValidateWorkflowPermissionAsync(long workflowId, OperationTypeEnum operationType)
        {
            // Service-to-service (Client Credentials) — bypass permission check
            if (_userContext?.Schema == Domain.Shared.Const.AuthSchemes.ItemIamClientIdentification)
                return;

            if (string.IsNullOrEmpty(_userContext?.UserId) ||
                !long.TryParse(_userContext.UserId, out var userId))
            {
                throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
            }

            var permission = await _permissionService.CheckWorkflowAccessAsync(userId, workflowId, operationType);
            if (!permission.Success)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"No permission to {operationType} workflow: {permission.ErrorMessage}");
            }
        }

        private static List<StageComponent> ParseComponents(string componentsJson)
        {
            if (string.IsNullOrWhiteSpace(componentsJson))
                return new List<StageComponent>();
            try
            {
                return JsonConvert.DeserializeObject<List<StageComponent>>(componentsJson)
                       ?? new List<StageComponent>();
            }
            catch
            {
                return new List<StageComponent>();
            }
        }

        /// <summary>
        /// Parse questions from a questionnaire structure_json.
        /// Tries four JSONPath patterns to handle different structure layouts.
        /// Question title fallback order: title → label → name → text
        /// </summary>
        private static List<QuestionOptionDto> ExtractQuestions(string structureJson)
        {
            if (string.IsNullOrWhiteSpace(structureJson))
                return new List<QuestionOptionDto>();

            try
            {
                var token = JToken.Parse(structureJson);
                var paths = new[]
                {
                    "$.sections[*].questions[*]",
                    "$.sections[*].items[*]",
                    "$.questions[*]",
                    "$.items[*]"
                };

                var seen   = new HashSet<string>();
                var result = new List<QuestionOptionDto>();

                foreach (var path in paths)
                {
                    foreach (var q in token.SelectTokens(path))
                    {
                        var idRaw = q["id"]?.ToString();
                        if (string.IsNullOrEmpty(idRaw) || !seen.Add(idRaw))
                            continue;

                        var title = (q["title"] ?? q["label"] ?? q["name"] ?? q["text"])?.ToString()
                                    ?? string.Empty;

                        // Parse selectable options (radio / checkbox / select questions)
                        var options = new List<QuestionOptionItemDto>();
                        var optionsNode = q["options"] ?? q["choices"] ?? q["items"];
                        if (optionsNode is JArray optArr)
                        {
                            foreach (var opt in optArr)
                            {
                                // opt may be a string, or an object with label/value fields
                                if (opt is JObject optObj)
                                {
                                    var label = (optObj["label"] ?? optObj["text"] ?? optObj["value"])?.ToString();
                                    var value = (optObj["value"] ?? optObj["label"] ?? optObj["text"])?.ToString();
                                    if (!string.IsNullOrEmpty(label))
                                        options.Add(new QuestionOptionItemDto { Label = label, Value = value ?? label });
                                }
                                else
                                {
                                    // Plain string option — label and value are the same
                                    var text = opt?.ToString();
                                    if (!string.IsNullOrEmpty(text))
                                        options.Add(new QuestionOptionItemDto { Label = text, Value = text });
                                }
                            }
                        }

                        result.Add(new QuestionOptionDto
                        {
                            Id      = idRaw,
                            Title   = title,
                            Type    = q["type"]?.ToString() ?? string.Empty,
                            Options = options
                        });
                    }
                }

                return result;
            }
            catch
            {
                return new List<QuestionOptionDto>();
            }
        }
    }
}
