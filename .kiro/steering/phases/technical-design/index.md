---
inclusion: manual
---

# technical-design：技术方案设计

## 职责定位

将业务规范和交互设计转化为完整的技术实现蓝图，产出架构决策、数据库 Schema、API 规范、项目文件结构、开发环境配置和开发任务列表，让开发者拿到文档即可开始编码。

---

## 输入

| 来源 | 读取文件 | 关键字段 |
|------|---------|---------|
| requirements-analysis | `docs/{module-name}/requirements-analysis/context.md` | 用户故事摘要、AC 完整列表、数据实体 |
| interaction-design | `docs/{module-name}/interaction-design/context.md` | 组件清单、Design Tokens 摘要、关键交互约定 |
| interaction-design（样式规范） | `specs/{module-name}_{YYYYMMDDHHmmss}/interaction-design/design.md` | Design Tokens 完整定义、视觉风格、组件状态规范 |

> **重要**：若 interaction-design 阶段已完成，技术方案中所有涉及 UI 的产出（包括组件树、任务描述，以及如有生成 demo.html 时）必须严格遵循 interaction-design 阶段定义的 Design Tokens 和视觉规范，不得自行定义新的样式变量或视觉风格。

---

## 输出文件

### specs 输出（可执行规格）

写入 `specs/{module-name}_{YYYYMMDDHHmmss}/technical-design/`：

| 文件 | 内容 |
|------|------|
| `requirements.md` | 技术方案范围、约束条件、验收标准 |
| `design.md` | 架构设计、技术选型决策、风险登记表、扩展计划 |
| `tasks.md` | 开发任务列表（按创建顺序排列，含具体操作步骤） |

### docs 输出（知识库文档）

写入 `docs/{module-name}/technical-design/`（修改已有文件，或在首次时新建）：

| 文件 | 类型 | 内容 |
|------|------|------|
| `context.md` | 必需 | 精简摘要，供 test-verification 读取 |
| `architecture-decisions.md` | 必需 | 架构决策记录（ADR），用户确认后锁定 |
| `component-tree.md` | 必需 | 组件拆分树、Props 接口、复用清单 |
| `database-schema.md` | 必需 | 数据库表结构、ER 图、迁移脚本 |
| `api-spec.md` | 必需 | API 端点定义、请求/响应格式、OpenAPI 规范 |
| `file-structure.md` | 必需 | 项目目录结构、文件职责、创建顺序 |
| `dev-setup.md` | 必需 | 开发环境配置、依赖清单、本地启动步骤 |
| `flowchart.md` | 附加 | 系统架构图 + 关键流程时序图 + ER 图 |
| `index.md` | 必需 | 文件清单 + 完整性校验结果 |

---

## 强制执行的 Skills（按序）

> **执行原则**：步骤 1-2 可并行；步骤 3 需用户确认后才能继续；步骤 4-6 可并行；步骤 7-8 可并行；步骤 9 最后执行。

### 第 1 步 — tech-stack-advisor（技术选型）
读取 `.kiro/steering/skills/tech-stack-advisor/SKILL.md`
输出：技术选型表格 → 写入 `specs/.../technical-design/design.md`

### 第 2 步 — risk-analysis（风险分析）
读取 `.kiro/steering/skills/risk-analysis/SKILL.md`
输出：风险登记表（含 🔴/🟡/🟢 评分）→ 写入 `specs/.../technical-design/design.md`

### 第 3 步 — architecture-confirm（架构确认）⚠️ 需用户确认
读取 `.kiro/steering/skills/architecture-confirm/SKILL.md`

**执行顺序**：
1. 先扫描项目文件结构，检测已有架构（前端框架、后端框架、数据库、ORM、部署方式等）
2. 将扫描结果与 `design.md` 中的技术选型草稿合并，生成完整确认清单
3. 逐项与用户确认：已有架构确认是否沿用，待定项目等待用户选择
4. 所有项目确认完毕后锁定架构

**后端架构默认选型（C# / ASP.NET Core）**：

> 本项目后端默认使用 C# + ASP.NET Core 技术栈。架构确认时必须向用户展示以下默认选型，等待用户逐项确认或修改：

