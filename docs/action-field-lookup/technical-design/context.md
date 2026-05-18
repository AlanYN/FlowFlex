# Context: Action Field Lookup — 技术方案设计（最终版）

> 更新于 2026-05-14，反映所有迭代后的最终架构。

## 技术栈

| 层   | 技术                                                     |
| ---- | -------------------------------------------------------- |
| 后端 | C# / ASP.NET Core / SqlSugar / Newtonsoft.Json           |
| 前端 | Vue 3 + TypeScript + Element Plus + TailwindCSS          |
| HTTP | IHttpClientFactory (后端) / defHttp Axios wrapper (前端) |

---

## 核心架构决策（最终版）

| 决策                | 最终方案                                              | 理由                                                          |
| ------------------- | ----------------------------------------------------- | ------------------------------------------------------------- |
| Lookup 配置存储位置 | `ActionDefinition.ActionConfig.lookupMappings`        | Lookup 属于 Action 本身，不随 TriggerMapping 变化             |
| 认证方式            | 通过 `{{integrationToken}}` 占位符从 contextData 获取 | contextData 中已有 token，无需再走 IntegrationId 链路         |
| IntegrationId 依赖  | 完全可选（不再是必要条件）                            | 用户在 Custom Headers 中配 `Bearer {{integrationToken}}` 即可 |
| endpoint 格式       | 支持完整 URL + `{{placeholder}}` 占位符               | 满足 OAuth2 场景（token endpoint ≠ API base URL）+ 动态查询   |
| headers 占位符      | 支持 `{{placeholder}}` 替换                           | 认证 token 从 contextData 自动注入                            |

---

## 数据结构（ActionConfig 中的 lookupMappings）

```json
{
  "url": "https://crm-dev.item.pub/crm/customers/v2/company-customers",
  "method": "POST",
  "headers": {
    "Authorization": "Bearer {{integrationToken}}",
    "x-tenant-id": "FUZE"
  },
  "body": "{\"customerName\": \"{{customerName}}\"}",
  "timeout": 30,
  "lookupMappings": [
    {
      "wfeField": "{{contactName}}",
      "apiField": "contactName",
      "lookup": {
        "endpoint": "https://crm-dev.item.pub/crm/dropdown-resources/v2/programs?name={{contactName}}",
        "displayPath": "name",
        "valuePath": "id",
        "responsePath": "data",
        "headers": {
          "Authorization": "Bearer {{integrationToken}}",
          "x-tenant-id": "FUZE"
        },
        "integrationId": null
      }
    }
  ]
}
```

---

## 后端文件清单

### 新增文件

| 文件                                                                    | 职责                                |
| ----------------------------------------------------------------------- | ----------------------------------- |
| `Application.Contracts/Dtos/Action/FieldLookupDto.cs`                   | 所有 Lookup 相关 DTO                |
| `Application.Contracts/IServices/Integration/IIntegrationHttpClient.cs` | 统一认证 HTTP 客户端接口            |
| `Application.Contracts/IServices/Action/IFieldLookupService.cs`         | Lookup 服务接口                     |
| `Application/Services/Integration/IntegrationHttpClient.cs`             | 认证 HTTP 客户端实现                |
| `Application/Services/Action/FieldLookupService.cs`                     | Lookup 服务实现（含直接 HTTP 调用） |

### 修改文件

| 文件                                                           | 修改内容                                                    |
| -------------------------------------------------------------- | ----------------------------------------------------------- |
| `WebApi/Controllers/Action/ActionController.cs`                | 新增 `POST lookup/preview` endpoint                         |
| `Application/Services/Action/ActionExecutionService.cs`        | 执行链路中从 ActionConfig 读取 lookupMappings 并调用 Lookup |
| `Domain.Shared/Models/ActionTriggerMappingWithDetails.cs`      | 新增 MappingConfig 字段（UnionAll 兼容）                    |
| `Application.Contracts/Dtos/Action/ActionDefinitionDto.cs`     | ActionTriggerMappingInfo 新增 MappingConfig 字段            |
| `SqlSugarDB/Repositories/Action/ActionDefinitionRepository.cs` | UnionAll query 中加 MappingConfig 列                        |

