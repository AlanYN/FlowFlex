# OW-672 Kafka ChangeLog 同步 — 开发上下文文档

> **目的**：提供完整的背景、决策链路、代码依据，供新开发者/AI 工具接手实现 Kafka 生产端。
> **Ticket**：OW-662 — Change Log and Sync with Ticketing System

---

## 一、背景与需求

### 业务需求

Ticketing System 通过 WFE（FlowFlex Workflow Engine）创建 Contract 审批 Case。需求是：WFE 中 Case 的所有变更历史（Change Log）要同步给 Ticketing System，供其在 Ticket Activity 中展示。

### 核心原则

- WFE 是 Source of Truth（唯一数据源）
- Ticketing System 仅消费展示，不反向写入 WFE
- 同步失败不影响 WFE 正常业务

### 技术决策链路

1. ~~推送（WFE 主动调 Ticketing API）~~：领导否决。理由：万一影响 Workflow 正常使用。
2. ~~Pull 模式（Ticketing 轮询 WFE API）~~：已实现 `GET /ow/change-logs/v1/flattened` 作为备用方案。
3. **Kafka 消息队列（最终方案）**：WFE 发消息到 Kafka，Ticketing 订阅消费。解耦彻底，不影响 WFE，天然重试。

---

## 二、已完成的工作

### 2.1 扁平化 ChangeLog 查询 API（已上线）

已实现一个 API，将内部 JSON 快照格式的 ChangeLog 转为扁平化的 `{field, oldValue, newValue}` 格式输出。

**端点**：`GET /ow/change-logs/v1/flattened`

**参数**：
| 参数 | 类型 | 说明 |
|------|------|------|
| entityId | string | 外部实体 ID（如 Ticket ID），二选一 |
| onboardingId | long | WFE Case ID，二选一 |
| since | DateTimeOffset | 增量拉取时间戳 |
| changesOnly | bool | true=只返回有字段变化的记录 |
| pageIndex | int | 页码 |
| pageSize | int | 页大小（max 100） |

**相关代码文件**：
- Controller: `WebApi/Controllers/OW/OperationChangeLogController.cs` — 最后一个端点 `GetFlattenedChangeLogsAsync`
- Service 接口: `Application.Contracts/IServices/Integration/IChangeLogFlattenService.cs`
- Service 实现: `Application/Services/Integration/ChangeLogFlattenService.cs`
- DTO: `Application.Contracts/Dtos/Integration/FlattenedChangeLogDto.cs`

### 2.2 ChangeLog 写入机制（已有，不用改）

所有 ChangeLog 通过 `BaseOperationLogService.LogOperationWithUserContextAsync()` 写入 `ff_operation_change_log` 表。这是唯一入口。

**关键代码**：`Application/Services/OW/ChangeLog/BaseOperationLogService.cs`

```csharp
// 第 85 行左右 - 所有日志最终走这里
public virtual async Task<bool> LogOperationWithUserContextAsync(...)
{
    var operationLog = new OperationChangeLog { ... };
    operationLog.InitNewId();
    bool result = await InsertWithRetryAsync(operationLog);  // 写入 DB
    // ← Kafka Produce 应插入此处（result == true 且 onboardingId 有值时）
    return result;
}
```

---

## 三、Kafka 设计方案

### 3.1 整体架构

```
WFE 业务操作（改字段/完成 Stage/提交问卷等）
    │
    ▼
BaseOperationLogService.LogOperationWithUserContextAsync()
    │
    ├── 1. 写入 ff_operation_change_log 表（已有）
    │
    └── 2. 查该 Case 的 entity_id 是否非空
            │
            ├── 为空 → 跳过（纯内部 Case，不发消息）
            │
            └── 非空 → Produce 消息到 Kafka Topic
                    │
                    ▼
            Kafka Topic: wfe-changelog
                    │
                    ▼
            Ticketing System (Consumer Group)
                    │
                    └── 写入 Ticket Activity
```

### 3.2 发送条件

**所有有 `onboardingId` 的 ChangeLog 都发送**，不区分内部/外部 Case。

通过消息中的 `origin` 字段区分来源：
| 条件 | origin 值 | 说明 |
|------|-----------|------|
| `ff_onboarding.entity_id IS NOT NULL` | `"external"` | 外部系统（如 Ticketing）创建的 Case |
| `ff_onboarding.entity_id IS NULL` | `"internal"` | WFE 内部创建的 Case |

