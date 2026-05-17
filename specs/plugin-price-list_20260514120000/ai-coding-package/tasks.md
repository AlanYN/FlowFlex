# Tasks: Plugin Price List — AI Coding Package

## 开发任务 Checklist

<!-- 来源: technical-design/tasks.md -->

### 第一组：数据层

- [ ] **TD-T01** 创建 Entity  
  文件：`packages/flowFlex-backend/Domain/Entities/OW/PluginPriceList.cs`  
  操作：继承 OwEntityBase，定义 CaseCode/CustomerCode/CustomerName/PriceListType/StartDate/EndDate/Data(JSONB)/Status 字段

- [ ] **TD-T02** 创建 Repository 接口  
  文件：`packages/flowFlex-backend/Domain/Repository/OW/IPluginPriceListRepository.cs`  
  操作：继承 IOwBaseRepository<PluginPriceList>，定义 GetByCaseCodeAsync(caseCode, tenantId, appCode)

- [ ] **TD-T03** 创建 Repository 实现  
  文件：`packages/flowFlex-backend/SqlSugarDB/Implements/OW/PluginPriceListRepository.cs`  
  操作：继承 OwBaseRepository<PluginPriceList>，实现 GetByCaseCodeAsync（含 IsValid + 租户过滤）

- [ ] **TD-T04** 创建 Migration  
  文件：`packages/flowFlex-backend/SqlSugarDB/Migrations/Migration_20260514000001_CreatePluginPriceListTable.cs`  
  操作：建表 ff_plugin_price_lists + 唯一索引

- [ ] **TD-T05** 注册 Migration  
  文件：`packages/flowFlex-backend/SqlSugarDB/Migrations/MigrationManager.cs`  
  操作：在 migrations 数组末尾追加新 migration

### 第二组：服务层

- [ ] **TD-T06** 创建 DTO  
  文件：`packages/flowFlex-backend/Application.Contracts/Dtos/OW/PluginPriceList/PluginPriceListInputDto.cs`  
  文件：`packages/flowFlex-backend/Application.Contracts/Dtos/OW/PluginPriceList/PluginPriceListOutputDto.cs`  
  操作：定义输入（CaseCode+Data 等）和输出（含 Permission 字段）DTO

- [ ] **TD-T07** 创建 Service 接口  
  文件：`packages/flowFlex-backend/Application.Contracts/IServices/OW/IPluginPriceListService.cs`  
  操作：定义 GetAsync(caseCode) / SaveAsync(input) / SubmitAsync(caseCode)，继承 IScopedService

- [ ] **TD-T08** 创建 Service 实现  
  文件：`packages/flowFlex-backend/Application/Services/OW/PluginPriceListService.cs`  
  操作：实现业务逻辑：  
  - 通过 caseCode 查 Onboarding 获取 ID  
  - 调用 IPermissionService.CheckCaseAccessAsync() 检查权限  
  - GET：返回数据 + permission 字段  
  - POST：Upsert 逻辑（已 submitted 拒绝）  
  - Submit：状态流转 draft → submitted

- [ ] **TD-T09** 创建 MapProfile  
  文件：`packages/flowFlex-backend/Application/Maps/PluginPriceListMapProfile.cs`  
  操作：Entity ↔ OutputDto 映射，注册到 Program.cs

### 第三组：API 层

- [ ] **TD-T10** 创建 Controller  
  文件：`packages/flowFlex-backend/WebApi/Controllers/OW/PluginPriceListController.cs`  
  操作：  
  - Route: `ow/plugin-price-lists/v{version:apiVersion}`  
  - [Authorize] + [PortalAccess]  
  - GET Action: [HttpGet], [WFEAuthorize(Case.Read)]  
  - POST Save Action: [HttpPost], [WFEAuthorize(Case.Operate)]  
  - POST Submit Action: [HttpPost("submit")], [WFEAuthorize(Case.Operate)]  
  - 使用 Success<T>(data) 返回响应

### 第四组：前端改造 + 部署

- [ ] **TD-T11** 修改 vite.config.js  
  文件：`amanda自己开发的前端项目/price-list-page/vite.config.js`  
  操作：添加 `base: '/plugins/price-list/'`

- [ ] **TD-T12** 前端 API 对接  
  文件：`amanda自己开发的前端项目/price-list-page/src/RateSheetBuilder.vue`  
  操作：  
  - onMounted: fetch GET API，用 caseCode 从 URL 参数获取  
  - handleSave: fetch POST API  
  - handleSubmit: fetch POST submit API  
  - 请求头带 Authorization（从 cookie/localStorage 获取 token）  
  - 加 loading 状态和错误提示

- [ ] **TD-T13** 前端只读模式  
  文件：`amanda自己开发的前端项目/price-list-page/src/RateSheetBuilder.vue`  
  操作：  
  - 根据 API 返回的 permission 字段判断  
  - read 模式：所有 input/select/textarea 加 disabled  
  - read 模式：隐藏 Save/Submit/Add/Delete 按钮  
  - submitted 状态也切换为只读

- [ ] **TD-T14** 重新构建 dist  
  文件：`amanda自己开发的前端项目/price-list-page/`  
  操作：`npm run build`，确认 dist 输出正确

- [ ] **TD-T15** 部署 dist 到前端 public  
  文件：`packages/flowFlex-common/public/plugins/price-list/`  
  操作：将 dist/ 内容复制到该目录

---

<!-- 来源: test-verification/tasks.md -->

### 第五组：测试验证

- [ ] **TV-T01** 手动验证后端 API（Swagger/Postman，TC-001~014）
- [ ] **TV-T02** 手动验证前端交互（浏览器，TC-015~018）
- [ ] **TV-T03** 记录问题清单
- [ ] **TV-T04** 确认所有 TC 通过