| 决策项 | 默认选型 | 说明 |
|--------|---------|------|
| 运行时 | .NET 8 (LTS) | 当前长期支持版本 |
| 框架 | ASP.NET Core Web API | RESTful API 标准框架 |
| ORM | Entity Framework Core | Code-First，支持迁移 |
| 认证方案 | JWT Bearer Token | 无状态认证 |
| 数据库 | PostgreSQL（默认）| 可替换为 SQL Server / MySQL / SQLite |
| 缓存 | 内存缓存（默认）| 可升级为 Redis |
| 日志 | Serilog | 结构化日志，支持多 Sink |
| 依赖注入 | 内置 DI 容器 | ASP.NET Core 原生 |
| 项目结构 | Clean Architecture | Controllers → Services → Repositories → Domain |

**后端框架检测逻辑**：

```
已有项目？
  ├── 是 → 扫描文件结构
  │         ├── 发现 .csproj / .sln / Program.cs → 确认为 C# 项目，激活 csharp skill
  │         ├── 发现其他后端语言特征 → 告知用户，询问是否切换为 C#
  │         └── 无法判断 → 询问用户确认后端技术栈
  └── 新项目 → 展示默认 C# 选型清单，询问用户是否采用默认选型或自定义
               用户确认后激活 csharp skill
```

**激活 csharp skill**：后端确认为 C# 项目后，立即激活 `skills/csharp/SKILL.md` 及所有子文件（`async.md`、`linq.md`、`nulls.md`、`types.md`、`collections.md`、`dispose.md`），后续所有后端相关步骤（数据库设计、API 设计、任务拆解）必须遵循 C# 编码规范。

输出：架构决策记录（含项目扫描摘要 + 前后端每项 ADR）→ 写入 `docs/.../technical-design/architecture-decisions.md`
**重要**：前端、后端、数据库、部署每一项都必须与用户逐项确认，不得跳过或自行假设。

---

> ⏸️ **确认节点**：步骤 3 完成后，展示架构决策清单，等待用户确认所有选型无误后，才能继续步骤 3.5 和步骤 4-6。

---

### 第 3.5 步 — frontend-framework-detection（前端框架检测）⚠️ 仅前端项目执行

> **执行条件**：需求中包含前端 UI 开发内容时执行；纯后端/API 项目跳过此步骤。

**检测逻辑**：

```
已有项目？
  ├── 是 → 扫描项目文件结构
  │         ├── 发现 React 特征（package.json 含 react、.tsx 文件、vite/react 配置）
  │         │     → 框架确认为 React，激活 React 系列 skills
  │         ├── 发现 Vue 特征（package.json 含 vue、.vue 文件、vite/vue 配置）
  │         │     → 框架确认为 Vue，激活 Vue 系列 skills
  │         └── 无法判断 → 询问用户确认框架
  └── 新项目 → 询问用户：将使用 React 还是 Vue 完成前端开发？
               等待用户确认后，激活对应框架 skills
```

**根据框架激活对应 Skills（三个 skill 同时激活，贯穿后续步骤）**：

| 框架 | 激活的 Skills |
|------|-------------|
| React | `item-api-layer-react` + `item-components-react` + `item-type-patterns-react` |
| Vue | `item-api-layer-vue` + `item-components-vue` + `item-type-patterns-vue` |

**各 Skill 在后续步骤中的应用范围**：

| Skill | 应用步骤 | 约束内容 |
|-------|---------|---------|
| `item-type-patterns-{framework}` | 步骤 4、6 | 组件 Props 类型定义、API 请求/响应类型必须遵循 namespace 模式和 `#/` alias |
| `item-components-{framework}` | 步骤 4 | 组件拆分时必须优先使用已有组件库，不得重复创建已有组件 |
| `item-api-layer-{framework}` | 步骤 6 | API 设计时前端调用层必须使用 `defHttp` + `useGlobSetting` 模式 |

输出：框架确认记录 → 追加写入 `docs/.../technical-design/architecture-decisions.md`

---

### 第 4 步 — component-breakdown（组件拆分）
读取 `.kiro/steering/skills/component-breakdown/SKILL.md`
依赖：步骤 3（技术栈已确认）+ 步骤 3.5（框架已确认）+ interaction-design 阶段的 Design Tokens 和视觉规范
输出：组件树 + Props 接口 → 写入 `docs/.../technical-design/component-tree.md`

> **样式规范继承**：组件树中每个 UI 组件的样式描述必须引用 `specs/.../interaction-design/design.md` 中定义的 Design Tokens（如 `--color-primary`、`--spacing-4` 等），不得重新定义。

