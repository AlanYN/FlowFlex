# WFE ChangeLog Kafka 对接文档

> **面向：** Ticketing System 开发团队  
> **Topic：** `EAG_WFE_CHANGE_LOG`  
> **生产方：** WFE（FlowFlex 工作流引擎）  
> **协议：** Kafka + JSON

---

## 1. 概述

WFE 在 Case（工作流实例）发生任何变更时，会实时发送 Kafka 消息。Ticketing System 消费这些消息，可在对应 Ticket 上展示操作活动历史。

**核心要点：**

- WFE 中每次变更产生一条 Kafka 消息
- 同一个 Case 的消息保证有序（相同 `onboardingId` → 相同分区）
- 使用 `changeLogId` 做幂等去重

---

## 2. 如何过滤你需要的消息

### 第一层过滤：只消费属于你的 Ticket 的消息

```
过滤条件：origin == "external" AND entityId != null
```

| 字段       | 使用方式                                                                                                                             |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `origin`   | `"external"` = 由你的系统通过 API 创建的 Case。`"internal"` = WFE 内部手动创建的 Case。**你只需要消费 `"external"` 的消息。**        |
| `entityId` | 这就是**你的 Ticket Number**（创建 Case 时通过 `POST /integration/external/v1/cases` 传入的值）。用它来定位这条活动属于哪个 Ticket。 |

### 第二层过滤：按需选择消费的操作类型

根据你的展示需求，通过 `operationType` 进一步筛选：

**场景 A：只展示字段值变更（有 diff 的）**

```
operationType in [
  "StaticFieldValueChange",      -- 表单字段变更（每次一个字段）
  "QuestionnaireAnswerSubmit",   -- 问卷首次提交（可能多个字段）
  "QuestionnaireAnswerUpdate",   -- 问卷答案修改
  "CaseUpdate"                   -- Case 级字段变更（名称/优先级/负责人等）
]
```

或者用更简单的判断：`changes.length > 0`

**场景 B：只展示状态变更事件**

```
operationType in [
  "CaseCreate", "CaseStart", "CasePause", "CaseResume",
  "CaseAbort", "CaseForceComplete", "StageTransition", ...
]
```

**场景 C：展示全部活动（推荐）**

不做 `operationType` 过滤，全部消费。字段变更用 `changes` 展示 diff，状态事件用 `title` 展示活动。

### 忽略以下消息：

- `origin == "internal"` — WFE 内部 Case，与你的 Ticket 无关
- `entityId == null` — 同上

---

## 3. 消息结构

```json
{
  "header": {
    "logId": "guid",
    "busId": "wfe-case-id",
    "tenantId": "租户标识",
    "tag": "ChangeLog",
    "timeStamp": 1784612254,
    "source": "WFE",
    "version": "1.0.0"
  },
  "body": {
    "content": {
      "changeLogId": "唯一雪花ID",
      "onboardingId": "wfe-case-id",
      "entityId": "你的Ticket编号",
      "entityType": "ticket",
      "origin": "external",
      "operationType": "StaticFieldValueChange",
      "operator": {
        "id": "用户ID",
        "name": "用户姓名"
      },
      "title": "人可读的操作标题",
      "description": "操作详细描述",
      "changes": [
        {
          "field": "字段名",
          "oldValue": "旧值",
          "newValue": "新值"
        }
      ]
    },
    "ext": {}
  }
}
```

---

## 4. 字段说明

### Header 字段

| 字段        | 类型   | 说明                                  |
| ----------- | ------ | ------------------------------------- |
| `logId`     | string | 消息唯一标识（UUID）                  |
| `busId`     | string | WFE Case ID（与 `onboardingId` 相同） |
| `tenantId`  | string | 租户 Code（如 `"SBFH"，"LT"`）。字段名为公司 Kafka 规范要求，且实际赋值为租户 Code。 |
| `tag`       | string | 固定值 `"ChangeLog"`                  |
| `timeStamp` | long   | 消息发送时间，Unix 时间戳（秒，UTC）  |
| `source`    | string | 固定值 `"WFE"`                        |
| `version`   | string | 消息格式版本（`"1.0.0"`）             |

### Body.content 字段

