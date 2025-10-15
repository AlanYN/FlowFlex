# FlowFlex 样式系统使用说明

> 基于 Item Element Plus 设计规范的模块化样式系统  
> 支持浅色/深色模式 + 蓝色/紫色主题切换

## 📁 目录结构

```
src/styles/
├── index.scss                   # 主入口文件
├── base.scss                    # 基础样式 + HSL 变量系统
├── fonts.scss                   # 字体定义
├── mixins.scss                  # SCSS Mixins
├── third-party.scss             # 第三方库（Tailwind）
├── json-syntax-highlight.scss   # JSON 语法高亮
│
├── design-system/               # 设计系统 Token
│   ├── index.scss              # 统一导出
│   └── tokens/
│       ├── colors-base.scss    # Layer 1: 基础颜色
│       ├── colors-semantic.scss # Layer 2: 语义颜色
│       ├── colors-theme-blue.scss    # Layer 3: 蓝色主题
│       ├── colors-theme-purple.scss  # Layer 3: 紫色主题
│       └── typography.scss     # Typography 层级
│
├── element-plus/               # Element Plus 定制
│   ├── index.scss
│   └── theme-variables.scss
│
├── components/                 # 自定义组件
│   ├── custom-switch.scss
│   ├── status-blocks.scss
│   └── loading-animations.scss
│
├── utilities/                  # 工具样式
│   ├── scrollbar.scss
│   ├── helpers.scss
│   └── item-sidebar.scss
│
└── legacy/                     # 向后兼容层
    ├── deprecated-colors.scss
    └── deprecated-classes.scss
```

## 🎨 三层颜色架构

### Layer 1: 基础颜色（colors-base.scss）

-   黑色/白色透明度系列：`--black-5` ~ `--black-100`、`--white-5` ~ `--white-100`
-   灰度系列：`--gray-50` ~ `--gray-900`
-   完整颜色系列：night, sky, sea, blue, teal, green, yellow, orange, red, rose, pink, purple

### Layer 2: 语义颜色（colors-semantic.scss）

-   Element Plus 文本：`--el-text-color-primary/regular/secondary`
-   Element Plus 背景：`--el-bg-color`、`--el-fill-color-*`
-   Element Plus 边框：`--el-border-color-*`
-   Element Plus 状态：`--el-color-success/warning/danger/info`

### Layer 3: 主题颜色

-   蓝色主题：`--primary-50` ~ `--primary-900`（colors-theme-blue.scss）
-   紫色主题：`--primary-50` ~ `--primary-900`（colors-theme-purple.scss，Item 品牌色）

## 📝 Typography 系统

| 层级 | CSS 变量        | 字号 | 字重 | 用途       |
| ---- | --------------- | ---- | ---- | ---------- |
| H1   | `--heading-1-*` | 48px | 700  | 页面主标题 |
| H2   | `--heading-2-*` | 36px | 700  | 章节标题   |
| H3   | `--heading-3-*` | 28px | 600  | 小节标题   |
| H4   | `--heading-4-*` | 24px | 600  | 卡片标题   |
| H5   | `--heading-5-*` | 20px | 600  | 组件标题   |
| H6   | `--heading-6-*` | 16px | 600  | 辅助标题   |
| XL   | `--text-xl-*`   | 20px | 400  | 大号正文   |
| LG   | `--text-lg-*`   | 18px | 400  | 较大正文   |
| Base | `--text-base-*` | 16px | 400  | 标准正文   |
| SM   | `--text-sm-*`   | 14px | 400  | 小号正文   |
| XS   | `--text-xs-*`   | 12px | 400  | 辅助文本   |

## 🚀 快速开始

### 1. 启动项目

```bash
npm run dev
```

### 2. 使用颜色变量

```vue
<template>
	<!-- ✅ 推荐：使用语义颜色 -->
	<div class="text-el-text-color-primary bg-el-bg-color">内容</div>

	<!-- ✅ 推荐：使用主题色 -->
	<div class="text-primary-500 border-primary-300">主题色内容</div>
</template>

<style scoped lang="scss">
// ✅ 推荐：使用 CSS 变量
.custom-element {
	color: var(--el-text-color-primary);
	background: var(--el-bg-color);
	border-color: var(--el-border-color);
}

// ✅ 主题色
.theme-element {
	color: var(--primary-500);
	border: 1px solid var(--primary-300);
}
</style>
```

### 3. 使用 Typography

```vue
<template>
	<!-- 使用 Tailwind 类 -->
	<h1 class="text-heading-1">主标题</h1>
	<h2 class="text-heading-2">二级标题</h2>
	<p class="text-base">正文内容</p>
</template>

<style scoped lang="scss">
// 或使用 CSS 变量
h1 {
	font-size: var(--heading-1-size);
	font-weight: var(--heading-1-weight);
	line-height: var(--heading-1-line-height);
}
</style>
```

## 📚 常用变量速查

