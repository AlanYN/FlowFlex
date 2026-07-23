using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Application.Services.OW;
using Item.Message.Kafka;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowFlex.Application.Notification;

public class ChangeLogKafkaHandler : INotificationHandler<ChangeLogCreatedEvent>
{
    private readonly IOnboardingEntityResolver _entityResolver;
    private readonly IChangeLogFlattenService _flattenService;
    private readonly IKafkaProducer<string, string> _producer;
    private readonly IdmUserDataClient _idmUserDataClient;
    private readonly IOptionsSnapshot<KafkaOptions> _kafkaOptions;
    private readonly ILogger<ChangeLogKafkaHandler> _logger;

    private const string MessageVersion = "1.0.0";
    private const string MessageSource = "WFE";
    private const string MessageTag = "ChangeLog";

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ChangeLogKafkaHandler(
        IOnboardingEntityResolver entityResolver,
        IChangeLogFlattenService flattenService,
        IKafkaProducer<string, string> producer,
        IdmUserDataClient idmUserDataClient,
        IOptionsSnapshot<KafkaOptions> kafkaOptions,
        ILogger<ChangeLogKafkaHandler> logger)
    {
        _entityResolver = entityResolver;
        _flattenService = flattenService;
        _producer = producer;
        _idmUserDataClient = idmUserDataClient;
        _kafkaOptions = kafkaOptions;
        _logger = logger;
    }

    public async Task Handle(ChangeLogCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = notification.OperationLog;

            var entityInfo = await _entityResolver.ResolveAsync(log.OnboardingId!.Value);

            var changes = _flattenService.ExtractChanges(log);
            var origin = entityInfo != null ? "external" : "internal";
            var tenantCode = await _idmUserDataClient.GetTenantCodeAsync(log.TenantId) ?? log.TenantId;

            var message = new
            {
                header = new
                {
                    logId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                    busId = log.OnboardingId.ToString(),
                    tenantId = tenantCode,
                    tag = MessageTag,
                    timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    source = MessageSource,
                    version = MessageVersion
                },
                body = new
                {
                    content = new
                    {
                        changeLogId = log.Id.ToString(),
                        onboardingId = log.OnboardingId.ToString(),
                        entityId = entityInfo?.EntityId,
                        entityType = entityInfo?.EntityType,
                        origin = origin,
                        operationType = log.OperationType,
                        @operator = log.OperatorId > 0
                            ? new { id = log.OperatorId.ToString(), name = log.OperatorName }
                            : null,
                        title = log.OperationTitle,
                        description = log.OperationDescription,
                        changes = changes
                    },
                    ext = new { }
                }
            };

            var messageJson = JsonSerializer.Serialize(message, CamelCaseOptions);
            var topic = _kafkaOptions.Value.Topics["WFEChangeLog"];
            var key = log.OnboardingId.ToString()!;

            await _producer.SendAsync(key, messageJson, new TopicPartition(topic, Partition.Any));

            _logger.LogDebug("Kafka message produced for ChangeLog {ChangeLogId} to topic {Topic}",
                log.Id, topic);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka produce failed for ChangeLog {ChangeLogId}, skipping",
                notification.OperationLog.Id);
        }
    }
}
