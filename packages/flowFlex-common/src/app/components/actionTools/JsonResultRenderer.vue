<template>
	<div class="json-result-renderer">
		<!-- JSON工具栏 -->
		<div class="json-toolbar">
			<div class="toolbar-left">
				<div class="flex items-center space-x-2">
					<slot name="label"></slot>
					<el-tag type="primary" size="small">JSON Data</el-tag>
				</div>
				<el-tag v-if="isActionResult && actionResultSummary" type="info" size="small">
					{{ actionResultSummary }}
				</el-tag>
				<el-tag v-if="props.editable" type="warning" size="small">Edit Mode</el-tag>
			</div>
			<div class="toolbar-right">
				<el-button-group>
					<el-button
						:icon="FullScreen"
						@click="openFullscreenDialog"
						size="small"
						title="Fullscreen View"
					>
						Fullscreen
					</el-button>
					<el-button
						:icon="DocumentCopy"
						@click="copyToClipboard"
						size="small"
						:loading="copyLoading"
					>
						Copy
					</el-button>
					<el-button
						:icon="Refresh"
						@click="formatJson"
						size="small"
						:loading="formatLoading"
					>
						Beautify
					</el-button>
				</el-button-group>
			</div>
		</div>

		<!-- JSON内容区域 -->
		<div class="json-content-wrapper">
			<el-scrollbar :max-height="maxHeight" class="json-scrollbar">
				<!-- 只读模式 -->
				<template v-if="!props.editable">
					<!-- eslint-disable-next-line vue/no-v-html -->
					<pre ref="jsonPreRef" class="json-content" v-html="formattedJsonHtml"></pre>
				</template>
				<!-- 编辑模式 -->
				<template v-else>
					<el-input
						v-model="editableContent"
						type="textarea"
						:rows="calculateRows"
						resize="vertical"
						placeholder="Enter JSON content..."
						class="json-editor"
						@input="handleEditInput"
						@blur="validateJson"
					/>
					<div v-if="editError" class="edit-error">
						<el-alert
							:title="editError"
							type="error"
							size="small"
							show-icon
							:closable="false"
						/>
					</div>
				</template>
			</el-scrollbar>
		</div>

		<!-- 编辑模式保存按钮 -->
		<div v-if="props.editable && hasChanges" class="edit-actions">
			<el-button-group>
				<el-button
					type="primary"
					:icon="Check"
					@click="saveChanges"
					size="small"
					:loading="saveLoading"
				>
					Save Changes
				</el-button>
				<el-button :icon="Close" @click="cancelEdit" size="small">Cancel</el-button>
			</el-button-group>
		</div>

		<!-- 搜索功能（可选） -->
		<div v-if="showSearch" class="json-search">
			<el-input
				v-model="searchQuery"
				placeholder="Search in JSON..."
				:prefix-icon="Search"
				size="small"
				clearable
				@input="handleSearch"
			/>
			<div v-if="searchResults.length > 0" class="search-results">
				Found {{ searchResults.length }} matches
			</div>
		</div>

		<!-- 全屏弹窗 -->
		<el-dialog
			v-model="fullscreenVisible"
			title="JSON Viewer - Fullscreen"
			:width="'90%'"
			:top="'5vh'"
			destroy-on-close
			append-to-body
			class="json-fullscreen-dialog"
		>
			<div class="fullscreen-content">
				<!-- 全屏工具栏 -->
				<div class="fullscreen-toolbar">
					<div class="toolbar-left">
						<div class="flex items-center space-x-2">
							<slot name="label"></slot>
							<el-tag type="primary" size="small">JSON Data</el-tag>
						</div>
						<el-tag
							v-if="isActionResult && actionResultSummary"
							type="info"
							size="small"
						>
							{{ actionResultSummary }}
						</el-tag>
						<el-tag v-if="props.editable" type="warning" size="small">Edit Mode</el-tag>
					</div>
					<div class="toolbar-right">
						<el-button-group>
							<el-button
								:icon="DocumentCopy"
								@click="copyToClipboard"
								size="small"
								:loading="copyLoading"
							>
								Copy
							</el-button>
							<el-button
								:icon="Refresh"
								@click="formatJson"
								size="small"
								:loading="formatLoading"
							>
								Format
							</el-button>
						</el-button-group>
					</div>
				</div>

				<!-- 全屏内容区域 -->
				<div class="fullscreen-json-content">
					<el-scrollbar max-height="70vh" class="json-scrollbar">
						<!-- 只读模式 -->
						<template v-if="!props.editable">
							<!-- eslint-disable-next-line vue/no-v-html -->
							<pre
								class="json-content fullscreen-json"
								v-html="formattedJsonHtml"
							></pre>
						</template>
						<!-- 编辑模式 -->
						<template v-else>
							<el-input
								v-model="editableContent"
								type="textarea"
								:rows="30"
								resize="vertical"
								placeholder="Enter JSON content..."
								class="json-editor fullscreen-editor"
								@input="handleEditInput"
								@blur="validateJson"
							/>
							<div v-if="editError" class="edit-error">
								<el-alert
									:title="editError"
									type="error"
									size="small"
									show-icon
									:closable="false"
								/>
							</div>
						</template>
					</el-scrollbar>
				</div>

				<!-- 全屏编辑保存按钮 -->
				<div v-if="props.editable && hasChanges" class="fullscreen-edit-actions">
					<el-button-group>
						<el-button
							type="primary"
							:icon="Check"
							@click="saveFullscreenChanges"
							size="small"
							:loading="saveLoading"
						>
							Save Changes
						</el-button>
						<el-button :icon="Close" @click="cancelEdit" size="small">Cancel</el-button>
					</el-button-group>
				</div>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { DocumentCopy, Refresh, Search, FullScreen, Check, Close } from '@element-plus/icons-vue';
