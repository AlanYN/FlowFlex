# FlowFlex AI Agent 开发规范

## 项目概述

**项目类型**: 工作流引擎 (Workflow Engine)
**架构模式**: Monorepo + Clean Architecture + Multi-Tenant
**技术栈**: Vue.js 3 + .NET 8 + PostgreSQL + SqlSugar ORM

## 架构规范

### Monorepo 结构规则

- **前端路径**: `packages/flowFlex-common/` - Vue.js 3 SPA
- **后端路径**: `packages/flowFlex-backend/` - .NET 8 Web API
- **修改前端时必须**: 检查是否需要同步修改后端 API 契约
- **修改后端时必须**: 检查前端 TypeScript 类型定义是否需要更新
- **禁止**: 在根目录直接放置业务代码文件

### Clean Architecture 层次规则

**严格遵循层级依赖关系**:
1. `WebApi/` → `Application/` → `Domain/`
2. `Application.Contracts/` ← `Application/`
3. `Infrastructure/` → `Application/`
4. `SqlSugarDB/` → `Infrastructure/`

**修改规则**:
- **修改 Domain 实体时**: 必须同步检查并更新 SqlSugarDB 迁移文件
- **添加新 API 端点时**: 必须按顺序创建 Controller → Service → Repository
- **修改数据模型时**: 必须依序更新 Domain → Application.Contracts → WebApi

## 代码规范

### 命名规范

**数据库约定**:
- 表名前缀: `ff_` (FlowFlex)
- 主键类型: Snowflake ID (long)
- 多租户字段: `AppCode` (必须存在于所有业务表)
- 审计字段: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `IsDeleted`

**C# 命名规范**:
- 实体类: PascalCase (例: `WorkflowStage`)
- 服务接口: `I{ServiceName}Service` (例: `IWorkflowService`)
- DTO 类: `{Entity}Dto` (例: `WorkflowDto`)
- 控制器: `{Entity}Controller` (例: `WorkflowController`)

**Vue.js 命名规范**:
- 组件文件: PascalCase (例: `WorkflowDesigner.vue`)
- 组合函数: `use{功能名}` (例: `useWorkflow`)
- 类型定义: PascalCase (例: `WorkflowStage`)

### 文件组织规则

**前端文件规则**:
- **页面组件**: `src/views/{模块}/{页面}.vue`
- **业务组件**: `src/components/{模块}/{组件}.vue`
- **类型定义**: `src/types/{模块}.ts`
- **API 服务**: `src/api/{模块}.ts`
- **状态管理**: `src/stores/{模块}.ts`

**后端文件规则**:
- **控制器**: `WebApi/Controllers/{模块}Controller.cs`
- **服务实现**: `Application/Services/{模块}Service.cs`
- **服务接口**: `Application.Contracts/Services/I{模块}Service.cs`
- **实体定义**: `Domain/Entities/{实体}.cs`
- **DTO 定义**: `Application.Contracts/Dtos/{模块}/{实体}Dto.cs`

## 工作流系统业务规则

### 核心实体关系

**工作流层次结构**:
```
Workflow (工作流)
├── WorkflowStage (阶段)
    ├── Questionnaire (问卷)
    ├── Checklist (清单)
    └── Action (操作)
```

**修改工作流实体时必须**:
- 检查 `AppCode` 多租户隔离
- 更新相关的 `StageProgress` 记录
- 维护事件溯源 (`Events` 表)

### 数据完整性规则

**JSONB 字段使用规范**:
- 问卷数据存储在 JSONB 列中
- 修改 JSONB 结构时必须提供迁移脚本
- 查询 JSONB 数据时使用 SqlSugar 的 JSON 操作符

**软删除规则**:
- 所有删除操作使用 `IsDeleted = true`
- 查询时必须添加 `WHERE IsDeleted = false` 过滤
- 级联删除关联数据时保持软删除一致性

## 开发流程规范

### 代码质量控制

**前端质量检查**:
- **每次提交前必须运行**: `pnpm lint` (ESLint + Prettier + Stylelint)
- **类型检查**: `pnpm type:check`
- **构建验证**: `pnpm build:production`

**后端质量检查**:
- **每次提交前必须运行**: `dotnet format`
- **单元测试**: `dotnet test`
- **构建验证**: `dotnet build`

### 环境配置规则