| 字段                 | 类型    | 说明                                                 |
| -------------------- | ------- | ---------------------------------------------------- |
| `changeLogId`        | string  | 变更日志唯一 ID。**用于幂等去重。**                  |
| `onboardingId`       | string  | WFE 内部 Case ID                                     |
| `entityId`           | string? | **你的 Ticket 编号。** 内部 Case 时为 null。         |
| `entityType`         | string? | 外部实体类型（如 `"ticket"`）。内部 Case 时为 null。 |
| `origin`             | string  | `"external"` 或 `"internal"`。**用此字段做过滤。**   |
| `operationType`      | string  | 操作类型（见第 5 节）                                |
| `operator`           | object? | 执行操作的用户。系统自动触发时为 null。              |
| `operator.id`        | string  | 用户 ID                                              |
| `operator.name`      | string  | 用户显示名                                           |
| `title`              | string  | 操作摘要（可直接展示）                               |
| `description`        | string  | 操作详细描述（可直接展示）                           |
| `changes`            | array   | 字段级 diff。状态事件类操作时可能为空数组 `[]`。     |
| `changes[].field`    | string  | 变更的字段名                                         |
| `changes[].oldValue` | string? | 旧值（首次设置时为 null）                            |
| `changes[].newValue` | string? | 新值（清除时为 null）                                |

---

## 5. 操作类型（operationType）

每条消息都有一个 `operationType` 表示发生了什么操作：

### 字段变更类（`changes` 数组有值）

| operationType               | 说明                                      | 示例                        |
| --------------------------- | ----------------------------------------- | --------------------------- |
| `StaticFieldValueChange`    | 表单字段值变更                            | Company Name: "ABC" → "XYZ" |
| `QuestionnaireAnswerSubmit` | 问卷首次提交                              | 多个字段被填写              |
| `QuestionnaireAnswerUpdate` | 问卷答案修改                              | 答案被更新                  |
| `CaseUpdate`                | Case 级字段变更（名称、优先级、负责人等） | Priority: "Medium" → "High" |

### 状态事件类（`changes` 通常为空 `[]`）

| operationType             | 说明                      |
| ------------------------- | ------------------------- |
| `CaseCreate`              | Case 被创建               |
| `CaseStart`               | Case 被启动（工作流激活） |
| `CasePause`               | Case 被暂停               |
| `CaseResume`              | Case 被恢复               |
| `CaseAbort`               | Case 被终止               |
| `CaseForceComplete`       | Case 被强制完成           |
| `CaseReactivate`          | Case 被重新激活           |
| `CaseDelete`              | Case 被删除               |
| `StageTransition`         | Case 流转到新阶段         |
| `ChecklistTaskComplete`   | 清单任务被完成            |
| `ChecklistTaskUncomplete` | 清单任务被取消完成        |
| `FileUpload`              | 文件被上传                |
| `PriorityChange`          | 优先级变更                |
| `ActionExecutionSuccess`  | 自动动作执行成功          |
| `ActionExecutionFailed`   | 自动动作执行失败          |

### 展示建议

- **字段变更类**：展示 `changes` 数组作为 diff（旧值 → 新值）
- **状态事件类**：展示 `title` 和/或 `description` 作为活动条目
- 简单场景下，所有类型都可以直接用 `title` 展示为活动流

---

## 6. 消费端实现指南

### 推荐 Consumer 配置

```
Topic: EAG_WFE_CHANGE_LOG（3 分区，同 Case 有序）
GroupId: ticketing_wfe_changelog_consumer
AutoOffsetReset: Latest（如需历史数据用 Earliest）
```

> **注意：** 同一个 Case（`onboardingId`）的消息保证落在同一分区内，顺序有保证。不同 Case 的消息可能在不同分区，跨 Case 无顺序保证（通常也不需要）。

### 处理逻辑（伪代码）

```python
for message in kafka_consumer:
    content = message.body.content

    # 第 1 步：过滤 — 只处理外部消息
    if content.origin != "external":
        continue

    # 第 2 步：通过 changeLogId 去重
    if already_processed(content.changeLogId):
        continue

    # 第 3 步：定位 Ticket
    ticket = find_ticket_by_number(content.entityId)
    if ticket is None:
        log_warning(f"未知的 entityId: {content.entityId}")
        continue

    # 第 4 步：在 Ticket 上创建活动记录
    create_ticket_activity(
        ticket_id=ticket.id,
        timestamp=message.header.timeStamp,
        actor=content.operator.name if content.operator else "System",
        title=content.title,
        description=content.description,
        changes=content.changes,
        operation_type=content.operationType,
        wfe_log_id=content.changeLogId  # 存储用于去重
    )

    # 第 5 步：提交 offset
    commit()
```

### 异常处理

- Kafka 暂时不可用时，Consumer 会从上次提交的 offset 恢复
- Topic 保留期内消息不会被删除
- 如果漏消费了消息，WFE 提供 REST API 做补偿拉取：
  `GET /ow/change-logs/v1/flattened?entityId=你的Ticket编号&since=2026-07-01T00:00:00Z`

---

## 7. 消息示例

### 示例 1：字段值变更

