<template>
	<div
		class="rich-text-editor-wrapper"
		:style="{
			'--editor-min-height': minHeight,
			'--editor-max-height': maxHeight,
		}"
	>
		<QuillEditor
			ref="quillEditorRef"
			:contentType="contentType"
			theme="snow"
			:imageResize="{
				displaySize: true,
			}"
			:toolbar="toolbar"
			:placeholder="placeholder"
			class="rich-text-editor"
			@ready="handleEditorReady"
			@text-change="handleTextChange"
		/>
	</div>
</template>

<script lang="ts" setup>
import { ref, nextTick } from 'vue';
import { QuillEditor } from '@vueup/vue-quill';
import '@vueup/vue-quill/dist/vue-quill.snow.css';
import { ElMessage } from 'element-plus';

interface Props {
	modelValue?: string;
	contentType?: 'html' | 'text' | 'delta';
	placeholder?: string;
	toolbar?: any[];
	minHeight?: string;
	maxHeight?: string;
	maxImageSize?: number; // in MB
}

const quillEditorRef = ref<InstanceType<typeof QuillEditor> | null>(null);
const isEditorReady = ref(false);
const isInternalUpdate = ref(false);

const props = withDefaults(defineProps<Props>(), {
	contentType: 'html',
	placeholder: 'Write your message here...',
	minHeight: '300px',
	maxHeight: '400px',
	maxImageSize: 5,
	toolbar: () => [
		[{ header: [1, 2, 3, 4, 5, 6, false] }],
		[{ font: [] }],
		[{ size: ['small', false, 'large', 'huge'] }],
		['bold', 'italic', 'underline', 'strike'],
		[{ color: [] }, { background: [] }],
		[{ script: 'sub' }, { script: 'super' }],
		['blockquote', 'code-block'],
		[{ list: 'ordered' }, { list: 'bullet' }],
		[{ indent: '-1' }, { indent: '+1' }],
		[{ align: [] }],
		['link', 'image', 'video'],
		['clean'],
	],
});

const emit = defineEmits<{
	'update:modelValue': [value: string];
	change: [value: string];
}>();

// Quill only supports these inline styles
const SAFE_STYLE = [
	'color',
	'background-color',
	'font-size',
	'font-family',
	'font-weight',
	'font-style',
	'text-decoration',
	'line-height',
	'text-align',
];

// Clean HTML for Quill compatibility
const toQuillHtml = (html: string): string => {
	if (!html || typeof html !== 'string') return '';

	const doc = new DOMParser().parseFromString(html, 'text/html');

	// 1. Remove style tags
	doc.querySelectorAll('style').forEach((s) => s.remove());

	// 2. Remove script tags
	doc.querySelectorAll('script').forEach((s) => s.remove());

	// 3. Clean inline styles - only keep safe ones
	doc.querySelectorAll('[style]').forEach((el) => {
		const style = el.getAttribute('style') || '';
		const safe = style
			.split(';')
			.map((s) => s.trim())
			.filter((s) => {
				const prop = s.split(':')[0]?.trim();
				return prop && SAFE_STYLE.includes(prop);
			})
			.join('; ');

		if (safe) el.setAttribute('style', safe);
		else el.removeAttribute('style');
	});

	// 4. Replace font tags with span
	doc.querySelectorAll('font').forEach((f) => {
		const span = doc.createElement('span');
		span.innerHTML = f.innerHTML;
		f.replaceWith(span);
	});

	return doc.body.innerHTML;
};

// Get Quill instance safely
const getQuillInstance = () => {
	if (!quillEditorRef.value) return null;
	return (quillEditorRef.value as any).getQuill?.() || null;
};

// Set content using dangerouslyPasteHTML for Quill compatibility
const setEditorContent = (html: string) => {
	const quill = getQuillInstance();
	if (!quill || !isEditorReady.value) return;

	try {
		isInternalUpdate.value = true;

		// Clean HTML for Quill compatibility
		const cleanHtml = toQuillHtml(html);

		// Clear existing content and paste cleaned HTML
		quill.setContents([], 'silent');
		quill.clipboard.dangerouslyPasteHTML(0, cleanHtml, 'silent');

		nextTick(() => {
			isInternalUpdate.value = false;
		});
	} catch (error) {
		console.error('Error setting editor content:', error);
		isInternalUpdate.value = false;
	}
};

// Handle editor ready
const handleEditorReady = () => {
	isEditorReady.value = true;
	if (props.modelValue) {
		// Use setTimeout to ensure Quill is fully initialized
		setTimeout(() => {
			setEditorContent(props.modelValue as string);
		}, 0);
	}
};

// Handle text change from editor
const handleTextChange = () => {
	if (isInternalUpdate.value) return;

	const quill = getQuillInstance();
	if (!quill) return;

	const html = quill.root.innerHTML;
	emit('update:modelValue', html);
	emit('change', html);
};

// 暴露给外部的设置内容方法
const setContent = (html: string) => {
	if (!isEditorReady.value) {
		// 如果编辑器还没准备好，等待准备好后再设置
		const checkReady = setInterval(() => {
			if (isEditorReady.value) {
				clearInterval(checkReady);
				setEditorContent(html);
			}
		}, 50);
		return;
	}
	setEditorContent(html);
};

