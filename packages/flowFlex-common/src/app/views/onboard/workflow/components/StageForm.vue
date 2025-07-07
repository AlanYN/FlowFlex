<template>
	<div class="stage-form">
		<el-form
			ref="formRef"
			:model="formData"
			:rules="rules"
			label-position="top"
			@submit.prevent="submitForm"
		>
			<el-form-item label="Stage Name" prop="name">
				<el-input v-model="formData.name" placeholder="Enter stage name" />
			</el-form-item>

			<el-form-item label="Description" prop="description">
				<el-input
					v-model="formData.description"
					type="textarea"
					:rows="3"
					placeholder="Enter stage description"
				/>
			</el-form-item>

			<el-form-item label="Assigned User Group" prop="defaultAssignedGroup">
				<el-select
					v-model="formData.defaultAssignedGroup"
					placeholder="Select user group"
					style="width: 100%"
				>
					<el-option
						v-for="item in defaultAssignedGroupOptions"
						:key="item.value"
						:label="item.key"
						:value="item.value"
					/>
				</el-select>
			</el-form-item>

			<el-form-item label="Default Assignee" prop="defaultAssignee">
				<el-input
					v-model="formData.defaultAssignee"
					placeholder="Enter default assignee role"
				/>
			</el-form-item>

			<el-form-item label="Estimated Duration" prop="estimatedDuration">
				<InputNumber
					v-model="formData.estimatedDuration as number"
					placeholder="e.g., 3 days"
				/>
			</el-form-item>

			<!-- Static Fields Section -->
			<el-form-item label="Static Fields" prop="staticFields">
				<div class="static-fields-container">
					<!-- Search Box -->
					<div class="search-container">
						<el-input
							v-model="searchQuery"
							placeholder="Search fields..."
							clearable
							class="search-input"
						>
							<template #prefix>
								<el-icon><Search /></el-icon>
							</template>
						</el-input>
					</div>

					<!-- Fields List -->
					<div class="fields-list">
						<div
							v-for="field in filteredStaticFields"
							:key="field.vIfKey"
							class="field-item"
						>
							<el-checkbox
								:model-value="isFieldSelected(field.vIfKey)"
								@change="(checked) => toggleField(field.vIfKey, checked)"
							>
								{{ field.label }}
							</el-checkbox>
						</div>
					</div>

					<!-- Selected Fields Display -->
					<div v-if="selectedFieldsDisplay.length > 0" class="selected-fields-display">
						<el-tag
							v-for="field in selectedFieldsDisplay"
							:key="field.vIfKey"
							closable
							@close="removeField(field.vIfKey)"
							class="selected-field-tag"
						>
							{{ field.label }}
						</el-tag>
					</div>

					<div class="help-text">
						Select the required fields for this stage from the system fields.
					</div>
				</div>
			</el-form-item>

			<el-form-item label="Stage Color" prop="color">
				<div class="color-picker-container">
					<div class="color-grid">
						<div
							v-for="color in colorOptions"
							:key="color"
							class="color-option"
							:class="{ selected: formData.color === color }"
							:style="{ backgroundColor: color }"
							@click="formData.color = color"
						></div>
					</div>
				</div>
			</el-form-item>

			<div class="form-actions">
				<el-button @click="$emit('cancel')">Cancel</el-button>
				<el-button
					type="primary"
					:loading="loading"
					native-type="submit"
					:disabled="!isFormValid"
				>
					{{ isEditing ? 'Update Stage' : 'Add Stage' }}
				</el-button>
			</div>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, PropType, computed } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { Search } from '@element-plus/icons-vue';
import InputNumber from '@/components/form/InputNumber/index.vue';
import { stageColorOptions, StageColorType } from '@/enums/stageColorEnum';
import { Options } from '#/setting';
import staticFieldsData from '../static-field.json';

// 接口定义
interface Stage {
	id: string;
	name: string;
	description?: string;
	defaultAssignedGroup: string;
	defaultAssignee: string;
	estimatedDuration: number;
	requiredFieldsJson: string;
	staticFields?: string[];
	order: number;
	color?: StageColorType;
}

interface StaticField {
	label: string;
	vIfKey: string;
	formProp: string;
}

// 颜色选项
const colorOptions = stageColorOptions;

// Props
const props = defineProps({
	stage: {
		type: Object as PropType<Stage | null>,
		default: null,
	},
	isEditing: {
		type: Boolean,
		default: false,
	},
	loading: {
		type: Boolean,
		default: false,
	},
});

const defaultAssignedGroupOptions = ref<Options[]>([
	{
		value: 'Sales',
		key: 'Sales',
	},
	{
		value: 'Account Management',
		key: 'Account Management',
	},
	{
		value: 'IT',
		key: 'IT',
	},
	{
		value: 'Legal',
		key: 'Legal',
	},
	{
		value: 'Operations',
		key: 'Operations',
	},
	{
		value: 'Finance',
		key: 'Finance',
	},
	{
		value: 'Customer',
		key: 'Customer',
	},
]);

// Static Fields 相关数据
const searchQuery = ref('');
const staticFields = ref<StaticField[]>(staticFieldsData.formFields);

