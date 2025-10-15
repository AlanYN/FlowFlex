<template>
	<div class="new-workflow-form p-1">
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

			<el-form-item label="Set as active workflow" class="switch-group-item">
				<div class="switch-container">
					<el-switch
						v-model="isActiveSwitch"
						class="ml-2"
						inline-prompt
						style="
							--el-switch-on-color: var(--el-color-success);
							--el-switch-off-color: var(--el-color-danger);
						"
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
						style="
							--el-switch-on-color: var(--el-color-success);
							--el-switch-off-color: var(--el-color-danger);
						"
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
					:disabled="loading"
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
	status: 'active' as 'active' | 'inactive',
	isDefault: false, // 初始值为 false，由后续逻辑决定
});

// 开关状态计算属性
const isActiveSwitch = computed({
	get: () => formData.status === 'active',
	set: (value: boolean) => {
		formData.status = value ? 'active' : 'inactive';
	},
});

// 检查系统中是否已有默认工作流
const checkAndSetDefaultValue = async () => {
	try {
		// 只在非编辑模式下执行此逻辑
		if (props.isEditing) {
			console.log('编辑模式，跳过默认值检查');
			return;
		}

		console.log('开始检查系统中的默认工作流...');
		const res = await getWorkflowList();

		if (res.code === '200' && res.data) {
			const workflowCount = res.data.length;
			const hasDefaultWorkflow = res.data.some((workflow: any) => workflow.isDefault);

			// 如果系统为空（没有任何工作流），设为默认
			if (workflowCount === 0) {
				formData.isDefault = true;
				console.log('系统为空，设置新工作流为默认');
			} else if (!hasDefaultWorkflow) {
				// 如果系统中没有默认工作流，新工作流自动设为默认
				formData.isDefault = true;
				console.log('系统中没有默认工作流，设置新工作流为默认');
			} else {
				// 如果系统中已有默认工作流，新工作流设为非默认
				formData.isDefault = false;
				console.log('系统中已有默认工作流，新工作流设为非默认');
			}
		} else {
			// 如果无法获取工作流列表，默认设为 true（假设系统为空）
			formData.isDefault = true;
			console.log('无法获取工作流列表，设置为默认');
		}
	} catch (error) {
		console.warn('检查默认工作流失败，设置为默认:', error);
		// 出错时默认设为 true，确保至少有一个默认工作流
		formData.isDefault = true;
	}
};

// 监听初始数据变化，用于编辑模式
watch(
	() => props.initialData,
	(newData) => {
		if (newData && Object.keys(newData).length > 0) {
			// 编辑模式：有初始数据时使用初始数据
			formData.name = newData.name || '';
			formData.description = newData.description || '';

			formData.status = newData.status || 'active';
			formData.isDefault = Object.keys(newData).includes('isDefault')
				? !!newData.isDefault
				: false; // 编辑模式下不自动设为默认
		} else if (!props.isEditing) {
			// 创建模式：没有初始数据时检查默认值
			checkAndSetDefaultValue();
		}
	},
	{ immediate: true, deep: true }
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

// 组件挂载时检查默认值（仅在非编辑模式下）
onMounted(async () => {
	// 创建模式下，无论是否有初始数据都要检查默认值
	if (!props.isEditing) {
		await checkAndSetDefaultValue();
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
});

// 表单引用
const formRef = ref<FormInstance>();

// 计算是否禁用默认工作流选项
const isDefaultDisabled = computed(() => {
	return formData.status === 'inactive';
});

// 提交表单
const submitForm = async () => {
	if (!formRef.value) return;

	await formRef.value.validate((valid, fields) => {
		if (valid) {
			emit('submit', {
				...formData,
			});
		}
	});
};

// 定义事件
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped lang="scss">
.new-workflow-form {
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
	color: var(--el-text-color-primary);
	line-height: 1.4;
	padding-bottom: 0;
}

:deep(.el-input__wrapper),
:deep(.el-textarea__wrapper) {
	box-shadow: 0 0 0 1px var(--el-border-color) inset;
	@apply rounded-xl;
}

:deep(.el-input__wrapper:hover),
:deep(.el-textarea__wrapper:hover) {
	box-shadow: 0 0 0 1px var(--el-color-primary-light-5) inset;
}

:deep(.el-input__wrapper.is-focus),
:deep(.el-textarea__wrapper.is-focus) {
	box-shadow: 0 0 0 1px var(--el-color-primary) inset;
}

:deep(.el-button--primary) {
	background-color: var(--el-color-primary);
	border-color: var(--el-color-primary);
}

:deep(.el-button--primary:hover) {
	background-color: var(--el-color-primary-dark-2);
	border-color: var(--el-color-primary-dark-2);
}

:deep(.el-button--primary.is-disabled),
:deep(.el-button--primary.is-disabled:hover) {
	background-color: var(--el-color-primary-light-5);
	border-color: var(--el-color-primary-light-5);
	color: var(--el-color-white);
	cursor: not-allowed;
	opacity: 0.6;
}
</style>