消费方可按 `origin` 过滤：只关心外部 Case 就筛 `origin == "external"`。

### 3.3 消息格式

遵循公司统一 Kafka 消息结构规范（header + body 信封模式）：

```json
{
    "header": {
        "logId": "e07af77aad7e43f4bf4a07ed943b1c16.191.17173993183334429",
        "busId": "2043948125940486145",
        "tenantId": "UT",
        "tag": "ChangeLog",
        "timeStamp": 1723619137,
        "source": "WFE",
        "version": "1.0.0"
    },
    "body": {
        "content": {
            "changeLogId": "1776150728735",
            "onboardingId": "2043948125940486145",
            "entityId": "TK-12345",
            "entityType": "ticket",
            "origin": "external",
            "operationType": "StaticFieldValueChange",
            "operator": {
                "id": "8712",
                "name": "Amanda Li"
            },
            "title": "Static Field Value Changed: Company Name",
            "description": "Static field 'Company Name' value has been changed...",
            "changes": [
                {
                    "field": "Company Name",
                    "oldValue": "ABC Inc",
                    "newValue": "ABC Logistics"
                }
            ]
        },
        "ext": {}
    }
}
```

**Header 字段映射**：

| 字段 | 是否必填 | WFE 取值 | 说明 |
|------|----------|----------|------|
| logId | 否 | `Guid.NewGuid().ToString()` | 消息唯一标识 |
| busId | 否，尽可能提供 | `onboardingId` | 业务数据唯一编码 |
| tenantId | 否，尽可能提供 | 外部 Case 从 Onboarding 取，内部 Case 从 ChangeLog 实体取 | 租户 ID |
| tag | 否 | `"ChangeLog"` | 业务标签 |
| timeStamp | 是 | Unix 时间戳（秒） | 发送消息时间戳 |
| source | 是 | `"WFE"` | 消息来源项目编码 |
| version | 是 | `"1.0.0"`（代码常量） | 消息版本号 |

**Body.content 字段说明**：

| 字段 | 说明 |
|------|------|
| changeLogId | 雪花 ID（全局唯一），Consumer 端做幂等去重 |
| onboardingId | WFE Case ID |
| entityId | 外部实体 ID（如 Ticket ID），internal Case 时为 null |
| entityType | 外部实体类型（如 "ticket"），internal Case 时为 null |
| origin | `"external"` / `"internal"` — 区分 Case 来源 |
| operationType | PascalCase 枚举字符串，消费方可据此区分事件类型 |
| operator | 操作人 `{id, name}`，系统操作时为 null |
| title | 操作标题（人可读） |
| description | 操作详细描述 |
| changes | 扁平化字段 diff 数组 `[{field, oldValue, newValue}]`，可能为空 `[]` |

### 3.4 Topic 设计

```
Topic 名称: wfe-changelog（或按环境区分: wfe-changelog-staging / wfe-changelog-production）
分区策略: 按 onboardingId hash → 同一个 Case 的所有消息保序
```

### 3.5 发送时机

**逐条实时发送**。每次 ChangeLog 成功写入 DB 后，立即 produce 一条消息。不批量攒，不定时轮询。

### 3.6 容错

- Produce 失败只打 Warning 日志，不影响 WFE 主业务流程（fire-and-forget 风格）
- Kafka 本身有 ACK 机制保证消息不丢（配置 `acks=all`）
- Consumer 端通过 `messageId`（changeLogId）做幂等，防止重复消费

---

## 四、架构设计（追问共识）

### 4.1 整体架构模式：MediatR 中介者

采用 MediatR 事件驱动，与 BaseOperationLogService 解耦：

```
BaseOperationLogService.LogOperationWithUserContextAsync()
    │
    ├── 1. 写入 ff_operation_change_log 表（已有）
    │
    └── 2. if (result && onboardingId.HasValue)
            │
            └── await _mediator.Publish(new ChangeLogCreatedEvent(operationLog))
                    │
                    ▼
            ChangeLogKafkaHandler (MediatR INotificationHandler)
                    │
                    ├── IOnboardingEntityResolver.ResolveAsync(onboardingId)
                    │       → 查缓存/DB 拿 (entityId, entityType, tenantId)
                    │       → entityId 为空则 return（跳过）
                    │
                    ├── IChangeLogFlattenService.ExtractChanges(operationLog)
                    │       → 解析出 List<FieldChangeDto>
                    │
                    └── 组装消息 → IKafkaProducer.SendAsync(key, json, topic)
                            │
                            └── try-catch: 失败只打 Warning，不抛出
```

