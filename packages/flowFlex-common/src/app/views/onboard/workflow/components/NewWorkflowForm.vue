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

			<el-form-item label="Set as active workflow" class="radio-group-item">
				<el-radio-group v-model="formData.status">
					<el-radio :value="'active'">Active</el-radio>
					<el-radio :value="'inactive'">Inactive</el-radio>
				</el-radio-group>
			</el-form-item>

			<el-form-item label="Set as default workflow" class="radio-group-item">
				<el-radio-group v-model="formData.isDefault" :disabled="isDefaultDisabled">
					<el-radio :value="true" :disabled="isDefaultDisabled">Default</el-radio>
					<el-radio :value="false">Not Default</el-radio>
				</el-radio-group>
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
import { ref, reactive, computed, watch } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { getTimeZoneOffsetForTimezone, timeZoneConvert } from '@/hooks/time';
import { projectDate } from '@/settings/projectSetting';

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
	isDefault: true,
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
				: true;
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

.radio-group-item {
	margin-top: 12px;
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
