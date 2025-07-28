<template>
	<div class="new-question-editor">
		<h4 class="editor-title">{{ isEditing ? 'Edit Question' : 'Add New Question' }}</h4>
		<el-form :model="newQuestion" label-position="top">
			<el-row :gutter="16">
				<el-col :span="12">
					<el-form-item label="Question Type">
						<el-select
							v-model="newQuestion.type"
							placeholder="Select question type"
							style="width: 100%"
							@change="handleQuestionTypeChange"
						>
							<template #prefix>
								<div v-if="newQuestion.type" class="type-option">
									<el-icon class="type-icon">
										<component :is="getQuestionTypeIcon(newQuestion.type)" />
									</el-icon>
								</div>
							</template>
							<el-option
								v-for="type in questionTypes"
								:key="type.id"
								:label="type.name"
								:value="type.id"
							>
								<div class="type-option">
									<el-icon class="type-icon">
										<component :is="type.icon" />
									</el-icon>
									<span class="type-option-name">{{ type.name }}</span>
									<el-tag
										v-if="type.isNew"
										size="small"
										type="success"
										class="type-option-tag"
									>
										New
									</el-tag>
								</div>
							</el-option>
						</el-select>
					</el-form-item>
				</el-col>
				<el-col :span="12">
					<el-form-item label="Required">
						<el-switch v-model="newQuestion.required" />
					</el-form-item>
				</el-col>
			</el-row>
			<el-form-item label="Question Text" required>
				<el-input v-model="newQuestion.question" placeholder="Enter question text" />
			</el-form-item>
			<el-form-item label="Question Description">
				<el-input
					v-model="newQuestion.description"
					type="textarea"
					:rows="2"
					placeholder="Enter question description or help text"
				/>
			</el-form-item>

			<!-- 选项编辑器 -->
			<OptionsEditor
				v-if="needsOptions(newQuestion.type)"
				:options="newQuestion.options"
				:new-option="newOptionForEditor"
				@update-option-value="handleOptionValueUpdate"
				@update-option-label="handleOptionLabelUpdate"
				@add-option="handleAddOption"
				@remove-option="handleRemoveOption"
				@add-other-option="handleAddOtherOption"
			/>

			<!-- 网格编辑器 -->
			<GridEditor
				v-if="needsGrid(newQuestion.type)"
				:question-type="newQuestion.type"
				:rows="newQuestion.rows"
				:columns="newQuestion.columns"
				:new-row="newRow"
				:new-column="newColumn"
				:require-one-response-per-row="newQuestion.requireOneResponsePerRow"
				@add-row="handleAddRow"
				@remove-row="handleRemoveRow"
				@add-column="handleAddColumn"
				@remove-column="handleRemoveColumn"
				@add-other-column="handleAddOtherColumn"
				@update-new-row-label="(label) => (newRow.label = label)"
				@update-new-column-label="(label) => (newColumn.label = label)"
				@update-require-one-response-per-row="
					(value) => (newQuestion.requireOneResponsePerRow = value)
				"
			/>

			<!-- 线性量表编辑器 -->
			<LinearScaleEditor
				v-if="needsLinearScale(newQuestion.type)"
				:min="newQuestion.min"
				:max="newQuestion.max"
				:min-label="newQuestion.minLabel"
				:max-label="newQuestion.maxLabel"
				@update-min="(value) => (newQuestion.min = value)"
				@update-max="(value) => (newQuestion.max = value)"
				@update-min-label="(label) => (newQuestion.minLabel = label)"
				@update-max-label="(label) => (newQuestion.maxLabel = label)"
			/>

			<!-- 评分编辑器 -->
			<RatingEditor
				v-if="needsRating(newQuestion.type)"
				:max="newQuestion.max"
				:icon-type="newQuestion.iconType"
				@update-max="(value) => (newQuestion.max = value)"
				@update-icon-type="(type) => (newQuestion.iconType = type)"
			/>

			<div class="action-buttons">
				<el-button
					type="primary"
					@click="handleAddQuestion"
					:disabled="!newQuestion.question || !newQuestion.type"
					:icon="Plus"
					class="add-question-btn"
				>
					{{ isEditing ? 'Update Question' : 'Add Question' }}
				</el-button>

				<!-- 编辑模式下的取消按钮 -->
				<el-button
					v-if="isEditing"
					type="info"
					@click="handleCancelEdit"
					class="cancel-edit-btn"
				>
					Cancel
				</el-button>
			</div>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, onMounted } from 'vue';
import { Plus } from '@element-plus/icons-vue';
import OptionsEditor from './OptionsEditor.vue';
import GridEditor from './GridEditor.vue';
import LinearScaleEditor from './LinearScaleEditor.vue';
import RatingEditor from './RatingEditor.vue';

interface QuestionType {
	id: string;
	name: string;
	icon: string;
	isNew?: boolean;
}

