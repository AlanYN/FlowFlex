# Design — 测试用例与问题清单

> 阶段：test-verification  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 一、后端 Service 单元测试（xUnit + Moq）

### DocumentSnapshotService

| TC ID | 测试方法名 | 前置条件 | 操作 | 预期结果 | 验证方式 | 关联规则 |
|-------|-----------|---------|------|---------|---------|---------|
| TC-BE-01 | `CreateAsync_ValidInput_ReturnsUnitId` | 无同名文档 | 传入合法 InputDto | 返回新文档的 unitId（非空 UUID 字符串） | Assert.NotEmpty | — |
| TC-BE-02 | `SaveAsync_NewDocument_InsertsRecord` | unit_id 不存在 | 调用 SaveAsync | DB 新增一条记录，revision = 1 | Repository mock INSERT 被调用 | BR-01 |
| TC-BE-03 | `SaveAsync_ExistingDocument_UpdatesRecord` | unit_id 已存在 | 调用 SaveAsync | DB UPDATE 该条记录，revision +1 | Repository mock UPDATE 被调用 | BR-01 |
| TC-BE-04 | `GetByUnitIdAsync_NotFound_ThrowsCRMException` | unit_id 不存在 | 调用 GetByUnitIdAsync | 抛出 CRMException，错误码对应"文档不存在" | Assert.ThrowsAsync | — |
| TC-BE-05 | `DeleteAsync_SetsIsValidFalse` | 文档存在 | 调用 DeleteAsync | `is_valid = FALSE`，物理行仍存在 | Repository mock UPDATE is_valid 被调用 | BR-02 |
| TC-BE-06 | `QueryAsync_WithKeyword_FiltersTitle` | 有 5 条文档 | 传入 keyword="销售" | 只返回标题含"销售"的文档 | Assert 结果数量 | — |
| TC-BE-07 | `QueryAsync_WithDocType_FiltersCorrectly` | Sheet/Doc/Slide 各 2 条 | 传入 docType="Sheet" | 只返回 Sheet 类型 | Assert 所有结果 docType=Sheet | — |
| TC-BE-08 | `QueryAsync_Paged_ReturnsCorrectPage` | 共 25 条文档 | pageIndex=2, pageSize=10 | 返回第 11~20 条，total=25 | Assert PagedResult 结构 | — |
| TC-BE-09 | `GetByUnitIdAsync_DifferentTenant_NotFound` | 同 unitId 但不同 tenant | 以 tenant-B 身份查询 tenant-A 的文档 | 抛 CRMException 或返回空 | 多租户隔离验证 | BR-05 |

### DocumentOperationLogService

| TC ID | 测试方法名 | 前置条件 | 操作 | 预期结果 | 验证方式 | 关联规则 |
|-------|-----------|---------|------|---------|---------|---------|
| TC-BE-10 | `AddLogAsync_ValidInput_ReturnsId` | — | 传入合法 LogInputDto | 返回非空日志 ID | Assert.NotEmpty | — |
| TC-BE-11 | `GetCurrentRevisionAsync_ReturnsMaxRevision` | 有 3 条日志 revision 分别为 1/2/3 | 调用 GetCurrentRevisionAsync | 返回 3 | Assert.Equal(3, result) | — |
| TC-BE-12 | `AddLogAsync_FailureSilent_DoesNotThrow` | Repository 抛异常 | 调用 AddLogAsync | 方法正常返回，不向上抛异常 | Assert NoThrow | BR-04 |

---

## 二、后端 API 集成测试

| TC ID | 接口 | 场景 | 预期状态码 | 预期响应 |
|-------|------|------|-----------|---------|
| TC-API-01 | GET /snapshots/:unitId | 文档存在 | 200 | 包含 dataJson、revision |
| TC-API-02 | GET /snapshots/:unitId | 文档不存在 | 404 | code != "200" |
| TC-API-03 | POST /snapshots | 合法请求 | 200 | 返回 unitId |
| TC-API-04 | POST /snapshots/:unitId/save | 首次保存（INSERT） | 200 | true |
| TC-API-05 | POST /snapshots/:unitId/save | 二次保存（UPDATE） | 200 | true |
| TC-API-06 | DELETE /snapshots/:id | 文档存在 | 200 | true，DB is_valid=FALSE |
| TC-API-07 | POST /snapshots/query | 含 keyword + docType | 200 | 返回分页结果，total 正确 |
| TC-API-08 | POST /logs | 合法请求 | 200 | 返回日志 ID |
| TC-API-09 | GET /logs/:unitId | 有日志记录 | 200 | 返回分页日志列表 |
| TC-API-10 | GET /logs/:unitId/revision | 有日志 | 200 | 返回正确 revision 数值 |
| TC-API-11 | POST /snapshots（无 token） | 未认证请求 | 401 | 拒绝访问 |

---

## 三、SignalR 协同测试

