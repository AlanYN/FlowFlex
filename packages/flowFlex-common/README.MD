# 

这是一个基于 Vue 3 + vite + element Plus 的前端项目，使用 Composition API 进行开发，旨在提供高效、可扩展的前端应用架构。

## 快速开始

👉 **新团队成员请先阅读：[开发环境设置指南](docs/DEV_SETUP.md)**

## 项目介绍

是一个企业级的前端解决方案，它包括了常用的配置、样式和组件，以支持大型项目的开发。

## 安装

### 安装 pnpm

如果您的系统尚未安装 `pnpm`，请先安装它。`pnpm` 是一个高效的包管理工具，可以更快地安装依赖并节省磁盘空间。

```bash
npm install -g pnpm
```

### 克隆并安装依赖

克隆项目到本地并使用 `pnpm` 安装依赖。

```bash
git clone https://yourprojectrepository.com
cd `项目文件`
pnpm install
```

## 使用说明

### 启动开发服务器

首选启动测试环境服务器：

```bash
pnpm serve:test
```

其他启动命令：

```bash
pnpm serve  # 启动默认开发环境
pnpm serve:item  # 启动 item 开发环境
pnpm serve:staging  # 启动 staging 环境
pnpm serve:preview  # 启动预览环境
```

### 构建项目

构建不同环境的项目：

```bash
pnpm build:production  # 构建生产环境
```

## 常用命令

- **启动项目**：`pnpm serve:test`
- **构建项目**：
  - `pnpm build:production` - 构建 uni 产品环境
- **项目分析**：`pnpm build:analyze`
- **单元测试**：`pnpm test`

## 环境配置

项目配置文件位于 `<root>/configs/linc/` 目录下，开发环境配置在 `/dev`，生产环境配置在 `/prod`。

## 代码格式化

项目使用 ESLint 和 Prettier 进行代码格式化。建议在 VSCode 中安装这两个插件以获得最佳开发体验。

## 提交规范

项目使用 `.commitlintrc.cjs` 配置文件来规范 Git 提交信息，确保所有提交信息都符合预定的格式。这有助于维护项目的历史记录清晰和易于管理。

## 样式配置

项目使用 `tailwind.config.js` 来配置 Tailwind CSS，包括自定义颜色、字体、插件等。这有助于保持样式的一致性和可维护性。

## 文件和目录结构

以下是项目的主要文件和目录结构说明：

```
├── configs/               # 配置文件目录
│   ├── linc/              # 环境配置文件
│   └── proxy/             # 代理配置文件
├── public/                # 静态资源目录
│   └── index.html         # 入口 HTML 文件
├── src/                   # 源代码目录
│   ├── apis/              # API 相关
│   ├── components/        # Vue 组件
│   ├── styles/            # 样式文件
│   ├── views/             # 页面视图组件
│   └── main.ts            # Vue 入口文件
├── .commitlintrc.cjs      # Git 提交规范配置
├── .stylelintrc.cjs       # 样式规范配置
├── .prettierrc.cjs        # Prettier 配置文件
├── tailwind.config.js     # Tailwind CSS 配置文件
└── package.json           # 项目依赖和脚本
```

## 依赖说明

- **lodash-es**：用于更好的树摇优化。
- **dayjs**：轻量级时间库，替代 moment。
- **pinia**：Vue 3 的状态管理库。
- **axios + rxjs**：用于 HTTP 请求和数据流处理。
- **vue-router**：Vue 3 的路由管理。
- **tailwindcss**：CSS 框架。
- **element plus**：Vue 3 UI 框架。

## 贡献

欢迎贡献代码，请遵循以下步骤：

1. Fork 项目
2. 创建您的功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 发起 Pull Request