import type { OutputType } from '@/utils/output-type-detector';
import {
	applySyntaxHighlight,
	formatJsonString,
	isValidJson,
} from '@/utils/json-syntax-highlighter';
import { isActionResultType, getActionResultSummary } from '@/utils/output-type-detector';

// Props定义
interface Props {
	outputType: OutputType;
	maxHeight?: string;
	showSearch?: boolean;
	enableSyntaxHighlight?: boolean;
	editable?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	maxHeight: '400px',
	showSearch: false,
	enableSyntaxHighlight: true,
	editable: false,
});

// Emits定义
const emits = defineEmits<{
	copy: [outputType: OutputType];
	format: [outputType: OutputType];
	search: [query: string, results: Array<{ index: number; text: string }>];
	save: [newData: any, outputType: OutputType];
	edit: [isEditing: boolean];
}>();

// 响应式数据
const jsonPreRef = ref<HTMLPreElement>();
const copyLoading = ref(false);
const formatLoading = ref(false);
const saveLoading = ref(false);
const searchQuery = ref('');
const searchResults = ref<Array<{ index: number; text: string }>>([]);

// 编辑模式相关
const editableContent = ref('');
const originalContent = ref('');
const editError = ref('');

// 全屏弹窗相关
const fullscreenVisible = ref(false);

// 计算属性
const jsonData = computed(() => props.outputType.data);
const isActionResult = computed(() => isActionResultType(props.outputType));
const actionResultSummary = computed(() =>
	isActionResult.value ? getActionResultSummary(props.outputType) : ''
);

// 编辑相关计算属性
const hasChanges = computed(() => editableContent.value !== originalContent.value);

// 计算编辑器行数
const calculateRows = computed(() => {
	const lines = editableContent.value.split('\n').length;
	return Math.max(10, Math.min(lines + 2, 25));
});

// 格式化的JSON字符串
const formattedJsonString = computed(() => {
	try {
		if (typeof jsonData.value === 'string') {
			// 如果是字符串，尝试解析
			const parsed = JSON.parse(jsonData.value);
			return JSON.stringify(parsed, null, 2);
		} else if (typeof jsonData.value === 'object' && jsonData.value !== null) {
			// 如果是对象，直接格式化
			return JSON.stringify(jsonData.value, null, 2);
		} else {
			// 其他类型转为字符串
			return String(jsonData.value);
		}
	} catch (error) {
		console.error('JSON formatting error:', error);
		return String(jsonData.value);
	}
});