> **框架组件约束**（步骤 3.5 激活后强制执行）：
> - React 项目：组件拆分前必须先检查 `src/app/components/ui/` 是否已有对应组件（参考 `item-components-react` 组件清单），已有组件直接引用，不得重复创建；Props 类型定义必须遵循 `item-type-patterns-react` 的 namespace 模式，写入 `src/types/{feature}.d.ts`
> - Vue 项目：组件拆分前必须先检查 `src/app/components/` 是否已有对应组件（参考 `item-components-vue` 组件清单），优先使用 Element Plus 内置组件；Props 类型定义必须遵循 `item-type-patterns-vue` 的 namespace 模式

### 第 5 步 — database-design（数据库设计）
读取 `.kiro/steering/skills/database-design/SKILL.md`
依赖：步骤 3（数据库选型已确认）
输出：数据库 Schema + ER 图 + 迁移脚本 → 写入 `docs/.../technical-design/database-schema.md`

> **C# 后端约束**（步骤 3 确认为 C# 项目后强制执行）：
> - 数据库表结构必须同步提供 EF Core Code-First 的 Entity 类定义（遵循 `types.md` 中的值类型规范，`decimal` 用于金额字段，避免 `float`/`double`）
> - 迁移脚本使用 EF Core Migrations（`dotnet ef migrations add`），不手写 SQL DDL
> - 所有 Entity 属性必须标注可空性（启用 C# Nullable Reference Types），遵循 `nulls.md` 规范
> - 集合导航属性初始化为空集合，避免 NRE（如 `public List<Order> Orders { get; set; } = [];`）

### 第 6 步 — api-design（API 设计）
读取 `.kiro/steering/skills/api-design/SKILL.md`
依赖：步骤 5（数据模型已确定）
输出：API 端点规范 + OpenAPI 文档 → 写入 `docs/.../technical-design/api-spec.md`

> **前端 API 调用层约束**（步骤 3.5 激活后强制执行）：
> - React 项目：前端 API 调用层必须遵循 `item-api-layer-react` 规范，使用 `defHttp` + `useGlobSetting` 模式，API 文件写入 `src/app/apis/{feature}/index.ts`；请求/响应类型必须使用 `item-type-patterns-react` 的 namespace 定义，通过 `#/` alias 导入
> - Vue 项目：前端 API 调用层必须遵循 `item-api-layer-vue` 规范，同样使用 `defHttp` + `useGlobSetting` 模式，额外注意 Vue 版本的 `ssoCode` 等请求头自动注入；请求/响应类型必须使用 `item-type-patterns-vue` 的 namespace 定义

> **C# 后端约束**（步骤 3 确认为 C# 项目后强制执行）：
> - API Controller 遵循 Clean Architecture 分层：`Controllers` 只负责路由和参数绑定，业务逻辑写入 `Services`，数据访问写入 `Repositories`
> - 所有 Service 方法必须是 `async Task<T>`，禁止使用 `.Result` / `.Wait()`（遵循 `async.md` 规范）
> - Controller Action 返回 `ActionResult<T>` 或 `IResult`，统一错误响应格式
> - 所有 async 方法参数末尾必须接受 `CancellationToken cancellationToken = default`
> - LINQ 查询注意避免多次枚举（遵循 `linq.md` 规范），EF Core 查询在 Service 层调用 `.ToListAsync()` 后再传递
> - 使用 `IDisposable` / `IAsyncDisposable` 的资源（DbContext、HttpClient 等）必须通过 DI 注入，不手动 `new`（遵循 `dispose.md` 规范）

> **前后端 API 格式统一约束**（前后端同时存在时强制执行）：
>
> 后端响应结构必须与前端 `defHttp` 期望的格式完全一致，字段命名统一使用 **camelCase**：
>
> ```json
> // 统一响应包装格式
> { "code": 0, "message": "success", "data": { ... } }
>
> // 分页列表格式
> { "code": 0, "message": "success", "data": { "items": [...], "total": 100 } }
> ```
>
> ASP.NET Core 必须配置 JSON 序列化为 camelCase：
> ```csharp
> builder.Services.AddControllers()
>     .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
> ```
>
> **⚠️ 雪花 ID（Snowflake ID）强制转 string**：
> 雪花 ID 为 `long`（Int64），超过 JavaScript `Number.MAX_SAFE_INTEGER`（2^53-1），直接返回数字会导致前端精度丢失。
> 所有包含雪花 ID 的字段（`Id`、外键、关联 ID 等）**必须序列化为 string 返回前端**：
>
> ```csharp
> // Entity / DTO 中的 ID 字段加上转换 Attribute
> [JsonConverter(typeof(SnowflakeIdConverter))]
> public long Id { get; set; }
>
> // 或全局注册 long → string 转换器（推荐）
> builder.Services.AddControllers().AddJsonOptions(o => {
>     o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
>     o.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
> });
> ```
>
> 前端接收到的 ID 类型为 `string`，前端类型定义中所有 ID 字段必须声明为 `string`，不得使用 `number`。

