<template>
	<div class="grid-editor">
		<div class="grid-section">
			<div class="grid-header">
				<label class="grid-label">Grid Configuration</label>
			</div>

			<!-- 行列编辑区域 - 左右布局 -->
			<div class="grid-editor-layout">
				<!-- 左侧：行编辑 -->
				<div class="grid-column-editor">
					<div class="grid-column-header">
						<h4 class="grid-column-title">Rows</h4>
					</div>

					<!-- 行列表 -->
					<div class="grid-items-container">
						<div v-for="(row, index) in rows" :key="row.id" class="grid-editor-item">
							<span class="grid-item-number">{{ index + 1 }}.</span>
							<span class="grid-item-label">{{ row.label }}</span>
							<el-button
								type="danger"
								text
								size="small"
								@click="removeRow(row.id)"
								class="grid-delete-btn"
							>
								<el-icon><Close /></el-icon>
							</el-button>
						</div>

						<!-- 添加行输入 -->
						<div class="grid-add-item">
							<span class="grid-item-number">{{ rows.length + 1 }}.</span>
							<el-input
								:model-value="newRow.label"
								placeholder="Add row"
								class="grid-add-input"
								@input="updateNewRowLabel"
								@keyup.enter="addRow"
							/>
							<el-button
								type="primary"
								size="small"
								@click="addRow"
								:disabled="!newRow.label.trim()"
							>
								Add
							</el-button>
						</div>
					</div>
				</div>

				<!-- 右侧：列编辑 -->
				<div class="grid-column-editor">
					<div class="grid-column-header">
						<h4 class="grid-column-title">Columns</h4>
					</div>

					<!-- 列列表 -->
					<div class="grid-items-container">
						<div v-for="column in columns" :key="column.id" class="grid-editor-item">
							<el-icon class="grid-column-icon"><Check /></el-icon>
							<span class="grid-item-label">{{ column.label }}</span>
							<el-button
								type="danger"
								text
								size="small"
								@click="removeColumn(column.id)"
								class="grid-delete-btn"
							>
								<el-icon><Close /></el-icon>
							</el-button>
						</div>

						<!-- 添加列输入 -->
						<div class="grid-add-item">
							<el-icon class="grid-column-icon"><Check /></el-icon>
							<el-input
								:model-value="newColumn.label"
								placeholder="Add column"
								class="grid-add-input"
								@input="updateNewColumnLabel"
								@keyup.enter="addColumn"
							/>
							<el-button
								type="primary"
								size="small"
								@click="addColumn"
								:disabled="!newColumn.label.trim()"
							>
								Add
							</el-button>
						</div>
					</div>
				</div>
			</div>

			<!-- 网格选项 -->
			<div class="grid-options">
				<el-checkbox
					v-if="questionType === 'checkbox_grid'"
					:model-value="requireOneResponsePerRow"
					@change="updateRequireOneResponsePerRow"
					class="grid-option-checkbox"
				>
					Require a response in each row
				</el-checkbox>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { Close, Check } from '@element-plus/icons-vue';

interface GridItem {
	id: string;
	label: string;
}

interface NewGridItem {
	label: string;
}

interface Props {
	questionType: string;
	rows: GridItem[];
	columns: GridItem[];
	newRow: NewGridItem;
	newColumn: NewGridItem;
	requireOneResponsePerRow: boolean;
}

defineProps<Props>();

const emits = defineEmits<{
	'add-row': [];
	'remove-row': [id: string];
	'add-column': [];
	'remove-column': [id: string];
	'update-new-row-label': [label: string];
	'update-new-column-label': [label: string];
	'update-require-one-response-per-row': [value: boolean];
}>();

const addRow = () => {
	emits('add-row');
};

const removeRow = (id: string) => {
	emits('remove-row', id);
};

const addColumn = () => {
	emits('add-column');
};

const removeColumn = (id: string) => {
	emits('remove-column', id);
};

const updateNewRowLabel = (label: string) => {
	emits('update-new-row-label', label);
};

const updateNewColumnLabel = (label: string) => {
	emits('update-new-column-label', label);
};

const updateRequireOneResponsePerRow = (value: boolean) => {
	emits('update-require-one-response-per-row', value);
};
</script>

<style scoped lang="scss">
.grid-editor {
	margin-top: 1.5rem;
	padding: 1rem;
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	background-color: var(--primary-25);
}

.grid-section {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.grid-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 0.5rem;
}

.grid-label {
	font-size: 0.875rem;
	font-weight: 600;
	color: var(--primary-800);
}

.grid-editor-layout {
	display: flex;
	gap: 2rem;
	margin-top: 1rem;
}

.grid-column-editor {
	flex: 1;
	min-width: 0;
}

.grid-column-header {
	margin-bottom: 1rem;
}

.grid-column-title {
	font-size: 1rem;
	font-weight: 500;
	color: var(--primary-800);
	margin: 0;
}

.grid-items-container {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.grid-editor-item {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.5rem;
	border-radius: 0.375rem;
	transition: background-color 0.2s;
}

.grid-editor-item:hover {
	background-color: var(--primary-25);
}

.grid-item-number {
	font-size: 0.875rem;
	color: var(--primary-600);
	min-width: 1.5rem;
}

.grid-item-label {
	flex: 1;
	font-size: 0.875rem;
	color: var(--primary-700);
}

.grid-column-icon {
	color: var(--primary-500);
	font-size: 1rem;
}

.grid-delete-btn {
	opacity: 0.6;
	transition: opacity 0.2s;
}

.grid-delete-btn:hover {
	opacity: 1;
}

.grid-add-item {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.5rem;
	border: 1px dashed var(--primary-200);
	border-radius: 0.375rem;
	margin-top: 0.5rem;
}

.grid-add-input {
	flex: 1;
}

.grid-options {
	margin-top: 1.5rem;
	padding-top: 1rem;
	border-top: 1px solid var(--primary-100);
}

.grid-option-checkbox {
	font-size: 0.875rem;
}

/* 深色模式支持 */
.dark .grid-editor {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

.dark .grid-label {
	color: var(--primary-200);
}

.dark .grid-column-title {
	color: var(--primary-200);
}

.dark .grid-editor-item:hover {
	background-color: var(--primary-600);
}

.dark .grid-item-number {
	color: var(--primary-300);
}

.dark .grid-item-label {
	color: var(--primary-200);
}

.dark .grid-column-icon {
	color: var(--primary-400);
}

.dark .grid-add-item {
	border-color: var(--primary-500);
}

.dark .grid-options {
	border-top-color: var(--primary-600);
}
</style>
