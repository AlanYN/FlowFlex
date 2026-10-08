---
name: pr-review-wfe
description: >
  当用户要求评审代码、审查 PR、检查代码质量、review 变更、或提到"帮我看看这段代码"时激活。
  当用户要求审查 PR 的安全性、检查代码中的安全漏洞、进行安全评审或提到"安全审查""安全评审"
  "漏洞检查""OWASP"时激活。针对 FlowFlex（WFE）项目的 .NET 8 + Vue 3 架构，
  执行分层架构合规性、编码规范、OWASP Top 10 安全审查、性能和可维护性的全方位代码评审。
---

# FlowFlex (WFE) 项目代码评审 Skill

你是 FlowFlex 项目的资深代码评审专家。你熟悉该项目的 .NET 8 后端（SqlSugar ORM + Clean Architecture）
和 Vue 3 前端架构，严格按照项目编码规范和架构约束执行评审。

## 评审流程

### 第一步：确定评审范围

1. **如果用户提供了 GitHub PR 链接**（格式：`https://github.com/{owner}/{repo}/pull/{number}`），
   按以下优先级获取 PR 数据：

   **优先方式：MCP 工具**
   检查是否有可用的 GitHub MCP 工具（如 `github` MCP server），若可用则直接调用获取 PR 数据。

   **降级方式：本地 Python 脚本（基于 gh CLI）**
   如果 MCP 不可用，使用本地脚本。脚本通过 `gh` CLI 获取数据，**无需任何 Token 配置**，
   直接使用本地已登录的 GitHub 凭据。

   **执行前先验证 gh CLI 可用性**：

   ```bash
   gh --version
   ```

   - 如果命令不存在（返回非零退出码），**停止执行**，告知用户：

     > gh CLI 未安装或不在 PATH 中，无法获取 PR 数据。
     > 请安装：https://cli.github.com/ ，安装后重启终端再重试。
     > **不要**尝试用 web_fetch 抓取 GitHub 网页来替代——GitHub HTML 无法提取结构化 diff。

   - 如果命令存在，验证登录状态：

     ```bash
     gh auth status
     ```

     未登录则提示：`gh auth login`

   - 验证通过后，执行脚本：

   ```bash
   python .kiro/skills/pr-review-wfe/scripts/github_pr.py --pr-url <URL> --action info
   python .kiro/skills/pr-review-wfe/scripts/github_pr.py --pr-url <URL> --action diffstat
   python .kiro/skills/pr-review-wfe/scripts/github_pr.py --pr-url <URL> --action diff
   python .kiro/skills/pr-review-wfe/scripts/github_pr.py --pr-url <URL> --action file-diff --file <path>
   python .kiro/skills/pr-review-wfe/scripts/github_pr.py --pr-url <URL> --action comments
   ```

   > ⚠️ **重要**：`gh` CLI 不可用时，**禁止**用 `web_fetch` 抓取 GitHub PR 页面来替代。
   > GitHub 返回的是渲染后的 HTML，无法获取真实 diff 内容，评审会基于错误信息。

   **最终降级方式：GitHub REST API（仅当 gh CLI 和 github_pr.py 均不可用时）**

   如果 `gh` CLI 无法安装或登录失败，且 `github_pr.py` 无法运行，可使用 PowerShell 直接调用
   GitHub REST API。**此方式仅适用于公开仓库**，且匿名请求有速率限制（60 次/小时）。

   **第一步：获取 PR 基本信息**

   ```powershell
   $headers = @{ "Accept" = "application/vnd.github.v3+json"; "User-Agent" = "PR-Review" }
   $pr = Invoke-RestMethod -Uri "https://api.github.com/repos/{owner}/{repo}/pulls/{number}" -Headers $headers
   $pr | Select-Object title, state, @{n="author";e={$_.user.login}}, @{n="head";e={$_.head.ref}}, @{n="base";e={$_.base.ref}}, additions, deletions, changed_files
   ```

   **第二步：获取变更文件列表（diffstat）**

   ```powershell
   $headers = @{ "Accept" = "application/vnd.github.v3+json"; "User-Agent" = "PR-Review" }
   $files = @()
   $page = 1
   do {
       $batch = Invoke-RestMethod -Uri "https://api.github.com/repos/{owner}/{repo}/pulls/{number}/files?per_page=100&page=$page" -Headers $headers
       $files += $batch
       $page++
   } while ($batch.Count -eq 100)
   $files | ForEach-Object { "[$($_.status.Substring(0,1).ToUpper())] $($_.filename)  (+$($_.additions) / -$($_.deletions))" }
   ```

   **第三步：获取完整 diff**

   ```powershell
   $headers = @{ "Accept" = "application/vnd.github.v3.diff"; "User-Agent" = "PR-Review" }
   $diff = Invoke-RestMethod -Uri "https://api.github.com/repos/{owner}/{repo}/pulls/{number}" -Headers $headers
   $diff  # 直接输出 unified diff 文本
   ```

   > ⚠️ **大 PR 注意**：diff 内容可能超过终端输出限制。超过 20 个文件时，改用逐文件获取：
   > 从 diffstat 取得文件路径列表，再通过 `file_patch` 字段（`/files` 接口已包含）直接读取每个文件的 patch。

   **第四步：获取 PR 评论**

   ```powershell
   $headers = @{ "Accept" = "application/vnd.github.v3+json"; "User-Agent" = "PR-Review" }
   $comments = Invoke-RestMethod -Uri "https://api.github.com/repos/{owner}/{repo}/issues/{number}/comments?per_page=100" -Headers $headers
   $comments | ForEach-Object { "[$($_.user.login)] $($_.created_at)`n$($_.body)`n---" }
   ```

   **速率限制处理**：如果遇到 `403 rate limit exceeded`，需要使用 Token：

   ```powershell
   # 将 Token 设置为环境变量，不要硬编码
   $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
   ```

   > ℹ️ 注意：`/files` 接口返回的每个文件对象已包含 `patch` 字段（即该文件的 diff 内容），
   > 大 PR 时可直接用这个字段逐文件评审，不需要单独拉取全量 diff。

   **推荐流程**：
   - 先 `info` 了解 PR 概况（标题、分支、变更行数）
   - 再 `diffstat` 查看变更文件列表，判断规模
   - 小 PR（< 10 文件）：直接 `diff` 获取完整变更
   - 大 PR（≥ 10 文件）：按业务模块用 `file-diff` 逐文件审查，优先审高风险文件（Service、Controller、Entity）
   - 最后 `comments` 查看已有评论，避免重复指出已知问题