---

> ⏸️ **确认节点**：步骤 4-6 完成后，展示组件树、数据库 Schema、API 端点总览，等待用户确认无遗漏后，才能继续步骤 7-8。

---

### 第 7 步 — file-structure-generator（文件结构生成）
读取 `.kiro/steering/skills/file-structure-generator/SKILL.md`
依赖：步骤 4、5、6
输出：项目目录结构 + 文件职责 + 创建顺序 → 写入 `docs/.../technical-design/file-structure.md`

### 第 8 步 — dev-environment-setup（开发环境配置）
读取 `.kiro/steering/skills/dev-environment-setup/SKILL.md`
依赖：步骤 3（技术选型已锁定）
输出：环境配置清单 + 依赖包 + 启动步骤 → 写入 `docs/.../technical-design/dev-setup.md`

### 第 9 步 — task-breakdown（任务拆解）
读取 `.kiro/steering/skills/task-breakdown/SKILL.md`
依赖：步骤 7（文件结构）、步骤 8（环境配置）
输出：完整开发任务列表 → 写入 `specs/.../technical-design/tasks.md`

---

## 各步骤产出汇总

| 步骤 | Skill | 产出文件 | 位置 |
|------|-------|---------|------|
| 1 | tech-stack-advisor | `design.md` | specs |
| 2 | risk-analysis | `design.md` | specs |
| 3 | architecture-confirm | `architecture-decisions.md` | docs |
| 3.5 | frontend-framework-detection | `architecture-decisions.md`（追加框架确认记录） | docs |
| 4 | component-breakdown | `component-tree.md` | docs |
| 5 | database-design | `database-schema.md` | docs |
| 6 | api-design | `api-spec.md` | docs |
| 7 | file-structure-generator | `file-structure.md` | docs |
| 8 | dev-environment-setup | `dev-setup.md` | docs |
| 9 | task-breakdown | `tasks.md` | specs |

---

## 根本性缺陷检测

发现以下情况时，必须阻断并回退到 requirements-analysis：

| 类型 | 示例 |
|------|------|
| 业务逻辑矛盾 | 两个 US 的 AC 相互冲突 |
| 关键流程缺失 | 核心业务流程没有错误处理路径 |
| 数据一致性不可能 | 业务规则要求两个互斥的数据状态同时存在 |
| 权限模型不可实现 | 权限设计按规格无法实现 |

---

## 执行规范

```
步骤 1-2：并行执行 tech-stack-advisor + risk-analysis
步骤 3：执行 architecture-confirm，展示决策清单，等待用户逐项确认
         ├── 后端默认选型：C# + ASP.NET Core + EF Core + PostgreSQL（展示完整默认清单）
         ├── 已有项目 → 扫描 .csproj/.sln 确认是否为 C# 项目
         ├── 新项目 → 展示默认 C# 选型，等待用户确认或自定义
         ├── 用户确认为 C# 项目 → 激活 csharp skill（SKILL.md + async/linq/nulls/types/collections/dispose）
         ├── 用户有疑问 → 提供备选方案对比，重新确认
         └── 用户确认所有决策 → 锁定架构，继续步骤 3.5

步骤 3.5：前端框架检测（仅前端项目执行）
         ├── 已有项目 → 扫描文件结构，自动识别 React / Vue
         ├── 新项目 → 询问用户选择框架，等待确认
         ├── React → 激活 item-api-layer-react + item-components-react + item-type-patterns-react
         ├── Vue → 激活 item-api-layer-vue + item-components-vue + item-type-patterns-vue
         └── 纯后端项目 → 跳过此步骤，直接进入步骤 4-6

步骤 4-6：并行执行 component-breakdown + database-design + api-design
         ├── component-breakdown 必须读取 interaction-design/design.md 中的 Design Tokens
         ├── component-breakdown（前端项目）必须遵循 item-components-{framework} 组件清单，不重复创建已有组件
         ├── component-breakdown（前端项目）Props 类型必须遵循 item-type-patterns-{framework} namespace 模式
         ├── api-design（前端项目）前端调用层必须遵循 item-api-layer-{framework} 的 defHttp 模式
         └── 所有 UI 组件样式描述必须引用已定义的 CSS 变量，不得重新定义
步骤 6 完成后：展示组件树、数据库 Schema、API 端点总览
         ├── 用户发现遗漏 → 补充后重新确认
         └── 用户确认完整 → 继续步骤 7-8

步骤 7-8：并行执行 file-structure-generator + dev-environment-setup
步骤 9：执行 task-breakdown（依赖 7+8 的输出）

demo.html 生成规则（条件性，非必须）：
  ├── 若任务中包含需要生成 demo.html 的要求
  └── 则必须严格遵循 docs/.../interaction-design/demo.html 和
      specs/.../interaction-design/design.md 中定义的 Design Tokens、
      视觉风格和组件状态规范，CSS 变量名保持一致，不得另起炉灶

最终：生成所有输出文件，标记阶段为 completed
```

