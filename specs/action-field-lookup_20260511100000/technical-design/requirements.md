# Requirements: Action Field Lookup — 技术方案需求

## 技术方案验收标准

### TR-001: IntegrationHttpClient 服务

- AC-TR-001-1: 新建 `IIntegrationHttpClient` 接口，继承 `IScopedService`
- AC-TR-001-2: 支持 GET/POST 方法，接收 integrationId + relativePath + additionalHeaders + timeout
- AC-TR-001-3: 自动查询 Integration 实体获取 EndpointUrl + AuthMethod + EncryptedCredentials
- AC-TR-001-4: 使用 `IEncryptionService.Decrypt()` 解密凭证
- AC-TR-001-5: 根据 AuthMethod 正确设置认证 Headers（ApiKey/BasicAuth/OAuth2/BearerToken）
- AC-TR-001-6: 合并 additionalHeaders（lookup 自定义 headers），冲突时 additionalHeaders 优先
- AC-TR-001-7: 使用 `IIntegrationApiLogService.LogApiCallAsync()` 记录请求日志
- AC-TR-001-8: 支持可配置超时（默认 10s）
- AC-TR-001-9: 返回统一的 `IntegrationHttpResponse`（statusCode, body, isSuccess, error）

### TR-002: FieldLookupService 服务

- AC-TR-002-1: 新建 `IFieldLookupService` 接口，继承 `IScopedService`
- AC-TR-002-2: `FetchLookupOptionsAsync` 解析 MappingConfig JSON 中的 fieldMappings
- AC-TR-002-3: 识别有 `lookup` 属性的字段，使用 `Task.WhenAll` 并行调用
- AC-TR-002-4: 使用 `JToken.SelectToken(responsePath)` 提取选项数组
- AC-TR-002-5: 使用 `JToken.SelectToken(displayPath/valuePath)` 提取每个选项的 display 和 value
- AC-TR-002-6: 单字段 lookup 失败时标记 `lookup_failed`，不影响其他字段
- AC-TR-002-7: `PreviewLookupAsync` 支持前端 Test 按钮，返回前 10 条 + totalCount

### TR-003: 执行链路集成

- AC-TR-003-1: 在 `ActionExecutionService.ExecuteActionAsync` 中，当 triggerMappingId 有值时查询 MappingConfig
- AC-TR-003-2: 通过 TriggerMapping 的 TriggerType="Integration" + TriggerSourceId 获取 IntegrationId
- AC-TR-003-3: 调用 `FieldLookupService.FetchLookupOptionsAsync()` 获取选项列表
- AC-TR-003-4: 将 lookup 结果存入 `ActionExecution.ExecutionOutput` 的 `lookupMetadata` 字段
- AC-TR-003-5: 无 lookup 配置时，行为与当前完全一致（零影响）

### TR-004: Preview API Endpoint

- AC-TR-004-1: 新增 `POST /action/v1/lookup/preview` endpoint
- AC-TR-004-2: 接收 `LookupPreviewRequest`（integrationId, endpoint, displayPath, valuePath, responsePath, headers）
- AC-TR-004-3: 返回 `LookupPreviewResponse`（success, options[], error, totalCount）
- AC-TR-004-4: 需要 `[Authorize]` 和 `[WFEAuthorize(PermissionConsts.Tool.Read)]`

### TR-005: MappingConfig 保存 API

- AC-TR-005-1: 新增 `PUT /action/v1/trigger-mappings/{id}/mapping-config` endpoint
- AC-TR-005-2: 接收 MappingConfig JSON body，更新 ActionTriggerMapping.MappingConfig 字段
- AC-TR-005-3: 验证 JSON 格式合法性
- AC-TR-005-4: 需要 `[Authorize]` 和 `[WFEAuthorize(PermissionConsts.Tool.Update)]`

### TR-006: 前端组件

- AC-TR-006-1: `LookupConfigPanel.vue` 组件实现完整的配置 + 预览功能
- AC-TR-006-2: 修改 `ActionConfigDialog.vue` 的 Field Mapping 表格，增加 Lookup 列和 expand
- AC-TR-006-3: 保存时将 lookup 配置序列化到 MappingConfig，加载时正确回显
- AC-TR-006-4: TypeScript 类型定义完整
