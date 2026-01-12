# 需求文档

## 简介

本文档定义了 FlowFlex 系统中 Stage Condition（阶段条件）功能的后端需求规范。该功能允许用户在 Workflow 的 Stage 之间配置条件规则，当满足特定条件时，可以触发跳转到指定 Stage 或执行预设的 Action。此功能旨在增强 Workflow 的网状流程支持，改善当前只能支持直线型流程的限制，使工作流程更加灵活和智能。

**注意：本文档仅涵盖后端 API 和业务逻辑，不包括前端开发工作。**

---

## 功能概述

### 核心功能说明

Stage Condition 是一个工作流条件分支引擎，允许在 Stage 完成时根据预设条件自动决定下一步流程走向。

**主要能力：**
1. **条件配置** - 为每个 Stage 配置一个或多个条件，每个条件包含规则和动作
2. **规则评估** - 基于 Stage 中 Component 的实际数据（Checklist 状态、Questionnaire 答案等）评估条件
3. **动作执行** - 条件满足时执行配置的动作（跳转 Stage、触发 Action 等）
4. **流程控制** - 实现工作流的动态分支，支持跳过、回退、提前结束等场景

### 业务流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Stage Condition 执行流程                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐                                                           │
│  │ Stage 完成   │                                                           │
│  └──────┬───────┘                                                           │
│         │                                                                   │
│         ▼                                                                   │
│  ┌──────────────────────┐                                                   │
│  │ 获取该 Stage 的所有   │                                                   │
│  │ Conditions (按顺序)   │                                                   │
│  └──────┬───────────────┘                                                   │
│         │                                                                   │
│         ▼                                                                   │
│  ┌──────────────────────┐    否    ┌─────────────────────┐                  │
│  │ 是否有未评估的条件？  │─────────▶│ 进入 Fallback Stage │                  │
│  └──────┬───────────────┘          │ (默认: 下一阶段)    │                  │
│         │ 是                        └─────────────────────┘                  │
│         ▼                                                                   │
│  ┌──────────────────────┐                                                   │
│  │ 获取 Component 数据   │                                                   │
│  │ (详见下方说明)        │                                                   │
│  └──────┬───────────────┘                                                   │
│         │                                                                   │
│         ▼                                                                   │
│  ┌──────────────────────┐    否    ┌─────────────────────┐                  │
│  │ 评估条件规则         │─────────▶│ 评估下一个条件      │──┐               │
│  │ (详见下方说明)        │          └─────────────────────┘  │               │
│  └──────┬───────────────┘                                   │               │
│         │ 是 (条件满足)                                      │               │
│         ▼                                                   │               │
│  ┌──────────────────────┐                                   │               │
│  │ 执行配置的动作       │                                   │               │
│  │ (详见下方说明)        │                                   │               │
│  └──────┬───────────────┘                                   │               │
│         │                                                   │               │
│         ▼                                                   │               │
│  ┌──────────────────────┐                                   │               │
│  │ 更新 Onboarding 状态 │◀──────────────────────────────────┘               │
│  │ 进入目标 Stage       │                                                   │
│  └──────────────────────┘                                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### 步骤详解：获取 Component 数据

根据条件规则中配置的数据源，从对应的 Component 获取实际数据值：

| Component 类型 | 数据获取方式 | 可用字段示例 |
|---------------|-------------|-------------|
| **Required Fields** | 从 Onboarding 的 DynamicData 中读取静态字段值 | CustomerName, Email, Phone, Amount 等 |
| **Checklist** | 查询 ChecklistTaskInstance 表获取任务状态 | TaskStatus (Completed/Pending), CompletedCount, TotalCount |
| **Questionnaire** | 查询 QuestionnaireAnswer 表获取问题答案 | Answer (文本/选项值), AnsweredAt, AnsweredBy |
| **File Attachments** | 查询 Attachment 表获取文件信息 | FileCount, HasAttachment (true/false) |

