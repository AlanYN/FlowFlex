<template>
	<div class="new-workflow-form">
		<el-form
			ref="formRef"
			:model="formData"
			:rules="rules"
			label-position="top"
			@submit.prevent="submitForm"
		>
			<el-form-item label="Workflow Name" prop="name">
				<el-input v-model="formData.name" placeholder="Enter workflow name" />
			</el-form-item>

			<el-form-item label="Description" prop="description">
				<el-input
					v-model="formData.description"
					type="textarea"
					placeholder="Enter workflow description"
					:rows="3"
				/>
			</el-form-item>

			<div class="date-fields">
				<el-form-item label="Start Date" prop="startDate" class="date-field">
					<el-date-picker
						v-model="formData.startDate"
						:default-value="getTimeZoneOffsetForTimezone()"
						:value-format="projectDate"
						type="date"
						placeholder="Select start date"
						style="width: 100%"
						clearable
					/>
				</el-form-item>

				<el-form-item label="End Date (Optional)" prop="endDate" class="date-field">
					<el-date-picker
						v-model="formData.endDate"
						:default-value="getTimeZoneOffsetForTimezone()"
						:value-format="projectDate"
						type="date"
						placeholder="Select end date"
						style="width: 100%"
						clearable
						:disabled="formData.isDefault"
						:disabled-date="disabledEndDate"
					/>
				</el-form-item>
			</div>

			<el-form-item label="Set as active workflow" class="switch-group-item">
				<div class="switch-container">
					<el-switch
						v-model="isActiveSwitch"
						class="ml-2"
						inline-prompt
						style="--el-switch-on-color: #13ce66; --el-switch-off-color: #ff4949"
						active-text="Active"
						inactive-text="Inactive"
					/>
				</div>
			</el-form-item>

			<el-form-item label="Set as default workflow" class="switch-group-item">
				<div class="switch-container">
					<el-switch
						v-model="formData.isDefault"
						class="ml-2"
						inline-prompt
						style="--el-switch-on-color: #13ce66; --el-switch-off-color: #ff4949"
						active-text="Default"
						inactive-text="Not Default"
						:disabled="isDefaultDisabled"
					/>
				</div>
			</el-form-item>

			<div class="form-actions">
				<el-button @click="$emit('cancel')">Cancel</el-button>
				<el-button
					type="primary"
					native-type="submit"
					:loading="loading"
					:disabled="!isFormValid || loading"
				>
					{{ isEditing ? 'Update Workflow' : 'Create Workflow' }}
				</el-button>
			</div>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { getTimeZoneOffsetForTimezone, timeZoneConvert } from '@/hooks/time';
import { projectDate } from '@/settings/projectSetting';
import { getWorkflowList } from '@/apis/ow';

// 定义 props
interface Props {
	initialData?: {
		[key: string]: any;
	};
	isEditing?: boolean;
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	initialData: () => ({}),
	isEditing: false,
	loading: false,
});

// 表单数据
const formData = reactive({
	name: '',
	description: '',
	startDate: '',
	endDate: '',
	status: 'active' as 'active' | 'inactive',
	isDefault: false, // 改为 false，让后面的逻辑来设置
});

// 开关状态计算属性
const isActiveSwitch = computed({
	get: () => formData.status === 'active',
	set: (value: boolean) => {
		formData.status = value ? 'active' : 'inactive';
	},
});

// 监听初始数据变化，用于编辑模式
watch(
	() => props.initialData,
	(newData) => {
		if (newData) {
			formData.name = newData.name || '';
			formData.description = newData.description || '';
			formData.startDate = timeZoneConvert(newData?.startDate || '');
			formData.endDate = timeZoneConvert(newData?.endDate || '');
			formData.status = newData.status || 'active';
			formData.isDefault = Object.keys(newData).includes('isDefault')
				? !!newData.isDefault
				: false; // 编辑模式下不自动设为默认
		} else if (!props.isEditing) {
			// 如果不是编辑模式且没有初始数据，检查默认值
			checkAndSetDefaultValue();
		}
	},
	{ immediate: true, deep: true }
);

// 监听 isDefault 的变化
watch(
	() => formData.isDefault,
	(newValue) => {
		if (newValue) {
			formData.endDate = '';
		}
	}
);

// 监听 status 的变化
watch(
	() => formData.status,
	(newValue) => {
		// 当状态设置为 inactive 时，自动设置为非默认工作流
		if (newValue === 'inactive') {
			formData.isDefault = false;
		}
	}
);