---

## 前端文件清单

### 新增文件

| 文件                                                   | 职责                                |
| ------------------------------------------------------ | ----------------------------------- |
| `src/types/action-field-lookup.d.ts`                   | TypeScript 类型定义                 |
| `src/app/apis/action/field-lookup.ts`                  | `previewLookupOptions` API 函数     |
| `src/app/components/actionTools/LookupConfigPanel.vue` | Lookup 配置面板组件（含 Test 预览） |

### 修改文件

| 文件                                                    | 修改内容                                                                |
| ------------------------------------------------------- | ----------------------------------------------------------------------- |
| `src/app/components/actionTools/ActionConfigDialog.vue` | 新增 Pre-Execution Lookup 区域，保存/加载到 actionConfig.lookupMappings |

---

## API 端点

| 方法 | 路径                        | 用途                          |
| ---- | --------------------------- | ----------------------------- |
| POST | `/action/v1/lookup/preview` | 预览 lookup 选项（Test 按钮） |

---

## 执行流程（最终版）

```
Stage 完成 → ActionTriggerEventHandler → ActionExecutionService.ExecuteActionAsync()
  │
  ├── 获取 ActionDefinition
  ├── 读取 ActionConfig["lookupMappings"]
  │
  ├── 如果有 lookupMappings：
  │   └── FieldLookupService.FetchLookupOptionsAsync(lookupMappings, contextData)
  │       ├── 对每个 lookup 字段：
  │       │   ├── 对 endpoint 做 {{placeholder}} 替换（用 contextData）
  │       │   ├── 对 headers 做 {{placeholder}} 替换（如 {{integrationToken}}）
  │       │   ├── 如果 integrationId > 0 → IntegrationHttpClient（带 Integration 认证）
  │       │   └── 如果 integrationId = 0 → 直接 HTTP 调用（用替换后的 headers 认证）
  │       │
  │       └── 返回 List<FieldLookupResult>
  │
  ├── EnrichContextWithLookupValues（将匹配结果注入 contextData）
  │
  └── HttpApiActionExecutor.ExecuteAsync()（正常执行 Action）
```

---

## 用户配置指南

### Lookup 配置示例

| 字段           | 填写值                                                        | 说明                               |
| -------------- | ------------------------------------------------------------- | ---------------------------------- |
| Options Source | `https://crm-dev.item.pub/crm/dropdown-resources/v2/programs` | 完整 URL（OAuth2 场景）            |
| Display Field  | `name`                                                        | 选项中用于匹配的显示字段           |
| Value Field    | `id`                                                          | 匹配成功后传给目标 API 的值        |
| Response Path  | `data`                                                        | 响应中选项数组的路径               |
| Custom Headers | `Authorization: Bearer {{integrationToken}}`                  | 认证 token 从 contextData 自动替换 |

### 支持的占位符

- endpoint 中：`{{fieldName}}` → 从 contextData 中取对应字段值（用于动态查询过滤）
- headers 中：`{{integrationToken}}` → 从 contextData 中取 token 值（用于认证）
- 所有 contextData 中的字段都可用作占位符

---

## 已删除/废弃的代码

| 项目                                                           | 状态   |
| -------------------------------------------------------------- | ------ |
| `PUT /action/v1/trigger-mappings/{id}/mapping-config` endpoint | 已删除 |
| `IActionManagementService.UpdateMappingConfigAsync`            | 已删除 |
| `updateMappingConfig` 前端 API 函数                            | 已删除 |
| 从 TriggerMapping.MappingConfig 读取 Lookup 的逻辑             | 已删除 |
