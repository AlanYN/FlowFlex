---
inclusion: manual
---

# Skill: dev-environment-setup — 开发环境配置

## 职责

基于技术选型，生成完整的开发环境配置清单，包括运行时安装、依赖包列表、环境变量模板、配置文件和本地启动步骤。

---

## 输入

- `specs/{project-name}_{YYYY-MM-DD}/technical-design/architecture-decisions.md`（技术选型）
- `specs/{project-name}_{YYYY-MM-DD}/technical-design/database-schema.md`（数据库选型）

---

## 执行步骤

### 步骤 1：运行时环境检查清单

列出需要安装的运行时环境：

**Node.js 项目**
- Node.js 版本（推荐 LTS，如 20.x）
- 包管理器（npm / yarn / pnpm）
- 版本管理工具（nvm / fnm）

**Python 项目**
- Python 版本（如 3.11+）
- 虚拟环境工具（venv / conda / poetry）

**数据库**
- 数据库服务（PostgreSQL / MySQL / SQLite）
- 数据库管理工具（TablePlus / DBeaver / pgAdmin）

**其他工具**
- Git
- Docker（如需容器化）
- 代码编辑器插件推荐

### 步骤 2：依赖包清单

分类列出所有需要安装的包：

**生产依赖（dependencies）**
- 框架核心包
- 数据库 ORM
- 认证库
- 工具库

**开发依赖（devDependencies）**
- TypeScript + 类型定义
- 测试框架
- Linter + Formatter
- 构建工具

### 步骤 3：配置文件清单

列出需要创建的配置文件：
- `package.json` — 项目配置
- `tsconfig.json` — TypeScript 配置
- `.eslintrc` — ESLint 规则
- `.prettierrc` — 代码格式化规则
- `vite.config.ts` / `next.config.js` — 构建配置
- `prisma/schema.prisma` — 数据库 Schema（如使用 Prisma）
- `docker-compose.yml` — 本地数据库容器（推荐）

### 步骤 4：环境变量模板

生成 `.env.example` 文件内容，列出所有必需的环境变量。

### 步骤 5：本地启动步骤

生成完整的从零启动命令序列。

---

## 输出格式

写入 `specs/{project-name}_{YYYY-MM-DD}/technical-design/dev-setup.md`：

```markdown
# Development Environment Setup

## 前置要求

### 必须安装
| 工具 | 版本要求 | 安装命令 / 下载地址 | 验证命令 |
|------|---------|------------------|---------|
| Node.js | >= 20.x LTS | `nvm install 20` 或 https://nodejs.org | `node -v` |
| pnpm | >= 8.x | `npm install -g pnpm` | `pnpm -v` |
| PostgreSQL | >= 15.x | `brew install postgresql@15` 或 Docker | `psql --version` |
| Git | >= 2.x | https://git-scm.com | `git --version` |

### 推荐安装
| 工具 | 用途 | 安装方式 |
|------|------|---------|
| TablePlus | 数据库 GUI | https://tableplus.com |
| VS Code 插件：ESLint | 代码检查 | VS Code 扩展市场 |

## 依赖包清单

### 生产依赖
```json
{
  "dependencies": {
    // 框架
    "next": "^14.0.0",
    "react": "^18.0.0",
    // 数据库
    "@prisma/client": "^5.0.0",
    // 认证
    "next-auth": "^4.0.0",
    // 工具
    "zod": "^3.0.0"
  }
}
```

### 开发依赖
```json
{
  "devDependencies": {
    "typescript": "^5.0.0",
    "@types/node": "^20.0.0",
    "eslint": "^8.0.0",
    "prettier": "^3.0.0",
    "vitest": "^1.0.0",
    "prisma": "^5.0.0"
  }
}
```

## 配置文件

### .env.example
```env
# 数据库
DATABASE_URL="postgresql://user:password@localhost:5432/dbname"

# 认证
NEXTAUTH_SECRET="your-secret-here"
NEXTAUTH_URL="http://localhost:3000"

# 第三方服务（如有）
# STRIPE_SECRET_KEY=""
```

### docker-compose.yml（本地数据库）
```yaml
version: '3.8'
services:
  db:
    image: postgres:15
    environment:
      POSTGRES_USER: user
      POSTGRES_PASSWORD: password
      POSTGRES_DB: dbname
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
volumes:
  postgres_data:
```

## 本地启动步骤

```bash
# 1. 克隆项目
git clone {repo-url}
cd {project-name}

# 2. 安装 Node.js（使用 nvm）
nvm install 20
nvm use 20

# 3. 安装依赖
pnpm install

# 4. 配置环境变量
cp .env.example .env
# 编辑 .env 填入实际值

# 5. 启动数据库（使用 Docker）
docker-compose up -d

# 6. 执行数据库迁移
pnpm prisma migrate dev

# 7. 填充种子数据（如有）
pnpm prisma db seed

# 8. 启动开发服务器
pnpm dev
# 访问 http://localhost:3000
```

## 常用开发命令
| 命令 | 用途 |
|------|------|
| `pnpm dev` | 启动开发服务器 |
| `pnpm build` | 构建生产版本 |
| `pnpm test` | 运行测试 |
| `pnpm lint` | 代码检查 |
| `pnpm prisma studio` | 打开数据库 GUI |
| `pnpm prisma migrate dev` | 执行数据库迁移 |
```

---

## 澄清检查点（SETUPAMB）

以下情况标记为 SETUPAMB，需用户确认：
- 是否有现有项目基础（monorepo / 已有 package.json）
- 操作系统差异（Windows 需要额外配置）
- 是否需要 Docker 容器化整个应用
- CI/CD 环境变量管理方式
- 是否有内网 npm 镜像源