### 4.2 新增组件清单

| 组件 | 类型 | 职责 |
|------|------|------|
| `ChangeLogCreatedEvent` | IDomainEvent (INotification) | 携带完整 OperationChangeLog 实体 |
| `ChangeLogKafkaHandler` | INotificationHandler | 组装消息并 produce 到 Kafka |
| `IOnboardingEntityResolver` | 接口 + 实现 | 通过 onboardingId 查 (entityId, entityType, tenantId)，内置 IMemoryCache |
| `IChangeLogFlattenService.ExtractChanges()` | 新增 public 方法 | 解析单条 OperationChangeLog 为 field diff 列表 |

### 4.3 BaseOperationLogService 改动

```csharp
// 构造函数新增 IMediator 参数（7 个子类需透传）
private readonly IMediator _mediator;

public virtual async Task<bool> LogOperationWithUserContextAsync(...)
{
    var operationLog = new OperationChangeLog { ... };
    operationLog.InitNewId();
    bool result = await InsertWithRetryAsync(operationLog);

    // ★ 新增：发布 MediatR 事件 ★
    if (result && onboardingId.HasValue)
    {
        await _mediator.Publish(new ChangeLogCreatedEvent(operationLog));
    }

    return result;
}
```

**关键决策**：
- `onboardingId` 为 null 时不发布事件（纯内部 Case，提前短路）
- 传完整 `OperationChangeLog` 实体（内存中已有，不需再查 DB）
- 同步 `await`（已在后台线程中，不阻塞 HTTP）
- 异常由 Handler 内部 catch，不影响 `LogOperationWithUserContextAsync` 返回值

### 4.4 ChangeLogKafkaHandler 伪代码

```csharp
public class ChangeLogKafkaHandler : INotificationHandler<ChangeLogCreatedEvent>
{
    private readonly IOnboardingEntityResolver _entityResolver;
    private readonly IChangeLogFlattenService _flattenService;
    private readonly IKafkaProducer<string, string> _producer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly ILogger<ChangeLogKafkaHandler> _logger;

    private const string MessageVersion = "1.0.0";

    public async Task Handle(ChangeLogCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var log = notification.OperationLog;

            // 1. 查 EntityId（带缓存）
            var entityInfo = await _entityResolver.ResolveAsync(log.OnboardingId!.Value);
            if (entityInfo == null || string.IsNullOrEmpty(entityInfo.EntityId))
                return; // 无外部关联，跳过

            // 2. 解析 changes
            var changes = _flattenService.ExtractChanges(log);

            // 3. 组装消息（公司规范信封）
            var message = new
            {
                header = new
                {
                    logId = Guid.NewGuid().ToString(),
                    busId = log.OnboardingId.ToString(),
                    tenantId = log.TenantId?.ToString(),
                    tag = "ChangeLog",
                    timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    source = "WFE",
                    version = MessageVersion
                },
                body = new
                {
                    content = new
                    {
                        changeLogId = log.Id.ToString(),
                        onboardingId = log.OnboardingId.ToString(),
                        entityId = entityInfo.EntityId,
                        entityType = entityInfo.EntityType,
                        operationType = log.OperationType.ToString(),  // PascalCase 原样
                        @operator = log.OperatorId.HasValue
                            ? new { id = log.OperatorId.ToString(), name = log.OperatorName }
                            : null,  // 系统操作时为 null
                        title = log.OperationTitle,
                        description = log.OperationDescription,
                        changes = changes  // 可能为空数组 []，照发
                    },
                    ext = new { }
                }
            };

            // 4. 序列化 + 发送
            var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var topic = _kafkaOptions.Value.Topics["WFEChangeLog"];
            var key = log.OnboardingId.ToString();  // 同 Case 保序

            await _producer.SendAsync(key, messageJson, new TopicPartition(topic, Partition.Any));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka produce failed for ChangeLog {ChangeLogId}, skipping",
                notification.OperationLog.Id);
        }
    }
}
```

### 4.5 IOnboardingEntityResolver

