<template>
	<div class="options-editor">
		<div class="options-section">
			<div class="options-header">
				<label class="options-label">Options</label>
			</div>

			<div class="options-input-grid">
				<div class="option-input-item">
					<label class="input-label">Value</label>
					<el-input
						:model-value="newOption.value"
						placeholder="Option value"
						class="option-input"
						@input="updateOptionValue"
					/>
				</div>
				<div class="option-input-item">
					<label class="input-label">Label</label>
					<div class="label-input-group">
						<el-input
							:model-value="newOption.label"
							placeholder="Option label"
							class="option-input"
							@input="updateOptionLabel"
						/>
						<el-button
							type="primary"
							@click="addOption"
							:disabled="!newOption.value || !newOption.label"
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
					<div v-for="option in options" :key="option.id" class="option-item">
						<div class="option-content">
							<span class="option-label-text">{{ option.label }}</span>
							<span class="option-value-text">({{ option.value }})</span>
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
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { Delete } from '@element-plus/icons-vue';

interface Option {
	id: string;
	value: string;
	label: string;
}

interface NewOption {
	value: string;
	label: string;
}

interface Props {
	options: Option[];
	newOption: NewOption;
}

defineProps<Props>();

const emits = defineEmits<{
	'update-option-value': [value: string];
	'update-option-label': [label: string];
	'add-option': [];
	'remove-option': [id: string];
}>();

const updateOptionValue = (value: string) => {
	emits('update-option-value', value);
};

const updateOptionLabel = (label: string) => {
	emits('update-option-label', label);
};

const addOption = () => {
	emits('add-option');
};

const removeOption = (id: string) => {
	emits('remove-option', id);
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

.options-input-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
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
}

.option-content {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.option-label-text {
	font-weight: 500;
	color: var(--primary-800);
}

.option-value-text {
	font-size: 0.875rem;
	color: var(--primary-600);
}

.delete-option-btn {
	color: var(--el-color-danger) !important;
}

.delete-option-btn:hover {
	background-color: var(--el-color-danger-light-9) !important;
}

/* 深色模式支持 */
.dark .options-label,
.dark .input-label,
.dark .current-options-label {
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

.dark .option-label-text {
	color: var(--primary-100);
}

.dark .option-value-text {
	color: var(--primary-300);
}
</style>
