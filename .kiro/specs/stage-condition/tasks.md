# 实现计划: Stage Condition

## 概述

本实现计划将 Stage Condition 功能分解为可执行的编码任务，按照增量开发方式组织，确保每个步骤都能构建在前一步骤之上。

---

## 任务列表

- [x] 1. 创建数据模型和数据库迁移
  - [x] 1.1 创建 StageCondition 实体类
    - 在 `Domain/Entities/OW/` 目录下创建 `StageCondition.cs`
    - 继承 `OwEntityBase` 基类
    - 定义所有字段：StageId、WorkflowId、Name、Description、RulesJson、ActionsJson、FallbackStageId、IsActive、Status
    - _Requirements: 1.1-1.9, 2.9, 3.11_

  - [x] 1.2 创建数据库迁移脚本
    - 创建 `ff_stage_condition` 表的 SQL 脚本
    - 添加外键约束和唯一约束（stage_id + tenant_id）
    - 创建索引
    - _Requirements: 1.8_

- [x] 2. 创建 DTO 和接口定义
  - [x] 2.1 创建 StageCondition DTOs
    - 在 `Application.Contracts/Dtos/OW/StageCondition/` 目录下创建
    - `StageConditionInputDto.cs` - 创建/更新输入
    - `StageConditionOutputDto.cs` - 查询输出
    - `ConditionValidationResult.cs` - 验证结果
    - _Requirements: 1.1-1.5, 6.5_

  - [x] 2.2 创建 RulesEngine 相关 DTOs
    - `ConditionEvaluationResult.cs` - 评估结果
    - `RuleEvaluationDetail.cs` - 规则评估详情
    - `ActionExecutionResult.cs` - 动作执行结果
    - `ActionExecutionContext.cs` - 动作执行上下文
    - _Requirements: 4.3-4.6_

  - [x] 2.3 创建 Component 数据 DTOs
    - `ChecklistData.cs` - Checklist 组件数据
    - `QuestionnaireData.cs` - Questionnaire 组件数据
    - `AttachmentData.cs` - 附件组件数据
    - `AvailableComponent.cs` - 可用组件
    - `AvailableField.cs` - 可用字段
    - _Requirements: 7.1-7.6_

  - [x] 2.4 创建服务接口
    - `IStageConditionService.cs` - 条件 CRUD 服务接口
    - `IRulesEngineService.cs` - 规则引擎服务接口
    - `IComponentDataService.cs` - 组件数据服务接口
    - `IActionExecutor.cs` - 动作执行器接口
    - _Requirements: 1.1-1.9, 4.1-4.12, 7.1-7.6_

- [x] 3. 实现 Component 数据服务
  - [x] 3.1 实现 ComponentDataService
    - 创建 `ComponentDataService.cs`
    - 实现 `GetChecklistDataAsync` - 从 ChecklistTaskInstance 获取数据
    - 实现 `GetQuestionnaireDataAsync` - 从 QuestionnaireAnswer 获取数据
    - 实现 `GetAttachmentDataAsync` - 从 Attachment 获取数据
    - 实现 `GetFieldsDataAsync` - 从 Onboarding.DynamicData 获取数据
    - _Requirements: 7.1-7.4_

  - [x] 3.2 实现可用组件查询
    - 实现 `GetAvailableComponentsAsync` - 返回 Stage 的可用组件列表
    - 实现 `GetAvailableFieldsAsync` - 返回组件的可用字段列表
    - 只返回当前 Stage 和之前 Stage 的组件
    - _Requirements: 7.5-7.6, 2.4_

- [x] 4. 实现 RulesEngine 服务
  - [x] 4.1 创建 RuleUtils 工具类
    - 创建 `RuleUtils.cs` 静态类
    - 实现 `Today()` - 返回当前日期
    - 实现 `DaysBetween(start, end)` - 计算天数差
    - 实现 `IsWorkday(date)` - 判断工作日
    - 实现 `IsEmpty(value)` - 判断空值
    - 实现 `InList(value, list)` - 判断列表包含
    - _Requirements: 10.1-10.4_

  - [x] 4.2 实现 RulesEngineService
    - 创建 `RulesEngineService.cs`
    - 配置 ReSettings 注册自定义函数
    - 实现 `BuildInputDataAsync` - 构建规则输入数据对象
    - 实现 `EvaluateConditionAsync` - 执行规则评估
    - 处理评估结果（所有规则 IsSuccess=true 时条件满足）
    - _Requirements: 4.1-4.5, 2.1-2.8_

  - [ ]* 4.3 编写 RulesEngineService 单元测试
    - 测试各种表达式评估（比较、字符串、空值、数学运算）
    - 测试嵌套规则（AND/OR 组合）
    - 测试自定义函数调用
    - 测试无效 JSON 处理
    - _Requirements: 2.1-2.8, 10.1-10.4_