// HTML转义函数
const escapeHtml = (text: string): string => {
	const div = document.createElement('div');
	div.textContent = text;
	return div.innerHTML;
};

// 带语法高亮的HTML
const formattedJsonHtml = computed(() => {
	if (!props.enableSyntaxHighlight) {
		return escapeHtml(formattedJsonString.value);
	}

	// 应用语法高亮
	return applySyntaxHighlight(formattedJsonString.value);
});

// 复制到剪贴板
const copyToClipboard = async () => {
	copyLoading.value = true;

	try {
		await navigator.clipboard.writeText(formattedJsonString.value);
		ElMessage.success('JSON copied to clipboard');
		emits('copy', props.outputType);
	} catch (error) {
		console.error('Copy failed:', error);
		// 降级方案：使用传统方法
		try {
			const textArea = document.createElement('textarea');
			textArea.value = formattedJsonString.value;
			document.body.appendChild(textArea);
			textArea.select();
			document.execCommand('copy');
			document.body.removeChild(textArea);
			ElMessage.success('JSON copied to clipboard');
			emits('copy', props.outputType);
		} catch (error) {
			console.error('Copy failed:', error);
		}
	} finally {
		copyLoading.value = false;
	}
};

// 格式化JSON
const formatJson = async () => {
	formatLoading.value = true;

	try {
		if (!isValidJson(jsonData.value)) {
			throw new Error('Invalid JSON format');
		}
		// 注意：这里不能直接修改props，应该通过事件通知父组件
		const formattedData = formatJsonString(jsonData.value, 2);
		// 可以考虑通过事件将格式化后的数据传递给父组件
		console.log('Formatted JSON:', formattedData);
		ElMessage.success('JSON formatted successfully');
		emits('format', props.outputType);
	} finally {
		formatLoading.value = false;
	}
};

// 搜索处理
const handleSearch = (query: string) => {
	if (!query.trim()) {
		searchResults.value = [];
		return;
	}

	try {
		const jsonString = formattedJsonString.value.toLowerCase();
		const searchTerm = query.toLowerCase();
		const matches: Array<{ index: number; text: string }> = [];

		let index = 0;
		while ((index = jsonString.indexOf(searchTerm, index)) !== -1) {
			matches.push({
				index,
				text: formattedJsonString.value.substr(index, query.length),
			});
			index += searchTerm.length;
		}

		searchResults.value = matches;
		emits('search', query, matches);
	} catch (error) {
		console.error('Search error:', error);
		searchResults.value = [];
	}
};

// 编辑模式相关方法
const initializeEditContent = () => {
	if (props.editable) {
		originalContent.value = formattedJsonString.value;
		editableContent.value = formattedJsonString.value;
		editError.value = '';
		emits('edit', true);
	}
};

const handleEditInput = () => {
	editError.value = '';
};

const validateJson = () => {
	if (!editableContent.value.trim()) {
		editError.value = '';
		return;
	}

	try {
		JSON.parse(editableContent.value);
		editError.value = '';
	} catch (error) {
		editError.value = `Invalid JSON: ${(error as Error).message}`;
	}
};

const saveChanges = async () => {
	if (editError.value) {
		ElMessage.error('Please fix JSON errors before saving');
		return;
	}

	if (!editableContent.value.trim()) {
		ElMessage.error('Content cannot be empty');
		return;
	}

	saveLoading.value = true;
	try {
		const parsedData = JSON.parse(editableContent.value);
		emits('save', parsedData, props.outputType);
		originalContent.value = editableContent.value;
		ElMessage.success('Changes saved successfully');
	} catch (error) {
		ElMessage.error(`Failed to save: ${(error as Error).message}`);
	} finally {
		saveLoading.value = false;
	}
};

