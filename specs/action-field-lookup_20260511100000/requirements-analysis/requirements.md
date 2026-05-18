# Requirements: Field Lookup（第三方选项数据拉取）

## 概述

在现有 Action 字段映射（field mapping）基础上，扩展 `lookup` 能力：允许配置某些字段从第三方系统实时拉取选项列表，为后续的 AI 匹配或人工选择提供数据基础。

**核心价值**：不管最终是否启用 AI 匹配，"从第三方拉取选项数据"这一基础设施是必须的。本阶段聚焦于此基础能力的构建。

---

## 用户故事

### US-001: 配置 Lookup 字段

**As a** 系统管理员（Integration 配置人员）
**I want** 在 Action 的字段映射配置中，为特定字段启用"Lookup"模式并配置选项来源
**So that** 系统在执行 Action 时能从第三方系统获取该字段的合法选项值

**验收标准（AC）**：

- AC-001-1: 在 Action 字段映射配置 UI 中，每行 mapping 可以勾选"AI Match"开关（toggle）
- AC-001-2: 勾选后展开 lookup 配置面板，包含以下字段：
  - Options Source（endpoint）：相对于 Integration base URL 的 API 路径
  - Display Field（displayPath）：选项中用于显示的字段 JSONPath
  - Value Field（valuePath）：选项中用于传值的字段 JSONPath
  - Response Path（responsePath，可选）：响应中选项数组的 JSONPath
- AC-001-3: 取消勾选时，lookup 配置面板收起，已填写的配置保留但不生效
- AC-001-4: 保存时，lookup 配置写入 `ActionTriggerMapping.MappingConfig` 的 JSONB 字段中
- AC-001-5: lookup 配置中的 endpoint 使用的是关联 Integration 的 base URL + 认证信息

### US-002: 运行时拉取选项列表

**As a** 系统（Action 执行引擎）
**I want** 在 Action 执行时，对配置了 lookup 的字段，自动调用第三方 API 获取选项列表
**So that** 选项数据始终是最新的，无需维护本地字典

**验收标准（AC）**：

- AC-002-1: Action 执行时，识别 MappingConfig 中有 `lookup` 配置的字段
- AC-002-2: 使用关联 Integration 的 EndpointUrl + 认证信息，调用 lookup.endpoint 获取选项列表
- AC-002-3: 使用 `responsePath` 从响应中提取选项数组（默认为根级数组）
- AC-002-4: 使用 `displayPath` 和 `valuePath` 从每个选项中提取显示文本和值
- AC-002-5: 多个 lookup 字段的选项列表获取应并行执行（`Task.WhenAll`）
- AC-002-6: 获取到的选项列表作为结构化数据返回，供下游消费（AI 匹配或人工选择）

### US-003: Lookup 调用降级处理

**As a** 系统（Action 执行引擎）
**I want** 当 lookup API 调用失败时，不阻断整个 Action 执行流程
**So that** 系统具备容错能力，单个字段的 lookup 失败不影响其他字段

**验收标准（AC）**：

- AC-003-1: 单个 lookup API 调用失败（超时、网络错误、非 2xx 响应）时，该字段标记为 `lookup_failed`
- AC-003-2: 其他字段的 lookup 不受影响，继续正常执行
- AC-003-3: lookup 调用超时阈值为 10 秒
- AC-003-4: 失败信息记录到 ActionExecution 的 ExecutionOutput 中，便于排查
- AC-003-5: 没有 lookup 配置的普通字段不受任何影响，走原有直接映射逻辑

### US-004: Integration HTTP Client 封装

**As a** 开发者
**I want** 有一个统一的 `IntegrationHttpClient` 服务，能根据 IntegrationId 自动获取认证信息并发起 HTTP 请求
**So that** lookup 调用和未来的其他 Integration 相关 HTTP 调用都能复用此基础设施

**验收标准（AC）**：

- AC-004-1: 新建 `IIntegrationHttpClient` 接口和 `IntegrationHttpClient` 实现
- AC-004-2: 支持根据 IntegrationId 自动获取 Integration 的 EndpointUrl 和认证信息
- AC-004-3: 支持 4 种认证方式：ApiKey、BasicAuth、OAuth2、BearerToken
- AC-004-4: 支持 GET/POST 方法
- AC-004-5: 支持配置超时时间（默认 10 秒）
- AC-004-6: 请求和响应日志记录到 IntegrationApiLog

### US-005: Lookup 配置中的 IntegrationId 关联

**As a** 系统管理员
**I want** lookup 配置自动关联到当前 Action 所属的 Integration
**So that** 不需要在每个 lookup 字段中重复指定 Integration 信息

**验收标准（AC）**：

- AC-005-1: lookup 的 endpoint 相对于 Action 所关联的 Integration 的 EndpointUrl
- AC-005-2: 通过 ActionTriggerMapping（TriggerType = "Integration", TriggerSourceId = IntegrationId）反查关联的 Integration，使用该 Integration 的认证信息（EndpointUrl + EncryptedCredentials + AuthMethod）
- AC-005-3: 如果需要使用不同的 Integration（跨系统查询），lookup 配置中可选填 `integrationId` 字段覆盖默认关联
- AC-005-4: 前端配置 UI 中，默认不显示 integrationId 字段（使用当前 Integration），提供"高级"选项展开

### US-006: 选项列表预览（前端，Action 编辑弹窗内）

**As a** 系统管理员
**I want** 在 Action 编辑弹窗的字段映射区域配置 lookup 后，能预览拉取到的选项列表
**So that** 可以验证配置是否正确，无需等到 Action 实际执行时才发现问题

**验收标准（AC）**：

- AC-006-1: Action 编辑弹窗中，字段映射区域的 lookup 配置面板提供"Test"按钮
- AC-006-2: 点击后调用后端 API，使用当前配置的 endpoint + Integration 认证拉取选项
- AC-006-3: 返回前 10 条选项数据，以表格形式展示（Display | Value）
- AC-006-4: 如果调用失败，显示错误信息
- AC-006-5: 预览不影响任何持久化数据

---

## 非功能需求

| 类别     | 要求                                                      |
| -------- | --------------------------------------------------------- |
| 性能     | 单个 lookup API 调用超时 ≤ 10s；多个 lookup 并行执行      |
| 兼容性   | 向后兼容：无 lookup 配置的 mapping 行为不变               |
| 安全性   | 认证信息通过 IntegrationHttpClient 统一管理，不在前端暴露 |
| 可观测性 | lookup 调用记录到 IntegrationApiLog，包含请求/响应/耗时   |
| 可扩展性 | 为后续 AI 匹配预留接口：选项列表以结构化格式返回          |

---

## 范围边界

### 本阶段包含（In Scope）

- MappingConfig 中 lookup 属性的数据模型设计
- IntegrationHttpClient 基础设施
- Action 执行链路中 lookup 选项拉取逻辑
- 前端 mapping 配置 UI 中的 lookup 开关和配置面板
- 前端 lookup 预览功能
- 降级处理

### 本阶段不包含（Out of Scope）

- AI 匹配逻辑（等 Amanda 确认后再做）
- 审批 UI 中的 AI Assisted 标记
- 选项列表缓存（后续优化）
- 精确匹配前置判断（后续 AI 阶段再加）