**数据获取流程：**
1. 解析条件规则中的 `sourceStageId` 和 `componentType`
2. 根据 `componentType` 调用对应的数据服务
3. 使用 `fieldPath` 定位具体字段值
4. 返回字段的实际值用于后续比较

#### 步骤详解：评估条件规则

对每个条件的规则组进行逻辑评估：

**评估流程：**
```
条件 = {
  "logic": "AND",
  "rules": [
    { "field": "checklist.status", "operator": "Equals", "value": "Completed" },
    { "field": "questionnaire.creditScore", "operator": "GreaterThan", "value": "700" }
  ]
}

评估过程：
1. 规则1: checklist.status == "Completed" → true
2. 规则2: questionnaire.creditScore > 700 → true (假设实际值为 750)
3. AND 逻辑: true AND true → true (条件满足)
```

**操作符评估逻辑（共14种）：**

| 操作符 | 符号 | 评估逻辑 | 示例 |
|-------|------|---------|------|
| 等于 | = | actualValue == expectedValue | "Active" == "Active" → true |
| 不等于 | ≠ | actualValue != expectedValue | "Active" != "Inactive" → true |
| 大于 | > | actualValue > expectedValue | 750 > 700 → true |
| 小于 | < | actualValue < expectedValue | 500 < 700 → true |
| 大于等于 | >= | actualValue >= expectedValue | 700 >= 700 → true |
| 小于等于 | <= | actualValue <= expectedValue | 700 <= 700 → true |
| 包含 | Contains | actualValue.Contains(expectedValue) | "Hello World".Contains("World") → true |
| 不包含 | Does Not Contain | !actualValue.Contains(expectedValue) | "Hello".Contains("World") → false → true |
| 开头是 | Starts With | actualValue.StartsWith(expectedValue) | "Hello World".StartsWith("Hello") → true |
| 结尾是 | Ends With | actualValue.EndsWith(expectedValue) | "Hello World".EndsWith("World") → true |
| 为空 | Is Empty | string.IsNullOrEmpty(actualValue) | "" → true, null → true |
| 不为空 | Is Not Empty | !string.IsNullOrEmpty(actualValue) | "value" → true |
| 在列表中 | In List | expectedList.Contains(actualValue) | ["A","B","C"].Contains("B") → true |
| 不在列表中 | Not In List | !expectedList.Contains(actualValue) | ["A","B","C"].Contains("D") → false → true |

**空值处理：**
- 如果字段值为 null 或不存在，除 IsEmpty 外的所有比较都返回 false
- IsEmpty 操作符对 null 值返回 true

#### 步骤详解：执行配置的动作

当条件满足时，按顺序执行配置的动作列表：

**动作类型及执行逻辑（共7种）：**

| 动作类型 | 描述 | 执行逻辑 | 参数 |
|---------|------|---------|------|
| **Go to Stage** | 跳转到特定阶段 | 更新 Onboarding.CurrentStageId，将中间阶段标记为 Skipped | targetStageId |
| **Skip Stage** | 跳过下一阶段 | 将下一阶段标记为 Skipped，进入下下阶段 | 无 |
| **End Workflow** | 立即完成工作流 | 将 Onboarding 标记为 Completed，忽略剩余阶段 | 无 |
| **Send Notification** | 发送通知 | 调用通知服务发送邮件/短信 | recipientType, recipientId, templateId |
| **Update Field** | 更新字段值 | 更新 Onboarding 的 DynamicData 中指定字段 | fieldPath, newValue |
| **Trigger Action** | 执行预定义动作 | 调用 ActionExecutionService 执行 Tools 中的 Action | actionDefinitionId, parameters |
| **Assign User** | 重新分配用户 | 更新阶段的 Assignee 字段 | assigneeType (User/Team), assigneeId |

**GoToStage 执行流程：**
```
当前 Stage: Stage 2 (Order: 2)
目标 Stage: Stage 5 (Order: 5)

执行步骤：
1. 验证目标 Stage 存在且属于同一 Workflow
2. 将 Stage 3, Stage 4 标记为 Skipped 状态
3. 更新 Onboarding.CurrentStageId = Stage5.Id
4. 记录 StageProgress 变更日志
```

