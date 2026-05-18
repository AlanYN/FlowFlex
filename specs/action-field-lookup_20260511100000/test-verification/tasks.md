# Tasks: Action Field Lookup — 测试任务

## 任务概览

| 分组         | 任务数 | 预估工时    |
| ------------ | ------ | ----------- |
| 后端单元测试 | 2      | 0.5 天      |
| API 集成测试 | 1      | 0.25 天     |
| 前端组件测试 | 1      | 0.25 天     |
| 降级场景验证 | 1      | 0.25 天     |
| **合计**     | **5**  | **1.25 天** |

---

## 第一组：后端单元测试

### TASK-TV-001: IntegrationHttpClient 单元测试

- **文件**：`packages/flowFlex-backend/Tests/Services/Integration/IntegrationHttpClientTests.cs`（新增）
- **覆盖用例**：TC-BE-001 ~ TC-BE-011
- **内容**：
  - Mock `IIntegrationRepository`、`IEncryptionService`、`IHttpClientFactory`、`IIntegrationApiLogService`
  - 验证 4 种认证方式的 Header 设置
  - 验证 additionalHeaders 合并和覆盖逻辑
  - 验证超时处理
  - 验证 URL 拼接
  - 验证日志记录调用
- **依赖**：TASK-TD-002 完成

### TASK-TV-002: FieldLookupService 单元测试

- **文件**：`packages/flowFlex-backend/Tests/Services/Action/FieldLookupServiceTests.cs`（新增）
- **覆盖用例**：TC-BE-020 ~ TC-BE-031
- **内容**：
  - Mock `IIntegrationHttpClient`
  - 验证 MappingConfig 解析逻辑
  - 验证 JSONPath 提取（responsePath / displayPath / valuePath）
  - 验证并行执行（Task.WhenAll）
  - 验证单字段降级不影响其他字段
  - 验证 maxOptionsPerField 截断
  - 验证 Preview 返回前 10 条
- **依赖**：TASK-TD-003 完成

---

## 第二组：API 集成测试

### TASK-TV-003: Controller Endpoint 集成测试

- **文件**：`packages/flowFlex-backend/Tests/Controllers/Action/ActionControllerLookupTests.cs`（新增）
- **覆盖用例**：TC-BE-040 ~ TC-BE-054
- **内容**：
  - 使用 WebApplicationFactory 或 Mock 测试
  - 验证 Preview endpoint 正常响应
  - 验证 MappingConfig 保存 endpoint
  - 验证权限控制（未授权返回 401）
  - 验证执行链路中 lookup 结果存储
  - 验证无 lookup 时行为不变
- **依赖**：TASK-TD-004, TASK-TD-005 完成

---

## 第三组：前端组件测试

### TASK-TV-004: 前端组件手动测试清单

- **覆盖用例**：TC-FE-001 ~ TC-FE-015
- **内容**：
  - LookupConfigPanel 组件：初始渲染、必填验证、Test 按钮、Custom Headers
  - ActionConfigDialog 集成：Lookup 列显示、展开/收起、保存/回显、验证
  - 手动测试步骤文档
- **依赖**：TASK-TD-007, TASK-TD-008 完成

---

## 第四组：降级场景验证

### TASK-TV-005: 降级场景端到端验证

- **覆盖用例**：TC-DG-001 ~ TC-DG-006
- **内容**：
  - 配置一个 Integration 连接到测试 Mock API
  - 模拟各种失败场景（超时、500、非 JSON、认证失败）
  - 验证单字段降级不阻断流程
  - 验证无 lookup 配置时零影响
  - 记录测试结果到问题清单
- **依赖**：所有开发任务完成
