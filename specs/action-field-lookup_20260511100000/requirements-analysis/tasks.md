# Tasks: Field Lookup（第三方选项数据拉取）

## 任务概览

| 分组         | 任务数 | 预估工时 |
| ------------ | ------ | -------- |
| 后端基础设施 | 4      | 1.5 天   |
| 后端业务逻辑 | 3      | 1 天     |
| 前端配置 UI  | 3      | 1 天     |
| 联调验证     | 2      | 0.5 天   |
| **合计**     | **12** | **4 天** |

---

## 第一组：后端基础设施

### TASK-001: 创建 IntegrationHttpResponse DTO

- **文件**：`Application.Contracts/Dtos/Integration/IntegrationHttpResponseDto.cs`
- **内容**：定义 IntegrationHttpClient 的统一响应模型
- **依赖**：无

### TASK-002: 创建 IIntegrationHttpClient 接口

- **文件**：`Application.Contracts/IServices/Integration/IIntegrationHttpClient.cs`
- **内容**：定义 GetAsync / PostAsync 方法签名
- **依赖**：TASK-001

### TASK-003: 实现 IntegrationHttpClient 服务

- **文件**：`Application/Services/Integration/IntegrationHttpClient.cs`
- **内容**：
  - 根据 IntegrationId 查询 Integration 实体
  - 解密认证凭证
  - 根据 AuthMethod 设置 HTTP Headers（ApiKey / BasicAuth / OAuth2 / BearerToken）
  - 拼接 EndpointUrl + relativePath 发起请求
  - 记录 IntegrationApiLog
  - 超时处理（默认 10s）
- **依赖**：TASK-002

### TASK-004: 创建 Lookup 相关 DTO

- **文件**：`Application.Contracts/Dtos/Action/FieldLookupDto.cs`
- **内容**：
  - `LookupConfig`：endpoint, displayPath, valuePath, responsePath, integrationId
  - `FieldMappingItem`：wfeField, apiField, lookup?
  - `MappingConfigModel`：fieldMappings, lookupConfig
  - `OptionItem`：display, value
  - `FieldLookupResult`：apiField, status, options, error
  - `LookupPreviewRequest`：integrationId, endpoint, displayPath, valuePath, responsePath
  - `LookupPreviewResponse`：success, options, error, totalCount
- **依赖**：无

---

## 第二组：后端业务逻辑

### TASK-005: 创建 IFieldLookupService 接口

- **文件**：`Application.Contracts/IServices/Action/IFieldLookupService.cs`
- **内容**：定义 FetchLookupOptionsAsync / PreviewLookupAsync 方法签名
- **依赖**：TASK-004

### TASK-006: 实现 FieldLookupService

- **文件**：`Application/Services/Action/FieldLookupService.cs`
- **内容**：
  - 解析 MappingConfig JSON 中的 fieldMappings
  - 识别有 lookup 配置的字段
  - 并行调用 IntegrationHttpClient.GetAsync() 获取选项列表
  - 使用 JSONPath（SelectToken）从响应中提取选项数组
  - 使用 displayPath / valuePath 提取每个选项的显示文本和值
  - 单字段失败降级处理（标记 lookup_failed，不阻断其他字段）
  - 实现 PreviewLookupAsync（供前端 Test 按钮使用）
- **依赖**：TASK-003, TASK-005

### TASK-007: 集成到 Action 执行链路 + 暴露 Preview API

- **文件**：
  - `Application/Services/Action/ActionExecutionService.cs`（修改）
  - `WebApi/Controllers/OW/ActionController.cs`（修改，新增 preview endpoint）
- **内容**：
  - 在 ExecuteActionAsync 中，当 triggerMappingId 有值时：
    - 查询 ActionTriggerMapping 获取 MappingConfig
    - 调用 FieldLookupService.FetchLookupOptionsAsync()
    - 将 lookup 结果存入 ExecutionOutput 的 `lookupMetadata` 字段
  - 新增 `POST /ow/actions/v1/lookup/preview` API endpoint
- **依赖**：TASK-006

---

## 第三组：前端配置 UI

### TASK-008: 创建 LookupConfigPanel 组件

- **文件**：`packages/flowFlex-common/src/app/views/actions/components/lookup-config-panel.vue`
- **内容**：
  - 4 个输入框：Options Source / Display Field / Value Field / Response Path
  - Test 按钮 + 预览结果表格（Display | Value，最多 10 条）
  - 调用 preview API 获取预览数据
  - 加载状态和错误提示
- **依赖**：TASK-007（需要 preview API）

### TASK-009: 修改 Action 编辑弹窗的字段映射区域

- **文件**：`packages/flowFlex-common/src/app/views/actions/components/field-mapping-section.vue`（新增）
- **内容**：
  - 每行 mapping 增加"AI Match"toggle 开关
  - 勾选后展开 LookupConfigPanel
  - 取消勾选时收起面板
  - 保存时将 lookup 配置序列化到 MappingConfig JSON
  - 集成到现有 Action 编辑弹窗中
- **依赖**：TASK-008

### TASK-010: 前端 API 定义和类型

- **文件**：
  - `packages/flowFlex-common/src/app/apis/action-lookup.ts`（新增）
  - `packages/flowFlex-common/src/types/action-lookup.d.ts`（新增）
- **内容**：
  - `previewLookupOptions(data: LookupPreviewRequest): Promise<LookupPreviewResponse>`
  - `updateMappingConfig(mappingId: string, config: MappingConfigModel): Promise<void>`
  - TypeScript 类型定义
- **依赖**：无

---

## 第四组：联调验证

### TASK-011: 端到端联调

- **内容**：
  - 配置一个 Integration 连接到测试 API
  - 创建 Action，配置字段映射 + lookup
  - 前端 Test 按钮验证预览功能
  - 触发 Action 执行，验证 lookup 选项正确拉取
  - 验证 ExecutionOutput 中包含 lookupMetadata
- **依赖**：TASK-007, TASK-009

### TASK-012: 降级场景验证

- **内容**：
  - 模拟 lookup endpoint 超时（>10s）→ 验证标记为 lookup_failed
  - 模拟 lookup endpoint 返回 500 → 验证不阻断其他字段
  - 模拟 Integration 认证失败 → 验证错误信息记录
  - 验证无 lookup 配置的普通字段不受影响
- **依赖**：TASK-011