**TriggerAction 执行流程：**
```
ActionDefinitionId: 123
Parameters: { "recipient": "admin@example.com", "template": "approval_needed" }

执行步骤：
1. 从 ActionDefinition 表获取 Action 配置
2. 验证 Action 已启用且属于同一租户
3. 构建执行上下文 (OnboardingId, StageId, TenantId 等)
4. 调用 ActionExecutionService.ExecuteAsync()
5. 记录执行结果到 ActionExecution 表
```

**多动作执行顺序：**
```
Actions: [
  { "type": "TriggerAction", "actionId": 10, "order": 1 },  // 先发送通知
  { "type": "GoToStage", "targetStageId": 5, "order": 2 }   // 再跳转阶段
]

执行顺序：按 order 字段升序执行，确保动作按预期顺序完成
```

### 数据模型概览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           数据模型关系                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Workflow (1) ──────────────────────────────────────────┐                   │
│       │                                                 │                   │
│       │ 1:N                                             │                   │
│       ▼                                                 │                   │
│  ┌─────────────┐                                        │                   │
│  │   Stage     │                                        │                   │
│  │ ─────────── │                                        │                   │
│  │ - Id        │                                        │                   │
│  │ - Name      │                                        │                   │
│  │ - Order     │                                        │                   │
│  │ - Components│                                        │                   │
│  └──────┬──────┘                                        │                   │
│         │                                               │                   │
│         │ 1:N                                           │                   │
│         ▼                                               │                   │
│  ┌──────────────────────┐                               │                   │
│  │   StageCondition     │                               │                   │
│  │ ──────────────────── │                               │                   │
│  │ - Id                 │                               │                   │
│  │ - StageId            │                               │                   │
│  │ - Name               │                               │                   │
│  │ - Order              │                               │                   │
│  │ - RulesJson          │───────────────────────────────┤                   │
│  │ - ActionsJson        │                               │                   │
│  │ - FallbackStageId    │───────────────────────────────┘                   │
│  │ - IsActive           │         (引用同 Workflow 的 Stage)                │
│  └──────────────────────┘                                                   │
│                                                                             │
│  RulesJson 结构:                    ActionsJson 结构:                       │
│  ┌────────────────────┐             ┌────────────────────┐                  │
│  │ {                  │             │ [                  │                  │
│  │   "logic": "AND",  │             │   {                │                  │
│  │   "rules": [       │             │     "type": "GoTo",│                  │
│  │     {              │             │     "targetId": 5, │                  │
│  │       "stageId",   │             │     "order": 1     │                  │
│  │       "component", │             │   },               │                  │
│  │       "field",     │             │   {                │                  │
│  │       "operator",  │             │     "type": "Trig",│                  │
│  │       "value"      │             │     "actionId": 10 │                  │
│  │     }              │             │   }                │                  │
│  │   ]                │             │ ]                  │                  │
│  │ }                  │             └────────────────────┘                  │
│  └────────────────────┘                                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 核心设计原则

1. **Condition 和 Action 合并** - 在 Stage 的 Condition 配置中直接选择要执行的 Action
2. **Action 只在 Tools 模块创建** - 其他位置不可创建 Action，Condition 只能引用 Tools 中已定义的 Action
3. **条件数据源限制** - 只能选择当前 Stage 和之前 Stage 的 Component 数据作为条件判断依据
4. **首个匹配生效** - 多个条件按顺序评估，第一个满足的条件执行其动作
5. **条件冲突检测** - 系统需要检测并处理多个条件可能产生的冲突

---

## 术语表

