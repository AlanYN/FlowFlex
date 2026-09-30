# 开发环境配置 — Univer 集成

> 最后更新：2026-09-22

---

## 一、前置依赖

| 工具 | 版本要求 | 说明 |
|------|---------|------|
| Node.js | ≥ 18.12 | 前端构建 |
| pnpm | ≥ 8.10 | 包管理器 |
| .NET SDK | 8.0 | 后端构建 |
| PostgreSQL | 14+ | 数据库 |

---

## 二、前端安装步骤

```bash
# 工作目录：packages/flowFlex-common/

# 1. 安装依赖（Univer 相关包已全部写在 package.json 里，直接安装即可）
pnpm install

# 2. 修改 tailwind.config.ts（添加 important: '#app'）
# 3. 修改 vite.config.ts（添加 /ws 代理，见 INTEGRATION_GUIDE.md 第十节）

# 4. 启动开发服务器
pnpm dev
```

`package.json` 中声明的 Univer 依赖（版本统一为 `0.25.1`，由 `pnpm.overrides` 锁定）：

| 分组 | 包 |
|------|----|
| 基座 | `@univerjs/presets`、`@univerjs/core` |
| Sheets | `@univerjs/preset-sheets-core` / `-drawing` / `-conditional-formatting` / `-filter` / `-hyper-link` / `-data-validation` / `-find-replace` / `-note` / `-sort` / `-table` / `-thread-comment` |
| Docs | `@univerjs/preset-docs-core` / `-drawing` / `-hyper-link` / `-thread-comment` |
| Slides（无 preset，用底层插件组合） | `@univerjs/slides`、`@univerjs/slides-ui`、`@univerjs/engine-render`、`@univerjs/ui`、`@univerjs/docs`、`@univerjs/docs-ui`、`@univerjs/drawing` |

> `@univerjs/network` 未安装：协同编辑走原生 WebSocket（`src/app/composables/useCollaboration.ts`
> ↔ `WebApi/Hubs/CollabWebSocketEndpoint.cs`），不使用 Univer 自带的网络层。

---

## 三、后端配置步骤

```bash
# 工作目录：packages/flowFlex-backend/

# 1. 恢复依赖
dotnet restore

# 2. 构建确认无错误
dotnet build

# 3. 运行（Migration 会在启动时自动执行）
dotnet run --project WebApi
```

**Migration 验证：** 应用启动后，检查数据库是否新增 `ff_document_snapshot` 和 `ff_document_operation_log` 两张表。

```sql
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('ff_document_snapshot', 'ff_document_operation_log');
```

---

## 四、Tailwind 配置修改

```typescript
// tailwind.config.ts
export default {
  content: ['./src/**/*.{vue,js,ts}'],
  important: '#app',   // ← 添加此行，防止 Tailwind reset 破坏 Univer UI
  // ... 其余配置保持不变
}
```

⚠️ **修改后必须验证：** 打开现有页面（如工作流列表、问卷管理），确认样式与修改前一致。

---

## 五、Vite 配置修改

```typescript
// vite.config.ts（追加到现有 optimizeDeps.include 数组）
optimizeDeps: {
  include: [
    // ... 现有内容 ...
    '@univerjs/presets',
    '@univerjs/preset-sheets-core',
    '@univerjs/preset-docs-core',
  ],
},
```

---

## 六、实时协同的连接配置

> 最初设计用 SignalR + `@univerjs/network`，实际落地改为**原生 WebSocket**（浏览器无法直接完成
> SignalR 握手），详见 INTEGRATION_GUIDE.md 第十节与 `technical-design/api-spec.md` 第三节。

前端连接地址（`src/app/composables/useCollaboration.ts`）：

```
wss://{host}/ws/collab/{unitId}?userId=...&userName=...&access_token={jwtToken}
```

本地开发时前端连 `ws://localhost:5173/ws/collab/...`，由 `vite.config.ts` 的 `/ws` 代理
（`ws: true`）转发到 `VITE_PROXY_URL` 指向的后端。

---

## 七、常见问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| Univer 编辑器渲染空白 | 容器高度为 0 或 auto | 确认 height prop 传入明确像素值 |
| `ERR_UNSUPPORTED_ESM_URL_SCHEME` | Windows 路径兼容性 | 参考 INTEGRATION_GUIDE.md 第二章路径修复 |
| 样式错乱（Univer 工具栏不对） | Tailwind reset 未隔离 | 确认 `important: '#app'` 已加入 tailwind.config.ts |
| SignalR 连接失败 | CORS 或认证 | 检查 Program.cs 的 CORS 配置是否包含 WebSocket 升级头 |
| Web Worker 报错 404 | MIME 类型错误 | 生产环境确认 Kestrel 对 `.js` 返回 `application/javascript` |
