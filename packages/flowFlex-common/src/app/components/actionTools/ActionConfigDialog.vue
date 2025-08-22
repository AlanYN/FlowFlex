<template>
	<!-- External toggle button for variables panel -->
	<div class="action-config-drawer">
		<Teleport to="body">
			<div v-if="visible" class="variables-toggle-external" @click="showVariablesPanel">
				<div class="external-toggle-button">
					<icon icon="tabler:variable-plus" class="text-primary-500" />
					<div class="variables-toggle-text">Available Variables</div>
				</div>
			</div>
		</Teleport>

		<el-drawer
			v-model="visible"
			:title="dialogTitle"
			:size="drawerSize"
			direction="rtl"
			@close="onCancel"
		>
			<el-scrollbar class="action-config-scrollbar">
				<div class="flex gap-4 w-full h-full min-h-0">
					<div v-if="leftPanelVisible" class="flex-1 min-w-0 min-h-0 flex flex-col">
						<el-scrollbar ref="scrollbarRefLeft" class="h-full">
							<VariablesPanel
								:stage-id="triggerSourceId"
								:action-type="formData.type"
							/>
						</el-scrollbar>
					</div>

					<div
						class="action-config-container pr-4 flex-1 min-w-0 min-h-0 flex flex-col"
						v-loading="loading"
					>
						<el-scrollbar ref="scrollbarRefRight" class="h-full">
							<el-form
								ref="formRef"
								:model="formData"
								:rules="rules"
								label-position="top"
								label-width="120px"
							>
								<!-- Basic Info -->
								<el-form-item label="Action Name" prop="name">
									<el-input
										v-model="formData.name"
										placeholder="Enter action name"
									/>
								</el-form-item>

								<el-form-item label="Description" prop="description">
									<el-input
										v-model="formData.description"
										type="textarea"
										:rows="3"
										placeholder="Enter action description"
									/>
								</el-form-item>

								<el-form-item label="Action Type" prop="type">
									<el-radio-group
										v-model="formData.type"
										@change="handleActionTypeChange"
									>
										<el-radio
											v-for="type in actionTypes"
											:key="type.value"
											:value="type.value"
											class="action-type-option"
											:disabled="isEditing"
										>
											<div class="flex items-center space-x-3">
												<div
													class="flex items-center justify-center w-10 h-10 rounded-lg bg-gray-100 dark:bg-gray-700"
												>
													<el-icon class="text-primary-500" size="20">
														<component :is="type.icon" />
													</el-icon>
												</div>
												<div class="flex">
													<span
														class="font-medium text-gray-900 dark:text-white"
													>
														{{ type.label }}
													</span>
												</div>
											</div>
										</el-radio>
									</el-radio-group>
								</el-form-item>

								<!-- Action Configuration -->
								<div v-if="formData.type" class="action-config-section">
									<!-- Python Script Configuration -->
									<PythonConfig
										v-if="formData.type === 'python'"
										v-model="formData.actionConfig"
										@test="onTest"
										:testing="testing"
										:test-result="testResult"
										ref="pythonConfigRef"
										:id-editing="isEditing"
									/>

									<!-- HTTP API Configuration -->
									<HttpConfig
										v-else-if="formData.type === 'http'"
										v-model="formData.actionConfig"
										@test="onTest"
										:testing="testing"
										:test-result="testResult"
										ref="httpConfigRef"
										:id-editing="isEditing"
									/>
								</div>
							</el-form>
						</el-scrollbar>
					</div>
				</div>
			</el-scrollbar>

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="onCancel">Cancel</el-button>
					<el-button type="primary" @click="onSave" :loading="saving">
						{{ isEditing ? 'Update' : 'Add' }} Action
					</el-button>
				</div>
			</template>
		</el-drawer>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
import { Operation, Connection } from '@element-plus/icons-vue';
import PythonConfig from './PythonConfig.vue';
import HttpConfig from './HttpConfig.vue';
import VariablesPanel from './VariablesPanel.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';

import { addAction, ActionType, updateAction, testRunActionNoId } from '@/apis/action';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { ActionItem } from '#/action';

const { scrollbarRef: scrollbarRefLeft, updateScrollbarHeight: updateScrollbarHeightLeft } =
	useAdaptiveScrollbar(80);

const { scrollbarRef: scrollbarRefRight, updateScrollbarHeight: updateScrollbarHeightRight } =
	useAdaptiveScrollbar(80);