- **Stage_Condition（阶段条件）**: 定义在某个 Stage 完成后需要评估的条件规则及其触发的动作
- **Condition_Rule（条件规则）**: 由 Component、Field、Operator 和 Value 组成的单个比较表达式
- **Condition_Rule_Group（条件规则组）**: 多个条件规则通过 AND/OR 逻辑组合
- **Condition_Action（条件动作）**: 当条件满足时执行的操作
- **Stage（阶段）**: Workflow 中的一个步骤
- **Workflow（工作流）**: 由多个 Stage 组成的业务流程
- **Component（组件）**: Stage 中的 Required Fields、Checklist、Questionnaire、File Attachments 等
- **Field（字段）**: Component 中的数据项（如 Checklist Task 的状态、Questionnaire 的答案）
- **Operator（操作符）**: 用于比较的运算符
- **Target_Stage（目标阶段）**: 条件满足时跳转的目标 Stage
- **Fallback_Stage（回退阶段）**: 条件不满足时的默认行为（通常为下一个 Stage）
- **Action_Definition（动作定义）**: 在 Tools 模块中预定义的可执行操作

---

# 第一部分：基本功能（MVP）

## 需求 1: 条件配置管理 API

**用户故事:** 作为工作流管理员，我希望能够通过 API 在阶段之间添加条件，以便根据阶段结果创建动态的工作流路径。

#### 验收标准

1. Stage_Condition_API 应当提供创建条件的 POST 端点
2. Stage_Condition_API 应当提供更新条件的 PUT 端点
3. Stage_Condition_API 应当提供删除条件的 DELETE 端点
4. Stage_Condition_API 应当提供获取特定 Stage 所有条件的 GET 端点
5. Stage_Condition_API 应当提供获取特定 Workflow 所有条件的 GET 端点
6. 当创建条件时，Stage_Condition_API 应当要求提供条件名称
7. 当创建条件时，Stage_Condition_API 应当验证 Stage 存在且属于指定的 Workflow
8. Stage_Condition_API 应当支持每个 Stage 配置多个条件
9. Stage_Condition_API 应当存储同一 Stage 上多个条件的评估顺序（order 字段）

## 需求 2: 条件规则定义

**用户故事:** 作为工作流管理员，我希望能够基于阶段组件数据定义条件规则，以便创建精确的分支逻辑。

#### 验收标准

1. Condition_Rule 应当支持从当前或之前的阶段选择数据源 Stage（不能选择未来的阶段）
2. Condition_Rule 应当支持从选定 Stage 中选择 Component 类型（Required Fields、Checklist、Questionnaire、File Attachments）
3. Condition_Rule 应当支持从选定 Component 中选择具体的 Field/Line Item
4. Condition_Rule 应当支持以下操作符（共14种）：
   - 等于 (=)
   - 不等于 (≠)
   - 大于 (>)
   - 小于 (<)
   - 大于等于 (>=)
   - 小于等于 (<=)
   - 包含 (Contains)
   - 不包含 (Does Not Contain)
   - 开头是 (Starts With)
   - 结尾是 (Ends With)
   - 为空 (Is Empty)
   - 不为空 (Is Not Empty)
   - 在列表中 (In List)
   - 不在列表中 (Not In List)
5. Condition_Rule 应当支持存储比较值
6. Stage_Condition 应当支持在规则组内使用 AND 逻辑组合多个规则
7. 条件规则数据应当以 JSON 格式存储在数据库中

## 需求 3: 条件动作配置

**用户故事:** 作为工作流管理员，我希望能够配置条件满足时执行的动作，以便自动化工作流转换和操作。

#### 验收标准

1. Condition_Action 应当支持以下动作类型（共7种）：
   - "Go to Stage": 跳转到 Workflow 中的特定目标 Stage (Jump to a specific workflow stage)
   - "Skip Stage": 跳过下一个阶段继续 (Skip the next stage and continue)
   - "End Workflow": 立即完成工作流 (Complete the workflow immediately)
   - "Send Notification": 向用户或团队发送邮件/短信 (Send email/SMS to user or team)
   - "Update Field": 自动更新字段值 (Automatically update a field value)
   - "Trigger Action": 执行预定义的动作 (Execute a predefined action)
   - "Assign User": 将阶段重新分配给特定用户/团队 (Reassign stage to specific user/team)
