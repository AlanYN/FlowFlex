<template>
	<div class="options-editor">
		<div class="options-section">
			<div class="options-header">
				<label class="options-label">Options</label>
			</div>

			<div class="options-input-section">
				<div class="option-input-item">
					<label class="input-label">Option Label</label>
					<div class="label-input-group">
						<el-input
							:model-value="newOption.label"
							placeholder="Enter option label"
							class="option-input"
							@input="updateOptionLabel"
						/>
						<el-button
							type="primary"
							@click="addOption"
							:disabled="!newOption.label"
							class="add-option-btn"
						>
							Add
						</el-button>
					</div>
				</div>
			</div>
		</div>

		<!-- 当前选项列表 -->
		<div v-if="options.length > 0" class="current-options">
			<div class="current-options-container">
				<label class="current-options-label">Current Options</label>
				<div class="options-list">
					<div
						v-for="option in options"
						:key="option.id"
						class="option-item"
						:class="{ 'option-item-other': option.isOther }"
					>
						<div class="option-content">
							<span class="option-label-text">
								{{ option.label }}
								<el-tag
									v-if="option.isOther"
									size="small"
									type="warning"
									class="other-tag"
								>
									Other
								</el-tag>
							</span>
							<!-- <span class="option-value-text">({{ option.value }})</span> -->
						</div>
						<el-button
							type="danger"
							link
							size="small"
							@click="removeOption(option.id)"
							:icon="Delete"
							class="delete-option-btn"
						/>
					</div>
				</div>

				<!-- 添加Other选项按钮 -->
			</div>
		</div>
		<div v-if="!hasOtherOption" class="add-other-section">
			<el-button type="success" size="small" @click="addOtherOption" class="add-other-btn">
				<el-icon><Plus /></el-icon>
				Add "Other" Option
			</el-button>
			<span class="other-help-text">Allow users to specify their own answer</span>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Delete, Plus } from '@element-plus/icons-vue';

interface Option {
	id: string;
	value: string;
	label: string;
	isOther?: boolean;
}

interface NewOption {
	value: string;
	label: string;
}

interface Props {
	options: Option[];
	newOption: NewOption;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update-option-value': [value: string];
	'update-option-label': [label: string];
	'add-option': [];
	'add-other-option': [];
	'remove-option': [id: string];
}>();

// 检查是否已有Other选项
const hasOtherOption = computed(() => {
	return props.options.some((option) => option.isOther);
});

const updateOptionLabel = (label: string) => {
	emit('update-option-label', label);
};

const addOption = () => {
	emit('add-option');
};

const addOtherOption = () => {
	emit('add-other-option');
};

const removeOption = (id: string) => {
	emit('remove-option', id);
};
</script>

<style scoped lang="scss">
.options-editor {
	margin-top: 1rem;
}

.options-section {
	margin-bottom: 1rem;
}

.options-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 1rem;
}

.options-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
}

.options-input-section {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.option-input-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.input-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
}

.label-input-group {
	display: flex;
	gap: 0.5rem;
}

.option-input {
	flex: 1;
}

.add-option-btn {
	flex-shrink: 0;
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
	color: white !important;
}

.add-option-btn:hover {
	background-color: var(--primary-700) !important;
	border-color: var(--primary-700) !important;
}

.add-option-btn:disabled {
	background-color: var(--primary-200) !important;
	border-color: var(--primary-200) !important;
}

.current-options-container {
	border: 1px solid var(--primary-200);
	border-radius: 0.375rem;
	padding: 0.75rem;
	background-color: var(--primary-50);
}

.current-options-label {
	display: block;
	margin-bottom: 0.5rem;
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--primary-700);
}

.options-list {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.option-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0.5rem;
	border: 1px solid var(--primary-100);
	border-radius: 0.375rem;
	background-color: var(--el-bg-color);
	transition: background-color 0.2s;
}

.option-item:hover {
	background-color: var(--primary-25);
}

.option-item-other {
	background-color: var(--el-color-warning-light-9);
	border: 1px solid var(--el-color-warning-light-7);
}

.option-content {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	flex: 1;
}

.option-label-text {
	font-weight: 500;
	color: var(--primary-700);
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.other-tag {
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
}

.option-value-text {
	font-size: 0.75rem;
	color: var(--primary-500);
	font-family: 'Courier New', monospace;
}

.delete-option-btn {
	color: var(--el-color-danger);
	padding: 0.25rem;
}

.delete-option-btn:hover {
	background-color: var(--el-color-danger-light-9);
}

.add-other-section {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	margin-top: 1rem;
	padding: 0.75rem;
	background-color: var(--el-color-success-light-9);
	border: 1px dashed var(--el-color-success-light-7);
	border-radius: 0.375rem;
}

.add-other-btn {
	align-self: flex-start;
}

.other-help-text {
	font-size: 0.75rem;
	color: var(--el-color-success);
	font-style: italic;
}

/* 深色模式支持 */
.dark .options-label,
.dark .input-label,
.dark .current-options-label,
.dark .option-label-text {
	color: var(--primary-200);
}

.dark .current-options-container {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

.dark .option-item {
	background-color: var(--primary-800);
	border-color: var(--primary-600);
}

.dark .option-item:hover {
	background-color: var(--primary-600);
}

.dark .option-item-other {
	background-color: var(--el-color-warning-dark-2);
	border-color: var(--el-color-warning);
}

.dark .option-value-text {
	color: var(--primary-400);
}

.dark .add-other-section {
	background-color: var(--el-color-success-dark-2);
	border-color: var(--el-color-success);
}

.dark .other-help-text {
	color: var(--el-color-success-light-3);
}
</style>