**前端环境变量**:
- 开发环境: `.env.development`
- 预览环境: `.env.preview`
- 生产环境: `.env.production`
- **修改环境变量时**: 必须同步更新所有环境文件

**后端配置管理**:
- 配置文件位置: `WebApi/appsettings.{Environment}.json`
- 数据库连接字符串使用环境变量
- **禁止**: 在代码中硬编码敏感信息

## API 开发规范

### RESTful API 约定

**URL 路径规范**:
- 资源路径: `/api/{version}/{controller}`
- 操作方法: GET (查询), POST (创建), PUT (更新), DELETE (删除)
- 分页查询: 使用 `PagedResult<T>` 返回格式

**响应格式统一**:
- 成功响应: `ApiResponse<T>`
- 错误响应: 标准化错误码和消息
- **必须**: 使用全局异常处理中间件

### 认证授权规则

**JWT 认证流程**:
- 登录端点: `/api/auth/login`
- Token 刷新: `/api/auth/refresh`
- **多租户验证**: 每个请求验证 `AppCode` 权限

## 数据库操作规范

### SqlSugar ORM 使用规则

**仓储模式**:
- 继承 `BaseRepository<TEntity>`
- 实现对应的接口定义
- **查询过滤**: 自动应用 `AppCode` 和 `IsDeleted` 过滤器

**迁移管理**:
- 迁移文件位置: `SqlSugarDB/Migrations/`
- 命名格式: `{Timestamp}_{描述}.cs`
- **执行迁移**: `dotnet run --project SqlSugarDB migrate`

### 数据库性能规则

**索引策略**:
- 多租户字段 `AppCode` 必须建索引
- 外键字段必须建索引
- 查询频繁的 JSONB 字段使用 GIN 索引

## 部署和运维规范

### Docker 容器化

**构建规则**:
- 前端镜像: 基于 nginx:alpine
- 后端镜像: 基于 mcr.microsoft.com/dotnet/aspnet:8.0
- **多阶段构建**: 减少镜像体积

**环境隔离**:
- 开发环境: `docker-compose.dev.yml`
- 生产环境: `docker-compose.yml`

## 安全规范

### 输入验证

**前端验证**:
- 使用 Element Plus 验证规则
- **必须**: 在后端同时进行验证

**后端验证**:
- 使用 FluentValidation 进行请求验证
- **JSONB 数据**: 必须验证结构和内容

### 数据保护

**敏感数据处理**:
- 密码使用 BCrypt 哈希
- **禁止**: 在日志中记录敏感信息
- API 响应中移除敏感字段

## 文档维护规则

### 关键文档同步更新

**API 文档**:
- 修改 API 时必须更新 Swagger 注释
- 重要变更必须更新 `CLAUDE.md`

**README 维护**:
- 修改根目录 `README.md` 时必须同步更新 `Docs/` 下的文档
- 新增功能时更新快速开始指南

## 禁止事项

### 严格禁止的操作

**架构违规**:
- **禁止**: Domain 层引用 Infrastructure 层
- **禁止**: 跨层直接访问数据库
- **禁止**: 在 WebApi 层直接操作数据库

**数据安全**:
- **禁止**: 硬删除数据（除非特殊业务需求）
- **禁止**: 跨租户数据访问
- **禁止**: 在前端存储敏感信息

**代码质量**:
- **禁止**: 提交未通过 linting 的代码
- **禁止**: 使用 `any` 类型（TypeScript）
- **禁止**: 注释掉的代码提交到主分支

### 性能禁忌

**数据库操作**:
- **禁止**: N+1 查询问题
- **禁止**: 在循环中执行数据库查询
- **禁止**: 返回全表数据（必须分页）

**前端性能**:
- **禁止**: 在 computed 中执行异步操作
- **禁止**: 组件中直接操作 DOM
- **禁止**: 过大的组件文件（建议 < 500 行）

## 应急处理规范

### 生产环境紧急修复

**热修复流程**:
1. 在 `hotfix` 分支进行修复
2. 通过完整的 CI/CD 流程
3. 部署后立即验证核心功能
4. 24小时内合并回主分支

**回滚准备**:
- 数据库迁移必须提供回滚脚本
- 保留前一版本的 Docker 镜像
- 关键配置变更必须可逆

---

**最后更新**: 2025-09-10
**适用版本**: FlowFlex v1.0+
**维护责任**: AI Agent 自动遵循并执行以上规范