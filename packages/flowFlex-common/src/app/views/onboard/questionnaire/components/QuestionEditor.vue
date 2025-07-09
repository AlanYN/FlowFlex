<template>
	<div class="new-question-editor">
		<h4 class="editor-title">Add New Question</h4>
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
				:new-option="newOption"
				@update-option-value="(value) => (newOption.value = value)"
				@update-option-label="(label) => (newOption.label = label)"
				@add-option="handleAddOption"
				@remove-option="handleRemoveOption"
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

			<el-button
				type="primary"
				@click="handleAddQuestion"
				:disabled="!newQuestion.question || !newQuestion.type"
				:icon="Plus"
				class="add-question-btn"
			>
				Add Question
			</el-button>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import { Plus } from '@element-plus/icons-vue';
import OptionsEditor from './OptionsEditor.vue';
import GridEditor from './GridEditor.vue';
import LinearScaleEditor from './LinearScaleEditor.vue';

interface QuestionType {
	id: string;
	name: string;
	icon: string;
	description: string;
	isNew?: boolean;
}

interface Props {
	questionTypes: QuestionType[];
	pressentQuestionType: string;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'add-question': [question: any];
	'change-question-type': [type: string];
}>();

watch(
	() => props.pressentQuestionType,
	(newVal) => {
		newQuestion.type = newVal;
	}
);
// 新问题数据
const newQuestion = reactive({
	type: props.pressentQuestionType,
	question: '',
	description: '',
	required: true,
	options: [] as Array<{ id: string; value: string; label: string }>,
	rows: [] as Array<{ id: string; label: string }>,
	columns: [] as Array<{ id: string; label: string }>,
	requireOneResponsePerRow: false,
	// 线性量表相关字段
	min: 1,
	max: 5,
	minLabel: '',
	maxLabel: '',
});

// 新选项数据
const newOption = reactive({
	value: '',
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

// 处理问题类型改变
const handleQuestionTypeChange = (type: string) => {
	emits('change-question-type', type);
	newQuestion.type = type;
	// 清空选项和网格数据
	newQuestion.options = [];
	newQuestion.rows = [];
	newQuestion.columns = [];
	newQuestion.requireOneResponsePerRow = false;

	// 重置线性量表字段
	newQuestion.min = 1;
	newQuestion.max = 5;
	newQuestion.minLabel = '';
	newQuestion.maxLabel = '';

	// 清空输入框
	newOption.value = '';
	newOption.label = '';
	newRow.label = '';
	newColumn.label = '';
};

// 添加选项
const handleAddOption = () => {
	if (!newOption.value.trim() || !newOption.label.trim()) return;

	newQuestion.options.push({
		id: `option-${Date.now()}`,
		value: newOption.value,
		label: newOption.label,
	});

	newOption.value = '';
	newOption.label = '';
};

// 删除选项
const handleRemoveOption = (id: string) => {
	newQuestion.options = newQuestion.options.filter((option) => option.id !== id);
};

// 添加行
const handleAddRow = () => {
	if (!newRow.label.trim()) {
		return;
	}

	const newRowData = {
		id: `row-${Date.now()}`,
		label: newRow.label,
	};

	newQuestion.rows.push(newRowData);
	newRow.label = '';
};

// 删除行
const handleRemoveRow = (id: string) => {
	newQuestion.rows = newQuestion.rows.filter((row) => row.id !== id);
};

// 添加列
const handleAddColumn = () => {
	if (!newColumn.label.trim()) {
		return;
	}

	const newColumnData = {
		id: `column-${Date.now()}`,
		label: newColumn.label,
	};

	newQuestion.columns.push(newColumnData);
	newColumn.label = '';
};

// 删除列
const handleRemoveColumn = (id: string) => {
	newQuestion.columns = newQuestion.columns.filter((column) => column.id !== id);
};

// 添加问题
const handleAddQuestion = () => {
	if (!newQuestion.question.trim() || !newQuestion.type) return;

	const question = {
		id: `question-${Date.now()}`,
		...newQuestion,
		options: [...newQuestion.options],
		rows: [...newQuestion.rows],
		columns: [...newQuestion.columns],
	};

	// 发送完整的问题数据到父组件
	emits('add-question', question);

	// 重置新问题表单，但保持问题类型不变
	newQuestion.question = '';
	newQuestion.description = '';
	newQuestion.required = true;
	newQuestion.options = [];
	newQuestion.rows = [];
	newQuestion.columns = [];
	newQuestion.requireOneResponsePerRow = false;
	// 重置线性量表字段
	newQuestion.min = 1;
	newQuestion.max = 5;
	newQuestion.minLabel = '';
	newQuestion.maxLabel = '';
	// 不重置 newQuestion.type，保持当前选择的类型
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

const getQuestionTypeIcon = (type: string) => {
	const questionType = props.questionTypes.find((t) => t.id === type);
	return questionType ? questionType.icon : 'Document';
};
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
	width: 100%;
	margin-top: 1rem;
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
	color: white !important;
}

.add-question-btn:hover {
	background-color: var(--primary-700) !important;
	border-color: var(--primary-700) !important;
}

.add-question-btn:disabled {
	background-color: var(--primary-200) !important;
	border-color: var(--primary-200) !important;
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
