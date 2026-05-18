# Design: Action Field Lookup — 测试用例 + 问题清单

## 1. 后端单元测试用例

### 1.1 IntegrationHttpClient 测试

| TC-ID     | 测试场景                          | 输入                                                | 预期结果                                             |
| --------- | --------------------------------- | --------------------------------------------------- | ---------------------------------------------------- |
| TC-BE-001 | ApiKey 认证 GET 请求              | integrationId(ApiKey), "/api/users"                 | 请求头包含 X-API-Key 和 Authorization: ApiKey        |
| TC-BE-002 | BasicAuth 认证 GET 请求           | integrationId(BasicAuth), "/api/data"               | 请求头包含 Authorization: Basic {base64}             |
| TC-BE-003 | OAuth2 认证 GET 请求              | integrationId(OAuth2), "/api/items"                 | 先获取 token，再用 Bearer token 请求                 |
| TC-BE-004 | BearerToken 认证 GET 请求         | integrationId(BearerToken), "/api/list"             | 请求头包含 Authorization: Bearer {token}             |
| TC-BE-005 | additionalHeaders 合并            | headers: {"X-Custom": "val"}                        | 请求头包含 X-Custom: val                             |
| TC-BE-006 | additionalHeaders 覆盖认证 header | headers: {"Authorization": "custom"}                | Authorization 被覆盖为 "custom"                      |
| TC-BE-007 | 超时处理                          | timeout=1s, 目标延迟 5s                             | 返回 IsSuccess=false, Error 包含 timeout             |
| TC-BE-008 | Integration 不存在                | integrationId=999999                                | 返回 IsSuccess=false, Error 包含 "not found"         |
| TC-BE-009 | URL 拼接正确性                    | EndpointUrl="https://api.com/", path="/users"       | 请求 URL 为 "https://api.com/users"                  |
| TC-BE-010 | URL 拼接（无尾斜杠）              | EndpointUrl="https://api.com", path="users"         | 请求 URL 为 "https://api.com/users"                  |
| TC-BE-011 | 日志记录                          | 任意成功请求                                        | IntegrationApiLog 有对应记录                         |
| TC-BE-012 | 完整 URL 模式                     | path="https://other.com/api/users"                  | 请求 URL 为完整 URL，不拼接 EndpointUrl              |
| TC-BE-013 | OAuth2 + 完整 URL                 | AuthMethod=OAuth2, path="https://api.crm.com/users" | 先从 EndpointUrl 获取 token，再用 token 请求完整 URL |

### 1.2 FieldLookupService 测试

| TC-ID     | 测试场景                      | 输入                                    | 预期结果                                      |
| --------- | ----------------------------- | --------------------------------------- | --------------------------------------------- |
| TC-BE-020 | 正常解析 MappingConfig        | 含 2 个 lookup 字段的 JSON              | 返回 2 个 FieldLookupResult                   |
| TC-BE-021 | 无 lookup 字段                | 所有字段无 lookup 属性                  | 返回空列表                                    |
| TC-BE-022 | MappingConfig 为空 JObject    | {}                                      | 返回空列表                                    |
| TC-BE-023 | responsePath 提取数组         | responsePath="$.data", 响应含 data 数组 | 正确提取 data 下的选项                        |
| TC-BE-024 | responsePath 为空（根级数组） | responsePath=null, 响应为根级数组       | 正确提取根级选项                              |
| TC-BE-025 | displayPath/valuePath 提取    | displayPath="$.name", valuePath="$.id"  | 每个选项正确提取 display 和 value             |
| TC-BE-026 | 单字段 lookup 失败降级        | 字段 A 成功，字段 B 超时                | A 返回 success，B 返回 lookup_failed          |
| TC-BE-027 | 并行执行验证                  | 3 个 lookup 字段，每个延迟 1s           | 总耗时 ≈ 1s（非 3s）                          |
| TC-BE-028 | maxOptionsPerField 截断       | 响应含 300 条，maxOptions=200           | 返回 200 条                                   |
| TC-BE-029 | responsePath 指向非数组       | responsePath="$.name"（字符串）         | 返回 lookup_failed                            |
| TC-BE-030 | 自定义 integrationId 覆盖     | lookup.integrationId=另一个 ID          | 使用指定的 Integration 认证                   |
| TC-BE-031 | Preview 返回前 10 条          | 响应含 50 条                            | PreviewLookupAsync 返回 10 条 + totalCount=50 |

### 1.3 ActionExecutionService 集成测试

