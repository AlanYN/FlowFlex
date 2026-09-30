# Tasks — 测试任务列表

> 阶段：test-verification  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 后端单元测试任务

- [ ] TV-BE-01：实现 `DocumentSnapshotServiceTests.cs`
  - 文件：`Tests/FlowFlex.Tests/OW/DocumentSnapshotServiceTests.cs`
  - 覆盖 TC-BE-01 ~ TC-BE-09（9 个用例）
  - 参考：`// Arrange / Act / Assert` 模式，Mock `IDocumentSnapshotRepository`

- [ ] TV-BE-02：实现 `DocumentOperationLogServiceTests.cs`
  - 文件：`Tests/FlowFlex.Tests/OW/DocumentOperationLogServiceTests.cs`
  - 覆盖 TC-BE-10 ~ TC-BE-12（3 个用例）
  - 特别验证 TC-BE-12（BR-04 fire-and-forget 不抛异常）

- [ ] TV-BE-03：运行后端测试并确认通过
  - 命令：`dotnet test packages/flowFlex-backend/Tests/FlowFlex.Tests`
  - 通过标准：0 个 Failed，覆盖率 ≥ 80%（Service 层）

---

## 后端 API 集成测试任务

- [ ] TV-API-01：验证 DocumentSnapshotController 端点（TC-API-01 ~ TC-API-07）
  - 使用 Swagger / Postman / xUnit WebApplicationFactory
  - 每个端点至少 happy path + 1 个 error case

- [ ] TV-API-02：验证 DocumentOperationLogController 端点（TC-API-08 ~ TC-API-10）

- [ ] TV-API-03：验证未认证请求返回 401（TC-API-11）

---

## SignalR 协同测试任务

- [ ] TV-HUB-01：SignalR Hub 单元/集成测试（TC-HUB-01 ~ TC-HUB-05）
  - 重点：乐观锁 cs_ack/cs_rej 逻辑（TC-HUB-02、TC-HUB-03）
  - 可用 xUnit + `IHubContext` mock 或 WebSocket 客户端测试

---

## 前端组件测试任务

- [ ] TV-FE-01：`CreateDocumentDialog` 测试（TC-FE-01 ~ TC-FE-05）
  - 工具：@vue/test-utils + Jest
  - 重点：名称空值校验（TC-FE-01）+ 创建后 emit('created')（TC-FE-02）

- [ ] TV-FE-02：`DocumentCard` 测试（TC-FE-06 ~ TC-FE-09）

- [ ] TV-FE-03：`DocumentEditor` 离开确认测试（TC-FE-10 ~ TC-FE-13）
  - 重点：三种选择（保存并离开/不保存/取消）行为验证

- [ ] TV-FE-04：`documentEditorStore` 测试（TC-FE-14 ~ TC-FE-15）
  - 重点：TC-FE-14 验证 logMutation fire-and-forget 不抛异常（BR-04）

---

## 样式回归测试任务

- [ ] TV-STYLE-01：tailwind.config.ts 修改后，人工检查以下页面样式无变化
  - 工作流列表页（/onboard/onboardWorkflow）
  - 问卷管理页（/onboard/questionnaire）
  - 清单页（/onboard/checklist）
  - Dropdown、Popover、el-select 等浮层组件

- [ ] TV-STYLE-02：文档模块样式验证
  - 文档列表页卡片布局、空状态正常
  - 编辑器页 Univer 内部 toolbar/菜单样式不受影响
  - 编辑器容器高度 calc(100vh-56px) 正确占满

---

## 问题追踪任务

- [ ] TV-ISS-01：验证 ISS-01（z-index 冲突）—— Tailwind 改动后所有 el-dropdown/el-popover 正常显示
- [ ] TV-ISS-02：验证 ISS-02（Web Worker MIME）—— 在 Staging 环境确认 Worker 正常加载
