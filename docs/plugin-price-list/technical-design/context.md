# Context: Plugin Price List — 技术方案设计阶段

> 本文件为下游阶段（测试验证）提供输入摘要。

---

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端 | C# / ASP.NET Core 8 / SqlSugar ORM |
| 数据库 | PostgreSQL（JSONB） |
| 前端 | Vue 3 + Vite（已有原型） |
| 认证 | JWT Bearer（同域名 cookie 自动携带） |

---

## 数据库摘要

- 表名：`ff_plugin_price_lists`
- 主键：雪花 ID（long）
- 核心字段：`case_code`（唯一标识）、`data`（JSONB，存储完整 sections）、`status`（draft/submitted/approved）
- 唯一约束：`(tenant_id, app_code, case_code) WHERE is_valid = TRUE`
- 审计字段：create_date, modify_date, create_by, modify_by, create_user_id, modify_user_id
- 租户隔离：tenant_id + app_code

---

## API 端点摘要

| Method | Route | 权限 | 说明 |
|--------|-------|------|------|
| GET | `/api/ow/plugin-price-lists/v1?caseCode={caseCode}` | Case:View | 获取 Price List + permission 字段 |
| POST | `/api/ow/plugin-price-lists/v1` | Case:Operate | 保存/更新（Upsert） |
| POST | `/api/ow/plugin-price-lists/v1/submit` | Case:Operate | 标记为 submitted |

**权限检查流程**：caseCode → 查 ff_onboarding → 获取 onboarding.Id → IPermissionService.CheckCaseAccessAsync()

**GET 响应设计**：
- 有数据：200 + 完整 Price List + `permission: "write"/"read"`
- 无数据但有权限：200 + `data: null` + `permission: "write"/"read"`
- 无权限：403

---

## 风险摘要

| 风险 | 缓解 |
|------|------|
| CaseCode 不存在 | 返回 404 + 明确错误信息 |
| 并发保存冲突 | 本期接受"最后写入胜出" |
| JSONB 数据过大 | 单条 < 1MB，可接受 |

---

## 文件结构摘要

**后端新增文件（10 个）**：
- `Domain/Entities/OW/PluginPriceList.cs`
- `Domain/Repository/OW/IPluginPriceListRepository.cs`
- `SqlSugarDB/Implements/OW/PluginPriceListRepository.cs`
- `SqlSugarDB/Migrations/Migration_20260514000001_CreatePluginPriceListTable.cs`
- `Application.Contracts/Dtos/OW/PluginPriceList/PluginPriceListInputDto.cs`
- `Application.Contracts/Dtos/OW/PluginPriceList/PluginPriceListOutputDto.cs`
- `Application.Contracts/IServices/OW/IPluginPriceListService.cs`
- `Application/Services/OW/PluginPriceListService.cs`
- `Application/Maps/PluginPriceListMapProfile.cs`
- `WebApi/Controllers/OW/PluginPriceListController.cs`

**前端改动文件（2 个）**：
- `price-list-page/vite.config.js`（base path）
- `price-list-page/src/RateSheetBuilder.vue`（API 对接 + 只读模式）

**部署**：
- `packages/flowFlex-common/public/plugins/price-list/`（dist 文件，由 Nginx serve）