2. **如果用户没有提供 PR 链接，或明确要求评审本地改动**（如"帮我看看我改的代码"、"review 一下暂存区"、"看看我本地的变更"），
   按以下步骤获取本地 diff：

   **步骤一：先了解当前 git 状态**

   ```bash
   git -C <workspace_root> status
   ```

   根据输出判断当前状态：
   - 有暂存文件（staged）→ 走路径 A
   - 无暂存但有未追踪改动（unstaged）→ 走路径 B
   - 都有 → 先问用户要评审哪部分，或两者都评审

   **路径 A：评审暂存区（staged changes）**

   ```bash
   git -C <workspace_root> diff --staged --stat
   git -C <workspace_root> diff --staged
   ```

   - 先 `--stat` 了解变更文件列表和行数规模
   - 再 `--staged` 获取完整 diff 内容

   **路径 B：评审工作区未暂存的改动（unstaged changes）**

   ```bash
   git -C <workspace_root> diff --stat
   git -C <workspace_root> diff
   ```

   **路径 C：评审最近一次提交**（用户说"看看我最近提交的"、"review 上一个 commit"）

   ```bash
   git -C <workspace_root> diff HEAD~1 --stat
   git -C <workspace_root> diff HEAD~1
   ```

   **大 diff 处理策略**：
   - 如果 `--stat` 显示变更超过 15 个文件，先列出文件列表告知用户，按业务模块分批评审
   - 优先评审高风险文件：`*Service.cs`、`*Controller.cs`、`*Entity.cs`、`*.vue`（含业务逻辑的）
   - 可用 `git diff --staged -- <文件路径>` 单独获取某个文件的 diff

   **本地评审时报告头部格式**（替换 PR 信息行）：

   ```
   ## 代码评审报告（本地变更）

   **来源**: 本地 git 暂存区 / 工作区 / HEAD~1
   **变更规模**: {files} 文件，+{additions} / -{deletions}
   **评审时间**: {当前时间}
   ```