| TC ID | 场景 | 前置条件 | 操作 | 预期结果 | 关联规则 |
|-------|------|---------|------|---------|---------|
| TC-HUB-01 | 正常 Join | — | 客户端 A 调用 Join(unitId, userId) | 其他在线成员收到 `join` 事件 | — |
| TC-HUB-02 | Ingest 版本匹配 | currentRev=5 | A 发送 baseRev=5 的 Changeset | A 收到 cs_ack(revision=6)，其他成员收到 new_cs | BR-03 |
| TC-HUB-03 | Ingest 版本冲突 | currentRev=5 | A 发送 baseRev=4 的 Changeset | A 收到 cs_rej | BR-03 |
| TC-HUB-04 | 断开连接 | A、B 在线 | A 断开 | B 收到 `leave` 事件 | — |
| TC-HUB-05 | 并发冲突重试 | currentRev=5 | A、B 同时发送 baseRev=5 | 一方收到 cs_ack，另一方收到 cs_rej 后重试成功 | BR-03 |

---

## 四、前端组件测试（@vue/test-utils + Jest）

### CreateDocumentDialog

| TC ID | 场景 | 操作 | 预期结果 |
|-------|------|------|---------|
| TC-FE-01 | 名称为空提交 | 不填名称，点确认 | 错误提示出现，弹窗不关闭，emit('update:modelValue') 未触发 |
| TC-FE-02 | 正常创建 Sheet | 填名称，选 Sheet，点确认 | 调用 createSnapshot API，emit('created', unitId) |
| TC-FE-03 | 正常创建 Doc | 填名称，选 Doc，点确认 | emit('created', unitId)，docType 为 'Doc' |
| TC-FE-04 | 默认类型为 Sheet | 弹窗打开 | Sheet 类型卡片有 selected 样式 |
| TC-FE-05 | 取消关闭清空 | 点取消 | 表单清空，emit('update:modelValue', false) |

### DocumentCard

| TC ID | 场景 | 操作 | 预期结果 |
|-------|------|------|---------|
| TC-FE-06 | 点击卡片跳转 | 点击卡片主体 | emit('click') 触发 |
| TC-FE-07 | 删除确认 | 点删除菜单项 | ElMessageBox 二次确认弹出 |
| TC-FE-08 | 标题超长截断 | 传入 40 字标题 | 不超出卡片宽度，显示省略号 |
| TC-FE-09 | 类型图标渲染 | docType='Sheet' | 显示绿色 Sheet 图标 |

### DocumentEditor 页（离开确认）

| TC ID | 场景 | 前置条件 | 操作 | 预期结果 |
|-------|------|---------|------|---------|
| TC-FE-10 | 无变更直接离开 | hasUnsaved=false | 点返回 | 直接导航，不弹确认框 |
| TC-FE-11 | 有变更点「不保存离开」| hasUnsaved=true | 点返回→选「不保存离开」 | 导航成功，saveDocument 未被调用 |
| TC-FE-12 | 有变更点「保存并离开」| hasUnsaved=true | 点返回→选「保存并离开」 | saveDocument 被调用，导航成功 |
| TC-FE-13 | 有变更点「取消」 | hasUnsaved=true | 点返回→选「取消」 | 停留在编辑器页，不导航 |

### documentEditorStore

| TC ID | 场景 | 操作 | 预期结果 |
|-------|------|------|---------|
| TC-FE-14 | logMutation fire-and-forget | addOperationLog 抛异常 | store action 不抛异常，返回正常 | BR-04 |
| TC-FE-15 | saveDocument 成功 | 正常调用 | hasUnsaved 变为 false |

---

## 五、样式回归测试

| TC ID | 页面 | 检查项 | 通过标准 |
|-------|------|--------|---------|
| TC-STYLE-01 | 工作流列表页 | Tailwind 改动后样式无变化 | 视觉比对无差异 |
| TC-STYLE-02 | 问卷管理页 | 同上 | 视觉比对无差异 |
| TC-STYLE-03 | 文档列表页 | Univer UI 不受 Tailwind 影响 | Univer 内部 toolbar 样式正常 |
| TC-STYLE-04 | 编辑器页 | Univer Sheet/Doc 编辑器渲染正常 | 编辑器占满容器，无白边 |

---

## 六、问题清单（已识别风险项）

| 问题 ID | 描述 | 严重度 | 关联风险 | 建议处理 |
|---------|------|--------|---------|---------|
| ISS-01 | `important: '#app'` 可能影响 Element Plus Popover/Dropdown 的 z-index | 中 | R-01 | 改动后逐一检查所有下拉/弹窗组件 |
| ISS-02 | Univer Web Worker 在生产环境可能 MIME 错误导致功能降级 | 中 | R-04 | 上线前在类 Prod 环境验证 Worker 正常加载 |
| ISS-03 | `workbookData` 大 JSON（>10MB）传入时内存占用 | 低 | R-02 | 监控内存，必要时分片或压缩 |
| ISS-04 | SignalR 连接 token 传递方式（query string）在某些代理下可能被截断 | 低 | — | 如有问题改为 Header 认证 |