// 检查系统中是否已有默认工作流
const checkAndSetDefaultValue = async () => {
	try {
		// 只在非编辑模式下执行此逻辑
		if (props.isEditing) {
			return;
		}

		const res = await getWorkflowList();
		if (res.code === '200' && res.data) {
			const hasDefaultWorkflow = res.data.some((workflow: any) => workflow.isDefault);
			// 如果系统中没有默认工作流，新工作流设为默认
			// 如果系统中已有默认工作流，新工作流设为非默认
			formData.isDefault = !hasDefaultWorkflow;
		} else {
			// 如果无法获取工作流列表，默认设为 true（假设系统为空）
			formData.isDefault = true;
		}
	} catch (error) {
		console.warn('Failed to check default workflow:', error);
		// 出错时默认设为 true
		formData.isDefault = true;
	}
};

// 组件挂载时检查默认值（仅在非编辑模式下）
onMounted(() => {
	if (!props.isEditing && !props.initialData) {
		checkAndSetDefaultValue();
	}
});

// 表单验证规则
const rules = reactive<FormRules>({
	name: [
		{ required: true, message: 'Please enter workflow name', trigger: 'blur' },
		{ min: 1, max: 50, message: 'Length should be 1 to 50 characters', trigger: 'blur' },
	],
	description: [
		{ required: false, message: 'Please enter workflow description', trigger: 'blur' },
	],
	startDate: [{ required: true, message: 'Please select start date', trigger: 'change' }],
	endDate: [
		{
			validator: (rule: any, value: string, callback: Function) => {
				if (value && formData.startDate && new Date(value) < new Date(formData.startDate)) {
					callback(new Error('End date cannot be earlier than start date'));
				} else {
					callback();
				}
			},
			trigger: 'change',
		},
	],
});

// 表单引用
const formRef = ref<FormInstance>();

// 计算表单是否有效
const isFormValid = computed(() => {
	return !!formData.name && !!formData.startDate;
});

// 计算是否禁用默认工作流选项
const isDefaultDisabled = computed(() => {
	return formData.status === 'inactive';
});

// 禁用结束日期的函数 - 结束日期不能早于开始日期
const disabledEndDate = (time: Date) => {
	if (!formData.startDate) {
		return false;
	}
	const startDate = new Date(formData.startDate);
	return time < startDate;
};

// 提交表单
const submitForm = async () => {
	if (!formRef.value) return;

	await formRef.value.validate((valid, fields) => {
		if (valid) {
			emit('submit', {
				...formData,
				startDate: timeZoneConvert(formData?.startDate || '', true),
				endDate: timeZoneConvert(formData?.endDate || '', true),
			});
		}
	});
};

// 定义事件
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped>
.new-workflow-form {
	padding: 0;
	margin-top: 5px;
}

.date-fields {
	display: flex;
	gap: 16px;
}

.date-field {
	flex: 1;
}

.switch-group-item {
	margin-top: 12px;
}

.switch-container {
	display: flex;
	align-items: center;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	margin-top: 20px;
	padding-right: 10px;
}

:deep(.el-form-item) {
	margin-bottom: 16px;
}

:deep(.el-form-item__label) {
	font-weight: 500;
	margin-bottom: 4px;
	color: #303133;
	line-height: 1.4;
	padding-bottom: 0;
}

:deep(.el-input__wrapper),
:deep(.el-textarea__wrapper) {
	border-radius: 4px;
	box-shadow: 0 0 0 1px #dcdfe6 inset;
}

:deep(.el-input__wrapper:hover),
:deep(.el-textarea__wrapper:hover) {
	box-shadow: 0 0 0 1px var(--primary-400, #4989f5) inset;
}

:deep(.el-input__wrapper.is-focus),
:deep(.el-textarea__wrapper.is-focus) {
	box-shadow: 0 0 0 1px var(--primary-500, #2468f2) inset;
}

:deep(.el-button--primary) {
	background-color: var(--primary-500, #2468f2);
	border-color: var(--primary-500, #2468f2);
}

:deep(.el-button--primary:hover) {
	background-color: var(--primary-600, #1d5ad8);
	border-color: var(--primary-600, #1d5ad8);
}

:deep(.el-button--primary.is-disabled),
:deep(.el-button--primary.is-disabled:hover) {
	background-color: #a0cfff;
	border-color: #a0cfff;
	color: #fff;
	cursor: not-allowed;
	opacity: 0.6;
}
</style>