interface Props {
	questionTypes: QuestionType[];
	pressentQuestionType: string;
	editingQuestion?: any;
	isEditing?: boolean;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'add-question': [question: any];
	'update-question': [question: any];
	'change-question-type': [type: string];
	'cancel-edit': [];
}>();

// 初始化表单数据的函数
const getInitialFormData = () => ({
	type: props.pressentQuestionType,
	question: '',
	description: '',
	required: false,
	options: [] as Array<{ id: string; value: string; label: string; isOther?: boolean }>,
	rows: [] as Array<{ id: string; label: string; isOther?: boolean }>,
	columns: [] as Array<{ id: string; label: string; isOther?: boolean }>,
	requireOneResponsePerRow: false,
	min: 1,
	max: 5,
	minLabel: '',
	maxLabel: '',
	iconType: 'star',
});

// 新问题数据 - 使用计算属性来处理编辑状态
const newQuestion = reactive(getInitialFormData());

// 新选项数据
const newOption = reactive({
	label: '',
});

// 新行数据
const newRow = reactive({
	label: '',
});

// 新列数据
const newColumn = reactive({
	label: '',
});

// 为 OptionsEditor 提供的选项对象（包含自动生成的 value）
const newOptionForEditor = computed(() => {
	const generatedValue = newOption.label
		? generateOptionValue(newOption.label, newQuestion.options)
		: '';

	return {
		value: generatedValue,
		label: newOption.label,
	};
});

// 监听问题类型变化
const updateQuestionType = (type: string) => {
	newQuestion.type = type;
};

// 当进入编辑模式时填充数据
const loadEditingData = () => {
	if (props.editingQuestion && props.isEditing) {
		Object.assign(newQuestion, {
			type: props.editingQuestion.type,
			question: props.editingQuestion.question,
			description: props.editingQuestion.description,
			required: props.editingQuestion.required,
			options: [...(props.editingQuestion.options || [])],
			rows: [...(props.editingQuestion.rows || [])],
			columns: [...(props.editingQuestion.columns || [])],
			requireOneResponsePerRow: props.editingQuestion.requireOneResponsePerRow || false,
			min: props.editingQuestion.min || 1,
			max: props.editingQuestion.max || 5,
			minLabel: props.editingQuestion.minLabel || '',
			maxLabel: props.editingQuestion.maxLabel || '',
			iconType: props.editingQuestion.iconType || 'star',
		});
	} else if (!props.isEditing) {
		// 非编辑模式时使用当前选中的问题类型
		newQuestion.type = props.pressentQuestionType;
	}
};

// 重置表单
const resetForm = () => {
	Object.assign(newQuestion, getInitialFormData());
	newOption.label = '';
	newRow.label = '';
	newColumn.label = '';
};

// 生成选项 value 的工具函数
const generateOptionValue = (label: string, existingOptions: Array<{ value: string }>) => {
	const cleanedLabel = label.trim();

	let baseValue = cleanedLabel
		.toLowerCase()
		.replace(/\s+/g, '_')
		.replace(/[^a-z0-9_]/g, '')
		.replace(/_{2,}/g, '_')
		.replace(/^_|_$/g, '');

	if (!baseValue || baseValue.length === 0) {
		baseValue = `option_${Date.now()}`;
	}

	let finalValue = baseValue;
	let counter = 1;

	const existingValues = new Set(existingOptions.map((option) => option.value));

	while (existingValues.has(finalValue)) {
		finalValue = `${baseValue}_${counter}`;
		counter++;

		if (counter > 1000) {
			finalValue = `${baseValue}_${Date.now()}`;
			break;
		}
	}

	return finalValue;
};

// 处理选项 value 更新（自动生成，不允许手动修改）
const handleOptionValueUpdate = (value: string) => {
	console.log('Value update ignored, using auto-generated value instead:', value);
};

// 处理选项 label 更新
const handleOptionLabelUpdate = (label: string) => {
	newOption.label = label;
};

// 处理问题类型改变
const handleQuestionTypeChange = (type: string) => {
	emits('change-question-type', type);
	updateQuestionType(type);

	// 清空选项和网格数据
	newQuestion.options = [];
	newQuestion.rows = [];
	newQuestion.columns = [];
	newQuestion.requireOneResponsePerRow = false;
	newQuestion.min = 1;
	newQuestion.max = 5;
	newQuestion.minLabel = '';
	newQuestion.maxLabel = '';
	newQuestion.max = 5;
	newQuestion.iconType = 'star';

	// 清空输入框
	newOption.label = '';
	newRow.label = '';
	newColumn.label = '';
};

// 添加选项
const handleAddOption = () => {
	if (!newOption.label.trim()) return;

	const generatedValue = generateOptionValue(newOption.label, newQuestion.options);

	newQuestion.options.push({
		id: `option-${Date.now()}`,
		value: generatedValue,
		label: newOption.label,
	});

	newOption.label = '';
};