interface Props {
	modelValue?: boolean;
	action?: ActionItem | null;
	isEditing?: boolean;
	triggerSourceId?: string;
	workflowId?: string;
	loading?: boolean;
	triggerType?: TriggerTypeEnum;
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: false,
	action: null,
	isEditing: false,
	triggerSourceId: '',
	workflowId: '',
	triggerType: TriggerTypeEnum.Stage,
});

const emit = defineEmits(['update:modelValue', 'saveSuccess', 'cancel']);

// Form data
const formRef = ref();
const saving = ref(false);
const testing = ref(false);
const testResult = ref<any>(null);
const pythonConfigRef = ref(); // For getting Python config component reference
const httpConfigRef = ref(); // For getting HTTP config component reference
const leftPanelVisible = ref(false); // Controls the visibility of the left variables panel

const formData = reactive<ActionItem>({
	id: '',
	name: '',
	type: 'python',
	description: '',
	actionConfig: {},
});

// Computed
const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const dialogTitle = computed(() => {
	return props.isEditing ? 'Edit Action' : 'Add New Action';
});

const drawerSize = computed(() => {
	return leftPanelVisible.value ? '80%' : '40%';
});

const buttonLeftPosition = computed(() => {
	const drawerWidth = leftPanelVisible.value ? 0.8 : 0.4;
	return `calc(100vw - ${drawerWidth * 100}vw - 30px)`;
});

const showVariablesPanel = () => {
	leftPanelVisible.value = !leftPanelVisible.value;
	nextTick(() => {
		updateScrollbarHeightLeft();
	});
};

// Action Types
const actionTypes = [
	{
		label: 'Python Script',
		value: 'python',
		icon: Operation,
		description: 'Execute custom Python code when stage completes',
	},
	{
		label: 'HTTP API',
		value: 'http',
		icon: Connection,
		description: 'Send HTTP request to external API endpoint',
	},
];

// Form Rules
const rules = {
	name: [{ required: true, message: 'Please enter action name', trigger: 'blur' }],
	type: [{ required: true, message: 'Please select action type', trigger: 'change' }],
};

const getDefaultConfig = (type: string) => {
	if (type === 'python') {
		return {
			sourceCode: `def main(customer_name: str):
    return {
        "greeting": f"Hello, {customer_name}!",
    }`,
		};
	} else if (type === 'http') {
		return {
			url: '',
			method: 'GET',
			headers: {
				'Content-Type': 'application/json',
			},
			body: '',
			timeout: 30,
			followRedirects: true,
		};
	}
	return {};
};

// Methods
const resetForm = () => {
	formData.id = '';
	formData.name = '';
	formData.type = 'python';
	formData.description = '';
	formData.actionConfig = getDefaultConfig('python');
	testResult.value = null;
};

// Watch for action prop changes
watch(
	() => props.action,
	(newAction) => {
		if (newAction) {
			Object.assign(formData, { ...newAction });
		} else {
			resetForm();
		}
	},
	{ immediate: true, deep: true }
);

const handleActionTypeChange = (type: string) => {
	formData.actionConfig = getDefaultConfig(type);
};

// Handle test result - 参考 detail.vue 的 handleTestResult 逻辑
const onTest = async () => {
	// Force get current config values from components

	try {
		testing.value = true;
		testResult.value = null;
		// Execute test
		const testOutput = await testRunActionNoId({
			actionType: formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
			actionConfig: JSON.stringify(formData.actionConfig),
		});

		if (testOutput.code == '200') {
			testResult.value = testOutput.data;
		}
	} finally {
		testing.value = false;
	}
};