- [x] 5. 实现 Action 执行器
  - [x] 5.1 创建 ConditionAction 模型
    - 创建 `ConditionAction.cs` - 动作配置模型
    - 定义动作类型枚举：GoToStage、SkipStage、EndWorkflow、SendNotification、UpdateField、TriggerAction、AssignUser
    - _Requirements: 3.1_

  - [x] 5.2 实现 ActionExecutor
    - 创建 `ActionExecutor.cs`
    - 实现 `ExecuteActionsAsync` - 按 order 顺序执行动作
    - 实现 `ExecuteGoToStageAsync` - 跳转到目标 Stage
    - 实现 `ExecuteSkipStageAsync` - 跳过下一阶段
    - 实现 `ExecuteEndWorkflowAsync` - 结束工作流
    - 实现 `ExecuteSendNotificationAsync` - 发送通知
    - 实现 `ExecuteUpdateFieldAsync` - 更新字段
    - 实现 `ExecuteTriggerActionAsync` - 触发预定义动作
    - 实现 `ExecuteAssignUserAsync` - 分配用户
    - _Requirements: 3.1-3.11, 4.6-4.10_

  - [x] 5.3 实现 Stage 跳转逻辑
    - 更新 Onboarding.CurrentStageId
    - 将被跳过的中间阶段标记为 Skipped 状态
    - 更新 StagesProgress
    - _Requirements: 4.7-4.9_

  - [ ]* 5.4 编写 ActionExecutor 单元测试
    - 测试各种动作类型执行
    - 测试动作顺序执行
    - 测试动作失败后继续执行
    - _Requirements: 3.1-3.11_

- [x] 6. 实现 StageCondition 服务
  - [x] 6.1 实现 StageConditionService CRUD
    - 创建 `StageConditionService.cs`
    - 实现 `CreateAsync` - 创建条件（验证 Stage 唯一性）
    - 实现 `UpdateAsync` - 更新条件
    - 实现 `DeleteAsync` - 删除条件
    - 实现 `GetByIdAsync` - 获取条件详情
    - 实现 `GetByStageIdAsync` - 按 Stage 查询条件
    - 实现 `GetByWorkflowIdAsync` - 按 Workflow 查询所有条件
    - _Requirements: 1.1-1.8_

  - [x] 6.2 实现条件验证服务
    - 实现 `ValidateAsync` - 验证条件配置
    - 验证 RulesJson 格式符合 RulesEngine 规范
    - 验证引用的 Stages 存在且活动
    - 验证引用的 ActionDefinitions 存在且启用
    - 验证 ActionsJson 格式正确
    - 检测循环路径并返回警告
    - _Requirements: 1.9, 6.1-6.6_

  - [x] 6.3 实现租户隔离和权限检查
    - 所有查询添加 TenantId 过滤
    - 创建/更新操作验证 WorkflowWrite 权限
    - 查询操作验证 WorkflowRead 权限
    - _Requirements: 5.1-5.5_

  - [ ]* 6.4 编写 StageConditionService 单元测试
    - 测试 CRUD 操作
    - 测试 Stage 唯一性约束
    - 测试验证逻辑
    - _Requirements: 1.1-1.9, 6.1-6.6_

- [x] 7. 检查点 - 确保所有测试通过
  - 运行所有单元测试
  - 如有问题请询问用户

- [x] 8. 实现 API 控制器
  - [x] 8.1 创建 StageConditionController
    - 创建 `StageConditionController.cs`
    - POST `/api/stage-conditions` - 创建条件
    - PUT `/api/stage-conditions/{id}` - 更新条件
    - DELETE `/api/stage-conditions/{id}` - 删除条件
    - GET `/api/stage-conditions/{id}` - 获取条件详情
    - 添加 `[WFEAuthorize]` 权限验证
    - _Requirements: 1.1-1.3, 5.2-5.3_

  - [x] 8.2 扩展 StageController
    - GET `/api/stages/{stageId}/condition` - 获取 Stage 的条件
    - GET `/api/stages/{stageId}/available-components` - 获取可用组件
    - _Requirements: 1.4, 7.5_

  - [x] 8.3 扩展 WorkflowController
    - GET `/api/workflows/{workflowId}/conditions` - 获取 Workflow 所有条件
    - _Requirements: 1.5_

  - [x] 8.4 创建验证端点
    - POST `/api/stage-conditions/{id}/validate` - 验证条件配置
    - GET `/api/components/{componentId}/available-fields` - 获取可用字段
    - _Requirements: 1.9, 7.6_

