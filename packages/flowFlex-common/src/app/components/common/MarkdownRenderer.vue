<template>
	<div class="markdown-body p-4" :class="customClass" v-html="sanitizedHtml"></div>
</template>

<script setup lang="ts">
import { computed, watch, onMounted } from 'vue';
import MarkdownIt from 'markdown-it';
import hljs from 'highlight.js';
import taskLists from 'markdown-it-task-lists';
import anchor from 'markdown-it-anchor';
import DOMPurify from 'dompurify';
import { useTheme } from '@/utils/theme';

/**
 * Markdown 渲染选项
 */
interface MarkdownOptions {
	// 是否启用代码高亮（默认: true）
	highlight?: boolean;
	// 是否启用任务列表（默认: true）
	taskLists?: boolean;
	// 是否启用锚点链接（默认: true）
	anchors?: boolean;
	// 自定义 CSS 类名
	customClass?: string;
}

/**
 * 组件 Props
 */
interface MarkdownRendererProps {
	// Markdown 内容
	content: string;
	// 可选配置
	options?: MarkdownOptions;
}

const props = withDefaults(defineProps<MarkdownRendererProps>(), {
	content: '',
	options: () => ({
		highlight: true,
		taskLists: true,
		anchors: true,
		customClass: '',
	}),
});

// 初始化 markdown-it 实例
const md = new MarkdownIt({
	html: true,
	linkify: true,
	typographer: true,
	highlight: (str, lang) => {
		// 如果禁用代码高亮，返回转义后的代码
		if (props.options?.highlight === false) {
			return md.utils.escapeHtml(str);
		}

		// 尝试使用 highlight.js 进行语法高亮
		if (lang && hljs.getLanguage(lang)) {
			try {
				return hljs.highlight(str, { language: lang }).value;
			} catch (error) {
				console.warn(`Failed to highlight code for language: ${lang}`, error);
			}
		}

		// 返回转义后的原始代码
		return md.utils.escapeHtml(str);
	},
});

// 添加任务列表插件
if (props.options?.taskLists !== false) {
	md.use(taskLists, { enabled: true });
}

// 添加锚点插件
if (props.options?.anchors !== false) {
	md.use(anchor, { permalink: false });
}

// 配置外部链接安全属性
const defaultRender =
	md.renderer.rules.link_open ||
	function (tokens: any, idx: number, options: any, _env: any, self: any) {
		return self.renderToken(tokens, idx, options);
	};

md.renderer.rules.link_open = function (
	tokens: any,
	idx: number,
	options: any,
	_env: any,
	self: any
) {
	const token = tokens[idx];
	const hrefIndex = token.attrIndex('href');

	if (hrefIndex >= 0) {
		const href = token.attrs?.[hrefIndex][1] || '';

		// 检查是否为外部链接
		if (href.startsWith('http://') || href.startsWith('https://')) {
			// 添加 target="_blank"
			token.attrSet('target', '_blank');
			// 添加 rel="noopener noreferrer" 以提高安全性
			token.attrSet('rel', 'noopener noreferrer');
		}
	}

	return defaultRender(tokens, idx, options, _env, self);
};

// 自定义 CSS 类名
const customClass = computed(() => props.options?.customClass || '');

// DOMPurify 配置
const purifyConfig: DOMPurify.Config = {
	ADD_ATTR: ['target', 'rel'],
	ADD_TAGS: ['input'], // 用于任务列表
};

// 获取当前主题
const theme = useTheme();

// 动态加载 highlight.js 主题
const loadHighlightTheme = (isDark: boolean) => {
	// 移除旧的主题样式
	const existingLink = document.getElementById('hljs-theme');
	if (existingLink) {
		existingLink.remove();
	}

	// 创建新的样式链接
	const link = document.createElement('link');
	link.id = 'hljs-theme';
	link.rel = 'stylesheet';
	link.href = isDark
		? 'https://cdn.jsdelivr.net/npm/highlight.js@11.11.1/styles/github-dark.min.css'
		: 'https://cdn.jsdelivr.net/npm/highlight.js@11.11.1/styles/github.min.css';
	document.head.appendChild(link);
};

// 监听主题变化
watch(
	() => theme.theme,
	(newTheme) => {
		loadHighlightTheme(newTheme === 'dark');
	},
	{ immediate: true }
);