// 表单数据
const formData = reactive({
	name: '',
	description: '',
	defaultAssignedGroup: '',
	defaultAssignee: '',
	estimatedDuration: null as number | null,
	requiredFieldsJson: '',
	staticFields: [] as string[],
	color: colorOptions[Math.floor(Math.random() * colorOptions.length)] as StageColorType, // 随机默认颜色
	order: 0,
});

// 表单验证规则
const rules = reactive<FormRules>({
	name: [
		{ required: true, message: 'Please enter stage name', trigger: 'blur' },
		{ min: 1, max: 50, message: 'Length should be 1 to 50 characters', trigger: 'blur' },
	],
	defaultAssignedGroup: [
		{ required: true, message: 'Please select user group', trigger: 'change' },
	],
	estimatedDuration: [
		{ required: true, message: 'Please enter estimated duration', trigger: 'change' },
	],
});

// 计算属性
const isFormValid = computed(() => {
	return !!formData.name && !!formData.defaultAssignedGroup && !!`${formData.estimatedDuration}`;
});

// 过滤的静态字段
const filteredStaticFields = computed(() => {
	if (!searchQuery.value) {
		return staticFields.value;
	}
	return staticFields.value.filter((field) =>
		field.label.toLowerCase().includes(searchQuery.value.toLowerCase())
	);
});

// 已选中字段的显示信息
const selectedFieldsDisplay = computed(() => {
	return staticFields.value.filter((field) => formData.staticFields.includes(field.vIfKey));
});

// 表单引用
const formRef = ref<FormInstance>();

// Static Fields 相关方法
const isFieldSelected = (fieldKey: string): boolean => {
	return formData.staticFields.includes(fieldKey);
};

const toggleField = (fieldKey: string, checked: boolean) => {
	if (checked) {
		if (!formData.staticFields.includes(fieldKey)) {
			formData.staticFields.push(fieldKey);
		}
	} else {
		const index = formData.staticFields.indexOf(fieldKey);
		if (index > -1) {
			formData.staticFields.splice(index, 1);
		}
	}
};

const removeField = (fieldKey: string) => {
	const index = formData.staticFields.indexOf(fieldKey);
	if (index > -1) {
		formData.staticFields.splice(index, 1);
	}
};

// 初始化表单数据
onMounted(() => {
	if (props.stage) {
		Object.keys(formData).forEach((key) => {
			if (key === 'color') {
				formData[key] =
					props.stage && props.stage?.color
						? (props.stage[key] as StageColorType)
						: (colorOptions[
								Math.floor(Math.random() * colorOptions.length)
						  ] as StageColorType);
			} else if (key === 'staticFields') {
				// 初始化 staticFields
				formData[key] = props.stage?.staticFields || [];
			} else {
				formData[key] = props.stage ? props.stage[key] : '';
			}
		});
	} else {
		// 为新阶段选择一个随机颜色
		formData.color = colorOptions[
			Math.floor(Math.random() * colorOptions.length)
		] as StageColorType;
		formData.staticFields = [];
	}
});

// 提交表单
const submitForm = async () => {
	if (!formRef.value) return;

	await formRef.value.validate((valid, fields) => {
		if (valid) {
			emit('submit', { ...formData });
		}
	});
};

// 定义事件
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped>
.stage-form {
	padding: 10px;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	margin-top: 20px;
}

.help-text {
	font-size: 12px;
	color: #909399;
	margin-top: 8px;
	font-style: italic;
}

.color-picker-container {
	width: 100%;
}

.color-grid {
	display: grid;
	grid-template-columns: repeat(6, 1fr);
	gap: 8px;
	margin-top: 4px;
}

.color-option {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	cursor: pointer;
	transition: all 0.2s;
	border: 2px solid transparent;
}

.color-option:hover {
	transform: scale(1.1);
}

.color-option.selected {
	border-color: #333;
	transform: scale(1.1);
}

.disabled-btn {
	opacity: 0.6;
	cursor: not-allowed;
}

/* Static Fields 样式 */
.static-fields-container {
	width: 100%;
	border: 1px solid #e4e7ed;
	border-radius: 6px;
	padding: 16px;
	background-color: #fafafa;
}

.search-container {
	margin-bottom: 16px;
}

.search-input {
	width: 100%;
}

.fields-list {
	max-height: 300px;
	overflow-y: auto;
	border: 1px solid #e4e7ed;
	border-radius: 4px;
	background-color: white;
	padding: 8px;
}

.field-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 8px 12px;
	border-radius: 4px;
	transition: background-color 0.2s;
}

.field-item:hover {
	background-color: #f5f7fa;
}

.field-category {
	font-size: 12px;
	color: #909399;
	font-style: italic;
}

:deep(.el-checkbox) {
	flex: 1;
}

:deep(.el-checkbox__label) {
	font-size: 14px;
	color: #606266;
}

/* Selected Fields Display */
.selected-fields-display {
	margin-top: 12px;
	padding-top: 12px;
	border-top: 1px solid #e4e7ed;
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.selected-field-tag {
	margin: 0;
	background-color: #ecf5ff;
	border-color: #b3d8ff;
	color: #409eff;
}

.selected-field-tag:hover {
	background-color: #d9ecff;
}
</style>