const cancelEdit = () => {
	if (hasChanges.value) {
		ElMessageBox.confirm(
			'Are you sure you want to cancel? All changes will be lost.',
			'Cancel Edit',
			{
				confirmButtonText: 'Yes, Cancel',
				cancelButtonText: 'Continue Editing',
				type: 'warning',
			}
		)
			.then(() => {
				editableContent.value = originalContent.value;
				editError.value = '';
			})
			.catch(() => {
				// User cancelled
			});
	} else {
		editableContent.value = originalContent.value;
		editError.value = '';
	}
};

// 全屏弹窗相关方法
const openFullscreenDialog = () => {
	fullscreenVisible.value = true;
};

const saveFullscreenChanges = async () => {
	await saveChanges();
	if (!editError.value) {
		fullscreenVisible.value = false;
	}
};

// 监听数据变化和编辑模式变化，同步编辑内容
watch(
	[() => props.outputType.data, () => props.editable],
	() => {
		if (props.editable) {
			initializeEditContent();
		}
	},
	{ deep: true, immediate: true }
);
</script>

<style scoped lang="scss">
.json-result-renderer {
	@apply w-full;
}

.json-toolbar {
	@apply flex items-center justify-between px-3 rounded-t-xl;
}

.toolbar-left {
	@apply flex items-center space-x-2;
}

.toolbar-right {
	@apply flex items-center space-x-2;
}

.json-content-wrapper {
	@apply bg-white dark:bg-gray-900 rounded-b-xl;
}

.json-scrollbar {
	@apply w-full;
}

.json-content {
	@apply font-mono text-sm leading-relaxed p-2 m-0 whitespace-pre-wrap break-words;
	background: transparent;
	border: none;
	color: var(--el-text-color-primary);
}

.json-search {
	@apply mt-3 p-3 bg-gray-50 dark:bg-gray-800 rounded-xl;
}

.search-results {
	@apply mt-2 text-xs text-gray-600 dark:text-gray-400;
}

/* 编辑模式样式 */
.json-editor {
	@apply font-mono text-sm;
}

.edit-error {
	@apply mt-2;
}

.edit-actions {
	@apply mt-3 p-3 bg-gray-50 dark:bg-gray-800 rounded-xl flex justify-end;
}

/* 全屏弹窗样式 */
.json-fullscreen-dialog {
	:deep(.el-dialog__body) {
		@apply p-0;
	}
}

.fullscreen-content {
	@apply h-full;
}

.fullscreen-toolbar {
	@apply flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700;
}

.fullscreen-json-content {
	@apply p-4;
}

.fullscreen-json {
	@apply text-base leading-relaxed;
}

.fullscreen-editor {
	@apply text-base;

	:deep(.el-textarea__inner) {
		@apply font-mono;
	}
}

.fullscreen-edit-actions {
	@apply p-4 border-t border-gray-200 dark:border-gray-700 flex justify-end bg-gray-50 dark:bg-gray-800;
}

/* 响应式设计 */
@media (max-width: 640px) {
	.json-toolbar {
		@apply flex-col space-y-2 items-stretch;
	}

	.toolbar-left,
	.toolbar-right {
		@apply justify-center;
	}

	.fullscreen-toolbar {
		@apply flex-col space-y-2 items-stretch;
	}

	.edit-actions,
	.fullscreen-edit-actions {
		@apply justify-center;
	}
}

/* 暗色模式适配 */
html.dark {
	.json-toolbar {
		background: var(--el-bg-color-page);
	}

	.json-content-wrapper {
		background: var(--el-bg-color);
	}

	.json-search {
		background: var(--el-bg-color-page);
	}

	.edit-actions {
		background: var(--el-bg-color-page);
	}

	.fullscreen-toolbar {
		background: var(--el-bg-color-page);
		border-color: var(--el-border-color);
	}

	.fullscreen-edit-actions {
		background: var(--el-bg-color-page);
		border-color: var(--el-border-color);
	}
}
</style>
