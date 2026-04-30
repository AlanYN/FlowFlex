---
inclusion: manual
---

# Skill: file-structure-generator — 项目文件结构生成

## 职责

基于技术选型和组件树，生成完整的项目目录结构，明确每个文件的职责、内容概要和创建顺序，让开发者知道需要创建哪些文件。

---

## 输入

- `specs/{project-name}_{YYYY-MM-DD}/technical-design/architecture-decisions.md`（技术选型）
- `specs/{project-name}_{YYYY-MM-DD}/technical-design/component-tree.md`（组件树）
- `specs/{project-name}_{YYYY-MM-DD}/technical-design/api-spec.md`（API 端点）
- `specs/{project-name}_{YYYY-MM-DD}/technical-design/database-schema.md`（数据模型）

---

## 执行步骤

### 步骤 1：确定项目类型

根据技术选型确定目录结构模板：
- Next.js App Router
- Next.js Pages Router
- React + Vite（SPA）
- Node.js + Express（纯后端）
- Monorepo（前后端分离）

### 步骤 2：生成目录树

按功能模块组织文件，遵循以下原则：
- 按功能（feature-based）而非按类型（type-based）组织
- 共享代码放在 `shared/` 或 `lib/` 目录
- 每个功能模块自包含（组件 + hooks + types + utils）

### 步骤 3：标注每个文件的职责

每个文件必须说明：
- 文件用途（一句话）
- 导出内容（主要 export）
- 关联的 US / 组件 / API

### 步骤 4：生成创建顺序

按依赖关系排列文件创建顺序（被依赖的先创建）：
1. 配置文件（tsconfig、eslint 等）
2. 类型定义（types/）
3. 工具函数（lib/utils/）
4. 数据库 Schema 和迁移
5. API 路由
6. 原子组件（Atoms）
7. 复合组件（Molecules / Organisms）
8. 页面（Pages）

---

## 输出格式

写入 `specs/{project-name}_{YYYY-MM-DD}/technical-design/file-structure.md`：

```markdown
# Project File Structure

## 技术栈
{框架 + 版本}

## 目录结构

```
{project-name}/
├── .env.example                    # 环境变量模板
├── .eslintrc.json                  # ESLint 配置
├── .prettierrc                     # Prettier 配置
├── docker-compose.yml              # 本地开发数据库
├── package.json
├── tsconfig.json
│
├── prisma/                         # 数据库（如使用 Prisma）
│   ├── schema.prisma               # 数据库 Schema
│   ├── migrations/                 # 迁移文件（自动生成）
│   └── seed.ts                     # 种子数据
│
├── src/
│   ├── app/                        # Next.js App Router 页面
│   │   ├── layout.tsx              # 根布局
│   │   ├── page.tsx                # 首页（US-001）
│   │   ├── (auth)/                 # 认证路由组
│   │   │   ├── login/page.tsx      # 登录页（US-002）
│   │   │   └── register/page.tsx   # 注册页（US-002）
│   │   └── api/                    # API 路由
│   │       └── {resource}/
│   │           └── route.ts        # API 端点
│   │
│   ├── components/                 # UI 组件
│   │   ├── atoms/                  # 原子组件
│   │   │   ├── Button.tsx
│   │   │   └── Input.tsx
│   │   ├── molecules/              # 分子组件
│   │   └── organisms/              # 有机体组件
│   │
│   ├── lib/                        # 工具库
│   │   ├── db.ts                   # 数据库连接
│   │   ├── auth.ts                 # 认证配置
│   │   └── utils.ts                # 通用工具函数
│   │
│   ├── types/                      # TypeScript 类型定义
│   │   ├── index.ts                # 导出所有类型
│   │   └── {domain}.ts             # 按领域分类的类型
│   │
│   └── hooks/                      # 自定义 React Hooks
│       └── use{Feature}.ts
│
└── tests/                          # 测试文件
    ├── unit/                       # 单元测试
    ├── integration/                # 集成测试
    └── e2e/                        # E2E 测试
```

## 文件职责说明

### 核心文件

| 文件路径 | 职责 | 主要导出 | 关联 |
|---------|------|---------|------|
| `src/lib/db.ts` | Prisma 客户端单例 | `prisma` | 所有 API 路由 |
| `src/lib/auth.ts` | NextAuth 配置 | `authOptions` | 认证相关页面 |
| `src/types/index.ts` | 全局类型定义 | 所有 TS 类型 | 全项目 |

### API 路由文件

| 文件路径 | HTTP 方法 | 端点 | 关联 US |
|---------|---------|------|---------|
| `src/app/api/auth/register/route.ts` | POST | /api/auth/register | US-001 |

### 组件文件

| 文件路径 | 组件名 | 层级 | 关联 US |
|---------|--------|------|---------|
| `src/components/atoms/Button.tsx` | Button | Atom | 全局 |

## 文件创建顺序

### 第一批（基础配置，无依赖）
1. `package.json` — 项目配置和依赖
2. `tsconfig.json` — TypeScript 配置
3. `.env.example` — 环境变量模板
4. `docker-compose.yml` — 数据库容器

### 第二批（数据层）
5. `prisma/schema.prisma` — 数据库 Schema
6. `src/types/index.ts` — 类型定义
7. `src/lib/db.ts` — 数据库连接

### 第三批（服务层）
8. `src/lib/auth.ts` — 认证配置
9. `src/app/api/*/route.ts` — API 路由（按依赖顺序）

### 第四批（UI 层）
10. `src/components/atoms/` — 原子组件
11. `src/components/molecules/` — 分子组件
12. `src/components/organisms/` — 有机体组件
13. `src/app/**/page.tsx` — 页面

### 第五批（测试）
14. `tests/unit/` — 单元测试
15. `tests/integration/` — 集成测试

## 总文件数统计
| 类别 | 文件数 |
|------|--------|
| 配置文件 | {N} |
| 类型定义 | {N} |
| API 路由 | {N} |
| 组件 | {N} |
| 页面 | {N} |
| 测试 | {N} |
| **合计** | **{N}** |
```

---

## 澄清检查点（STRUCTAMB）

以下情况标记为 STRUCTAMB，需用户确认：
- 是否有现有项目需要融入（而非从零开始）
- 测试文件是否与源文件同目录（`*.test.ts`）还是独立目录
- 是否需要 Storybook 组件文档
- 国际化（i18n）文件结构
- 是否需要 PWA 配置