```csharp
public interface IOnboardingEntityResolver
{
    Task<OnboardingEntityInfo?> ResolveAsync(long onboardingId);
}

public record OnboardingEntityInfo(string EntityId, string? EntityType, long? TenantId);
```

内部实现使用 `IMemoryCache`，TTL 5 分钟。

### 4.6 解析逻辑复用

在 `IChangeLogFlattenService` 接口新增方法：

```csharp
List<FieldChangeDto> ExtractChanges(OperationChangeLog log);
```

实现中委托给已有的 `private ParseChanges()` 方法（将其改为 private → 被新 public 方法调用）。

---

## 五、项目中 Kafka 的现有状态

### 5.1 依赖已引入

```xml
<!-- Application.Contracts/Application.Contracts.csproj -->
<PackageReference Include="Item.Message.Kafka" Version="8.0.4" />
```

### 5.2 但目前没有任何代码使用

整个项目中没有 `using Item.Message.Kafka` 的代码。这是第一次使用 Kafka。

### 5.3 需要做的事

1. ~~查明 `Item.Message.Kafka` 包的 API~~ ✅ 已确认
2. 在 `appsettings.json` 中配置 Kafka bootstrap servers + Topics
3. 在 DI 中注册 Kafka Producer（`AddKafkaProducer<string, string>()` + `AddKafkaFactory()`）
4. 创建 `ChangeLogCreatedEvent`（IDomainEvent）
5. 创建 `IOnboardingEntityResolver` 接口及实现（含 IMemoryCache）
6. 在 `IChangeLogFlattenService` 新增 `ExtractChanges` public 方法
7. 创建 `ChangeLogKafkaHandler`（INotificationHandler）
8. 在 `BaseOperationLogService` 构造函数加 `IMediator`，7 个子类透传
9. 在 `LogOperationWithUserContextAsync` 写入成功后发布事件

---

## 六、已确认的决策

| # | 决策 | 结论 |
|---|------|------|
| 1 | 发送范围 | 所有有 `onboardingId` 的 ChangeLog 都发，通过 `origin` 字段区分内部/外部 |
| 2 | 发送时机 | 逐条实时发（写入 DB 成功后立即 produce） |
| 3 | 消息格式 | 公司规范信封（header + body），body.content 含扁平化 changes 数组 + origin 字段 |
| 4 | Topic 分区 | 按 onboardingId 作为 message key，Kafka 自动 hash 分配分区（不手动指定 partition） |
| 5 | 容错 | Produce 失败不阻塞主流程，Handler 内部 try-catch 打 Warning |
| 6 | Consumer | Ticketing 团队自行实现 |
| 7 | 幂等 | Consumer 通过 changeLogId 去重 |
| 8 | Kafka 集群 | 公司已有 |
| 9 | 架构模式 | MediatR 中介者模式，BaseOperationLogService 发布事件，Handler 负责 produce |
| 10 | Event 数据 | 携带完整 OperationChangeLog 实体（不传 ID 再查） |
| 11 | 发布时机 | `if (result && onboardingId.HasValue)` 时发布，onboardingId 为空不发布 |
| 12 | 执行方式 | 同步 `await _mediator.Publish(event)`（已在后台线程，不需再套异步） |
| 13 | EntityId 查询 | 独立 `IOnboardingEntityResolver` Service + IMemoryCache（TTL 5分钟） |
| 14 | Flatten 复用 | `IChangeLogFlattenService` 新增 `ExtractChanges(OperationChangeLog)` public 方法 |
| 15 | DI 注入 | BaseOperationLogService 构造函数加 IMediator，7 个子类透传 |
| 16 | 异常处理 | Handler 内部 catch，不向外抛，BaseOperationLogService 无感知 |
| 17 | Topic 配置 | 放 `appsettings.json` 的 `Kafka:Topics` 字典，通过 IOptions 读取 |
| 18 | header.logId | `Guid.NewGuid().ToString()`（不依赖链路上下文） |
| 19 | header.tenantId | 外部 Case 从 Onboarding 实体取，内部 Case 从 ChangeLog 实体的 TenantId 取 |
| 20 | header.version | 代码常量 `"1.0.0"`，升版本时改代码 |
| 21 | operationType 格式 | PascalCase 枚举字符串原样传（与 DB 存储和已有 API 一致） |
| 22 | operator 空值 | 原样传 null（系统自动操作时），消费方自行处理展示 |
| 23 | changes 为空 | 照发（`changes: []`），消费方通过 operationType 区分事件类型 |
| 24 | 序列化 | System.Text.Json + camelCase |
| 25 | Message Key | `onboardingId.ToString()`（保证同 Case 有序） |
| 26 | Topic 命名 | `item_wfe_changelog`（生产），非生产加环境后缀，最终需运维确认 |
| 27 | origin 字段 | `"external"`（EntityId 非空）/ `"internal"`（EntityId 为空），消费方据此过滤 |
| 28 | Kafka 集群地址 | dev 环境：`kafka-dev.item.pub:9092`，staging/prod：AWS MSK（Nacos 注入） |