2. 当配置"Go to Stage"动作时，API 应当验证目标 Stage 存在于同一 Workflow 中
3. 当配置"Trigger Action"动作时，API 应当验证 Action_Definition 存在于 Tools 模块中
4. 当配置"Send Notification"动作时，API 应当验证接收者（用户或团队）存在
5. 当配置"Update Field"动作时，API 应当验证目标字段存在且可写
6. 当配置"Assign User"动作时，API 应当验证目标用户或团队存在
7. Stage_Condition 应当支持每个条件配置多个动作（按顺序执行）
8. Stage_Condition 应当支持配置条件不满足时的 Fallback Stage ID
9. 如果未配置 Fallback_Stage，系统应当默认使用下一个顺序阶段
10. 条件动作数据应当以 JSON 格式存储在数据库中

## 需求 4: 条件评估执行引擎

**用户故事:** 作为系统，我希望在阶段完成时自动评估条件，以便工作流能够动态确定下一步。

#### 验收标准

1. 当 Stage 完成时，Condition_Evaluator 应当按 order 字段顺序获取该 Stage 的所有条件
2. Condition_Evaluator 应当根据指定 Components 的实际数据评估每个条件的规则
3. 当条件中的所有规则都满足时（AND 逻辑），Condition_Evaluator 应当将该条件标记为已满足
4. 当条件满足时，Condition_Evaluator 应当按顺序执行所有配置的动作
5. 当执行"Go to Stage"动作时，Workflow_Engine 应当将 Onboarding 的 CurrentStageId 更新为目标 Stage
6. 当执行"Go to Stage"动作时，Workflow_Engine 应当将被跳过的中间阶段标记为 Skipped 状态
7. 当执行"Skip Stage"动作时，Workflow_Engine 应当将下一阶段标记为 Skipped 并进入下下阶段
8. 当执行"End Workflow"动作时，Workflow_Engine 应当将 Onboarding 标记为 Completed
9. 当 Stage 没有条件满足时，Workflow_Engine 应当进入 Fallback_Stage（默认：下一个顺序阶段）
10. 如果多个条件都满足，Condition_Evaluator 应当只执行第一个满足条件的动作（首个匹配生效）
11. 当评估规则时，Condition_Evaluator 应当优雅地处理空值或缺失的字段值（视为不满足条件）

## 需求 5: 数据权限和租户隔离

**用户故事:** 作为系统管理员，我希望条件数据遵守租户隔离和用户权限，以便维护数据安全。

#### 验收标准

1. Stage_Condition_API 应当对所有条件操作应用租户隔离（TenantId 过滤）
2. Stage_Condition_API 应当在允许条件更改前验证用户具有 WorkflowWrite 权限
3. 当评估条件时，Condition_Evaluator 应当只访问同一租户内的数据
4. 当引用 Action_Definitions 时，Stage_Condition_API 应当只返回同一租户内的 Actions

## 需求 6: 条件验证服务

**用户故事:** 作为工作流管理员，我希望系统验证我的条件配置，以便避免创建无效的工作流路径。

#### 验收标准

1. 当保存条件时，Validation_Service 应当验证所有引用的 Stages 存在且处于活动状态
2. 当保存条件时，Validation_Service 应当验证所有引用的 Components 存在于源 Stage 中
3. 当保存条件时，Validation_Service 应当验证所有引用的 Fields 存在于 Component 中
4. 当保存条件时，Validation_Service 应当验证引用的 Action_Definitions 存在且已启用
5. 当配置"GoToStage"动作时，Validation_Service 应当阻止选择与源相同的 Stage（自循环）
6. Validation_Service 应当为所有验证失败返回清晰的错误代码和消息

## 需求 7: Component 数据源服务

**用户故事:** 作为工作流管理员，我希望使用各种阶段组件的数据作为条件源，以便创建全面的分支逻辑。

#### 验收标准

