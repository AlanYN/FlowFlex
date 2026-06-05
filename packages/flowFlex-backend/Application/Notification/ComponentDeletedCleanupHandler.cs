using System.Text.Json;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Application.Notification
{
    public class ComponentDeletedCleanupHandler :
        INotificationHandler<ChecklistDeletedEvent>,
        INotificationHandler<QuestionnaireDeletedEvent>,
        INotificationHandler<QuickLinkDeletedEvent>,
        INotificationHandler<StaticFieldDeletedEvent>
    {
        private readonly IStageRepository _stageRepository;
        private readonly ISqlSugarClient _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ComponentDeletedCleanupHandler> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        public ComponentDeletedCleanupHandler(
            IStageRepository stageRepository,
            ISqlSugarClient db,
            IDistributedCache cache,
            ILogger<ComponentDeletedCleanupHandler> logger)
        {
            _stageRepository = stageRepository;
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task Handle(ChecklistDeletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await CleanupStagesAsync(
                    notification.ChecklistId.ToString(),
                    (components, id) =>
                    {
                        var modified = false;
                        foreach (var comp in components.Where(c => c.Key == "checklist"))
                        {
                            if (comp.ChecklistIds?.Remove(notification.ChecklistId) == true)
                                modified = true;
                            comp.ChecklistNames?.Remove(notification.ChecklistName);
                        }
                        components.RemoveAll(c => c.Key == "checklist" && (c.ChecklistIds == null || !c.ChecklistIds.Any()));
                        return modified;
                    },
                    stage => { stage.ChecklistId = null; });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup stages for deleted checklist {ChecklistId}", notification.ChecklistId);
            }
        }

        public async Task Handle(QuestionnaireDeletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await CleanupStagesAsync(
                    notification.QuestionnaireId.ToString(),
                    (components, id) =>
                    {
                        var modified = false;
                        foreach (var comp in components.Where(c => c.Key == "questionnaires"))
                        {
                            if (comp.QuestionnaireIds?.Remove(notification.QuestionnaireId) == true)
                                modified = true;
                            comp.QuestionnaireNames?.Remove(notification.QuestionnaireName);
                        }
                        components.RemoveAll(c => c.Key == "questionnaires" && (c.QuestionnaireIds == null || !c.QuestionnaireIds.Any()));
                        return modified;
                    },
                    stage => { stage.QuestionnaireId = null; });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup stages for deleted questionnaire {QuestionnaireId}", notification.QuestionnaireId);
            }
        }

        public async Task Handle(QuickLinkDeletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await CleanupStagesAsync(
                    notification.QuickLinkId.ToString(),
                    (components, id) =>
                    {
                        var modified = false;
                        foreach (var comp in components.Where(c => c.Key == "quickLinks"))
                        {
                            if (comp.QuickLinkIds?.Remove(notification.QuickLinkId) == true)
                                modified = true;
                            comp.QuickLinkNames?.Remove(notification.QuickLinkName);
                        }
                        components.RemoveAll(c => c.Key == "quickLinks" && (c.QuickLinkIds == null || !c.QuickLinkIds.Any()));
                        return modified;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup stages for deleted quick link {QuickLinkId}", notification.QuickLinkId);
            }
        }

        public async Task Handle(StaticFieldDeletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await CleanupStagesAsync(
                    notification.FieldId.ToString(),
                    (components, id) =>
                    {
                        var modified = false;
                        foreach (var comp in components.Where(c => c.Key == "fields"))
                        {
                            var before = comp.StaticFields?.Count ?? 0;
                            comp.StaticFields?.RemoveAll(f => f.Id == id);
                            if ((comp.StaticFields?.Count ?? 0) < before)
                                modified = true;
                        }
                        return modified;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup stages for deleted static field {FieldId}", notification.FieldId);
            }
        }

        private async Task CleanupStagesAsync(
            string idStr,
            Func<List<StageComponent>, string, bool> patchComponents,
            Action<Stage> patchFk = null)
        {
            var stages = await _db.Queryable<Stage>()
                .Where(s => SqlFunc.ToString(s.ComponentsJson).Contains(idStr))
                .ToListAsync();

            if (!stages.Any()) return;

            foreach (var stage in stages)
            {
                try
                {
                    var components = DeserializeComponents(stage.ComponentsJson);
                    if (components == null) continue;

                    var modified = patchComponents(components, idStr);

                    patchFk?.Invoke(stage);

                    if (modified || patchFk != null)
                    {
                        stage.ComponentsJson = JsonSerializer.Serialize(components, JsonOptions);
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

        private static List<StageComponent> DeserializeComponents(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var normalized = NormalizeJson(json);
                if (string.IsNullOrWhiteSpace(normalized)) return null;
                return JsonSerializer.Deserialize<List<StageComponent>>(normalized, JsonOptions) ?? new List<StageComponent>();
            }
            catch (JsonException)
            {
                return new List<StageComponent>();
            }
        }

        private static string NormalizeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            var current = json.Trim();

            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrWhiteSpace(current)) return current;

                if (current.StartsWith("[") || current.StartsWith("{"))
                    break;

                if ((current.StartsWith("\"") && current.EndsWith("\"")) ||
                    (current.StartsWith("'") && current.EndsWith("'")))
                {
                    try
                    {
                        var inner = JsonSerializer.Deserialize<string>(current);
                        if (!string.IsNullOrWhiteSpace(inner))
                        {
                            current = inner.Trim();
                            continue;
                        }
                    }
                    catch { }
                }

                if (current.Contains("\\\""))
                {
                    current = current.Replace("\\\"", "\"").Trim();
                    continue;
                }

                break;
            }

            return current;
        }
    }
}
