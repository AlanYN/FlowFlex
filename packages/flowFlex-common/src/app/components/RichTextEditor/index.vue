<template>
	<div
		class="rich-text-editor-wrapper"
		:style="{
			'--editor-min-height': minHeight,
			'--editor-max-height': maxHeight,
		}"
	>
		<QuillEditor
			v-model:content="content"
			:contentType="contentType"
			theme="snow"
			:toolbar="toolbar"
			:placeholder="placeholder"
			class="rich-text-editor"
			@update:content="handleContentChange"
		/>
	</div>
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue';
import { QuillEditor } from '@vueup/vue-quill';
import '@vueup/vue-quill/dist/vue-quill.snow.css';
import { ElMessage } from 'element-plus';

interface Props {
	modelValue: string;
	contentType?: 'html' | 'text' | 'delta';
	placeholder?: string;
	toolbar?: any[];
	minHeight?: string;
	maxHeight?: string;
	maxImageSize?: number; // in MB
}

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

const content = ref(props.modelValue);

watch(
	() => props.modelValue,
	(newValue) => {
		if (newValue !== content.value) {
			content.value = newValue;
		}
	}
);

const handleContentChange = (value: string) => {
	emit('update:modelValue', value);
	emit('change', value);
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
