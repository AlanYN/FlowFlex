# Context: Action Field Lookup — 需求分析（最终版）

> 更新于 2026-05-14，反映所有迭代后的最终需求。

## 业务目标

为 WFE Action 执行链路提供"从第三方系统实时拉取选项列表"的基础能力，解决选项型字段（枚举、用户 ID、设施代码等）无法直接映射的问题。

---

## 核心用户故事摘要

| ID     | 角色   | 目标             | 关键实现                                            |
| ------ | ------ | ---------------- | --------------------------------------------------- |
| US-001 | 管理员 | 配置 Lookup 字段 | Action 编辑弹窗中 Pre-Execution Lookup 区域         |
| US-002 | 系统   | 运行时拉取选项   | FieldLookupService 并行调用 + JSONPath 提取         |
| US-003 | 系统   | 降级处理         | 单字段失败不阻断、10s 超时                          |
| US-004 | 开发者 | HTTP 基础设施    | IntegrationHttpClient + 直接 HTTP 调用双模式        |
| US-005 | 管理员 | 认证自动处理     | Custom Headers 中 `{{integrationToken}}` 占位符替换 |
| US-006 | 管理员 | 选项预览         | Test 按钮 + POST /action/v1/lookup/preview          |

---

## 数据存储

| 项目        | 位置                                                          |
| ----------- | ------------------------------------------------------------- |
| Lookup 配置 | `ff_action_definitions.action_config` → `lookupMappings` 数组 |
| 执行结果    | `ff_action_executions.execution_input` → `lookupResults`      |

---

## 范围边界

| In Scope（已实现）                            | Out of Scope（后续 AI 阶段）        |
| --------------------------------------------- | ----------------------------------- |
| ActionConfig 中 lookupMappings 数据模型       | AI 匹配逻辑（FieldMatchingService） |
| IntegrationHttpClient + 直接 HTTP 双模式      | 审批 UI 的 AI Assisted 标记         |
| FieldLookupService 选项拉取 + 占位符替换      | 选项列表缓存                        |
| 前端 Pre-Execution Lookup 配置 + 预览         | 精确匹配前置判断                    |
| 执行链路集成 + 降级处理                       | LLM Prompt 构建                     |
| endpoint 和 headers 的 `{{placeholder}}` 替换 | —                                   |

---

## 为 AI 匹配预留的扩展点

1. `FieldLookupResult` 返回 `List<OptionItem>`，AI 服务可直接消费
2. `ActionExecution.ExecutionInput` 中记录 lookupResults，AI 匹配结果可追加
3. `ActionConfig` 中可新增 `aiConfig` 字段控制 AI 匹配行为
4. 执行链路中 Lookup 返回后有明确的"下一步处理"插入点（`EnrichContextWithLookupValues` 之后）