---

## 七、待确认 / 待调研

| # | 问题 | 说明 | 状态 |
|---|------|------|------|
| 1 | `Item.Message.Kafka` 包的 API 接口 | 需要查包的 DLL 或文档，了解 IKafkaProducer 接口签名 | ✅ 已确认（见第十节 CRM 参照） |
| 2 | Kafka bootstrap servers 地址 | 需找运维要开发/预发/生产的地址 | ✅ 已确认：dev 用 `kafka-dev.item.pub:9092`，staging/prod 用 AWS MSK（Nacos 注入） |
| 3 | Topic 命名规范 | 公司是否有统一的 topic 命名约定 | ✅ 已确认：`item_wfe_changelog`（需运维最终确认） |
| 4 | BaseOperationLogService 注入 IMediator 是否有循环依赖 | 依赖链：Base→IMediator→Handler→Resolver/Flatten/Producer，无反向依赖，不会循环 | ✅ 已验证：无循环 |
| 5 | 查 Onboarding.EntityId 的性能 | 每次写 Log 都要查一次 Onboarding 表拿 EntityId，可能需要缓存 | ✅ 方案已定：IOnboardingEntityResolver + IMemoryCache TTL 5分钟 |
| 6 | 消息序列化格式 | JSON（推荐，通用）还是 Avro/Protobuf | ✅ 已确认：System.Text.Json + camelCase |
| 7 | Producer 生命周期 | IKafkaProducer 是 Scoped（IScopedService），BackgroundTaskQueue 场景是否安全 | ✅ 已验证：与现有 Scoped 服务行为一致，线上已稳定运行 |
| 8 | Kafka 集群认证方式 | 开发环境无认证 vs 生产 AWS MSK SASL_SSL | ✅ 已确认：dev 无认证（SecurityProtocol=0），staging/prod 用 AWS MSK SASL_SSL（SecurityProtocol=3），配置由 Nacos 注入 |

---

## 八、相关文件索引

| 类别 | 文件路径 |
|------|----------|
| Kafka 包引用 | `Application.Contracts/Application.Contracts.csproj` (Item.Message.Kafka 8.0.4) |
| ChangeLog 写入唯一入口 | `Application/Services/OW/ChangeLog/BaseOperationLogService.cs` |
| 扁平化解析 Service | `Application/Services/Integration/ChangeLogFlattenService.cs` |
| 扁平化 DTO | `Application.Contracts/Dtos/Integration/FlattenedChangeLogDto.cs` |
| 扁平化接口 | `Application.Contracts/IServices/Integration/IChangeLogFlattenService.cs` |
| Controller 端点 | `WebApi/Controllers/OW/OperationChangeLogController.cs` (flattened 端点) |
| Onboarding 实体 | `Domain/Entities/OW/Onboarding.cs` (entity_id 字段) |
| ChangeLog 实体 | `Domain/Entities/OW/OperationChangeLog.cs` |
| 现状分析文档 | `OW-662_现状分析.md` |
| 最终方案共识 | `OW-662_最终方案共识.md` |
| 方案讨论文档 | `OW-662_方案讨论_可配置ChangeLog同步.md` |

---

## 九、日志触发方式说明（给新开发者的注意事项）

当前 ChangeLog 的触发是**直接嵌入式调用**（不是 MediatR 事件、不是 AOP）。Kafka 推送通过在唯一入口发布 MediatR 事件实现解耦：

1. 所有 ChangeLog 最终汇聚到 `BaseOperationLogService.LogOperationWithUserContextAsync()`
2. 写入 DB 成功且 `onboardingId.HasValue` 时，发布 `ChangeLogCreatedEvent`
3. `ChangeLogKafkaHandler` 订阅该事件，负责判断是否发 Kafka、组装消息、produce
4. **重要**：该方法执行时可能已经不在 HTTP Request 上下文中（因为是后台任务），所以不能依赖 HttpContext