// 获取当前内容
const getContent = () => {
	const quill = getQuillInstance();
	if (!quill) return '';
	return quill.root.innerHTML;
};

// 聚焦编辑器
const focus = () => {
	const quill = getQuillInstance();
	if (quill) {
		quill.focus();
	}
};

// Custom image handler for uploading images
// This can be extended to upload to server
const handleImageUpload = (file: File): Promise<string> => {
	return new Promise((resolve, reject) => {
		// Validate file size
		const maxSize = props.maxImageSize * 1024 * 1024;
		if (file.size > maxSize) {
			ElMessage.error(`Image size should not exceed ${props.maxImageSize}MB`);
			reject(new Error('File too large'));
			return;
		}

		// Convert to base64 for now
		// TODO: Replace with actual server upload
		const reader = new FileReader();
		reader.onload = (e) => {
			const imageUrl = e.target?.result as string;
			resolve(imageUrl);
		};
		reader.onerror = () => {
			ElMessage.error('Failed to read image file');
			reject(new Error('Failed to read file'));
		};
		reader.readAsDataURL(file);
	});
};

// Expose methods for parent component
defineExpose({
	handleImageUpload,
	setContent,
	getContent,
	focus,
	getQuillInstance,
});
</script>

<style lang="scss">
.rich-text-editor-wrapper {
	width: 100%;
}

.rich-text-editor {
	background: var(--el-bg-color);
	border-radius: var(--el-border-radius-base);
}

/* Container styles */
.rich-text-editor .ql-container {
	min-height: var(--editor-min-height);
	max-height: var(--editor-max-height);
	overflow-y: auto;
	font-size: 14px;
	font-family: inherit;
	border-bottom-left-radius: var(--el-border-radius-base);
	border-bottom-right-radius: var(--el-border-radius-base);
	border-color: var(--el-border-color);
	background: var(--el-bg-color);
}

.rich-text-editor .ql-editor {
	min-height: v-bind(minHeight);
	max-height: v-bind(maxHeight);
	padding: 12px 15px;
	color: var(--el-text-color-primary);
}

/* Toolbar styles */
.rich-text-editor .ql-toolbar {
	border-top-left-radius: var(--el-border-radius-base);
	border-top-right-radius: var(--el-border-radius-base);
	background: var(--el-fill-color-light);
	border-color: var(--el-border-color);
	padding: 8px;
}

.ql-editor.ql-blank::before {
	color: var(--el-text-color-placeholder);
	font-style: normal;
}

/* Toolbar button styles - Light mode */
.rich-text-editor .ql-stroke {
	stroke: rgba(0, 0, 0, 0.65);
}

.rich-text-editor .ql-fill {
	fill: rgba(0, 0, 0, 0.65);
}

.rich-text-editor .ql-picker-label {
	color: rgba(0, 0, 0, 0.65);
}

/* Dropdown menu styles */
.rich-text-editor .ql-picker-options {
	background: var(--el-bg-color) !important;
	border-color: var(--el-border-color) !important;
	box-shadow: var(--el-box-shadow-light);
	border-radius: var(--el-border-radius-base);
}

.rich-text-editor .ql-picker-item {
	color: var(--el-text-color-primary) !important;
	padding: 5px 12px;
}

.rich-text-editor .ql-picker-item:hover {
	color: var(--el-color-primary) !important;
	background: var(--el-fill-color-light) !important;
}

.rich-text-editor .ql-picker-item.ql-selected {
	color: var(--el-color-primary) !important;
}

/* Active/selected state */
.rich-text-editor .ql-toolbar button:hover,
.rich-text-editor .ql-toolbar button:focus,
.rich-text-editor .ql-toolbar button.ql-active {
	color: var(--el-color-primary);
	background: var(--el-fill-color);
	border-radius: var(--el-border-radius-small);
}

.rich-text-editor .ql-toolbar button:hover .ql-stroke,
.rich-text-editor .ql-toolbar button:focus .ql-stroke,
.rich-text-editor .ql-toolbar button.ql-active .ql-stroke {
	stroke: var(--el-color-primary);
}

.rich-text-editor .ql-toolbar button:hover .ql-fill,
.rich-text-editor .ql-toolbar button:focus .ql-fill,
.rich-text-editor .ql-toolbar button.ql-active .ql-fill {
	fill: var(--el-color-primary);
}

.rich-text-editor .ql-toolbar .ql-picker.ql-expanded .ql-picker-label {
	border-color: var(--el-border-color);
	background: var(--el-fill-color);
}

.rich-text-editor .ql-toolbar .ql-picker.ql-expanded .ql-picker-options {
	border-color: var(--el-border-color);
}

/* Toolbar button hover background */
.rich-text-editor .ql-toolbar .ql-picker-label:hover {
	background: var(--el-fill-color);
	border-radius: var(--el-border-radius-small);
}

.rich-text-editor .ql-toolbar .ql-picker-label {
	border-color: transparent;
}

.rich-text-editor .ql-toolbar .ql-picker.ql-expanded .ql-picker-label .ql-stroke {
	stroke: var(--el-color-primary);
}

