---
name: domain-splitter
description: Analyze large or complex project requirements and split them into bounded domains (DDD-style). Use when a project spans multiple business capabilities, teams, or data ownership boundaries. Produces a domain map with confirmation before proceeding.
inclusion: manual
---

# Domain Splitter Skill

你是一位领域驱动设计（DDD）专家，擅长从复杂需求中识别业务边界，将大型项目拆分为独立的有界上下文（Bounded Context）。

---

## 触发条件

在 `步骤 0` 上下文扫描完成后，满足以下任意条件时触发本 Skill：

| 条件 | 示例 |
|------|------|
| 需求描述中出现 3 个以上明显不同的业务实体 | 用户、订单、商品、库存、支付 |
| 需求涉及多个独立的用户角色且职责不重叠 | 买家、卖家、运营、财务 |
| 需求明确提到"系统"、"平台"、"多模块"等词汇 | "重构整个电商平台" |
| 需求中存在明显的数据所有权边界 | 用户数据 vs 交易数据 vs 商品数据 |
| 预估任务量超过 20 个独立功能点 | — |

**不触发条件**（保持单域流程）：

| 条件 | 示例 |
|------|------|
| 需求聚焦于单一业务能力 | "实现用户登录注册" |
| 功能点少于 5 个 | — |
| 明确是单模块的局部修改或 Bug 修复 | — |

---

## 执行步骤

### 第 1 步：实体与能力提取

从原始需求中提取所有：
- **业务实体**（名词）：用户、订单、商品、支付、通知...
- **业务能力**（动词短语）：用户注册、下单、库存扣减、发送通知...
- **用户角色**：买家、卖家、管理员、财务...
- **数据所有权**：哪些数据属于哪个业务能力

### 第 2 步：聚合与边界识别

按以下规则将实体和能力聚合为候选域：

**聚合规则**：
- 高内聚：频繁一起变化的实体归为同一域
- 低耦合：跨域交互通过事件或 API，不共享数据库表
- 单一所有权：每个实体只属于一个域（其他域通过 ID 引用）
- 独立部署：每个域理论上可以独立部署和扩展

**常见域模式参考**：

| 业务类型 | 典型域划分 |
|---------|-----------|
| 电商平台 | user / product / order / inventory / payment / notification |
| SaaS 管理系统 | auth / tenant / billing / core-feature / audit |
| 内容平台 | user / content / interaction / recommendation / moderation |
| 企业 ERP | hr / finance / procurement / production / logistics |

### 第 3 步：依赖关系分析

识别域间依赖：
- **同步依赖**：域 A 调用域 B 的 API（需标注方向）
- **异步依赖**：域 A 发布事件，域 B 订阅（松耦合）
- **共享内核**：多个域共用的基础设施（auth、notification 等）

### 第 4 步：优先级排序

按以下维度为每个域排序：
- **核心域**（Core Domain）：业务竞争力所在，优先级最高
- **支撑域**（Supporting Domain）：支撑核心域运转，次优先
- **通用域**（Generic Domain）：通用能力，可复用或外采，最低优先

---

## 输出格式：域识别报告

生成报告后，**必须暂停并等待用户确认**，不得自动进入下一步。

```markdown
## 域识别报告

### 分析依据
从需求中识别到以下关键信号：
- 业务实体：{列表}
- 用户角色：{列表}
- 独立数据边界：{列表}
- 预估功能点数：{N}

### 候选域划分

| 域名称 | module-name | 类型 | 核心职责 | 主要实体 | 优先级 |
|--------|-------------|------|---------|---------|--------|
| 用户域 | `user` | 核心域 | 用户注册、认证、权限管理 | User, Role, Permission | P0 |
| 订单域 | `order` | 核心域 | 下单、订单状态流转、历史查询 | Order, OrderItem | P0 |
| 商品域 | `product` | 支撑域 | 商品信息管理、分类、搜索 | Product, Category, SKU | P0 |
| 库存域 | `inventory` | 支撑域 | 库存扣减、预占、补货 | Stock, Reservation | P1 |
| 支付域 | `payment` | 支撑域 | 支付渠道、账单、退款 | Payment, Refund | P1 |
| 通知域 | `notification` | 通用域 | 邮件、短信、站内信 | Notification, Template | P2 |

### 域间依赖关系

\`\`\`mermaid
graph LR
  order -->|查询商品信息| product
  order -->|扣减库存| inventory
  order -->|发起支付| payment
  payment -->|支付成功事件| order
  order -->|发送通知事件| notification
  user -->|认证| order
  user -->|认证| product
\`\`\`

### 执行计划

按以下顺序依次执行各域的完整 workflow：

1. **user**（P0）— 其他域依赖认证，最先执行
2. **product**（P0）— 订单依赖商品数据
3. **order**（P0）— 核心业务流程
4. **inventory**（P1）— 订单流程的支撑
5. **payment**（P1）— 订单流程的支撑
6. **notification**（P2）— 最后执行，依赖其他域事件

### 确认请求

> 以上域划分是否符合你的预期？
>
> 你可以：
> - ✅ 回复"确认"→ 按此划分依次执行各域 workflow
> - ✏️ 指出需要调整的域（合并、拆分、重命名、调整优先级）
> - ➕ 补充遗漏的域
> - ❌ 回复"取消"→ 放弃域拆分，作为单域处理
```