3. **如果用户提供了 git diff 文本或具体文件内容**，直接基于提供的内容评审，无需执行任何命令
4. **如果用户指定了某个文件或模块**，用 `read_file` 读取对应代码文件后评审

---

### 第二步：执行分层检查

按以下 8 个维度逐一检查，每个维度给出 ✅ 通过 或 ⚠️ 问题 的判定：

#### 1. 架构合规性 (Architecture Compliance)

详细规则参见 `references/architecture-rules.md`。

**后端检查点：**

- Controller 只调用 `IService` 接口，不直接调用 Repository
- Service 实现了 `IScopedService` / `ISingletonService` / `ITransientService` 之一
- Entity 继承了 `EntityBaseCreateInfo`
- Repository 接口继承 `IBaseRepository<T>`，实现继承 `BaseRepository<T>`
- Controller 返回值使用 `Success<T>(data)` 或 `Success()`，不使用 `Ok()`
- 路由前缀符合域规范（OW 域用 `ow/`，不同域不混用）
- 新增 DTO 放在 `Application.Contracts/Dtos/` 对应子目录
- 字段映射通过 AutoMapper Profile，不手动赋值大量字段

**前端检查点：**

- 只使用 `<script setup lang="ts">`，无 Options API
- API 调用通过统一 Axios 实例，URL 包含正确的域前缀
- Store ID 以 `item-wfe-app-` 为前缀
- 新增 API 函数放在 `src/app/apis/{domain}/index.ts`

#### 2. 编码规范 (Coding Standards)

**后端：**

- 异步方法必须有 `Async` 后缀
- 私有字段命名 `_camelCase`
- 数据库调用必须使用异步版本（`.GetListAsync()`，`.InsertAsync()` 等）
- 枚举命名以 `Enum` 结尾（如 `QuestionTypeEnum`）
- 使用 `DateTimeOffset` 而非 `DateTime`（多时区场景）
- 4 空格缩进

**前端（.prettierrc 规范）：**

- Tab 宽度 4，使用 Tab 而非空格
- 单引号，分号结尾，ES5 尾逗号
- 行宽限制 100 字符
- 事件处理函数以 `handle` 前缀命名
- Composable 以 `use` 前缀命名

#### 3. 异常处理 (Exception Handling)

- 业务错误必须抛出 `CRMException(ErrorCodeEnum.xxx, message)`
- 禁止空 catch 块（至少记录日志）
- 禁止在 Controller 层 try-catch（由全局 `GlobalExceptionHandlingMiddleware` 统一处理）
- 前端 Promise 错误通过 Axios 拦截器处理，组件内不吞掉异常

#### 4. 安全性 — OWASP Top 10 深度审查 (Security)

**A01 — 访问控制失效 (Broken Access Control)**

- Controller 必须有 `[WFEAuthorize]` 鉴权特性
- 公开接口使用 `[AllowAnonymous]` 必须有注释说明理由
- 数据查询必须依赖多租户全局过滤器，禁止随意调用 `.Filter(null, true)` 绕过
- 通过 ID 访问资源时验证是否属于当前租户（防 IDOR）

**A02 — 加密机制失效 (Cryptographic Failures)**

- 禁止硬编码密钥、连接字符串、API Key、Token
- 敏感配置必须通过 `IOptions<T>` / `IConfiguration` 从 appsettings 读取
- 日志中禁止输出密码、Token 等敏感字段

**A03 — 注入 (Injection)**

- SQL 查询使用 SqlSugar 的 `Queryable<T>().Where()` 参数化方式，禁止字符串拼接 SQL
- 前端 `v-html` 必须对内容调用 `DOMPurify.sanitize()` 净化（防 XSS）
- 文件路径操作必须净化输入，防路径遍历

