# Architecture Decisions: Plugin Price List

## ADR-001: JSONB 存储 vs 关系型拆分

**决策**：使用 JSONB 存储完整 sections 数据

**原因**：
- 数据结构嵌套深（sections → items → tiers → conditionValues）
- 读写都是整体操作，不需要单独查询某个 item
- 未来对接 BNP API 时直接取出 JSON 转换格式
- 避免创建 5+ 张关联表增加复杂度

**权衡**：无法对 JSONB 内部字段做高效查询（如"查所有包含某 chargeCode 的 Price List"），但当前需求不需要。

---

## ADR-002: 权限检查基于 Case 而非 Stage

**决策**：通过 caseCode 查 Onboarding，用 Case 级别权限判断

**原因**：
- Price List 是 Case 级别的数据（一个 Case 一份）
- 不绑定到特定 Stage（虽然入口在 Billing Setup Stage）
- Case 权限已包含 Stage 权限的继承关系

**流程**：caseCode → Onboarding.Id → IPermissionService.CheckCaseAccessAsync()

---

## ADR-003: GET 无数据时返回 200 而非 404

**决策**：Case 存在但无 Price List 时，返回 HTTP 200 + data: null + permission 字段

**原因**：
- 前端需要知道用户的权限级别（write/read），即使数据为空
- HTTP 404 语义上表示"资源不存在"，但这里资源（Case）是存在的
- 前端可以根据 `data === null` 判断是否需要从零创建

---

## ADR-004: 静态文件部署在 WFE 前端 public 目录

**决策**：前端 dist 放在 WFE 前端项目的 `public/plugins/price-list/`

**原因**：
- 前后端分离部署，Nginx 路由 `/api/*` 到后端，其他到前端
- 后端 `wwwroot/` 的文件无法被前端 Nginx 访问到
- `public/` 目录下的文件构建时原样复制到 dist
- Nginx `try_files $uri` 会先匹配静态文件，不会 fallback 到 SPA index.html
- 同域名，JWT token（cookie/localStorage）自动可用

---

## ADR-005: Upsert 语义 + 最后写入胜出

**决策**：POST 保存采用 Upsert，不加乐观锁

**原因**：
- 一个 Case 只有一份 Price List，不存在"创建多份"的场景
- 并发编辑概率低（通常只有 Sales 一人编辑）
- 本期简化实现，未来可通过 `modify_date` 字段加乐观锁

---

## ADR-006: 前端 token 通过 cookie 自动携带

**决策**：不在 URL 中传 token，依赖同域名 cookie

**原因**：
- 前端部署在同域名下（`/plugins/price-list/`）
- WFE 的 JWT token 存在 cookie 中，浏览器自动携带
- 避免 token 暴露在 URL 中（安全风险）