### 调用链路

```
业务 Service (如 OnboardingCrudService)
    → _backgroundTaskQueue.QueueBackgroundWorkItem(async token => { ... })
        → _onboardingLogService.LogOnboardingUpdateAsync(...)
            → BaseOperationLogService.LogOperationWithUserContextAsync(...)
                → InsertWithRetryAsync(operationLog)  // 写入 DB
                → await _mediator.Publish(new ChangeLogCreatedEvent(operationLog))
                        │
                        ▼
                ChangeLogKafkaHandler.Handle()
                    → IOnboardingEntityResolver (查 entityId，带缓存)
                    → IChangeLogFlattenService.ExtractChanges() (解析 changes)
                    → IKafkaProducer.SendAsync() (发送到 Kafka)
                    → 失败只打 Warning，不影响主流程
```

### 性能考虑

- Kafka Produce 本身是异步非阻塞的（只是把消息放入 Producer 内部 buffer），不会阻塞 ChangeLog 写入流程
- 查询 EntityId 通过 `IOnboardingEntityResolver` 内置 `IMemoryCache`（TTL 5 分钟），高频场景命中率极高
- 整个 Handler 是同步 await 执行（已在后台线程中），不再套额外异步层

---

## 十、CRM Kafka 参照分析与实施建议

> 基于 CRM 项目（`unis.crm`）中已有的 Kafka Producer 实现，对照 FlowFlex 场景得出的结论和注意事项。

### 10.1 `Item.Message.Kafka` 包 API（已从 CRM 确认）

CRM 中的使用方式明确了该包的核心接口：

```csharp
// Producer 接口（泛型：Key=string, Value=string）
IKafkaProducer<string, string>

// 发送方法
DeliveryResult<string, string> result = await _producer.SendAsync(key, valueJson, topicPartition);

// 同步发送（备选）
_producer.Send(key, valueJson, topicPartition);
```

**DI 注册方式**（参照 CRM `ServiceCollectionExtensions.cs`）：

```csharp
services
    .Configure<KafkaBasicConfig>(configuration.GetSection("Kafka"))
    .Configure<KafkaProducerConfig>(configuration.GetSection("Kafka:Producer"));

services.AddKafkaProducer<string, string>();
services.AddKafkaFactory();
```

**FlowFlex 只需要 Producer，不需要 Consumer**，所以不用注册 `AddKafkaConsumer` 和 `KafkaSubscribeOptions`。

### 10.2 配置结构（appsettings.json）

FlowFlex 需要添加的最小配置：

```json
"Kafka": {
  "BootstrapServers": [ "kafka-dev.item.pub:9092" ],
  "SecurityProtocol": 0,
  "Topics": {
    "WFEChangeLog": "item_wfe_changelog"
  },
  "Producer": {
    "Acks": -1,
    "EnableIdempotence": true,
    "MessageSendMaxRetries": "3"
  }
}
```

**注意**：CRM 中 AWS 环境使用了 MSK SASL/SSL 认证（`IsAwsServer`、`AccessKeyId`、`SecretAccessKey`）。需确认 FlowFlex 部署的 Kafka 集群是否也是 AWS MSK —— 如果是，需要参照 CRM 的 `KafkaOptions.GetKafkaProducerConfig()` 方法配置认证。

### 10.3 分区策略差异

| 项目 | 分区策略 | 原因 |
|------|----------|------|
| CRM | `tenantId % 5`（手动计算 partition） | 多租户场景，同租户数据保序 |
| FlowFlex | `onboardingId.ToString()` 作为 message key | 同 Case 保序，让 Kafka 默认 key hash 分配分区 |

**FlowFlex 建议**：不需要手动指定 partition。使用 `TopicPartition(topic, Partition.Any)` 或直接传 key 让 Kafka 自动 hash 分配。这样更简单，且分区数变更时无需改代码。

### 10.4 消息信封格式对比

~~CRM 有一套重型信封，FlowFlex 不需要照搬~~

**更正**：公司有统一的 Kafka 消息结构规范（header + body 信封模式），FlowFlex 必须遵循。已在 3.3 节更新为符合规范的格式。