- [x] 9. 集成条件评估到 Stage 完成流程
  - [x] 9.1 修改 OnboardingService.CompleteStageAsync
    - 在 Stage 完成后调用 RulesEngineService.EvaluateConditionAsync
    - 如果没有条件配置，直接进入下一阶段
    - 如果条件满足，执行配置的动作
    - 如果条件不满足，进入 Fallback Stage
    - _Requirements: 4.1-4.2, 4.11_

  - [x] 9.2 实现并发控制
    - 使用数据库事务包装整个评估流程
    - 使用行级锁（SELECT ... FOR UPDATE）防止并发修改
    - 确保只有一个请求成功执行条件评估
    - 事务提交后才记录变更日志
    - _Requirements: 9.1-9.5_

  - [x] 9.3 实现错误处理和容错
    - RulesJson 格式无效时记录警告并进入 Fallback
    - Component 数据获取失败时设为 null 继续评估
    - Action 执行失败时记录错误但继续后续 Action
    - 引用的 Stage/Action 被删除时标记条件为 Invalid
    - _Requirements: 11.1-11.6, 4.12_

- [x] 10. 实现日志记录
  - [x] 10.1 实现条件评估日志
    - 调用 OperationChangeLogService 记录评估结果
    - operationType: "ConditionEvaluate"
    - 记录 conditionId、conditionName、result、ruleEvaluations
    - _Requirements: 8.1-8.2_

  - [x] 10.2 实现动作执行日志
    - 记录每个动作的执行结果
    - operationType: "ConditionActionExecute"
    - 记录 beforeData、afterData、changedFields
    - 失败时记录 operationStatus: "Failed" 和 errorMessage
    - _Requirements: 8.3-8.4_

  - [x] 10.3 扩展 ChangeLog 查询
    - 支持通过 onboardingId 查询条件评估历史
    - 支持通过 stageId 查询条件评估历史
    - 支持通过 operationType 过滤日志
    - _Requirements: 8.5-8.7_

- [x] 11. 检查点 - 确保所有测试通过
  - 运行所有单元测试和集成测试
  - 如有问题请询问用户

- [x] 12. 依赖注入配置
  - [x] 12.1 注册服务
    - 在 DI 容器中注册 IStageConditionService (通过 IScopedService 自动注册)
    - 注册 IRulesEngineService (通过 IScopedService 自动注册)
    - 注册 IComponentDataService (通过 IScopedService 自动注册)
    - 注册 IConditionActionExecutor (通过 IScopedService 自动注册)
    - _Requirements: 所有_

- [x] 13. 最终检查点 - 编译通过
  - 所有代码编译成功
  - 接口重命名为 IConditionActionExecutor 避免与现有 IActionExecutor 冲突
  - 修复了所有类型引用和字段名称问题

- [x] 14. RulesJson 格式兼容性
  - [x] 14.1 支持前端自定义格式转换
    - 前端格式: `{"logic":"AND","rules":[{"fieldPath":"...","operator":"==","value":"..."}]}`
    - RulesEngine格式: `[{"WorkflowName":"StageCondition","Rules":[{"RuleName":"Rule1","Expression":"..."}]}]`
    - 在 StageConditionService.ValidateRulesJsonAsync 中实现格式检测和转换
    - 在 RulesEngineService.ExecuteRulesAsync 中实现运行时格式转换
    - 支持 AND/OR 逻辑组合
    - 支持多种操作符: ==, !=, >, <, >=, <=, contains, startswith, endswith, isnull, isnotnull, isempty, isnotempty

- [x] 15. 代码规范修复
  - [x] 15.1 修复 AppCode 大写问题
    - 将 "DEFAULT" 改为 "default" 符合代码规范

---

## 备注

- 标记 `*` 的任务为可选任务，可跳过以加快 MVP 开发
- 每个任务都引用了具体的需求编号以便追溯
- 检查点任务用于确保增量验证
