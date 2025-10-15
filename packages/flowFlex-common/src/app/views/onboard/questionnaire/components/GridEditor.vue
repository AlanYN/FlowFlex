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
							<div class="flex items-center gap-2">
								<span class="grid-item-number">{{ index + 1 }}.</span>
								<div v-if="!editingRows[row.temporaryId]" class="grid-item-label">
									{{ row.label }}
								</div>
								<el-input
									v-else
									:model-value="editingRows[row.temporaryId]"
									class="grid-edit-input"
									@input="updateEditingRow(row.temporaryId, $event)"
									@keyup.enter="saveRowEdit(row.temporaryId)"
									@blur="saveRowEdit(row.temporaryId)"
								/>
								<el-button
									v-if="!editingRows[row.temporaryId]"
									type="primary"
									link
									size="small"
									@click="startRowEdit(row.temporaryId, row.label)"
									:icon="Edit"
								/>
							</div>

							<div class="grid-item-actions">
								<el-button
									type="danger"
									link
									size="small"
									@click="removeRow(row.temporaryId)"
								>
									<el-icon><Close /></el-icon>
								</el-button>
							</div>
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
						<div
							v-for="(column, index) in columns"
							:key="column.id"
							class="grid-editor-item"
							:class="{ 'grid-item-other': column.isOther }"
						>
							<div class="flex items-center gap-2">
								<span class="grid-item-number">{{ index + 1 }}.</span>

								<!-- 列标签显示/编辑 -->
								<div
									v-if="!editingColumns[column.temporaryId]"
									class="grid-item-label"
								>
									{{ column.label }}
									<el-tag
										v-if="column.isOther"
										size="small"
										type="warning"
										class="other-tag"
									>
										Other
									</el-tag>
								</div>
								<el-input
									v-else
									:model-value="editingColumns[column.temporaryId]"
									class="grid-edit-input"
									@input="updateEditingColumn(column.temporaryId, $event)"
									@keyup.enter="saveColumnEdit(column.temporaryId)"
									@blur="saveColumnEdit(column.temporaryId)"
								/>
								<el-button
									v-if="!editingColumns[column.temporaryId]"
									type="primary"
									link
									size="small"
									@click="startColumnEdit(column.temporaryId, column.label)"
									:icon="Edit"
								/>
							</div>

							<div class="grid-item-actions">
								<el-button
									type="danger"
									link
									size="small"
									@click="removeColumn(column.temporaryId)"
								>
									<el-icon><Close /></el-icon>
								</el-button>
							</div>
						</div>

						<!-- 添加列输入 -->
						<div class="grid-add-item">
							<span class="grid-item-number">{{ columns.length + 1 }}.</span>
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

						<!-- 添加Other选项按钮 -->
						<div v-if="!hasOtherColumn" class="grid-add-other">
							<el-button
								type="primary"
								size="small"
								@click="addOtherColumn"
								class="add-other-btn"
								:icon="Plus"
							>
								Add "Other" Option
							</el-button>
							<span class="other-help-text">
								Allow users to specify their own answer
							</span>
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
import { computed, ref } from 'vue';
import { Close, Plus, Edit } from '@element-plus/icons-vue';

interface GridItem {
	id?: string;
	temporaryId: string;
	label: string;
	isOther?: boolean;
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

const props = defineProps<Props>();

const emits = defineEmits<{
	'add-row': [];
	'remove-row': [id: string];
	'add-column': [];
	'remove-column': [id: string];
	'add-other-column': [];
	'update-new-row-label': [label: string];
	'update-new-column-label': [label: string];
	'update-require-one-response-per-row': [value: boolean];
	'update-row-label': [id: string, label: string];
	'update-column-label': [id: string, label: string];
}>();

// 编辑状态管理
const editingRows = ref<Record<string, string>>({});
const editingColumns = ref<Record<string, string>>({});

// 检查是否已有Other列
const hasOtherColumn = computed(() => {
	return props.columns.some((column) => column.isOther);
});

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

const addOtherColumn = () => {
	emits('add-other-column');
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

// 行编辑功能
const startRowEdit = (id: string, currentLabel: string) => {
	editingRows.value[id] = currentLabel;
};

const updateEditingRow = (id: string, value: string) => {
	editingRows.value[id] = value;
};

const saveRowEdit = (id: string) => {
	console.log('saveRowEdit', id, editingRows.value);
	const newLabel = editingRows.value[id]?.trim();
	if (newLabel && newLabel !== props.rows.find((row) => row.id === id)?.label) {
		emits('update-row-label', id, newLabel);
	} else if (!newLabel) {
		// 如果输入为空，删除该项
		removeRow(id);
	}
	delete editingRows.value[id];
};

// 列编辑功能
const startColumnEdit = (id: string, currentLabel: string) => {
	editingColumns.value[id] = currentLabel;
};

const updateEditingColumn = (id: string, value: string) => {
	editingColumns.value[id] = value;
};

const saveColumnEdit = (id: string) => {
	console.log('saveColumnEdit', id, editingColumns.value);
	const newLabel = editingColumns.value[id]?.trim();
	if (newLabel && newLabel !== props.columns.find((column) => column.id === id)?.label) {
		emits('update-column-label', id, newLabel);
	} else if (!newLabel) {
		// 如果输入为空，删除该项
		removeColumn(id);
	}
	delete editingColumns.value[id];
};
</script>

<style scoped lang="scss">
.grid-editor {
	@apply rounded-xl;
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

.grid-editor-layout {
	display: flex;
	gap: 2rem;
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
	justify-content: space-between;
	gap: 0.75rem;
	padding: 0.5rem;
	transition: background-color 0.2s;
	@apply rounded-xl;
}

.grid-item-other {
	background-color: var(--el-color-white);
	border: 1px solid var(--el-color-primary);
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
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.grid-edit-input {
	flex: 1;
}

.grid-item-actions {
	display: flex;
	gap: 0.25rem;
	opacity: 0.6;
	transition: opacity 0.2s;
}

.grid-editor-item:hover .grid-item-actions {
	opacity: 1;
}

.other-tag {
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
}

.grid-column-icon {
	color: var(--primary-500);
	font-size: 1rem;
}

.grid-add-item {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.5rem;
	border: 1px dashed var(--el-border-color-light);
	margin-top: 0.5rem;
	@apply rounded-xl;
}

.grid-add-input {
	flex: 1;
}

.grid-add-other {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	margin-top: 1rem;
	padding: 0.75rem;
	background-color: var(--el-color-white);
	border: 1px dashed var(--el-color-black);
	@apply rounded-xl;
}

.add-other-btn {
	align-self: flex-start;
}

.other-help-text {
	font-size: 0.75rem;
	color: var(--el-color-primary);
	font-style: italic;
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
	background-color: var(--black);
	border-color: var(--el-border-color-dark);
}

.dark .grid-label {
	color: var(--el-color-white);
}

.dark .grid-column-title {
	color: var(--el-color-white);
}

.dark .grid-item-other {
	background-color: var(--el-color-black);
	border-color: var(--el-color-primary);
}

.dark .grid-item-number {
	color: var(--el-color-white);
}

.dark .grid-item-label {
	color: var(--primary-200);
}

.dark .grid-column-icon {
	color: var(--primary-400);
}

.dark .grid-add-item {
	border-color: var(--el-border-color-light);
}

.dark .grid-add-other {
	background-color: var(--el-color-black);
	border-color: var(--el-color-white);
}

.dark .other-help-text {
	color: var(--el-color-primary);
}

.dark .grid-options {
	border-top-color: var(--primary-600);
}
</style>
