# 事件存储系统 (Event Storage System)

## 概述

FlowFlex 系统现在支持将 `OnboardingStageCompletedEvent` 事件存储到专门的事件表 `ff_events` 中。这个系统提供了更好的事件管理、查询和分析能力。

## 数据库表结构

### ff_events 表

| 字段名 | 类型 | 描述 |
|--------|------|------|
| id | bigint | 主键 |
| event_id | varchar(100) | 事件ID (业务层生成的唯一标识) |
| event_type | varchar(100) | 事件类型 |
| event_version | varchar(20) | 事件版本 |
| event_timestamp | timestamptz | 事件时间戳 |
| aggregate_id | bigint | 聚合根ID |
| aggregate_type | varchar(50) | 聚合根类型 |
| event_source | varchar(50) | 事件来源 |
| event_data | jsonb | 事件数据 (JSON格式) |
| event_metadata | jsonb | 事件元数据 (JSON格式) |
| event_description | varchar(500) | 事件描述 |
| event_status | varchar(20) | 事件状态 |
| process_count | int | 处理次数 |
| last_processed_at | timestamptz | 最后处理时间 |
| error_message | varchar(2000) | 错误信息 |
| related_entity_id | bigint | 关联的业务实体ID |
| related_entity_type | varchar(50) | 关联的业务实体类型 |
| event_tags | jsonb | 事件标签 (JSON数组) |
| requires_retry | boolean | 是否需要重试 |
| next_retry_at | timestamptz | 下次重试时间 |
| max_retry_count | int | 最大重试次数 |
| tenant_id | varchar(50) | 租户ID |
| is_valid | boolean | 是否有效 |
| create_date | timestamptz | 创建时间 |
| create_by | varchar(100) | 创建者 |
| modify_date | timestamptz | 修改时间 |
| modify_by | varchar(100) | 修改者 |

## 索引设计

为了提高查询性能，系统创建了以下索引：

1. **idx_ff_events_event_id** - 事件ID唯一索引
2. **idx_ff_events_event_type** - 事件类型索引
3. **idx_ff_events_aggregate** - 聚合根索引
4. **idx_ff_events_tenant** - 租户ID索引
5. **idx_ff_events_status** - 事件状态索引
6. **idx_ff_events_source** - 事件来源索引
7. **idx_ff_events_retry** - 重试相关索引
8. **idx_ff_events_related_entity** - 关联实体索引
9. **idx_ff_events_timestamp** - 事件时间戳索引
10. **idx_ff_events_tags_gin** - 事件标签 GIN 索引
11. **idx_ff_events_data_gin** - 事件数据 GIN 索引
12. **idx_ff_events_tenant_type_time** - 复合索引：租户+事件类型+时间戳
13. **idx_ff_events_aggregate_type_time** - 复合索引：聚合根+事件类型+时间戳

## 事件处理流程

### 1. 事件发布
当 `OnboardingStageCompletedEvent` 事件被发布时，`OnboardingStageCompletedLogHandler` 会：

1. 接收事件
2. 保存到新的 `ff_events` 表
3. 保存到原有的 `ff_stage_completion_log` 表（保持向后兼容）
4. 处理错误情况并标记失败的事件

### 2. 事件存储
事件数据以 JSON 格式存储，包含：
- 完整的事件数据
- 事件元数据（路由标签、优先级等）
- 处理信息（处理者、处理时间等）

### 3. 错误处理
- 如果事件处理失败，会创建一个失败状态的事件记录
- 支持重试机制，使用指数退避策略
- 记录详细的错误信息

## 使用方法

### 1. 查询事件
```csharp
// 按事件类型查询
var events = await _eventService.GetEventsByTypeAsync("OnboardingStageCompleted", 30);

// 按聚合根ID查询
var events = await _eventService.GetEventsByAggregateIdAsync(onboardingId, "Onboarding");

// 分页查询
var request = new EventQueryRequest
{
    EventType = "OnboardingStageCompleted",
    PageIndex = 1,
    PageSize = 20
};
var result = await _eventService.QueryEventsAsync(request);
```

### 2. 事件统计
```csharp
var statistics = await _eventService.GetEventStatisticsAsync(7); // 最近7天的统计
```

### 3. 重试失败的事件
```csharp
// 获取需要重试的事件
var failedEvents = await _eventService.GetFailedEventsForRetryAsync();

// 重试特定事件
await _eventService.RetryFailedEventAsync(eventId);
```

### 4. 清理旧事件
```csharp
// 清理90天前的事件
var cleanedCount = await _eventService.CleanupOldEventsAsync(90);
```

## 数据库迁移

运行以下迁移脚本来创建事件表：

```csharp
// 创建表和索引
CreateEventsTable_20241220.Up(db);

// 如需回滚
CreateEventsTable_20241220.Down(db);
```

## 监控和维护

### 1. 事件状态监控
- **Published** - 事件已发布
- **Processed** - 事件已处理
- **Failed** - 事件处理失败
- **Retry** - 等待重试

### 2. 性能监控
- 事件处理时间
- 成功率
- 重试次数
- 错误率

### 3. 定期维护
- 清理过期事件
- 重试失败事件
- 监控存储空间使用情况

## 配置说明

### 重试策略
- 最大重试次数：3次
- 重试间隔：指数退避（2^n 分钟）
- 重试条件：事件状态为 "Failed" 且未超过最大重试次数

### 数据保留策略
- 默认保留90天的事件数据
- 可通过配置调整保留时间
- 支持软删除（标记为无效）

## 注意事项

1. **向后兼容**：新系统同时保存到 `ff_events` 和 `ff_stage_completion_log` 表
2. **性能考虑**：使用 JSON 字段存储事件数据，支持高效的查询和分析
3. **多租户支持**：所有事件都包含租户ID，支持多租户隔离
4. **错误恢复**：失败的事件会被标记并支持重试机制
5. **数据一致性**：事件处理失败不会影响其他事件处理器的执行

## 扩展性

该事件存储系统设计为通用的事件存储解决方案，可以轻松扩展支持其他类型的事件：

1. 添加新的事件类型
2. 扩展事件元数据结构
3. 实现自定义的事件处理器
4. 添加事件流处理功能

## 故障排除

### 常见问题

1. **事件重复**：检查事件ID是否唯一
2. **查询性能差**：确保相关索引已创建
3. **存储空间不足**：定期清理旧事件
4. **重试失败**：检查错误信息和重试配置

### 日志记录

系统会记录详细的结构化日志，包括：
- 事件处理开始和结束
- 错误信息和堆栈跟踪
- 性能指标
- 重试信息

通过这些日志可以有效地监控和调试事件处理过程。 