const onSave = async () => {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		saving.value = true;

		// Validate based on action type
		if (formData.type === 'python' && !formData.actionConfig.sourceCode) {
			ElMessage.error('Please enter Python script code');
			return;
		}

		if (formData.type === 'http' && !formData.actionConfig.url) {
			ElMessage.error('Please enter HTTP API URL');
			return;
		}

		// 根据 action type 准备不同的 actionConfig
		let cleanActionConfig: any = {};

		if (formData.type === 'python') {
			// Python 类型只需要 sourceCode
			cleanActionConfig = {
				sourceCode: formData.actionConfig.sourceCode,
			};
		} else if (formData.type === 'http') {
			// HTTP 类型需要符合 HttpApiConfigDto 的字段
			cleanActionConfig = {
				...formData.actionConfig,
				url: formData.actionConfig.url || '',
				method: formData.actionConfig.method || 'GET',
				headers: formData.actionConfig.headers || {},
				params: formData.actionConfig.params || {},
				body: formData.actionConfig.body || '',
				timeout: formData.actionConfig.timeout || 30,
				followRedirects: formData.actionConfig.followRedirects !== false, // 默认为 true
			};
		}

		const params = {
			...formData,
			actionConfig: JSON.stringify(cleanActionConfig),
			workflowId: props?.workflowId || null,
			actionType: formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
			triggerSourceId: props?.triggerSourceId || null,
			triggerType: props?.triggerType || null,
		};

		const res: any = formData.id
			? await updateAction(formData.id, params)
			: await addAction(params);
		if (res.code == '200') {
			ElMessage.success('Action added successfully');
			emit('saveSuccess', res.data);
			visible.value = false;
		} else {
			res?.msg && ElMessage.error(res?.msg);
		}
	} finally {
		saving.value = false;
	}
};

const onCancel = () => {
	visible.value = false;
	resetForm();
	emit('cancel');
};

const resetScrollbarHeight = () => {
	nextTick(() => {
		updateScrollbarHeightLeft();
		updateScrollbarHeightRight();
	});
};

defineExpose({
	resetScrollbarHeight,
});
</script>

<style scoped lang="scss">
.action-config-scrollbar {
	@apply h-full;
	max-height: calc(100vh - 120px);

	:deep(.el-scrollbar__view) {
		display: flex;
		flex-direction: column;
		min-height: 0;
	}

	:deep(.el-scrollbar__wrap) {
		/* avoid nested scrollbars fighting */
		max-height: 100%;
	}

	:deep(.el-scrollbar__bar.is-vertical > div) {
		@apply bg-gray-300 dark:bg-gray-600 opacity-80;
		border-radius: 4px;
		width: 6px;
	}

	:deep(.el-scrollbar__bar.is-vertical) {
		@apply opacity-80;
		width: 8px;
		right: 2px;
	}

	:deep(.el-scrollbar__bar.is-vertical:hover > div) {
		@apply bg-gray-400 dark:bg-gray-500;
	}
}

.action-config-container {
	@apply bg-white dark:bg-gray-800 min-h-full;
}

.section-header {
	@apply border-b border-gray-200 dark:border-gray-700 pb-3 mb-4;
}

.action-config-section {
	@apply space-y-4;
}

// 抽屉footer样式
.dialog-footer {
	@apply flex justify-end space-x-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800;
	position: sticky;
	bottom: 0;
	margin-top: auto;
}

.variables-toggle-external {
	position: fixed;
	left: v-bind(buttonLeftPosition);
	top: 10vh;
	transform: translateY(-50%);
	z-index: 9999;
	transition: left 0.3s ease;
	animation: delayedFadeIn 0.6s ease-out;
}

@keyframes delayedFadeIn {
	0%,
	50% {
		opacity: 0;
		transform: translateY(-50%) translateX(20px);
	}
	100% {
		opacity: 1;
		transform: translateY(-50%) translateX(0);
	}
}

.external-toggle-button {
	@apply border-l-2 border-primary-500 rounded-s-md cursor-pointer transition-all duration-300 bg-white flex flex-col items-center justify-center gap-2;
	width: 30px;
	height: 200px;

	&:hover {
		background-color: #f8fafc;
		border-color: #3b82f6;
		transform: scale(1.05);
		box-shadow: 0 6px 16px rgba(59, 130, 246, 0.3);
	}

	.dark & {
		background-color: #374151;
		border-color: #4b5563;

		&:hover {
			background-color: #4b5563;
			border-color: #3b82f6;
		}
	}

	.variables-toggle-text {
		writing-mode: vertical-rl;
	}
}

.variables-panel-container {
	@apply flex-1 min-w-0 min-h-0 flex flex-col bg-gray-50 dark:bg-gray-800;
	border-radius: 8px;
	border: 1px solid #e2e8f0;

	.dark & {
		border-color: #4b5563;
	}
}

.variables-panel-header {
	@apply flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800;
	border-top-left-radius: 8px;
	border-top-right-radius: 8px;
}

.action-config-drawer {
	:deep(.el-drawer__footer) {
		@apply p-0;
	}
}
</style>