```json
{
  "header": {
    "logId": "0bcd054a-0415-4f2e-a385-0cbcc7463ef8",
    "busId": "2079427175597084672",
    "tenantId": "1003",
    "tag": "ChangeLog",
    "timeStamp": 1784612254,
    "source": "WFE",
    "version": "1.0.0"
  },
  "body": {
    "content": {
      "changeLogId": "1784612250401",
      "onboardingId": "2079427175597084672",
      "entityId": "TK-00123",
      "entityType": "ticket",
      "origin": "external",
      "operationType": "StaticFieldValueChange",
      "operator": { "id": "8712", "name": "Kai Li" },
      "title": "Static Field Value Changed: Company Name",
      "description": "Static field 'Company Name' value has been changed by Kai Li. Fields: FieldValueJson",
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

### 示例 2：问卷提交（多个字段）

```json
{
  "header": {
    "logId": "c38e5e0b-f6b6-4755-8cae-a996a3e11f47",
    "busId": "2079427175597084672",
    "tenantId": "1003",
    "tag": "ChangeLog",
    "timeStamp": 1784612954,
    "source": "WFE",
    "version": "1.0.0"
  },
  "body": {
    "content": {
      "changeLogId": "1784612950373",
      "onboardingId": "2079427175597084672",
      "entityId": "TK-00123",
      "entityType": "ticket",
      "origin": "external",
      "operationType": "QuestionnaireAnswerSubmit",
      "operator": { "id": "8712", "name": "Kai Li" },
      "title": "Questionnaire Answer Submitted",
      "description": "Questionnaire answer has been submitted by Kai Li. Changes: qt1: r; qt2: t; qt3: 3",
      "changes": [
        { "field": "qt1", "oldValue": null, "newValue": "r" },
        { "field": "qt2", "oldValue": null, "newValue": "t" },
        { "field": "qt3", "oldValue": null, "newValue": "3" }
      ]
    },
    "ext": {}
  }
}
```

### 示例 3：阶段流转（无字段变更）

```json
{
  "header": {
    "logId": "6d7adc01-156e-4a3b-985a-f555df89011f",
    "busId": "2079427175597084672",
    "tenantId": "1003",
    "tag": "ChangeLog",
    "timeStamp": 1784613000,
    "source": "WFE",
    "version": "1.0.0"
  },
  "body": {
    "content": {
      "changeLogId": "1784613000001",
      "onboardingId": "2079427175597084672",
      "entityId": "TK-00123",
      "entityType": "ticket",
      "origin": "external",
      "operationType": "StageTransition",
      "operator": { "id": "8712", "name": "Kai Li" },
      "title": "Stage Completed: Document Review → Final Approval",
      "description": "Stage 'Document Review' completed and transitioned to 'Final Approval'",
      "changes": []
    },
    "ext": {}
  }
}
```

---

## 8. 常见问题

**Q：`entityId` 是什么？**  
A：就是你在创建 Case 时通过 `POST /integration/external/v1/cases` 传入的 Ticket Number。WFE 会存储它，并在该 Case 的所有 Kafka 消息中携带。

**Q：会收到重复消息吗？**  
A：极少数情况下会（Kafka rebalance、Producer 重试）。需要做幂等去重。

**Q：如何做幂等去重？**  
A：以下几种方式可选：

| 去重方式                                       | 说明                                                               | 推荐度                                   |
| ---------------------------------------------- | ------------------------------------------------------------------ | ---------------------------------------- |
| `changeLogId`                                  | WFE 写入 DB 时生成的雪花 ID，全局唯一且稳定（producer 重试不会变） | ⭐ 推荐                                  |
| `onboardingId` + `operationType` + `timeStamp` | Case + 操作类型 + 时间戳组合                                       | 可用，但极端情况下同秒可能有多条同类操作 |
| `header.logId`                                 | 消息级 UUID                                                        | 不推荐，producer 重试时会重新生成        |

建议直接用 `changeLogId` 作为去重键，存入你的数据库，消费前先查是否已处理过。

**Q：`operator` 为 null 是什么意思？**  
A：该操作是系统自动触发的（如自动化规则触发了字段变更）。展示时可显示为"系统"或"自动"。

**Q：`changes` 为空数组是什么意思？**  
A：该操作是状态事件（如 Case 暂停、阶段完成等），没有字段级别的 diff。用 `title` 和 `description` 展示即可。

**Q：时间戳的时区？**  
A：`header.timeStamp` 是 Unix 时间戳（秒，UTC）。展示时转换为你需要的时区。

**Q：漏消费了消息怎么补偿？**  
A：使用 REST API 补偿拉取：`GET /ow/change-logs/v1/flattened?entityId=你的Ticket编号&since=时间戳`

**Q：消息顺序保证？**  
A：同一个 Case（`onboardingId`）的消息保证有序（落在同一分区）。不同 Case 之间无顺序保证，但通常也不需要。

---

## 9. 联系方式

如有问题：

- WFE 团队：[联系方式]
- Topic 负责人：[姓名]
