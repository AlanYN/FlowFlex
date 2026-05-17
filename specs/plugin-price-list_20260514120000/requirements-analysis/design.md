# Design: Plugin Price List — 需求分析阶段

## 优先级矩阵（MoSCoW）

### Must Have（必须实现）

| ID | 功能 | 对应用户故事 |
|----|------|-------------|
| M-1 | GET API：按 caseId 获取 Price List | US-001 |
| M-2 | POST API：保存/更新 Price List | US-002 |
| M-3 | POST API：Submit 标记为已提交 | US-003 |
| M-4 | 权限判断：返回 write/read 权限级别 | US-004, US-005 |
| M-5 | 前端改造：localStorage → API 调用 | US-001, US-002, US-003 |
| M-6 | 前端改造：只读模式实现 | US-004 |
| M-7 | 数据库表创建 + Migration | US-006 |
| M-8 | 静态文件部署到 wwwroot | US-007 |

### Should Have（应该实现）

| ID | 功能 | 说明 |
|----|------|------|
| S-1 | 保存/提交时的 loading 状态 | 防止重复提交 |
| S-2 | 网络错误重试提示 | 提升用户体验 |
| S-3 | 403 无权限提示页面 | 友好的错误展示 |

### Could Have（可以实现）

| ID | 功能 | 说明 |
|----|------|------|
| C-1 | 自动保存（定时/离开页面前） | 防止数据丢失 |
| C-2 | 最后修改人/时间显示 | 协作可见性 |

### Won't Have（本期不做）

| ID | 功能 | 说明 |
|----|------|------|
| W-1 | BNP API 对接 | 未来扩展 |
| W-2 | 审批流程 | 未来扩展 |
| W-3 | 版本管理 | 未来扩展 |
| W-4 | 静态数据动态化 | 未来扩展 |

---

## 功能规格说明

### 数据流

```
用户操作 → 前端 Vue 组件 → HTTP API 请求（带 JWT）
                                    ↓
                          WFE 后端 Controller
                                    ↓
                          权限检查（Stage 权限）
                                    ↓
                          Service 层（业务逻辑）
                                    ↓
                          Repository 层（SqlSugar ORM）
                                    ↓
                          PostgreSQL（ff_plugin_price_lists 表）
```

### 数据结构

Price List 的核心数据以 JSONB 存储在 `data` 字段中，结构如下：

```json
{
  "sections": [
    {
      "locations": ["825-Northampton", "132-Ontario"],
      "expanded": true,
      "items": [
        {
          "chargeCode": "HANDLING-0128",
          "itemName": "Offload(Rate by Case qty)-Range Rate",
          "category": "HANDLING",
          "uom": "Case",
          "rateType": "Unit Price",
          "price": "0.560",
          "isTiered": true,
          "tiers": [
            { "from": "0", "to": "500", "price": "570" }
          ],
          "availableDimensions": ["OffloadType", "ContainerSize", "CaseQty"],
          "conditionValues": {
            "OffloadType": ["Floor Loaded"],
            "ContainerSize": ["40'"]
          },
          "customerDescription": "Container Offloading — Floor Loaded, 40'"
        }
      ]
    }
  ]
}
```

### 权限模型

```
WFE Quick Link 打开页面（URL 带 caseId + JWT token）
        ↓
后端验证 JWT → 获取 userId
        ↓
查询该 userId 对该 Case 所在 Stage 的权限
        ↓
├── Stage Assignee / Co-assignee → permission: "write"
├── 有 Stage 查看权限 → permission: "read"  
└── 无权限 → 403 Forbidden
```

---

## 约束与假设

### 约束

1. 后端必须使用 C# + ASP.NET Core，遵循 FlowFlex 现有架构
2. 数据库表名必须以 `ff_plugin_` 为前缀
3. 前端是 Amanda 已完成的 Vue 3 原型，只做 API 对接改造
4. 认证复用 WFE 现有的 JWT 机制

### 假设

1. Amanda 的前端原型功能完整，不需要新增 UI 功能
2. WFE 的 Stage 权限服务已有现成接口可调用
3. 前端部署在同域名下，不存在跨域问题
4. 一个 Case 只对应一份 Price List（不支持多版本）
