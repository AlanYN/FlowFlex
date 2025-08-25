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
							<!-- 选择模式 - 位于表单最前方 -->
							<div
								v-if="shouldShowConfigMode"
								class="mode-selection-section mb-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border"
							>
								<div class="flex items-center gap-4">
									<span
										class="text-sm font-medium text-gray-700 dark:text-gray-300"
									>
										Configuration Mode:
									</span>
									<el-radio-group
										v-model="configMode"
										@change="handleConfigModeChange"
									>
										<el-radio value="useExisting" :disabled="props.isEditing">
											<span class="text-sm">Use Existing Tool</span>
										</el-radio>
										<el-radio value="createAction" :disabled="props.isEditing">
											<span class="text-sm">Create Regular Action</span>
										</el-radio>
									</el-radio-group>
								</div>

								<!-- 选择已有工具的下拉框 -->
								<div v-if="useExistingTool" class="mt-4">
									<label
										class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
									>
										Select Existing Tool
									</label>
									<el-select
										v-model="selectedToolId"
										placeholder="Please select an existing tool"
										clearable
										filterable
										:loading="loadingExistingTools"
										@change="handleExistingToolSelect"
										class="w-full"
									>
										<el-option
											v-for="tool in existingToolsList"
											:key="tool.id"
											:label="`${tool.name} (${tool.actionCode || tool.id})`"
											:value="tool.id"
										>
											<div class="flex justify-between items-center">
												<span>{{ tool.name }}</span>
												<el-tag size="small" type="info">
													{{ getActionTypeName(tool.actionType) }}
												</el-tag>
											</div>
										</el-option>
									</el-select>
								</div>
							</div>

							<el-form
								ref="formRef"
								:model="formData"
								:rules="rules"
								label-position="top"
								label-width="120px"
								class="p-1 pr-4"
							>
								<!-- Basic Info -->
								<el-form-item label="Action Name" prop="name">
									<el-input
										v-model="formData.name"
										placeholder="Enter action name"
										:disabled="shouldDisableFields"
									/>
								</el-form-item>

								<el-form-item label="Condition" prop="condition">
									<el-select
										v-model="formData.condition"
										placeholder="Enter action id"
										:disabled="shouldDisableFields"
									>
										<el-option
											label="Stage Completed"
											value="Stage Completed"
										/>
									</el-select>
								</el-form-item>

								<el-form-item label="Description" prop="description">
									<el-input
										v-model="formData.description"
										type="textarea"
										:rows="3"
										placeholder="Enter action description"
										:disabled="shouldDisableFields"
									/>
								</el-form-item>

								<el-form-item label="Action Type" prop="type">
									<el-radio-group
										v-model="formData.type"
										@change="handleActionTypeChange"
										:disabled="shouldDisableFields"
									>
										<el-radio
											v-for="type in actionTypes"
											:key="type.value"
											:value="type.value"
											class="action-type-option"
											:disabled="isEditing || shouldDisableFields"
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
										:disabled="shouldDisableFields"
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
										:disabled="shouldDisableFields"
									/>
								</div>

								<el-form-item prop="IsTools" v-if="!shouldDisableFields">
									<el-checkbox
										v-model="formData.isTools"
										label="Is Tools"
										:disabled="shouldDisableFields"
									/>
								</el-form-item>
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

import {
	addAction,
	ActionType,
	updateAction,
	testRunActionNoId,
	getActionDefinitions,
	getActionDetail,
	ACTION_TYPE_MAPPING,
	addMappingAction,
} from '@/apis/action';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { ActionItem, ActionDefinition, ActionQueryRequest } from '#/action';

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

// 配置模式状态
type ConfigMode = 'useExisting' | 'createTool' | 'createAction';
const configMode = ref<ConfigMode>('useExisting'); // 配置模式
const useExistingTool = computed(() => configMode.value === 'useExisting'); // 是否使用已有工具
const selectedToolId = ref(''); // 选中的工具 ID
const loadingExistingTools = ref(false); // 加载已有工具列表状态
const existingToolsList = ref<ActionDefinition[]>([]); // 已有工具列表

const formData = reactive<ActionItem>({
	id: '',
	name: '',
	type: 'python',
	description: '',
	condition: 'Stage Completed',
	isTools: true, // 新建时默认为 true（工具模式），允许用户选择
	actionConfig: {},
});

// Computed
const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

// 监听弹窗打开状态，自动加载已有工具列表
watch(visible, async (newVisible) => {
	if (newVisible && useExistingTool.value && existingToolsList.value.length === 0) {
		// 当弹窗打开且处于使用已有工具模式时，自动加载工具列表
		await loadExistingTools();
	}
});

const dialogTitle = computed(() => {
	return props.isEditing ? 'Edit Action' : 'Add New Action';
});

// 控制是否显示配置模式区域
const shouldShowConfigMode = computed(() => {
	// 新建时（没有传入 action），总是显示 Configuration Mode 让用户选择
	return !props.action;
});

