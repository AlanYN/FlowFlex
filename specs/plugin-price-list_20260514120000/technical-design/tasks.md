# Tasks: Plugin Price List — 技术方案设计阶段

## 开发任务列表

### 第一组：数据层（无外部依赖，最先执行）

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T01 | 创建 Entity | `Domain/Entities/OW/PluginPriceList.cs` | 继承 OwEntityBase，定义所有字段 | ✅ completed |
| TD-T02 | 创建 Repository 接口 | `Domain/Repository/OW/IPluginPriceListRepository.cs` | 定义 GetByCaseCodeAsync 等方法 | ✅ completed |
| TD-T03 | 创建 Repository 实现 | `SqlSugarDB/Implements/OW/PluginPriceListRepository.cs` | 继承 OwBaseRepository，实现查询 | ✅ completed |
| TD-T04 | 创建 Migration | `SqlSugarDB/Migrations/Migration_20260514000001_CreatePluginPriceListTable.cs` | 建表 + 唯一索引 | ✅ completed |
| TD-T05 | 注册 Migration | `SqlSugarDB/Migrations/MigrationManager.cs` | 在 migrations 数组末尾追加 | ✅ completed |

### 第二组：服务层（依赖第一组）

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T06 | 创建 DTO | `Application.Contracts/Dtos/OW/PluginPriceList/` | InputDto + OutputDto | ✅ completed |
| TD-T07 | 创建 Service 接口 | `Application.Contracts/IServices/OW/IPluginPriceListService.cs` | 定义 GetAsync/SaveAsync/SubmitAsync | ✅ completed |
| TD-T08 | 创建 Service 实现 | `Application/Services/OW/PluginPriceListService.cs` | 业务逻辑 + 权限检查 | ✅ completed |
| TD-T09 | 创建 MapProfile | `Application/Maps/PluginPriceListMapProfile.cs` | Entity ↔ DTO 映射 | ✅ completed |

### 第三组：API 层（依赖第二组）

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T10 | 创建 Controller | `WebApi/Controllers/OW/PluginPriceListController.cs` | 3 个 Action + 权限属性 | ✅ completed |

### 第四组：前端改造 + 部署（依赖第三组）

| Task ID | 任务 | 文件路径 | 说明 | 状态 |
|---------|------|---------|------|------|
| TD-T11 | 修改 vite.config.js | `amanda自己开发的前端项目/price-list-page/vite.config.js` | base 改为 `/plugins/price-list/` | ✅ completed |
| TD-T12 | 前端 API 对接 | `amanda自己开发的前端项目/price-list-page/src/App.vue` | localStorage → fetch API | ✅ completed |
| TD-T13 | 前端只读模式 | `amanda自己开发的前端项目/price-list-page/src/App.vue` | 根据 permission 字段 disable UI | ✅ completed |
| TD-T14 | 重新构建 dist | `amanda自己开发的前端项目/price-list-page/` | npm run build | ✅ completed |
| TD-T15 | 部署 dist 到前端 public | `packages/flowFlex-common/public/plugins/price-list/` | 复制 dist 内容 | ✅ completed |

---

## 执行顺序

```
第一组（TD-T01~T05）→ 第二组（TD-T06~T09）→ 第三组（TD-T10）→ 第四组（TD-T11~T15）
```

同一组内的任务可并行执行。
