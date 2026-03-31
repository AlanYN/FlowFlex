---
inclusion: manual
---

# Skill: architecture-confirm — 技术架构确认

## 职责

在技术方案生成后、开发任务拆解前，先扫描项目现有架构，再与用户逐项确认前端、后端、数据库、部署等所有架构决策，确保技术选型无歧义，避免开发阶段返工。

---

## 输入

- `specs/{project-name}_{YYYY-MM-DD}/technical-design/design.md`（技术选型 + 架构设计草稿）
- `docs/{project-name}_{YYYY-MM-DD}/requirements-analysis/context.md`（业务约束）
- 项目文件结构（扫描结果）

---

## 执行步骤

### 步骤 0：项目扫描（MANDATORY，必须最先执行）

**在展示任何架构决策前，必须先扫描项目，检测已有架构**：

```
扫描目标文件/目录：
  ├── package.json / pom.xml / go.mod / requirements.txt / Cargo.toml
  │     → 识别语言、运行时、已安装依赖
  ├── vite.config.ts / webpack.config.js / next.config.js
  │     → 识别前端构建工具和框架
  ├── src/ 目录结构
  │     → .tsx/.jsx 文件 → React 项目
  │     → .vue 文件 → Vue 项目
  │     → src/app/apis/axios/ → 已有 defHttp 层
  │     → src/app/components/ui/ → 已有组件库
  ├── server/ / api/ / backend/ / app/ 目录
  │     → 识别后端框架（Express / NestJS / FastAPI / Spring 等）
  ├── prisma/schema.prisma / migrations/ / *.sql
  │     → 识别数据库类型和 ORM
  ├── docker-compose.yml / Dockerfile
  │     → 识别部署方式和基础设施
  └── .env / .env.example
        → 识别已配置的服务和环境变量
```

**扫描结论输出格式**：

> **项目架构扫描结论**
>
> | 层次 | 检测结果 | 置信度 |
> |------|---------|--------|
> | 前端框架 | React 18 + Vite（检测到 package.json + .tsx 文件） | 高 |
> | 前端组件库 | 已有 src/app/components/ui/（shadcn/ui 封装） | 高 |
> | 前端 API 层 | 已有 src/app/apis/axios/defHttp | 高 |
> | 后端框架 | NestJS（检测到 nest-cli.json） | 高 |
> | 数据库 | PostgreSQL + Prisma（检测到 prisma/schema.prisma） | 高 |
> | 缓存 | 未检测到 | — |
> | 部署 | Docker（检测到 docker-compose.yml） | 中 |
>
> 已有架构将作为默认选型，以下确认清单中标注"已有"的项目仍需用户确认是否沿用。

---

### 步骤 1：架构决策确认清单

基于扫描结果，逐项列出需要用户确认的架构决策。**已检测到的选型标注"已有"，未检测到的标注"待定"**：

**前端架构**
- 框架与版本（React / Vue + 版本号）
- 状态管理方案（Context / Zustand / Redux / Pinia）
- 路由方案（React Router / Next.js App Router / Vue Router）
- 样式方案（Tailwind / CSS Modules / Styled Components）
- 构建工具（Vite / Webpack / Turbopack）
- 组件库（已有 shadcn/ui 封装 / Element Plus / 从零搭建）
- API 调用层（已有 defHttp / 从零搭建 / 其他）

**后端架构**（如适用）
- 运行时与语言（Node.js / Python / Go / Java）
- 框架（Express / Fastify / NestJS / FastAPI / Spring Boot）
- ORM / 数据访问层（Prisma / TypeORM / Drizzle / SQLAlchemy）
- 认证方案（JWT / Session / OAuth / SSO）
- API 风格（REST / GraphQL / tRPC）

**数据库**
- 主数据库类型（PostgreSQL / MySQL / SQLite / MongoDB）
- 缓存层（Redis / 内存缓存 / 无）
- 文件存储（本地 / S3 / Cloudinary）

**部署与基础设施**
- 部署目标（Vercel / Docker / 本地 / 云服务）
- 环境变量管理方式
- CI/CD 需求

---

### 步骤 2：展示确认清单

以表格形式展示所有决策，标注状态：

```markdown
| 决策项 | 当前选型 | 来源 | 状态 | 备注 |
|--------|---------|------|------|------|
| 前端框架 | React 18 + Vite | 已有 | ✅ 请确认沿用 | |
| 组件库 | shadcn/ui 封装 | 已有 | ✅ 请确认沿用 | |
| 状态管理 | Zustand | 待定 | ⚠️ 待确认 | 是否需要持久化？ |
| 后端框架 | NestJS | 已有 | ✅ 请确认沿用 | |
| 数据库 | PostgreSQL + Prisma | 已有 | ✅ 请确认沿用 | |
| 缓存层 | 未检测到 | 待定 | ⚠️ 待确认 | 是否需要 Redis？ |
```

**每个"待确认"项必须等待用户明确回复后才能继续**，不得自行假设。

---

### 步骤 3：处理疑问与分歧

对每个"待确认"或用户提出疑问的项，提供：
- 当前选型的理由
- 备选方案对比表
- 推荐选择及依据

---

### 步骤 4：锁定架构

用户逐项确认所有决策后，生成"架构锁定声明"，后续阶段不得更改已锁定的技术选型。

---

## 输出格式

写入 `docs/{module-name}/technical-design/architecture-decisions.md`：

```markdown
# Architecture Decisions Record (ADR)

## 锁定时间
{YYYY-MM-DD HH:mm}

## 项目扫描摘要
| 层次 | 检测结果 | 置信度 |
|------|---------|--------|

## 已确认决策

### ADR-001：前端框架
- **决策**：{选择了什么}
- **来源**：{已有 / 新增}
- **状态**：✅ 已锁定
- **理由**：{依据}
- **影响**：{对开发的影响}

### ADR-002：后端框架
- **决策**：{选择了什么}
- **来源**：{已有 / 新增}
- **状态**：✅ 已锁定
- **理由**：{依据}
- **影响**：{对开发的影响}

（以此类推，前后端每项决策各一条 ADR）

## 约束条件
- {不可更改的约束}

## 状态
architecture-locked
```

---

## 澄清检查点（ARCHAMB）

以下情况必须在确认前解决：
- 检测到的已有架构与需求存在冲突
- 技术选型与已有基础设施冲突
- 选型存在许可证风险
- 团队对某技术无经验
- 选型会导致显著的性能或安全问题