// 计算是否应该禁用表单字段
const shouldDisableFields = computed(() => {
	// 编辑状态：根据 isTools 决定
	if (props.action) {
		return props.action.isTools === true;
	}

	// 新建状态：根据选择的配置模式决定
	if (configMode.value === 'createAction') {
		return false; // 创建普通 Action，允许编辑
	}
	if (configMode.value === 'createTool') {
		return false; // 创建新工具，允许编辑
	}
	if (configMode.value === 'useExisting') {
		return selectedToolId.value !== ''; // 选择了具体工具后才禁用
	}

	return false;
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
	condition: [{ required: true, message: 'Please select condition', trigger: 'change' }],
};

const getDefaultConfig = (type: string) => {
	if (type === 'python') {
		return {
			sourceCode: `def main(onboardingId: str):
    return {
        "greeting": f"{onboardingId}!",
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
	formData.isTools = true; // 新建时默认为工具模式
	visible.value = false;
	formData.actionConfig = getDefaultConfig('python');
	testResult.value = null;

	// 重置选择已有工具的状态
	selectedToolId.value = '';
	existingToolsList.value = [];

	// 重置配置模式为默认值
	configMode.value = 'useExisting';
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

// Action Type 名称映射方法
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};

// 处理模式变化
const handleConfigModeChange = async (mode: ConfigMode) => {
	// 清空当前选择
	selectedToolId.value = '';

	if (mode === 'useExisting') {
		// 使用已有工具：加载工具列表
		await loadExistingTools();
		formData.isTools = true;
		resetFormData();
	} else if (mode === 'createTool') {
		// 创建新工具：清空列表，设置为工具模式
		existingToolsList.value = [];
		formData.isTools = true;
		resetFormData();
	} else if (mode === 'createAction') {
		// 创建普通 Action：清空列表，设置为非工具模式
		existingToolsList.value = [];
		formData.isTools = false;
		resetFormData();
	}
};

// 加载已有工具列表
const loadExistingTools = async () => {
	try {
		loadingExistingTools.value = true;

		// 查询参数 - 只获取标记为工具的 action
		const params: ActionQueryRequest = {
			pageIndex: 1,
			pageSize: 1000, // 获取足够多的数据
			isTools: true,
		};

		const response = await getActionDefinitions(params);

		if (response.code === '200' && response.success) {
			// 过滤出标记为工具的 action
			existingToolsList.value =
				response.data.data?.filter((item: ActionDefinition) => {
					// 这里假设有一个字段标识是否为工具，如果没有则显示所有
					return true; // 临时显示所有，后续可根据实际字段调整
				}) || [];
		} else {
			ElMessage.error('Failed to load existing tools');
			existingToolsList.value = [];
		}
	} catch (error) {
		console.error('Failed to load existing tools:', error);
		ElMessage.error('Failed to load existing tools');
		existingToolsList.value = [];
	} finally {
		loadingExistingTools.value = false;
	}
};

// 处理选择已有工具
const handleExistingToolSelect = async (toolId: string) => {
	if (!toolId) {
		// 清空选择时重置表单
		resetFormData();
		return;
	}

	try {
		// 获取工具详情
		const response = await getActionDetail(toolId);

		if (response.code === '200' && response.data) {
			const toolDetail = response.data;

			// 填充表单数据（只读模式）
			formData.name = toolDetail.name || '';
			formData.description = toolDetail.description || '';
			formData.type = toolDetail.actionType === ActionType.PYTHON_SCRIPT ? 'python' : 'http';
			formData.actionConfig = JSON.parse(toolDetail.actionConfig || '{}');
			formData.id = toolDetail.id;
			formData.isTools = true;

			ElMessage.success('Tool details loaded successfully');
		} else {
			ElMessage.error('Failed to load tool details');
		}
	} catch (error) {
		console.error('Failed to load tool details:', error);
		ElMessage.error('Failed to load tool details');
	}
};

// 重置表单数据
const resetFormData = () => {
	formData.name = '';
	formData.description = '';
	formData.type = 'python';
	formData.actionConfig = getDefaultConfig('python');
};

// Handle test result - 参考 detail.vue 的 handleTestResult 逻辑
const onTest = async () => {
	// 如果字段被禁用（选择了已有工具），则不允许测试
	if (shouldDisableFields.value) {
		ElMessage.warning('Cannot test when using existing tool');
		return;
	}

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

		// 根据配置模式进行不同处理
		if (configMode.value === 'useExisting') {
			// 使用已有工具：创建映射关系
			if (!selectedToolId.value) {
				ElMessage.error('Please select an existing tool');
				return;
			}
			const params = {
				actionDefinitionId: selectedToolId.value,
				triggerSourceId: props?.triggerSourceId || null,
				triggerType: props?.triggerType || null,
				workFlowId: props?.workflowId || null,
			};
			const res = await addMappingAction(params);
			if (res.code == '200') {
				ElMessage.success('Action added successfully');
				emit('saveSuccess', {
					...formData,
					...res.data,
				});
				visible.value = false;
			} else {
				res?.msg && ElMessage.error(res?.msg);
			}
		} else {
			// 创建新工具或普通 Action：验证并保存
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
				actionType:
					formData.type === 'python' ? ActionType.PYTHON_SCRIPT : ActionType.HTTP_API,
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
	z-index: 4999;
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

.mode-selection-section {
	border-left: 4px solid #3b82f6;
	background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
	transition: all 0.3s ease;

	&:hover {
		background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	}

	.dark & {
		background: linear-gradient(135deg, #374151 0%, #4b5563 100%);
		border-left-color: #60a5fa;

		&:hover {
			background: linear-gradient(135deg, #4b5563 0%, #6b7280 100%);
			box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
		}
	}
}
</style>