| TC-ID     | 测试场景                   | 输入                                           | 预期结果                                       |
| --------- | -------------------------- | ---------------------------------------------- | ---------------------------------------------- |
| TC-BE-040 | 有 lookup 的执行链路       | triggerMappingId 有值，MappingConfig 含 lookup | ExecutionInput 包含 lookupResults              |
| TC-BE-041 | 无 triggerMappingId        | triggerMappingId=null                          | 跳过 lookup，行为不变                          |
| TC-BE-042 | MappingConfig 为空         | triggerMappingId 有值，MappingConfig={}        | 跳过 lookup，行为不变                          |
| TC-BE-043 | TriggerType 非 Integration | TriggerType="Stage"                            | 跳过 lookup（无 IntegrationId）                |
| TC-BE-044 | lookup 全部失败            | 所有 lookup 超时                               | Action 仍然执行，ExecutionInput 含 failed 记录 |

### 1.4 API Endpoint 测试

| TC-ID     | 测试场景                        | 输入                        | 预期结果                       |
| --------- | ------------------------------- | --------------------------- | ------------------------------ |
| TC-BE-050 | Preview 成功                    | 有效的 LookupPreviewRequest | 200, options 最多 10 条        |
| TC-BE-051 | Preview 失败（endpoint 不存在） | endpoint="/nonexistent"     | 200, success=false, error 有值 |
| TC-BE-052 | Preview 未授权                  | 无 Authorization header     | 401                            |
| TC-BE-053 | MappingConfig 保存成功          | 有效 JSON body              | 200, true                      |
| TC-BE-054 | MappingConfig 保存（无效 ID）   | id=999999                   | 404                            |

---

## 2. 前端测试用例

### 2.1 LookupConfigPanel 组件测试

| TC-ID     | 测试场景                 | 操作                                    | 预期结果                             |
| --------- | ------------------------ | --------------------------------------- | ------------------------------------ |
| TC-FE-001 | 初始渲染                 | 组件挂载                                | 显示 4 个输入框 + Test 按钮 disabled |
| TC-FE-002 | 填写必填字段后 Test 可用 | 填写 endpoint + displayPath + valuePath | Test 按钮 enabled                    |
| TC-FE-003 | Test 成功                | 点击 Test，API 返回成功                 | 显示选项表格                         |
| TC-FE-004 | Test 失败                | 点击 Test，API 返回错误                 | 显示 el-alert error                  |
| TC-FE-005 | Custom Headers 展开/收起 | 点击 Custom Headers 标题                | 切换展开状态                         |
| TC-FE-006 | 添加 Header 行           | 点击 Add Header                         | 新增空行                             |
| TC-FE-007 | 删除 Header 行           | 点击 ✕ 按钮                             | 移除该行                             |
| TC-FE-008 | disabled 状态            | props.disabled=true                     | 所有输入框和按钮 disabled            |

### 2.2 ActionConfigDialog Lookup 集成测试

| TC-ID     | 测试场景             | 操作                       | 预期结果                       |
| --------- | -------------------- | -------------------------- | ------------------------------ |
| TC-FE-010 | Lookup 列显示        | 打开 Field Mapping         | 每行显示 Lookup switch         |
| TC-FE-011 | 打开 Lookup 开关     | 切换 switch 为 on          | 展开 LookupConfigPanel         |
| TC-FE-012 | 关闭 Lookup 开关     | 切换 switch 为 off         | 收起面板，数据保留             |
| TC-FE-013 | 保存含 Lookup 配置   | 填写 lookup + 保存         | MappingConfig 包含 lookup 数据 |
| TC-FE-014 | 回显 Lookup 配置     | 编辑已有 Action            | Lookup 开关 on + 配置值正确    |
| TC-FE-015 | 保存验证（必填未填） | Lookup on 但 endpoint 为空 | 阻止保存，高亮字段             |

---

## 3. 降级场景专项测试

| TC-ID     | 场景                    | 模拟方式                       | 预期行为                                        |
| --------- | ----------------------- | ------------------------------ | ----------------------------------------------- |
| TC-DG-001 | Lookup API 超时         | 目标 endpoint 延迟 > 10s       | 该字段 status=lookup_failed，其他字段正常       |
| TC-DG-002 | Lookup API 返回 500     | Mock 返回 500                  | 该字段 status=lookup_failed，error 包含 500     |
| TC-DG-003 | Lookup API 返回非 JSON  | 返回 HTML                      | 该字段 status=lookup_failed，error 包含 parse   |
| TC-DG-004 | Integration 认证失败    | 错误的 credentials             | 该字段 status=lookup_failed，error 包含 401/403 |
| TC-DG-005 | responsePath 不存在     | responsePath="$.nonexistent"   | 该字段 status=lookup_failed                     |
| TC-DG-006 | 无 lookup 配置的 Action | MappingConfig 无 fieldMappings | 完全不触发 lookup，零影响                       |

---

## 4. 问题清单

| 问题 ID | 严重度 | 描述         | 状态 |
| ------- | ------ | ------------ | ---- |
| —       | —      | 暂无已知问题 | —    |

> 问题清单将在测试执行过程中更新。