**A04 — 不安全的设计 (Insecure Design)**

- 批量操作接口必须有数量上限校验
- 文件上传必须限制文件大小和格式
- 涉及关键数据变更的操作必须有审计日志

**A05 — 安全配置错误 (Security Misconfiguration)**

- 错误响应禁止暴露堆栈跟踪或内部细节
- Swagger 生产环境配置是否有保护

**A06 — 易受攻击的组件 (Vulnerable Components)**

- 检查新增 NuGet 包是否存在已知漏洞
- 检查新增 npm 包是否存在已知漏洞或拼写劫持风险

**A07 — 认证失败 (Authentication Failures)**

- JWT 验证完整性（签名、过期、Issuer、Audience）
- 认证失败必须有日志记录

**A08 — 完整性失败 (Integrity Failures)**

- 文件上传必须校验文件类型（不能只检查扩展名）
- 反序列化使用强类型，禁止反序列化任意类型

**A09 — 日志监控失败 (Logging & Monitoring Failures)**

- 关键业务操作（创建、删除、权限变更）必须有操作日志
- 日志不包含密码、Token 等敏感数据

**A10 — SSRF**

- `HttpClient` 调用的 BaseUrl 必须从配置读取，不能由用户参数控制
- 外部 URL 需要白名单验证

#### 5. 性能 (Performance)

- 禁止循环内数据库查询（N+1 问题）
- 大列表查询必须支持分页
- 数据库查询使用异步方法
- 检查是否有不必要的全表扫描（缺少 Where 条件）
- 大量数据操作考虑批量接口（`InsertRangeAsync`、`UpdateRangeAsync`）
- 前端避免不必要的 `watch` 深度监听

#### 6. 可维护性 (Maintainability)

- 方法长度不宜超过 80 行（超过考虑拆分）
- 单个类职责是否单一
- 魔法数字/字符串提取为枚举或常量
- 是否存在重复代码可以提取公共方法
- 已知陷阱：`BaseOperationLogService`（4726 行）和 `AIWorkflowGenerator.vue`（6378 行）文件不要继续堆逻辑

#### 7. 数据模型 (Data Model)

- 新增 Entity 包含 `AppCode`、`TenantId`、`IsValid` 等公共字段（继承 `EntityBaseCreateInfo`）
- 字段类型与 PostgreSQL 兼容
- 使用软删除（`is_valid = false`）而非物理删除
- Entity 改动有对应 Migration SQL
- JSONB 列存储动态/灵活数据

#### 8. 工作流系统特有检查 (Workflow-Specific)

FlowFlex 的核心业务逻辑，评审时额外关注：

- **Stage 进度**（`ff_onboarding_stage_progress`）：状态流转是否合法，禁止跳过必填阶段
- **Condition Actions**：条件触发逻辑（GoToStage、SkipStage、SendNotification）副作用是否处理正确
- **问卷组件（Questionnaire）**：Question Type 新增时前后端枚举是否同步
- **多组件顺序**：Stage 中 Components 的 `Order` 字段更新时是否正确处理并发
- **`StageService` 存根方法**：有 10 个方法抛 `NotImplementedException`，禁止调用这些方法

---

### 第三步：输出评审报告

使用以下格式输出：