与 CRM 的区别：
- CRM 的 header 更复杂（有 customerCode、userId、busType 等），因为 CRM 是多租户客户数据系统
- WFE 的 header 更简洁：核心必填字段 `source="WFE"`、`version="1.0.0"`、`timeStamp`，加上尽可能提供的 `busId`（onboardingId）和 `tenantId`
- body 结构一致：`body.content` 放业务数据，`body.ext` 放扩展字段（暂为空）

### 10.5 序列化

| 项目 | 方式 |
|------|------|
| CRM | Newtonsoft.Json + camelCase |
| FlowFlex 建议 | System.Text.Json + camelCase（与项目现有风格一致） |

```csharp
var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
```

### 10.6 容错对比

| 项目 | 失败处理 |
|------|----------|
| CRM | 失败写入历史表，有补偿重试机制 |
| FlowFlex | 失败只打 Warning 日志，不阻塞主流程（fire-and-forget） |

FlowFlex 的处理更简单，但**必须确保 try-catch 包住整个 produce 逻辑**：

```csharp
try
{
    await _producer.SendAsync(key, messageJson, topicPartition);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Kafka produce failed for ChangeLog {ChangeLogId}, skipping", operationLog.Id);
}
```

### 10.7 EntityId 缓存方案

**问题**：`BaseOperationLogService` 只有 `onboardingId`，需要查 DB 拿 `EntityId` 判断是否发消息。

**方案**：使用 `IMemoryCache`，缓存 `onboardingId → (entityId, entityType)` 映射，TTL 5 分钟。

理由：
- EntityId 一旦写入基本不变（外部系统创建时设定）
- 同一个 Case 在短时间内可能产生多条 ChangeLog（如批量改字段），缓存命中率极高
- MemoryCache 进程内零网络开销，适合这种高频低变更场景

```csharp
var cacheKey = $"onboarding:entity:{onboardingId}";
if (!_memoryCache.TryGetValue(cacheKey, out (string entityId, string entityType) cached))
{
    var onboarding = await _onboardingRepository.GetAsync(onboardingId.Value);
    cached = (onboarding?.EntityId, onboarding?.EntityType);
    _memoryCache.Set(cacheKey, cached, TimeSpan.FromMinutes(5));
}
if (string.IsNullOrEmpty(cached.entityId)) return; // 跳过，不发消息
```

### 10.8 Topic 命名建议

参考 CRM 新 topic 的命名模式（`item_{domain}_{entity}_{detail}`）：

| 环境 | Topic 名称 |
|------|-----------|
| Dev | `item_wfe_changelog_dev` |
| Staging | `item_wfe_changelog_staging` |
| Production | `item_wfe_changelog` |

命名理由：
- `item_` 前缀：公司统一前缀（CRM 新 topic 均用 `item_` 开头）
- `wfe`：来源系统标识
- `changelog`：业务内容
- 非生产环境加环境后缀

**最终命名需跟运维确认**是否符合公司 topic 注册规范。

### 10.9 实施注意事项清单

| # | 事项 | 说明 |
|---|------|------|
| 1 | 不要在 Handler 中使用 HttpContext | BaseOperationLogService 在后台任务中执行，无 HTTP 上下文 |
| 2 | Producer 是 Scoped（IScopedService） | 与现有 Scoped 服务行为一致，BackgroundTaskQueue 场景已验证安全 |
| 3 | BaseOperationLogService 只加 IMediator | 不直接注入 Producer/Cache/Repository，保持基类简洁；7 个子类需透传 |
| 4 | ChangeLogFlattenService 新增 public 方法 | `ExtractChanges(OperationChangeLog log)` → 委托给已有 private ParseChanges |
| 5 | 确认 Kafka 集群认证方式 | 开发环境可能无认证（SecurityProtocol=0），生产如果是 AWS MSK 需要 SASL_SSL 配置 |
| 6 | Message Key 使用 onboardingId.ToString() | 保证同一 Case 的消息有序，Kafka 自动 hash 分配分区 |
| 7 | Handler 内部 try-catch | 失败只打 Warning，不抛出，不影响其他 Handler 或 BaseOperationLogService |
| 8 | version 是代码常量 | `private const string MessageVersion = "1.0.0"`，改结构时升版本 |
| 9 | operator 可能为 null | 系统自动操作时 OperatorId/OperatorName 为空，原样传 null |
| 10 | changes 为空照发 | 纯事件通知（如 CasePause、StageTransition）也发消息，消费方通过 operationType 区分 |
