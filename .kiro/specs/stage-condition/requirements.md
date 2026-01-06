# 需求文档

## 简介

本文档定义了 FlowFlex 系统中 Stage Condition（阶段条件）功能的后端需求规范。该功能允许用户在 Workflow 的 Stage 上配置条件规则，当 Stage 完成时，根据条件评估结果决定下一步流程走向。此功能旨在增强 Workflow 的网状流程支持，使工作流程更加灵活和智能。

**注意：本文档仅涵盖后端 API 和业务逻辑，不包括前端开发工作。**

---

## 术语表

- **Stage_Condition（阶段条件）**: 定义在某个 Stage 完成后需要评估的条件规则及其触发的动作，每个 Stage 最多配置一个 Condition
- **Condition_Rule（条件规则）**: 使用 RulesEngine 表达式定义的条件判断逻辑
- **Condition_Action（条件动作）**: 当条件满足时执行的操作
- **Stage（阶段）**: Workflow 中的一个步骤
- **Workflow（工作流）**: 由多个 Stage 组成的业务流程
- **Component（组件）**: Stage 中的 Fields、Checklist、Questionnaire、File Attachments 等
- **RulesEngine（规则引擎）**: Microsoft RulesEngine，用于评估条件表达式
- **Target_Stage（目标阶段）**: 条件满足时跳转的目标 Stage
- **Fallback_Stage（回退阶段）**: 条件不满足时的默认行为（通常为下一个 Stage）
- **Action_Definition（动作定义）**: 在 Tools 模块中预定义的可执行操作
- **Operation_Change_Log（操作变更日志）**: 统一的日志记录，通过 api/ow/change-logs 接口访问

---

## 需求 1: 条件配置管理 API

**用户故事:** 作为工作流管理员，我希望能够通过 API 为 Stage 配置条件，以便根据阶段结果创建动态的工作流路径。

#### 验收标准

1. WHEN 创建条件时，THE Stage_Condition_API SHALL 提供 POST /api/stage-conditions 端点
2. WHEN 更新条件时，THE Stage_Condition_API SHALL 提供 PUT /api/stage-conditions/{id} 端点
3. WHEN 删除条件时，THE Stage_Condition_API SHALL 提供 DELETE /api/stage-conditions/{id} 端点
4. WHEN 查询 Stage 的条件时，THE Stage_Condition_API SHALL 提供 GET /api/stages/{stageId}/condition 端点
5. WHEN 查询 Workflow 的所有条件时，THE Stage_Condition_API SHALL 提供 GET /api/workflows/{workflowId}/conditions 端点
6. WHEN 创建条件时，THE Stage_Condition_API SHALL 验证条件名称不为空
7. WHEN 创建条件时，THE Stage_Condition_API SHALL 验证 Stage 存在且属于指定的 Workflow
8. WHEN 为已有条件的 Stage 创建新条件时，THE Stage_Condition_API SHALL 返回错误（每个 Stage 只能配置一个 Condition）
9. WHEN 验证条件配置时，THE Stage_Condition_API SHALL 提供 POST /api/stage-conditions/{id}/validate 端点

---

## 需求 2: RulesEngine 规则定义

**用户故事:** 作为工作流管理员，我希望能够使用 RulesEngine 表达式定义条件规则，以便创建灵活的分支逻辑。

#### 验收标准

1. THE Condition_Rule SHALL 使用 Microsoft RulesEngine 格式存储规则配置
2. THE Condition_Rule SHALL 支持 Lambda 表达式语法（如 "input.questionnaire.score >= 90"）
3. THE Condition_Rule SHALL 支持以下数据源 Component 类型：
   - Fields（从 Onboarding.DynamicData 读取）
   - Checklist（从 ChecklistTaskInstance 表查询）
   - Questionnaire（从 QuestionnaireAnswer 表查询）
   - File Attachments（从 Attachment 表查询）
4. WHEN 配置规则时，THE Condition_Rule SHALL 只能访问当前 Stage 和之前 Stage 的 Component 数据
5. THE Condition_Rule SHALL 支持以下操作符：
   - 比较操作符：==、!=、>、<、>=、<=
   - 字符串操作符：Contains、StartsWith、EndsWith
   - 空值操作符：!= null、== null
   - 逻辑操作符：&&、||、!
6. THE Condition_Rule SHALL 支持数学运算（如 "input.score * 0.8 + input.bonus > 100"）
7. THE Condition_Rule SHALL 支持嵌套规则（AND 嵌套 OR，OR 嵌套 AND）
8. THE RulesEngine SHALL 支持注册自定义函数（如 RuleUtils.DaysBetween、RuleUtils.IsEmpty）
9. THE Condition_Rule SHALL 以 JSON 格式存储在 RulesJson 字段中

---

## 需求 3: 条件动作配置

**用户故事:** 作为工作流管理员，我希望能够配置条件满足时执行的动作，以便自动化工作流转换和操作。

#### 验收标准