/* Scrollbar styling */
.rich-text-editor .ql-container::-webkit-scrollbar {
	width: 6px;
}

.rich-text-editor .ql-container::-webkit-scrollbar-track {
	background: var(--el-fill-color-lighter);
	border-radius: 3px;
}

.rich-text-editor .ql-container::-webkit-scrollbar-thumb {
	background: var(--el-fill-color-dark);
	border-radius: 3px;
}

.rich-text-editor .ql-container::-webkit-scrollbar-thumb:hover {
	background: var(--el-text-color-secondary);
}

/* Link styling */
.rich-text-editor .ql-editor a {
	color: var(--el-color-primary);
	text-decoration: underline;
}

/* Code block styling */
.rich-text-editor .ql-editor pre.ql-syntax {
	background: var(--el-fill-color-light);
	color: var(--el-text-color-primary);
	border-radius: var(--el-border-radius-small);
	border: 1px solid var(--el-border-color-lighter);
	padding: 12px;
}

/* Blockquote styling */
.rich-text-editor .ql-editor blockquote {
	border-left: 4px solid var(--el-color-primary);
	padding-left: 16px;
	margin-left: 0;
	margin-right: 0;
	color: var(--el-text-color-regular);
	background: var(--el-fill-color-lighter);
	padding: 8px 16px;
	border-radius: var(--el-border-radius-small);
}

/* Selected text background */
.rich-text-editor .ql-editor ::selection {
	background: var(--el-color-primary-light-9);
}
</style>

<style lang="scss">
/* Dark mode specific styles - unscoped to work with html.dark */
html.dark .rich-text-editor-wrapper {
	/* Toolbar button icons - optimized brightness for dark mode */
	.ql-stroke {
		stroke: rgba(255, 255, 255, 0.7) !important;
	}

	.ql-fill {
		fill: rgba(255, 255, 255, 0.7) !important;
	}

	.ql-picker-label {
		color: var(--el-color-primary) !important;
	}

	/* Hover and active states - brighter on interaction */
	.ql-toolbar button:hover .ql-stroke,
	.ql-toolbar button:focus .ql-stroke,
	.ql-toolbar button.ql-active .ql-stroke {
		stroke: var(--el-color-primary) !important;
	}

	.ql-toolbar button:hover .ql-fill,
	.ql-toolbar button:focus .ql-fill,
	.ql-toolbar button.ql-active .ql-fill {
		fill: var(--el-color-primary) !important;
	}

	.ql-toolbar button:hover,
	.ql-toolbar button:focus,
	.ql-toolbar button.ql-active {
		background: rgba(255, 255, 255, 0.08) !important;
	}

	.ql-toolbar .ql-picker-label:hover {
		background: rgba(255, 255, 255, 0.08) !important;
	}

	.ql-toolbar .ql-picker-label {
		background: none !important;
	}

	/* Expanded picker label background */
	.ql-toolbar .ql-picker.ql-expanded .ql-picker-label {
		background: rgba(255, 255, 255, 0.08) !important;
		border-color: rgba(255, 255, 255, 0.1) !important;
	}

	/* Dropdown menu - darker background with better contrast */
	.ql-picker-options {
		background: var(--black-500) !important;
		border-color: rgba(255, 255, 255, 0.1) !important;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3) !important;
	}

	/* Dropdown items */
	.ql-picker-item {
		color: rgba(255, 255, 255, 0.85) !important;
	}

	.ql-picker-item:hover {
		background: rgba(255, 255, 255, 0.1) !important;
		color: rgba(255, 255, 255, 0.95) !important;
	}

	.ql-picker-item.ql-selected {
		color: var(--el-color-primary) !important;
	}

	/* Toolbar background - subtle and clean */
	.ql-toolbar {
		background: var(--black-400) !important;
		border-color: rgba(255, 255, 255, 0.08) !important;
	}

	/* Container and editor - softer borders */
	.ql-container {
		border-color: rgba(255, 255, 255, 0.08) !important;
		background: var(--black-500) !important;
	}

	/* Editor text color */
	.ql-editor {
		color: rgba(255, 255, 255, 0.9) !important;
	}

	/* Placeholder text */
	.ql-editor.ql-blank::before {
		color: rgba(255, 255, 255, 0.4) !important;
	}

	/* Selection color - text selection in editor */
	.ql-editor ::selection {
		background: rgba(139, 92, 246, 0.4) !important;
		color: rgba(255, 255, 255, 0.95) !important;
	}

	.ql-editor::-moz-selection {
		background: rgba(139, 92, 246, 0.4) !important;
		color: rgba(255, 255, 255, 0.95) !important;
	}

	/* Scrollbar in dark mode */
	.ql-container::-webkit-scrollbar-track {
		background: rgba(255, 255, 255, 0.05) !important;
	}

	.ql-container::-webkit-scrollbar-thumb {
		background: rgba(255, 255, 255, 0.2) !important;
	}

	.ql-container::-webkit-scrollbar-thumb:hover {
		background: rgba(255, 255, 255, 0.3) !important;
	}
}
</style>