---

## 输出文件规范

### specs/.../technical-design/requirements.md

```markdown
# Technical Design — Requirements

## 交付范围

### 必须交付
| 交付物 | 验收标准 |
|--------|---------|
| 架构设计 | 包含前端/API/服务/数据库四层划分 |
| 架构决策记录 | 每项选型含理由、备选方案、权衡说明，用户已确认 |
| 组件树 | 覆盖所有 US 页面，含 Props 接口定义 |
| 数据库 Schema | 所有表结构、索引、外键、迁移脚本 |
| API 规范 | 所有端点的请求/响应格式、错误码 |
| 文件结构 | 完整目录树，每个文件标注职责和创建顺序 |
| 开发环境配置 | 从零到本地运行的完整步骤 |
| 风险登记表 | 每个架构组件至少一条风险，Critical 风险含应急预案 |
| 任务列表 | 所有 US 分解为复杂度 ≤ M 的开发任务，含具体操作步骤 |

### Out of Scope
- 具体代码实现：属于开发阶段职责
- 测试用例编写：属于 test-verification 职责

## 验收标准（EARS 格式）

### AC-TECH-1
WHEN 架构确认完成 THEN 系统 SHALL 生成用户已确认的架构决策记录，状态为 architecture-locked

### AC-TECH-2
WHEN 数据库设计完成 THEN 系统 SHALL 为每张表提供字段类型、约束、索引和外键定义

### AC-TECH-3
WHEN API 设计完成 THEN 系统 SHALL 为每个端点提供请求/响应 Schema 和错误码列表

### AC-TECH-4
WHEN 文件结构生成完成 THEN 系统 SHALL 提供按依赖顺序排列的文件创建清单

### AC-TECH-5
WHEN 任务拆解完成 THEN 系统 SHALL 确保所有任务含具体操作步骤，复杂度不超过 M（2-3天）

### AC-TECH-6
WHEN 风险分析完成 THEN 系统 SHALL 为每个 🔴 Critical 风险提供应急预案
```

### specs/.../technical-design/design.md

```markdown
# Technical Design — Design

## 技术选型

### 技术栈总览
| 层次 | 选型 | 版本 | 理由摘要 |
|------|------|------|---------|

### 详细决策

#### 决策 1：{技术选型标题}
- **选择**：{选择了哪个技术/框架}
- **理由**：{依据}
- **备选方案**：{其他技术}
- **权衡**：{接受的取舍}

## 系统架构

### 分层说明
| 层次 | 职责 | 技术 |
|------|------|------|

### 模块划分
| 模块 | 职责 | 关联 US |
|------|------|---------|

## 风险登记表

| ID | 风险 | 类别 | 影响 | 概率 | 评分 | 缓解措施 |
|----|------|------|------|------|------|---------|

### Critical 风险应急预案
- **RISK-001**：{若缓解措施失败，执行的应急方案}

## 扩展计划
| 阶段 | 用户规模 | 架构变化 |
|------|---------|---------|
```

### specs/.../technical-design/tasks.md