1. THE Condition_Action SHALL 支持以下动作类型：
   - Go to Stage: 跳转到 Workflow 中的特定目标 Stage
   - Skip Stage: 跳过下一个阶段继续
   - End Workflow: 立即完成工作流
   - Send Notification: 向用户或团队发送通知
   - Update Field: 自动更新字段值
   - Trigger Action: 执行 Tools 模块中预定义的动作
   - Assign User: 将阶段重新分配给特定用户/团队
2. WHEN 配置 Go to Stage 动作时，THE API SHALL 验证目标 Stage 存在于同一 Workflow 中
3. WHEN 配置 Go to Stage 动作时，THE API SHALL 阻止选择与源相同的 Stage（防止自循环）
4. WHEN 配置 Trigger Action 动作时，THE API SHALL 验证 Action_Definition 存在于 Tools 模块中且已启用
5. WHEN 配置 Send Notification 动作时，THE API SHALL 验证接收者（用户或团队）存在
6. WHEN 配置 Update Field 动作时，THE API SHALL 验证目标字段存在且可写
7. WHEN 配置 Assign User 动作时，THE API SHALL 验证目标用户或团队存在
8. THE Stage_Condition SHALL 支持每个条件配置多个动作（按 order 字段顺序执行）
9. THE Stage_Condition SHALL 支持配置条件不满足时的 Fallback Stage ID
10. IF 未配置 Fallback_Stage，THE System SHALL 默认使用下一个顺序阶段
11. THE Condition_Action SHALL 以 JSON 格式存储在 ActionsJson 字段中

---

## 需求 4: RulesEngine 条件评估服务

**用户故事:** 作为系统，我希望在阶段完成时使用 RulesEngine 自动评估条件，以便工作流能够动态确定下一步。

#### 验收标准

1. WHEN Stage 完成时，THE RulesEngine_Service SHALL 查询该 Stage 的 Condition 配置
2. IF Stage 没有配置 Condition，THE Workflow_Engine SHALL 直接进入下一个顺序阶段
3. THE RulesEngine_Service SHALL 构建输入数据对象，包含所有可用 Component 数据：
   - input.fields: Onboarding.DynamicData
   - input.checklist: ChecklistTaskInstance 数据
   - input.questionnaire: QuestionnaireAnswer 数据
   - input.attachments: Attachment 数据
4. THE RulesEngine_Service SHALL 使用 Microsoft RulesEngine 执行规则评估
5. WHEN 所有规则都返回 IsSuccess=true 时，THE RulesEngine_Service SHALL 判定条件满足
6. WHEN 条件满足时，THE Action_Executor SHALL 按 order 顺序执行所有配置的动作
7. WHEN 执行 Go to Stage 动作时，THE Workflow_Engine SHALL 更新 Onboarding.CurrentStageId 为目标 Stage
8. WHEN 执行 Go to Stage 动作时，THE Workflow_Engine SHALL 将被跳过的中间阶段标记为 Skipped 状态
9. WHEN 执行 Skip Stage 动作时，THE Workflow_Engine SHALL 将下一阶段标记为 Skipped 并进入下下阶段
10. WHEN 执行 End Workflow 动作时，THE Workflow_Engine SHALL 将 Onboarding 标记为 Completed
11. WHEN 条件不满足时，THE Workflow_Engine SHALL 进入 Fallback_Stage（默认：下一个顺序阶段）
12. WHEN 规则评估出错时，THE RulesEngine_Service SHALL 记录错误日志并进入 Fallback_Stage

---

## 需求 5: 数据权限和租户隔离

**用户故事:** 作为系统管理员，我希望条件数据遵守租户隔离和用户权限，以便维护数据安全。

#### 验收标准

1. THE Stage_Condition_API SHALL 对所有条件操作应用租户隔离（TenantId 过滤）
2. WHEN 创建或更新条件时，THE Stage_Condition_API SHALL 验证用户具有 WorkflowWrite 权限
3. WHEN 查询条件时，THE Stage_Condition_API SHALL 验证用户具有 WorkflowRead 权限
4. WHEN 评估条件时，THE RulesEngine_Service SHALL 只访问同一租户内的数据
5. WHEN 引用 Action_Definitions 时，THE Stage_Condition_API SHALL 只返回同一租户内的 Actions

---

## 需求 6: 条件验证服务

**用户故事:** 作为工作流管理员，我希望系统验证我的条件配置，以便避免创建无效的工作流路径。

#### 验收标准

1. WHEN 保存条件时，THE Validation_Service SHALL 验证 RulesJson 格式符合 RulesEngine 规范
2. WHEN 保存条件时，THE Validation_Service SHALL 验证所有引用的 Stages 存在且处于活动状态
3. WHEN 保存条件时，THE Validation_Service SHALL 验证引用的 Action_Definitions 存在且已启用
4. WHEN 保存条件时，THE Validation_Service SHALL 验证 ActionsJson 格式正确
5. THE Validation_Service SHALL 为所有验证失败返回清晰的错误代码和消息
6. WHEN 检测到循环路径时，THE Validation_Service SHALL 返回警告信息