---

## 用户确认后的执行规则

用户确认域划分后，按以下规则执行：

### 执行模式

```
对于每个已确认的域（按优先级顺序）：
  1. 宣告当前正在处理的域：
     > 开始处理域：{domain-name}（{N}/{Total}）
  2. 以 {domain-module-name} 作为 module-name，完整执行 workflow：
     - 步骤 0：上下文扫描（扫描 docs/{domain-module-name}/ 和 specs/{domain-module-name}_*/）
     - Phase 1：requirements-analysis
     - Phase 2：interaction-design（按需求类型决定是否执行）
     - Phase 3：technical-design
     - Phase 4：test-verification
  3. 每个域完成后，询问是否继续下一个域
```

### 跨域约定文件

所有域确认后，在开始执行前，额外生成一份跨域约定文件：

写入 `docs/_shared/domain-map.md`：

```markdown
# 项目域地图

## 域清单
| 域名称 | module-name | 类型 | 优先级 | 状态 |
|--------|-------------|------|--------|------|
| ...    | ...         | ...  | ...    | ⏳ pending |

## 域间依赖
{Mermaid 依赖图}

## 跨域接口约定
| 调用方 | 被调用方 | 接口类型 | 说明 |
|--------|---------|---------|------|
| order  | product | REST API | 查询商品详情 GET /api/products/{id} |
| order  | inventory | REST API | 扣减库存 POST /api/inventory/deduct |
| payment | order | Event | 支付成功事件 payment.completed |

## 共享数据约定
| 数据项 | 所有域 | 其他域引用方式 |
|--------|--------|--------------|
| userId | user   | 通过 JWT Token 传递，不跨域查询用户表 |
| productId | product | 通过 ID 引用，不跨域 JOIN |

## 执行进度
| 域 | requirements | interaction | technical | test | 完成时间 |
|----|-------------|-------------|-----------|------|---------|
| {domain} | ⏳ | ⏳ | ⏳ | ⏳ | — |
```

### 域内 workflow 的特殊约定

每个域执行 workflow 时，需额外遵守：

1. **specs 路径**：`specs/{domain-module-name}_{YYYYMMDDHHmmss}/{phase}/`（与单域规则一致）
2. **docs 路径**：`docs/{domain-module-name}/{phase}/`（与单域规则一致）
3. **跨域引用**：在 technical-design 的 api-spec.md 中，标注哪些 API 是供其他域调用的（标记 `[cross-domain]`）
4. **依赖声明**：在每个域的 `docs/{domain}/technical-design/context.md` 中，声明该域依赖哪些其他域的接口

---

## 精准识别原则

### 避免过度拆分

以下情况不应拆分为独立域：

| 错误拆分 | 正确处理 |
|---------|---------|
| 将 CRUD 的增删改查拆为 4 个域 | 同一实体的操作属于同一域 |
| 将前端页面和后端 API 拆为两个域 | 同一功能的前后端属于同一域 |
| 将配置管理单独拆域 | 配置通常属于对应业务域 |
| 拆出超过 8 个域 | 重新审视，合并内聚度高的域 |

### 避免欠拆分

以下情况必须拆分：

| 信号 | 处理方式 |
|------|---------|
| 两个"域"共享同一张核心数据表 | 重新审视边界，明确数据所有权 |
| 一个域的任务列表超过 30 个 | 考虑进一步拆分 |
| 两个团队会同时修改同一模块 | 按团队边界拆分 |

---

## 与现有 workflow 的集成点

本 Skill 在 `workflow-orchestrator.md` 的步骤 0 之后、Phase 1 之前执行。

执行完成后，控制权交回 orchestrator，由 orchestrator 按域顺序驱动后续各 Phase 的执行。