```markdown
# Technical Design — Tasks（开发任务列表）

## 概览
- 总任务数：{N} | P0 任务数：{N} | 预估工时：{N} 天

## 任务分组

### 第一组：环境搭建（无依赖，最先执行）

#### TASK-TD-001：初始化项目与开发环境
- **描述**：按照 docs/.../technical-design/dev-setup.md 的步骤初始化项目
- **具体操作**：
  1. 安装 Node.js（推荐使用 nvm：`nvm install 20`）
  2. 安装包管理器：`npm install -g pnpm`
  3. 初始化项目：`{init command}`
  4. 安装依赖：`pnpm install`
  5. 复制 `.env.example` 为 `.env` 并填入实际值
  6. 启动数据库：`docker-compose up -d`
- **优先级**：P0
- **复杂度**：XS（<4h）
- **依赖**：无
- **状态**：pending

### 第二组：数据层（依赖第一组）

#### TASK-TD-002：创建数据库 Schema 与迁移
- **描述**：按照 docs/.../technical-design/database-schema.md 创建所有表结构
- **具体操作**：
  1. 编写迁移文件（参考 database-schema.md）
  2. 执行迁移：`{migrate command}`
  3. 验证表结构已正确创建
- **优先级**：P0
- **复杂度**：S（<1d）
- **依赖**：TASK-TD-001
- **状态**：pending

### 第三组：API 层（依赖第二组）

#### TASK-TD-00N：实现 {端点名} API
- **描述**：按照 docs/.../technical-design/api-spec.md 实现 {HTTP方法} {路径}
- **涉及文件**：
  - 新建：`{具体文件路径，如 src/Features/Orders/OrdersController.cs}`
  - 新建：`{具体文件路径，如 src/Features/Orders/OrdersService.cs}`
  - 新建：`{具体文件路径，如 src/Features/Orders/OrdersRepository.cs}`
  - 新建：`{具体文件路径，如 src/Features/Orders/Dtos/OrderDto.cs}`
- **具体操作**：
  1. 按 file-structure.md 创建上述文件
  2. 实现 Controller → Service → Repository 分层
  3. 响应格式遵循 api-spec.md 统一包装结构 `{ code, message, data }`
  4. ID 字段通过 `LongToStringConverter` 序列化为 string
  5. 实现标准错误响应（参考 api-spec.md 错误码规范）
- **优先级**：P0 / P1
- **复杂度**：S / M
- **依赖**：TASK-TD-002
- **状态**：pending

### 第四组：UI 层（依赖第三组）

#### TASK-TD-00N：实现 {组件名} 组件
- **描述**：按照 docs/.../technical-design/component-tree.md 实现 {组件名}
- **涉及文件**：
  - 新建：`{具体文件路径，如 src/app/views/orders/index.tsx}`
  - 新建：`{具体文件路径，如 src/app/apis/orders/index.ts}`
  - 新建：`{具体文件路径，如 src/types/orders.d.ts}`
  - 修改：`{具体文件路径，如 src/app/router/modules/orders.ts}`
- **具体操作**：
  1. 按 file-structure.md 创建上述文件
  2. 定义 Props 接口（参考 component-tree.md，ID 字段类型为 `string`）
  3. 实现所有状态：default / loading / success / error / empty
  4. 应用 Design Tokens（使用 `specs/.../interaction-design/design.md` 中定义的 CSS 变量）
  5. 连接 API（使用 defHttp，参考 api-spec.md 前端调用示例）
- **优先级**：P0 / P1
- **复杂度**：XS / S / M
- **依赖**：TASK-TD-00N（对应 API 任务）
- **状态**：pending
```

> 任务 ID 前缀：`TASK-TD-`（Technical Design）

### docs/.../technical-design/context.md

```markdown
# Technical Design Context → test-verification

## 模块名称
{module-name}

## 归档路径
docs/{module-name}/
specs/{module-name}_{YYYYMMDDHHmmss}/

## 技术栈摘要
| 层次 | 技术 | 版本 |
|------|------|------|

## 数据库摘要
| 表名 | 主要字段 | 关联 US |
|------|---------|---------|

## API 端点摘要
| 方法 | 路径 | 描述 | 关联 US |
|------|------|------|---------|

## 核心模块
| 模块 | 职责 | 关联 US |
|------|------|---------|

## 任务列表摘要
| Task ID | 名称 | 优先级 | 复杂度 | 关联 US |
|---------|------|--------|--------|---------|

## 风险摘要
| Risk ID | 描述 | 评分 | 关键缓解措施 |
|---------|------|------|------------|

## 关键技术约定（供 test-verification 测试设计参考）
- {约定描述}

## 状态
confirmed
```