---

## 需求 7: Component 数据源服务

**用户故事:** 作为工作流管理员，我希望使用各种阶段组件的数据作为条件源，以便创建全面的分支逻辑。

#### 验收标准

1. THE Component_Data_Service SHALL 支持从 Fields 组件获取 DynamicData 字段值
2. THE Component_Data_Service SHALL 支持从 Checklist 组件获取以下数据：
   - status: 整体完成状态（Completed/Pending）
   - completedCount: 已完成任务数
   - totalCount: 总任务数
   - tasks[]: 各任务的详细状态
3. THE Component_Data_Service SHALL 支持从 Questionnaire 组件获取问题答案
4. THE Component_Data_Service SHALL 支持从 File Attachments 组件获取：
   - fileCount: 文件数量
   - hasAttachment: 是否有附件（true/false）
   - totalSize: 总文件大小
5. THE Component_Data_Service SHALL 提供 GET /api/stages/{stageId}/available-components 端点返回可用组件列表
6. THE Component_Data_Service SHALL 提供 GET /api/components/{componentId}/available-fields 端点返回可用字段列表

---

## 需求 8: 条件执行日志记录

**用户故事:** 作为工作流管理员，我希望跟踪条件评估结果，以便审计和排查工作流行为。

#### 验收标准

1. THE Condition_Logger SHALL 将日志统一记录到 api/ow/change-logs 接口
2. WHEN 条件评估完成时，THE Condition_Logger SHALL 记录以下信息：
   - operationType: "ConditionEvaluate"
   - onboardingId: 关联的 Onboarding ID
   - stageId: 关联的 Stage ID
   - operationDescription: 评估结果描述
   - extendedData: 包含 conditionId、conditionName、result、ruleEvaluations
3. WHEN 条件动作执行时，THE Condition_Logger SHALL 记录以下信息：
   - operationType: "ConditionActionExecute"
   - beforeData: 执行前状态
   - afterData: 执行后状态
   - changedFields: 变更的字段列表
   - extendedData: 包含 actionType、actionParameters
4. IF 动作执行失败，THE Condition_Logger SHALL 记录 operationStatus: "Failed" 和 errorMessage
5. THE Operation_Change_Log_API SHALL 支持通过 onboardingId 查询条件评估历史
6. THE Operation_Change_Log_API SHALL 支持通过 stageId 查询条件评估历史
7. THE Operation_Change_Log_API SHALL 支持通过 operationType 过滤日志

---

## 需求 9: 并发控制和事务处理

**用户故事:** 作为系统，我希望在条件评估和动作执行时保证数据一致性，以便避免并发冲突。

#### 验收标准

1. WHEN 评估条件时，THE RulesEngine_Service SHALL 使用数据库事务包装整个流程
2. THE RulesEngine_Service SHALL 使用行级锁（SELECT ... FOR UPDATE）防止并发修改
3. IF 事务执行失败，THE System SHALL 回滚所有变更并返回错误
4. WHEN 多个请求同时完成同一 Stage 时，THE System SHALL 确保只有一个请求成功执行条件评估
5. THE System SHALL 在事务提交后才记录变更日志

---

## 需求 10: RulesEngine 自定义函数

**用户故事:** 作为工作流管理员，我希望能够使用自定义函数扩展规则能力，以便处理复杂的业务场景。

#### 验收标准

1. THE RulesEngine SHALL 注册 RuleUtils 工具类，提供以下函数：
   - Today(): 返回当前日期
   - DaysBetween(start, end): 计算两个日期之间的天数
   - IsWorkday(date): 判断是否为工作日
   - IsEmpty(value): 判断字符串是否为空
   - InList(value, list): 判断值是否在列表中
2. THE Condition_Rule SHALL 支持在表达式中调用自定义函数
3. THE RulesEngine_Service SHALL 在启动时注册所有自定义函数
4. WHEN 自定义函数执行出错时，THE RulesEngine_Service SHALL 记录错误并返回 false

---

## 需求 11: 错误处理和容错

**用户故事:** 作为系统，我希望能够优雅地处理各种错误情况，以便保证工作流的稳定运行。

#### 验收标准

1. IF RulesJson 格式无效，THE RulesEngine_Service SHALL 记录警告日志并进入 Fallback_Stage
2. IF Component 数据获取失败，THE RulesEngine_Service SHALL 将对应字段值设为 null 并继续评估
3. IF Action 执行失败，THE Action_Executor SHALL 记录错误日志但不影响后续 Action 执行
4. IF 引用的 Stage 或 Action 被删除，THE System SHALL 将条件标记为 Invalid 状态
5. WHEN 条件为 Invalid 状态时，THE Workflow_Engine SHALL 跳过条件评估并进入 Fallback_Stage
6. THE System SHALL 为所有错误返回清晰的错误代码和消息
