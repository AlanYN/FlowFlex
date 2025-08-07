<template>
	<div class="code-editor-container">
		<div class="code-editor-header">
			<label class="text-base font-bold text-primary-800 dark:text-primary-300">
				{{ title }}
			</label>
			<p class="text-sm text-primary-600 dark:text-primary-400">
				{{ description }}
			</p>
		</div>
		<div ref="editorContainer" class="code-editor-wrapper"></div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import loader from '@monaco-editor/loader';

// Props
const props = withDefaults(defineProps<{
	modelValue?: string;
	language?: string;
	title?: string;
	description?: string;
	height?: string;
	readOnly?: boolean;
}>(), {
	modelValue: '',
	language: 'python',
	title: 'Code Editor',
	description: 'Write your Python code here',
	height: '300px',
	readOnly: false,
});

// Emits
const emit = defineEmits<{
	'update:modelValue': [value: string];
	'change': [value: string];
}>();

// Refs
const editorContainer = ref<HTMLElement>();
let editor: any = null;
let monaco: any = null;

// Initialize editor
const initEditor = async () => {
	if (!editorContainer.value) return;

	try {
		// Load Monaco editor using loader
		loader.config({
			paths: {
				'vs': 'https://unpkg.com/monaco-editor@0.52.2/min/vs'
			}
		});
		monaco = await loader.init();
		
		// Configure Monaco editor
		monaco.editor.defineTheme('custom-theme', {
			base: 'vs',
			inherit: true,
			rules: [],
			colors: {
				'editor.background': '#f8fafc',
				'editor.foreground': '#1e293b',
			},
		});

		// Create editor instance
		editor = monaco.editor.create(editorContainer.value, {
			value: props.modelValue,
			language: props.language,
			theme: 'custom-theme',
			automaticLayout: true,
			minimap: { enabled: false },
			scrollBeyondLastLine: false,
			readOnly: props.readOnly,
			fontSize: 14,
			lineNumbers: 'on',
			roundedSelection: false,
			scrollbar: {
				vertical: 'visible',
				horizontal: 'visible',
			},
			wordWrap: 'on',
			tabSize: 4,
			insertSpaces: true,
			detectIndentation: true,
			trimAutoWhitespace: true,
			largeFileOptimizations: true,
			contextmenu: true,
			quickSuggestions: true,
			suggestOnTriggerCharacters: true,
			acceptSuggestionOnEnter: 'on',
			wordBasedSuggestions: 'allDocuments',
			parameterHints: {
				enabled: true,
			},
			hover: {
				enabled: true,
			},
			formatOnPaste: true,
			formatOnType: true,
		});

		// Listen for content changes
		editor.onDidChangeModelContent(() => {
			const value = editor?.getValue() || '';
			emit('update:modelValue', value);
			emit('change', value);
		});

		// Set initial value
		if (props.modelValue) {
			editor.setValue(props.modelValue);
		}
	} catch (error) {
		console.error('Failed to initialize Monaco editor:', error);
		// If it's a passive event listener error, try to reinitialize
		if (error instanceof Error && error.message.includes('passive')) {
			console.warn('Passive event listener error detected, retrying...');
			setTimeout(() => {
				initEditor();
			}, 200);
		}
	}
};

// Update editor value when prop changes
const updateEditorValue = (value: string) => {
	if (editor) {
		const currentValue = editor.getValue();
		if (currentValue !== value) {
			editor.setValue(value);
		}
	}
};

// Update editor language
const updateEditorLanguage = (language: string) => {
	if (editor && monaco) {
		monaco.editor.setModelLanguage(editor.getModel()!, language);
	}
};

// Update read-only state
const updateReadOnly = (readOnly: boolean) => {
	if (editor) {
		editor.updateOptions({ readOnly });
	}
};

// Lifecycle
onMounted(async () => {
	// Delay initialization to ensure DOM is fully ready
	setTimeout(async () => {
		await initEditor();
	}, 100);
});

onBeforeUnmount(() => {
	if (editor) {
		editor.dispose();
	}
});

// Watchers
watch(() => props.modelValue, updateEditorValue);
watch(() => props.language, updateEditorLanguage);
watch(() => props.readOnly, updateReadOnly);

// Expose methods
defineExpose({
	getValue: () => editor?.getValue() || '',
	setValue: (value: string) => editor?.setValue(value),
	focus: () => editor?.focus(),
	dispose: () => editor?.dispose(),
});
</script>

<style scoped lang="scss">
.code-editor-container {
	@apply space-y-2;
}

.code-editor-header {
	@apply space-y-1;
}

.code-editor-wrapper {
	@apply border rounded-md border-primary-200 bg-white overflow-hidden;
	height: v-bind(height);
	min-height: 200px;
}

// Monaco editor custom styles
:deep(.monaco-editor) {
	@apply rounded-md;
}

:deep(.monaco-editor .margin) {
	@apply bg-gray-50;
}

:deep(.monaco-editor .monaco-editor-background) {
	@apply bg-white;
}

:deep(.monaco-editor .line-numbers) {
	@apply text-gray-500;
}

// Dark mode support
.dark {
	:deep(.monaco-editor) {
		@apply bg-gray-900;
	}

	:deep(.monaco-editor .margin) {
		@apply bg-gray-800;
	}

	:deep(.monaco-editor .monaco-editor-background) {
		@apply bg-gray-900;
	}

	:deep(.monaco-editor .line-numbers) {
		@apply text-gray-400;
	}
}
</style> 