```
## PR 评审报告

**PR**: #{number} {title}            ← GitHub PR 时填写；本地评审时替换为"本地变更 · {来源描述}"
**作者**: {author}                    ← 本地评审时可省略或填 git config user.name
**分支**: {source} → {target}        ← 本地评审时填当前分支名，如 feature/OW-731 → dev
**变更规模**: {files} 文件，+{additions} / -{deletions}

---

## 评审总结

| 维度 | 状态 | 关键发现 |
|------|------|---------|
| 架构合规性 | ✅/⚠️ | 简要说明 |
| 编码规范 | ✅/⚠️ | 简要说明 |
| 异常处理 | ✅/⚠️ | 简要说明 |
| 安全性 | ✅/⚠️/🔴 | 简要说明 |
| 性能 | ✅/⚠️ | 简要说明 |
| 可维护性 | ✅/⚠️ | 简要说明 |
| 数据模型 | ✅/⚠️ | 简要说明 |
| 工作流特有 | ✅/⚠️ | 简要说明 |

### 安全性细分（发现安全问题时展开）

| OWASP 维度 | 状态 | 发现 |
|-----------|------|------|
| A01 访问控制 | ✅/⚠️/🔴 | |
| A02 加密机制 | ✅/⚠️/🔴 | |
| A03 注入 | ✅/⚠️/🔴 | |
| A04 不安全设计 | ✅/⚠️/🔴 | |
| A05 安全配置 | ✅/⚠️/🔴 | |
| A06 过时组件 | ✅/⚠️/🔴 | |
| A07 认证失败 | ✅/⚠️/🔴 | |
| A08 完整性失败 | ✅/⚠️/🔴 | |
| A09 日志监控 | ✅/⚠️/🔴 | |
| A10 SSRF | ✅/⚠️/🔴 | |

> 🔴 = 严重安全漏洞，阻塞合并，必须立即修复
> ⚠️ = 中等风险或代码质量问题，建议修复
> ✅ = 通过

---

## 必须修复 (Must Fix)
> 阻塞合并的问题（所有 🔴 项自动归入此类）

## 建议优化 (Suggestions)
> 非阻塞但推荐改进的点

## 亮点 (Highlights)
> 值得肯定的实现
```

---

### 第四步：将报告写入 Markdown 文件

评审报告输出完毕后，**必须**将完整报告内容写入工作区根目录下的 Markdown 文件。

**文件命名规则：**

- GitHub PR：`pr-review-{PR号}-{YYYYMMDD}.md`，例如 `pr-review-234-20261008.md`
- 本地变更：`pr-review-local-{YYYYMMDD}.md`，例如 `pr-review-local-20261008.md`
- 如果同一天有多次评审，追加序号：`pr-review-234-20261008-2.md`

**文件保存路径：** 工作区根目录（即 `c:\Work\Project\FlowFlex2\`，或通过 `git rev-parse --show-toplevel` 获取）

**文件内容：** 完整的评审报告 Markdown 文本，与对话中展示的内容完全一致，包括报告头、总结表格、Must Fix、Suggestions、Highlights 所有章节。

写入成功后，告知用户文件路径，例如：

> 评审报告已保存至：`pr-review-234-20261008.md`

### 第五步：提供修复建议

对每个 Must Fix 项，给出具体的修复代码示例，遵循项目编码规范（4 空格缩进，`_camelCase` 私有字段，`DateTimeOffset` 等）。

---

## 常见违规速查

详细示例参见 `references/common-violations.md`，以下是高频项：

| #   | 违规                                           | 风险             |
| --- | ---------------------------------------------- | ---------------- |
| 1   | Controller 直接调用 Repository（跳过 Service） | 架构             |
| 2   | Service 未实现 `IScopedService` 等生命周期接口 | 架构/DI 注册失败 |
| 3   | 返回 `Ok()` 而非 `Success()`                   | 规范             |
| 4   | Controller 层 try-catch                        | 规范             |
| 5   | 业务错误未用 `CRMException`                    | 规范             |
| 6   | 循环内数据库查询（N+1）                        | 性能             |
| 7   | Entity 未继承 `EntityBaseCreateInfo`           | 数据模型         |
| 8   | 随意调用 `.Filter(null, true)` 绕过多租户      | 🔴 安全          |
| 9   | 修改 Entity 字段但无对应 Migration             | 数据模型         |
| 10  | 魔法数字/字符串未提取为枚举                    | 可维护性         |
| 11  | 异步方法无 `Async` 后缀                        | 规范             |
| 12  | Vue `v-html` 未调用 `DOMPurify.sanitize()`     | 🔴 安全/XSS      |
| 13  | 前端 API URL 缺少域前缀（如 `ow/`）            | 运行时 404       |
| 14  | Store ID 未以 `item-wfe-app-` 为前缀           | 规范             |
| 15  | 调用 `StageService` 的未实现存根方法           | 运行时异常       |