// 组件挂载时加载主题
onMounted(() => {
	loadHighlightTheme(theme.theme === 'dark');
});

// 渲染和清理 Markdown 内容
const sanitizedHtml = computed(() => {
	try {
		// 处理空内容
		if (!props.content || props.content.trim() === '') {
			return '';
		}

		// 使用 markdown-it 渲染 Markdown 为 HTML
		const html = md.render(props.content);

		// 使用 DOMPurify 清理 HTML，防止 XSS 攻击
		return DOMPurify.sanitize(html, purifyConfig);
	} catch (error) {
		console.error('Failed to render markdown:', error);
		return '<p class="text-red-500">Failed to render content</p>';
	}
});
</script>

<style>
/* 导入 GitHub Markdown CSS */
@import 'github-markdown-css/github-markdown.css';

/* 使用项目的 HSL 颜色变量系统 */
.markdown-body {
	color: hsl(var(--foreground));
	background-color: transparent;
	font-family: var(--font-sans);
}

/* 链接样式 */
.markdown-body a {
	color: hsl(var(--primary));
	text-decoration: none;
}

.markdown-body a:hover {
	text-decoration: underline;
}

/* 代码样式 */
.markdown-body code {
	background-color: hsl(var(--muted));
	color: hsl(var(--foreground));
	padding: 0.2em 0.4em;
	border-radius: 3px;
	font-family: var(--font-mono);
	font-size: 0.9em;
}

.markdown-body pre {
	background-color: hsl(var(--muted));
	border: 1px solid hsl(var(--border));
	border-radius: var(--radius);
	padding: 1em;
	overflow-x: auto;
}

.markdown-body pre code {
	background-color: transparent;
	padding: 0;
	border-radius: 0;
}

/* 表格样式 */
.markdown-body table {
	border-collapse: collapse;
	width: 100%;
	margin: 1em 0;
}

.markdown-body table tr {
	background-color: hsl(var(--card));
	border-top: 1px solid hsl(var(--border));
}

.markdown-body table tr:nth-child(2n) {
	background-color: hsl(var(--muted) / 0.3);
}

.markdown-body table th,
.markdown-body table td {
	border: 1px solid hsl(var(--border));
	padding: 0.6em 1em;
}

.markdown-body table th {
	font-weight: 600;
	background-color: hsl(var(--muted));
}

/* 引用块样式 */
.markdown-body blockquote {
	color: hsl(var(--muted-foreground));
	border-left: 4px solid hsl(var(--border));
	padding-left: 1em;
	margin: 1em 0;
}

/* 水平线样式 */
.markdown-body hr {
	background-color: hsl(var(--border));
	border: 0;
	height: 1px;
	margin: 1.5em 0;
}

/* 标题样式 */
.markdown-body h1,
.markdown-body h2,
.markdown-body h3,
.markdown-body h4,
.markdown-body h5,
.markdown-body h6 {
	color: hsl(var(--foreground));
	font-weight: 600;
	margin-top: 1.5em;
	margin-bottom: 0.5em;
	line-height: 1.25;
}

.markdown-body h1 {
	font-size: 2em;
	border-bottom: 1px solid hsl(var(--border));
	padding-bottom: 0.3em;
}

.markdown-body h2 {
	font-size: 1.5em;
	border-bottom: 1px solid hsl(var(--border));
	padding-bottom: 0.3em;
}

/* 列表样式 */
.markdown-body ul,
.markdown-body ol {
	padding-left: 2em;
	margin: 1em 0;
}

.markdown-body li {
	margin: 0.25em 0;
}

/* 任务列表样式 */
.markdown-body .task-list-item {
	list-style-type: none;
}

.markdown-body .task-list-item input[type='checkbox'] {
	margin-right: 0.5em;
}

/* 图片样式 */
.markdown-body img {
	max-width: 100%;
	border-radius: var(--radius);
	border: 1px solid hsl(var(--border));
}

/* 段落样式 */
.markdown-body p {
	margin: 1em 0;
	line-height: 1.6;
}

/* 强调样式 */
.markdown-body strong {
	font-weight: 600;
	color: hsl(var(--foreground));
}

.markdown-body em {
	font-style: italic;
}
</style>