const handleAddOtherOption = () => {
	newQuestion.options.push({
		id: `option-${Date.now()}`,
		value: generateOptionValue(newOption.label, newQuestion.options),
		label: newOption.label,
		isOther: true,
	});

	newOption.label = '';
};

// 删除选项
const handleRemoveOption = (id: string) => {
	newQuestion.options = newQuestion.options.filter((option) => option.id !== id);
};

// 添加行
const handleAddRow = () => {
	if (!newRow.label.trim()) return;

	newQuestion.rows.push({
		id: `row-${Date.now()}`,
		label: newRow.label,
	});

	newRow.label = '';
};

// 删除行
const handleRemoveRow = (id: string) => {
	newQuestion.rows = newQuestion.rows.filter((row) => row.id !== id);
};

// 添加列
const handleAddColumn = () => {
	if (!newColumn.label.trim()) return;

	newQuestion.columns.push({
		id: `column-${Date.now()}`,
		label: newColumn.label,
	});

	newColumn.label = '';
};

// 添加Other列
const handleAddOtherColumn = () => {
	// 检查是否已经有Other列
	const hasOther = newQuestion.columns.some((column) => column.isOther);
	if (hasOther) return;

	newQuestion.columns.push({
		id: `column-other-${Date.now()}`,
		label: 'Other',
		isOther: true,
	});
};

// 删除列
const handleRemoveColumn = (id: string) => {
	newQuestion.columns = newQuestion.columns.filter((column) => column.id !== id);
};

// 添加或更新问题
const handleAddQuestion = () => {
	if (!newQuestion.question.trim() || !newQuestion.type) return;

	const questionData = {
		id: props.isEditing ? props.editingQuestion.id : `question-${Date.now()}`,
		type: newQuestion.type,
		question: newQuestion.question,
		description: newQuestion.description,
		required: newQuestion.required,
		options: [...newQuestion.options],
		rows: [...newQuestion.rows],
		columns: [...newQuestion.columns],
		requireOneResponsePerRow: newQuestion.requireOneResponsePerRow,
		min: newQuestion.min,
		max: newQuestion.max,
		minLabel: newQuestion.minLabel,
		maxLabel: newQuestion.maxLabel,
		iconType: newQuestion.iconType,
	};

	if (props.isEditing) {
		emits('update-question', questionData);
		resetForm();
	} else {
		emits('add-question', questionData);
		resetForm();
	}
};

// 取消编辑
const handleCancelEdit = () => {
	emits('cancel-edit');
	resetForm();
};

// 工具方法
const needsOptions = (type: string) => {
	return ['multiple_choice', 'checkboxes', 'dropdown'].includes(type);
};

const needsGrid = (type: string) => {
	return ['multiple_choice_grid', 'checkbox_grid'].includes(type);
};

const needsLinearScale = (type: string) => {
	return type === 'linear_scale';
};

const needsRating = (type: string) => {
	return type === 'rating';
};

const getQuestionTypeIcon = (type: string) => {
	const questionType = props.questionTypes.find((t) => t.id === type);
	return questionType ? questionType.icon : 'Document';
};

// 组件挂载时处理编辑状态
onMounted(() => {
	if (props.isEditing && props.editingQuestion) {
		loadEditingData();
	}
});

// 暴露方法给父组件调用（替代 watch）
defineExpose({
	loadEditingData,
	resetForm,
	updateQuestionType,
});
</script>

<style scoped lang="scss">
.new-question-editor {
	padding: 1rem;
	background-color: var(--primary-50);
	border-radius: 0.375rem;
	border: 1px solid var(--primary-100);
}

.editor-title {
	font-weight: 600;
	color: var(--primary-800);
	margin: 0 0 1rem 0;
}

.type-option {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	width: 100%;
}

.type-option-name {
	flex: 1;
	min-width: 0;
}

.type-option-tag {
	flex-shrink: 0;
	font-size: 0.625rem;
	height: 1.125rem;
	line-height: 1;
	padding: 0.125rem 0.25rem;
}

.add-question-btn {
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
	color: white !important;
	flex: 1;
}

.add-question-btn:hover {
	background-color: var(--primary-700) !important;
	border-color: var(--primary-700) !important;
}

.add-question-btn:disabled {
	background-color: var(--primary-200) !important;
	border-color: var(--primary-200) !important;
}

.cancel-edit-btn {
	background-color: var(--el-color-info) !important;
	border-color: var(--el-color-info) !important;
	color: white !important;
	flex: 1;
}

.cancel-edit-btn:hover {
	background-color: var(--el-color-info-dark-2) !important;
	border-color: var(--el-color-info-dark-2) !important;
}

.action-buttons {
	display: flex;
	gap: 0.5rem;
	margin-top: 1rem;
}

/* 深色模式支持 */
.dark .new-question-editor {
	background-color: var(--primary-700);
	border-color: var(--primary-600);
}

.dark .editor-title {
	color: var(--primary-200);
}
</style>