### 颜色变量

| 类型 | 变量                        | 用途      |
| ---- | --------------------------- | --------- |
| 文本 | `--el-text-color-primary`   | 主要文本  |
| 文本 | `--el-text-color-regular`   | 常规文本  |
| 文本 | `--el-text-color-secondary` | 次要文本  |
| 背景 | `--el-bg-color`             | 主要背景  |
| 背景 | `--el-fill-color-light`     | 填充背景  |
| 边框 | `--el-border-color`         | 常规边框  |
| 边框 | `--el-border-color-hover`   | 悬停边框  |
| 主题 | `--primary-500`             | 主题主色  |
| 成功 | `--el-color-success`        | 成功状态  |
| 警告 | `--el-color-warning`        | 警告状态  |
| 危险 | `--el-color-danger`         | 危险/错误 |
| 信息 | `--el-color-info`           | 信息提示  |

### 布局变量

| 变量                       | 值   | 用途         |
| -------------------------- | ---- | ------------ |
| `--el-border-radius-base`  | 12px | 基础圆角     |
| `--el-border-radius-small` | 8px  | 小号圆角     |
| `--el-border-radius-large` | 16px | 大号圆角     |
| `--spacing`                | 4px  | 基础间距单位 |
| `--radius`                 | 10px | 通用圆角     |

## 🔧 最佳实践

### ✅ 推荐

```scss
// 1. 使用语义颜色变量
color: var(--el-text-color-primary);
background: var(--el-bg-color);

// 2. 使用主题色
color: var(--primary-500);

// 3. 使用 Typography 变量
font-size: var(--heading-2-size);
font-weight: var(--heading-2-weight);

// 4. 使用 Tailwind 类
.text-heading-1
.bg-primary-500
.text-el-text-color-primary
```

### ❌ 避免

```scss
// 1. 避免硬编码颜色
color: #333333; // ❌

// 2. 避免直接使用基础颜色（除非特殊需求）
color: var(--gray-500); // ⚠️

// 3. 避免硬编码字号
font-size: 24px; // ❌

// 4. Tailwind 不透明度语法对 CSS 变量无效
@apply bg-gray-800/80; // ❌
// 改用标准 CSS
background-color: rgba(68, 69, 69, 0.8); // ✅
```

## 🎯 主题切换

项目支持两个维度的主题切换：

### 1. 浅色/深色模式

-   通过添加/移除 `.dark` 类实现
-   所有语义颜色会自动切换

### 2. 蓝色/紫色主题

-   蓝色主题：默认配置
-   紫色主题：Item 品牌色
-   通过切换主题文件实现

## 🔄 向后兼容

所有旧的变量和类通过 `legacy/` 目录映射到新系统：

```scss
// 旧变量自动映射
--mainGray → var(--el-bg-color)
--customBlue → var(--el-color-primary)
--red-500 → var(--el-color-danger)

// 旧类名自动映射
.page_title_blod → .heading-1 样式
```

## 📦 组件开发示例

```vue
<template>
	<div class="custom-card">
		<h3 class="text-heading-3">卡片标题</h3>
		<p class="text-base text-el-text-color-regular">卡片内容</p>
		<button class="custom-button">操作按钮</button>
	</div>
</template>

<style scoped lang="scss">
.custom-card {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-border-radius-base);
	padding: calc(var(--spacing) * 4);

	&:hover {
		border-color: var(--el-border-color-hover);
		box-shadow: var(--shadow-md);
	}
}

.custom-button {
	background: var(--primary-500);
	color: white;
	padding: 8px 16px;
	border-radius: var(--el-border-radius-base);
	transition: all 0.3s;

	&:hover {
		background: var(--primary-600);
	}
}
</style>
```

## 🐛 故障排查

### 问题 1: 页面白屏

**解决方案**:

```bash
# 清理缓存
Remove-Item -Recurse -Force node_modules\.vite
Remove-Item -Recurse -Force dist
npm run dev
```

### 问题 2: 主题切换不生效

**解决方案**:

1. 检查 `base.scss` 是否正确加载
2. 确认 `.dark` 类是否正确应用
3. 查看浏览器开发工具的 Computed 样式

### 问题 3: Tailwind 类不生效

**解决方案**:

```bash
# 强制刷新
Ctrl + Shift + R
```

### 问题 4: 颜色显示不正确

**解决方案**:

1. 检查 `legacy/deprecated-colors.scss` 中的映射
2. 使用浏览器开发工具检查实际变量值

## 📖 相关资源

-   [Item Element Plus 设计规范](https://design.item.com/guidelines/element-plus-colors)
-   [Tailwind CSS 文档](https://tailwindcss.com/docs)
-   [Element Plus 文档](https://element-plus.org/)
-   [SCSS 官方文档](https://sass-lang.com/)

---

**最后更新**: 2025-10-11  
**版本**: v1.0.0（样式系统重构完成）