### docs/.../technical-design/api-spec.md

```markdown
# API Specification

## 统一响应格式（所有端点必须遵守）

### 标准响应包装
所有 API 响应必须使用以下统一结构，字段命名统一 camelCase：

| 场景 | 响应结构 |
|------|---------|
| 成功（单对象） | `{ "code": 0, "message": "success", "data": { ... } }` |
| 成功（分页列表） | `{ "code": 0, "message": "success", "data": { "items": [...], "total": 100 } }` |
| 成功（无数据） | `{ "code": 0, "message": "success", "data": null }` |
| 业务错误 | `{ "code": {业务错误码}, "message": "{错误描述}", "data": null }` |
| 系统错误 | `{ "code": 500, "message": "Internal Server Error", "data": null }` |

### 错误码规范
| 错误码 | 含义 |
|--------|------|
| 0 | 成功 |
| 400 | 请求参数错误 |
| 401 | 未认证 |
| 403 | 无权限 |
| 404 | 资源不存在 |
| 409 | 业务冲突（如重复创建） |
| 500 | 服务器内部错误 |

### ⚠️ ID 字段类型约定
所有雪花 ID 字段（`id`、外键、关联 ID）在 JSON 响应中**必须为 string 类型**，不得返回 number。
前端类型定义中所有 ID 字段声明为 `string`。

---

## 端点列表

### {功能模块名}

#### {HTTP方法} {路径}
- **描述**：{端点功能描述}
- **认证**：需要 / 不需要
- **关联 US**：US-{NNN}

**请求**
```json
// Headers
Authorization: Bearer {token}
Content-Type: application/json

// Body（POST/PUT）
{
  "fieldName": "string",   // 字段说明
  "pageNo": 1,             // 页码，从 1 开始
  "pageSize": 20           // 每页条数
}
```

**响应（200 OK）**
```json
{
  "code": 0,
  "message": "success",
  "data": {
    "id": "1234567890123456789",   // string — 雪花 ID
    "fieldName": "value",
    "createdAt": "2026-01-01T00:00:00Z"
  }
}
```

**响应（分页列表）**
```json
{
  "code": 0,
  "message": "success",
  "data": {
    "items": [
      { "id": "1234567890123456789", "fieldName": "value" }
    ],
    "total": 100
  }
}
```

**错误响应**
```json
{ "code": 404, "message": "资源不存在", "data": null }
```

**前端调用示例（defHttp）**
```ts
// src/app/apis/{feature}/index.ts
export const {feature}Api = {
  list: (params: {Feature}.ListParams) =>
    defHttp.get<{Feature}.ListResponse>({ url: Api().list, params }),
  create: (data: {Feature}.CreateParams) =>
    defHttp.post<{Feature}.Item>({ url: Api().create, data }),
};
```
```

### docs/.../technical-design/flowchart.md（附加）

必须同时包含：
1. 系统分层架构图（Mermaid graph）
2. 至少一张核心流程时序图（Mermaid sequenceDiagram）
3. 数据库 ER 图（Mermaid erDiagram，与 database-schema.md 保持一致）

---

## 完整性校验

| 文件 | 位置 | 缺失时 |
|------|------|--------|
| `requirements.md` | specs | 校验失败，阶段保持 `in_progress` |
| `design.md` | specs | 校验失败，阶段保持 `in_progress` |
| `tasks.md` | specs | 校验失败，阶段保持 `in_progress` |
| `context.md` | docs | 校验失败，阶段保持 `in_progress` |
| `architecture-decisions.md` | docs | 校验失败，阶段保持 `in_progress` |
| `component-tree.md` | docs | 校验失败，阶段保持 `in_progress` |
| `database-schema.md` | docs | 校验失败，阶段保持 `in_progress` |
| `api-spec.md` | docs | 校验失败，阶段保持 `in_progress` |
| `file-structure.md` | docs | 校验失败，阶段保持 `in_progress` |
| `dev-setup.md` | docs | 校验失败，阶段保持 `in_progress` |
| `index.md` | docs | 校验失败，阶段保持 `in_progress` |
| `flowchart.md` | docs | 可标记 `pending`，不阻断校验 |

所有必需文件均为 `generated` → 校验通过，标记 technical-design 为 `completed`。
