# Plugin 嵌入指南：将独立前端项目作为插件集成到 FlowFlex

## 概述

本文档记录了将 Amanda 的 Price List 独立 Vue 项目作为插件嵌入 FlowFlex 前端的完整过程、遇到的问题及解决方案。适用于未来类似的插件集成场景。

---

## 架构原理

```
用户浏览器
    │
    ▼
Nginx (try_files $uri $uri/ /index.html)
    │
    ├── /plugins/price-list/index.html  → 直接返回静态文件（不走 Vue Router）
    ├── /plugins/price-list/assets/*     → 直接返回静态文件
    ├── /api/ow/*                        → proxy 转发到后端
    └── 其他路径                          → fallback 到 /index.html（WFE SPA）
```

关键点：Nginx 的 `try_files $uri` 会优先匹配物理文件。只要插件的 dist 文件存在于 Nginx 的静态目录中，就会被直接返回，不经过 WFE 的 Vue Router 和路由守卫。

---

## 集成步骤

### 1. 配置 Vite base path

插件项目的 `vite.config.js` 必须设置 `base` 为部署路径：

```javascript
export default defineConfig({
  plugins: [vue()],
  base: '/plugins/price-list/',  // 必须与部署目录一致
})
```

这确保 build 产物中的 JS/CSS 引用路径正确（如 `/plugins/price-list/assets/App-xxx.js`）。

### 2. Build 插件项目

```bash
cd amanda自己开发的前端项目/price-list-page
npm install
npm run build
```

产物在 `dist/` 目录下。

### 3. 部署到 WFE 前端的 public 目录

```bash
cp -r dist/* packages/flowFlex-common/public/plugins/price-list/
```

`public/` 目录下的文件会在 WFE 前端 build 时被原样复制到最终的 `dist/` 中。

### 4. WFE 前端 build

```bash
cd packages/flowFlex-common
pnpm build:development   # 或 build:production 等
```

最终产物 `dist/plugins/price-list/` 包含插件的所有文件。

### 5. 部署

Docker/Nginx 部署时，`dist/` 整体放到 `/usr/share/nginx/html/dist/`，Nginx 的 `try_files` 会自动处理。

---

## 认证方案：共享 localStorage

插件页面和 WFE 主站在同一域名下，localStorage 是共享的。插件直接读取 WFE 存储的 JWT token：

```javascript
function getAuthToken() {
  try {
    const raw = localStorage.getItem('ITEMWFE_COMMON__LOCAL__KEY__')
    if (!raw) return null
    const parsed = JSON.parse(raw)
    const tokenObj = parsed?.value?.['TOKENOBJ__KEY__']?.value
    return tokenObj?.accessToken?.token || null
  } catch { return null }
}
```

API 调用时带上 Authorization header：

```javascript
headers['Authorization'] = `Bearer ${token}`
```

### 401 处理

当 token 过期或不存在时，API 返回 401，插件应跳转到 WFE 首页让路由守卫处理登录：

```javascript
function handleAuthError() {
  window.location.href = '/'
}
```

---

## API 路径：使用相对路径

```javascript
const API_BASE = '/api/ow/plugin-price-lists/v1'
```

使用相对路径（不写死域名），这样：
- dev 环境：请求 `https://flowflex-dev.item.com/api/ow/...`
- staging 环境：请求 `https://flowflex-staging.item.com/api/ow/...`
- production 环境：请求 `https://flowflex.item.com/api/ow/...`

Nginx 已有的 `/api/` 转发规则会把请求送到后端，不需要额外配置。

---

## 本地开发调试

### 问题：Vite dev server 的 SPA fallback 会拦截插件路径

通过 WFE 的 dev server（`localhost:5173`）访问 `/plugins/price-list/index.html` 时，Vite 的 history API fallback 中间件会将请求 fallback 到 WFE 的 `index.html`，导致 Vue Router 接管并显示 404。

**这是 dev 环境特有的问题，生产环境 Nginx 不受影响。**

### 解决方案：插件独立 dev server

在插件项目的 `vite.config.js` 中配置 proxy：

```javascript
export default defineConfig({
  plugins: [vue()],
  base: '/plugins/price-list/',
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'https://localhost:44391',  // 后端地址
        changeOrigin: true,
        secure: false,  // localhost 自签名证书
      }
    }
  }
})
```

启动：

```bash
cd amanda自己开发的前端项目/price-list-page
npx vite
```

访问：`http://localhost:5174/plugins/price-list/?caseCode=C00001`

### 替代方案：npx serve 验证 dist

验证 build 产物是否正确部署：

```bash
cd packages/flowFlex-common
npx serve dist -l 8080
```

访问：`http://localhost:8080/plugins/price-list/index.html?caseCode=C00001`

`serve` 是纯静态文件服务器，行为与 Nginx 一致（不会有 SPA fallback 问题）。但没有 API proxy，只能验证页面加载。

---

## 注意事项

### 1. Quick Link 传参

WFE Quick Link 传的是 Onboarding 的雪花 ID（`caseId`），不是 CaseCode。后端需要同时支持两种查询方式：

```csharp
if (long.TryParse(caseCode, out var onboardingId))
{
    // 按 ID 或 CaseCode 查找
    return await _db.Queryable<Onboarding>()
        .Where(x => (x.Id == onboardingId || x.CaseCode == caseCode) && x.IsValid)
        .FirstAsync();
}
```

前端读取参数时也要兼容两种参数名：

```javascript
function getCaseCode() {
  const params = new URLSearchParams(window.location.search)
  return params.get('caseCode') || params.get('caseId') || ''
}
```

### 2. 插件不共享 WFE 的 Vue 实例

插件是完全独立的 HTML 页面，有自己的 Vue app 实例。它不能使用 WFE 的：
- Vue Router
- Pinia store
- Element Plus 组件
- Axios 实例

如果需要 HTTP 客户端，用原生 `fetch` 即可（避免引入额外依赖）。

### 3. Nginx 配置无需修改

生产环境的 Nginx 已有配置足够：
- `try_files $uri $uri/ /index.html` — 物理文件存在时直接返回
- `/api/` 转发规则 — 已覆盖所有 API 请求

不需要为插件添加额外的 location 块。

### 4. dist 文件需要纳入版本控制

`packages/flowFlex-common/public/plugins/price-list/` 下的 build 产物需要提交到 git，因为 WFE 的 CI/CD 流程会从 `public/` 复制文件到最终 dist。

如果不想提交 build 产物，替代方案是在 WFE 的 CI 流程中加一步：先 build 插件项目，再 build WFE 前端。

### 5. 插件更新流程

每次修改插件源码后：

```bash
cd amanda自己开发的前端项目/price-list-page
npm run build
cp -r dist/* ../packages/flowFlex-common/public/plugins/price-list/
```

然后提交 `public/plugins/price-list/` 下的变更。

---

## 文件结构参考

```
packages/flowFlex-common/
├── public/
│   └── plugins/
│       └── price-list/          ← 插件 build 产物
│           ├── index.html
│           ├── assets/
│           │   ├── App-xxx.js
│           │   ├── App-xxx.css
│           │   └── ...
│           ├── unis-logo.jpg
│           └── item-logo.svg
├── dist/                        ← WFE build 后的最终产物（含插件）
│   └── plugins/
│       └── price-list/
│           └── ...
└── nginx.conf                   ← Nginx 配置（无需为插件修改）
```