1. Component_Data_Service 应当支持从 Required Fields 组件获取静态字段值
2. Component_Data_Service 应当支持从 Checklist 组件获取任务完成状态
3. Component_Data_Service 应当支持从 Questionnaire 组件获取问题答案
4. Component_Data_Service 应当支持从 File Attachments 组件获取文件数量
5. Component_Data_Service 应当提供 API 端点返回指定 Stage 的可用 Components 和 Fields 列表

---

# 第二部分：扩展功能

## 需求 8: 高级条件规则（扩展）

**用户故事:** 作为工作流管理员，我希望能够创建更复杂的条件规则，以便处理高级业务场景。

#### 验收标准

1. Stage_Condition 应当支持使用 OR 逻辑组合多个规则组
2. Stage_Condition 应当支持嵌套规则组以实现复杂条件（AND 嵌套 OR，OR 嵌套 AND）
3. 当源 Stage 有多个同类型 Component 实例时，Condition_Rule 应当支持选择具体实例
4. Condition_Rule 应当支持使用点号表示法访问嵌套字段值（例如："checklist.task1.status"）

## 需求 9: 高级动作参数配置（扩展）

**用户故事:** 作为工作流管理员，我希望能够配置更复杂的动作参数，以便实现更丰富的自动化场景。

#### 验收标准

1. 当配置"Send Notification"动作时，API 应当支持配置通知模板和动态变量
2. 当配置"Update Field"动作时，API 应当支持使用表达式计算新值
3. 当配置动作参数时，API 应当根据 Action_Definition 的 schema 验证参数
4. Condition_Action 应当支持从条件规则中引用字段值作为动作参数

## 需求 10: 条件冲突检测服务（扩展）

**用户故事:** 作为工作流管理员，我希望系统能够检测条件冲突，以便避免模糊的工作流行为。

#### 验收标准

1. Conflict_Detection_Service 应当检测同一 Stage 上条件之间的潜在冲突
2. Conflict_Detection_Service 应当识别具有重叠规则条件的情况
3. 当检测到冲突时，API 应当返回警告信息但允许保存
4. Conflict_Detection_Service 应当检测工作流中的潜在循环路径
5. Conflict_Detection_Service 应当检测由条件配置导致的不可达阶段

## 需求 11: 条件执行日志（扩展）

**用户故事:** 作为工作流管理员，我希望跟踪条件评估结果，以便审计和排查工作流行为。

#### 验收标准

1. Condition_Logger 应当记录每次条件评估的时间戳
2. Condition_Logger 应当记录哪些规则被评估及其各自的结果（true/false）
3. Condition_Logger 应当记录评估中使用的实际字段值
4. Condition_Logger 应当记录整体条件结果（Met/NotMet）
5. Condition_Logger 应当记录执行的动作及其结果
6. 如果动作执行失败，Condition_Logger 应当记录错误详情
7. Condition_Log_API 应当提供查询特定 Onboarding 条件评估历史的端点
8. Condition_Log_API 应当提供查询特定 Stage 条件评估历史的端点

## 需求 12: 高级验证和错误处理（扩展）

**用户故事:** 作为工作流管理员，我希望系统提供更全面的验证，以便确保工作流配置的完整性。

#### 验收标准

1. Validation_Service 应当验证操作符与字段数据类型兼容
2. 如果引用的 Stage、Component、Field 或 Action 被删除，系统应当将受影响的条件标记为 Invalid 状态
3. 当条件变为无效时，Workflow_Engine 应当跳过条件评估并记录警告日志
4. Condition_Log_API 应当根据用户权限过滤返回的日志

## 需求 13: 高级 Component 数据源支持（扩展）

**用户故事:** 作为工作流管理员，我希望能够访问更详细的组件数据，以便创建更精细的条件规则。

#### 验收标准

1. Component_Data_Service 应当支持 Checklist 组件的任务字段值（不仅仅是完成状态）
2. Component_Data_Service 应当支持 File Attachments 组件的文件元数据（文件类型、大小等）
3. Component_Data_Service 应当支持 Quick Links 组件作为数据源
4. Condition_Rule 应当支持使用点号表示法访问嵌套字段值

