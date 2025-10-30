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
			@opened="opened"
		>
			<div class="flex gap-4 w-full h-full min-h-0">
				<div v-if="leftPanelVisible" class="flex-1 min-w-0 min-h-0 flex flex-col">
					<el-scrollbar ref="scrollbarRefLeft" class="h-full">
						<VariablesPanel
							:stage-id="triggerSourceId"
							:action-actionType="formData.actionType"
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
							v-if="!isConfigModeDisabled"
							class="mode-selection-section mb-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-xl border"
						>
							<div class="flex items-center gap-4">
								<span class="text-sm font-medium">Configuration Mode:</span>
								<el-radio-group
									v-model="configMode"
									@change="(value) => handleConfigModeChange(value as ToolsType)"
									:disabled="isConfigModeDisabled"
								>
									<el-radio
										:value="ToolsType.UseTool"
										:disabled="props.isEditing || isConfigModeDisabled"
									>
										<span class="text-sm">Use Tool</span>
									</el-radio>
									<el-radio
										:value="ToolsType.MyTool"
										:disabled="props.isEditing || isConfigModeDisabled"
									>
										<span class="text-sm">My Action</span>
									</el-radio>
									<el-radio
										:value="ToolsType.NewTool"
										:disabled="props.isEditing || isConfigModeDisabled"
									>
										<span class="text-sm">Create New Action</span>
									</el-radio>
									<el-radio
										:value="ToolsType.SystemTools"
										:disabled="props.isEditing || isConfigModeDisabled"
									>
										<span class="text-sm">System Tool</span>
									</el-radio>
								</el-radio-group>
							</div>

							<!-- 选择已有工具的下拉框 -->
							<div v-if="useExistingTool" class="mt-4">
								<label class="block text-sm font-medium mb-2">Select Tool</label>

								<el-select
									v-model="selectedToolId"
									placeholder="Please select an existing tool"
									clearable
									filterable
									:loading="loadingExistingTools"
									@change="handleExistingToolSelect"
									class="w-full"
									:disabled="isConfigModeDisabled"
								>
									<el-option
										v-for="tool in existingToolsList"
										:key="tool.id"
										:label="`${tool.name} (${tool.actionCode || tool.id})`"
										:value="tool.id || ''"
									>
										<div class="flex justify-between items-center">
											<span>
												{{ tool.name }}({{ tool?.actionCode || tool?.id }})
											</span>
											<el-tag actionType="info">
												{{ getActionTypeName(tool.actionType) }}
											</el-tag>
										</div>
									</el-option>
								</el-select>
							</div>
						</div>
						<el-form
							v-if="configMode !== ToolsType.SystemTools"
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
									<el-option label="Stage Completed" value="Stage Completed" />
								</el-select>
							</el-form-item>

							<el-form-item label="Description" prop="description">
								<el-input
									v-model="formData.description"
									actionType="textarea"
									:rows="3"
									placeholder="Enter action description"
									:disabled="shouldDisableFields"
								/>
							</el-form-item>

							<el-form-item label="Action Type" prop="actionType">
								<el-radio-group
									v-model="formData.actionType"
									@change="(value) => handleActionTypeChange(value as ActionType)"
									:disabled="shouldDisableFields"
								>
									<el-radio
										v-for="actionType in actionTypes"
										:key="actionType.value"
										:value="actionType.value"
										class="action-actionType-option"
										:disabled="isEditing || shouldDisableFields"
									>
										<div class="flex items-center space-x-3">
											<div
												class="flex items-center justify-center w-10 h-10 rounded-xl bg-primary"
											>
												<el-icon class="text-white" size="20">
													<component :is="actionType.icon" />
												</el-icon>
											</div>
											<div class="flex">
												<span
													class="font-medium text-gray-900 dark:text-white"
												>
													{{ actionType.label }}
												</span>
											</div>
										</div>
									</el-radio>
								</el-radio-group>
							</el-form-item>

							<!-- Action Configuration -->
							<div v-if="formData.actionType" class="action-config-section">
								<!-- Python Script Configuration -->
								<PythonConfig
									v-if="formData.actionType === ActionType.PYTHON_SCRIPT"
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
									v-else-if="formData.actionType === ActionType.HTTP_API"
									v-model="formData.actionConfig"
									@test="onTest"
									@update:action-name="updateActionName"
									@ai-config-applied="handleAiConfigApplied"
									:testing="testing"
									:test-result="testResult"
									ref="httpConfigRef"
									:id-editing="isEditing"
									:disabled="shouldDisableFields"
								/>
							</div>

							<!-- <el-form-item prop="IsTools" v-if="!shouldDisableFields">
									<el-checkbox
										v-model="formData.isTools"
										label="Is Tool"
										:disabled="shouldDisableFields"
									/>
								</el-form-item> -->
						</el-form>
					</el-scrollbar>
				</div>
			</div>

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
import { ElMessage, useZIndex } from 'element-plus';
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
import { TriggerTypeEnum, ToolsType } from '@/enums/appEnum';
import { ActionItem, ActionDefinition, ActionQueryRequest } from '#/action';

const { scrollbarRef: scrollbarRefLeft, updateScrollbarHeight: updateScrollbarHeightLeft } =
	useAdaptiveScrollbar(80);

const { scrollbarRef: scrollbarRefRight, updateScrollbarHeight: updateScrollbarHeightRight } =
	useAdaptiveScrollbar(80);

// 使用Element Plus的z-index管理
const { nextZIndex } = useZIndex();

interface Props {
	modelValue?: boolean;
	action?: ActionItem | null;
	isEditing?: boolean;
	triggerSourceId?: string;
	workflowId?: string;
	loading?: boolean;
	triggerType?: TriggerTypeEnum;
	forceEditable?: boolean; // 强制允许编辑，忽略isTools限制
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: false,
	action: null,
	isEditing: false,
	triggerSourceId: '',
	workflowId: '',
	forceEditable: false,
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
const configMode = ref<ToolsType>(ToolsType.UseTool); // 配置模式，默认使用已有工具
const useExistingTool = computed(
	() =>
		configMode.value === ToolsType.UseTool ||
		configMode.value === ToolsType.SystemTools ||
		configMode.value === ToolsType.MyTool
); // 是否使用已有工具
const selectedToolId = ref(''); // 选中的工具 ID
const loadingExistingTools = ref(false); // 加载已有工具列表状态
const existingToolsList = ref<ActionDefinition[]>([]); // 已有工具列表
const isAiGenerated = ref(false); // 标识当前action是否为AI生成
const aiGeneratedConfig = ref<any>(null); // 存储AI生成的配置数据

const formData = reactive<ActionItem>({
	id: '',
	name: '',
	actionType: ActionType.PYTHON_SCRIPT,
	description: '',
	condition: 'Stage Completed',
	isTools: false, // 新建时默认为 true（工具模式），允许用户选择
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

// 计算是否应该禁用表单字段
const shouldDisableFields = computed(() => {
	// 如果强制允许编辑，直接返回false
	if (props.forceEditable) {
		return false;
	}

	if (disabledActionForMyTool.value) {
		return true;
	}

	// 编辑状态：根据 isTools 决定
	if (props.action) {
		return props.action.isTools === true;
	}

	// 新建状态：根据选择的配置模式决定
	if (configMode.value === ToolsType.NewTool) {
		return false; // 创建普通 Action，允许编辑
	}
	if (configMode.value === ToolsType.UseTool) {
		return true; // 使用已有工具时，表单应该被禁用
	}

	return false;
});

// 计算是否应该禁用配置模式选择
const isConfigModeDisabled = computed(() => {
	// 如果 forceEditable 为 true，禁用配置模式选择
	return props.forceEditable;
});

const drawerSize = computed(() => {
	return leftPanelVisible.value ? '80%' : '40%';
});

const buttonLeftPosition = computed(() => {
	const drawerWidth = leftPanelVisible.value ? 0.8 : 0.4;
	return `calc(100vw - ${drawerWidth * 100}vw - 30px)`;
});

// 为variables按钮分配z-index
const variablesButtonZIndex = ref<number>(0);

// 当drawer显示时分配一个固定的z-index
watch(visible, (isVisible) => {
	if (isVisible) {
		// 只在首次显示时分配z-index，避免每次计算都调用nextZIndex()
		nextTick(() => {
			variablesButtonZIndex.value = nextZIndex();
		});
	}
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
		value: ActionType.PYTHON_SCRIPT,
		icon: Operation,
		description: 'Execute custom Python code when stage completes',
	},
	{
		label: 'HTTP API',
		value: ActionType.HTTP_API,
		icon: Connection,
		description: 'Send HTTP request to external API endpoint',
	},
];

// Form Rules
const rules = {
	name: [{ required: true, message: 'Please enter action name', trigger: 'change' }],
	actionType: [{ required: true, message: 'Please select action actionType', trigger: 'change' }],
	condition: [{ required: true, message: 'Please select condition', trigger: 'change' }],
};

const getDefaultConfig = (actionType: ActionType) => {
	if (actionType === ActionType.PYTHON_SCRIPT) {
		return {
			sourceCode: `def main(onboardingId: str):
    return {
        "greeting": f"{onboardingId}!",
    }`,
		};
	} else if (actionType === ActionType.HTTP_API) {
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
	formData.actionType = ActionType.PYTHON_SCRIPT;
	formData.description = '';
	formData.isTools = false; // 新建时默认为工具模式
	formData.actionConfig = getDefaultConfig(ActionType.PYTHON_SCRIPT);
	formRef.value?.resetFields();
	visible.value = false;
	testResult.value = null;
	disabledActionForMyTool.value = false;

	// 重置选择已有工具的状态
	selectedToolId.value = '';
	existingToolsList.value = [];

	// 重置AI生成状态
	isAiGenerated.value = false;
	aiGeneratedConfig.value = null;

	// 重置配置模式为默认值
	// 如果 forceEditable 为 true 且没有 action，设置为 NewTool 模式
	if (props.forceEditable && !props.action) {
		configMode.value = ToolsType.NewTool;
	} else {
		configMode.value = ToolsType.UseTool;
	}
};

const handleActionTypeChange = (actionType: ActionType) => {
	formData.actionConfig = getDefaultConfig(actionType);
};

// Update action name from HttpConfig component
const updateActionName = (actionName: string) => {
	if (actionName && typeof actionName === 'string' && actionName.trim()) {
		formData.name = actionName.trim();
		console.log('📝 Action name updated in dialog:', actionName);
	}
};

// Handle AI config applied from HttpConfig component
const handleAiConfigApplied = (config: any) => {
	isAiGenerated.value = true;
	aiGeneratedConfig.value = config;
	console.log('🤖 AI config applied, marking action as AI-generated:', config);
};

// Action Type 名称映射方法
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};

// 处理模式变化
const handleConfigModeChange = async (mode: ToolsType) => {
	// 清空当前选择
	selectedToolId.value = '';
	resetFormData();
	await changeConfigModeChange(mode);
};

const disabledActionForMyTool = ref(false);
const changeConfigModeChange = async (mode: ToolsType) => {
	if (props.forceEditable) return;
	console.log('mode:', mode);
	if (mode === ToolsType.UseTool) {
		// 使用已有工具：加载工具列表
		await loadExistingTools(true);
		formData.isTools = true;
		selectedToolId.value = formData.id;
	} else if (mode === ToolsType.MyTool) {
		await loadExistingTools(false);
		formData.isTools = false;
		// 检查当前 action 是否在加载的列表中
		if (formData.id && existingToolsList.value.some((tool) => tool.id === formData.id)) {
			selectedToolId.value = formData.id;
			disabledActionForMyTool.value = false;
		} else {
			// 如果当前 action 不在列表中，清空选择但保留表单数据
			selectedToolId.value = '';
			disabledActionForMyTool.value = true;
		}
	} else if (mode === ToolsType.NewTool) {
		// 创建普通 Action：清空列表，设置为非工具模式
		existingToolsList.value = [];
		formData.isTools = false;
		selectedToolId.value = '';
	} else if (mode === ToolsType.SystemTools) {
		await loadExistingTools(false, true);
		formData.isTools = true;
		selectedToolId.value = formData.id;
	}
};

// 加载已有工具列表
const loadExistingTools = async (isTools: boolean, isSystemTools?: boolean) => {
	try {
		loadingExistingTools.value = true;

		// 查询参数 - 只获取标记为工具的 action
		const params: ActionQueryRequest = {
			pageIndex: 1,
			pageSize: 1000, // 获取足够多的数据
			isTools: isTools,
			isSystemTools: isSystemTools,
		};

		const response = await getActionDefinitions(params);

		if (response.code === '200' && response.success) {
			// 过滤出标记为工具的 action
			existingToolsList.value =
				response.data?.data.filter((item) => {
					return (
						!item.triggerType ||
						(item.triggerType && item.triggerType == props.triggerType)
					);
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

// Watch for action prop changes
watch(
	() => props.action,
	async (newAction) => {
		console.log('newAction:', newAction);
		if (newAction) {
			Object.keys(formData).forEach((key) => {
				formData[key] =
					newAction[key] == undefined || newAction[key] == null
						? formData[key]
						: newAction[key];
			});
			if (formData.actionType === ActionType.SYSTEM_TOOLS) {
				configMode.value = ToolsType.SystemTools;
			} else {
				configMode.value = newAction.isTools ? ToolsType.UseTool : ToolsType.MyTool;
			}
			await changeConfigModeChange(configMode.value);
		} else {
			resetForm();
		}
	},
	{ immediate: true, deep: true }
);

// Watch for forceEditable prop changes
watch(
	() => props.forceEditable,
	(forceEditable) => {
		if (forceEditable && !props.action) {
			// 如果 forceEditable 为 true 且没有 action，强制设置为 NewTool 模式
			configMode.value = ToolsType.NewTool;
			changeConfigModeChange(ToolsType.NewTool);
		}
	},
	{ immediate: true }
);

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
			formData.actionType = toolDetail.actionType;
			formData.actionConfig = JSON.parse(toolDetail.actionConfig || '{}');
			formData.id = toolDetail.id;
			formData.isTools = toolDetail.isTools || false;
			disabledActionForMyTool.value = false;
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
	formData.id = '';
	formData.name = '';
	formData.description = '';
	formData.actionType = ActionType.PYTHON_SCRIPT;
	formData.actionConfig = getDefaultConfig(ActionType.PYTHON_SCRIPT);
	formRef.value?.clearValidate();
	disabledActionForMyTool.value = false;
};

const onTest = async () => {
	try {
		testing.value = true;
		testResult.value = null;
		// Execute test
		const testOutput = await testRunActionNoId({
			actionType: formData.actionType,
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
	try {
		if (configMode.value !== ToolsType.SystemTools) {
			if (!formRef.value) return;
			await formRef.value.validate();
		}
		saving.value = true;
		// 判断是编辑模式还是新建模式，以及是否使用已有工具
		if (
			(!props.action &&
				(configMode.value === ToolsType.UseTool ||
					configMode.value === ToolsType.SystemTools ||
					configMode.value === ToolsType.MyTool) &&
				!props.forceEditable) ||
			selectedToolId.value // 确保有选中的工具ID
		) {
			// 新建模式 + 使用已有工具：创建映射关系
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
				emit('saveSuccess', {
					...formData,
					actionMappingId: res.data.id,
				});
				visible.value = false;
			} else {
				res?.msg && ElMessage.error(res?.msg);
			}
		} else {
			// 编辑模式 或 新建模式下的创建新工具/普通Action：验证并保存
			if (
				formData.actionType === ActionType.PYTHON_SCRIPT &&
				!formData.actionConfig.sourceCode
			) {
				ElMessage.error('Please enter Python script code');
				return;
			}

			if (formData.actionType === ActionType.HTTP_API && !formData.actionConfig.url) {
				ElMessage.error('Please enter HTTP API URL');
				return;
			}

			// 根据 action actionType 准备不同的 actionConfig
			let cleanActionConfig: any = {};

			if (formData.actionType === ActionType.PYTHON_SCRIPT) {
				// Python 类型只需要 sourceCode
				cleanActionConfig = {
					sourceCode: formData.actionConfig.sourceCode,
				};
			} else if (formData.actionType === ActionType.HTTP_API) {
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
				actionType: formData.actionType,
				triggerSourceId: props?.triggerSourceId || null,
				triggerType: props?.triggerType || null,
				isAIGenerated: isAiGenerated.value, // 添加AI生成标识
				aiGeneratedConfig: aiGeneratedConfig.value
					? JSON.stringify(aiGeneratedConfig.value)
					: null, // 添加AI生成的配置数据
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

const opened = () => {
	nextTick(() => {
		if (!props.action) {
			changeConfigModeChange(configMode.value);
		}
	});
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
	@apply bg-gray-300 dark:bg-gray-600 opacity-80 rounded-xl;
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

.action-config-container {
	@apply min-h-full;
}

.section-header {
	@apply border-b pb-3 mb-4;
}

.action-config-section {
	@apply space-y-4;
}

// 抽屉footer样式
.dialog-footer {
	@apply flex justify-end space-x-3 px-6 py-4 border-t;
	position: sticky;
	bottom: 0;
	margin-top: auto;
}

.variables-toggle-external {
	position: fixed;
	left: v-bind(buttonLeftPosition);
	top: 10rem;
	transform: translateY(-50%);
	z-index: v-bind(variablesButtonZIndex);
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
	@apply border-l-2 border-primary-500 rounded-s-md cursor-pointer transition-all duration-300 flex flex-col items-center justify-center gap-2;
	width: 30px;
	height: 200px;
	background-color: var(--el-bg-color);

	&:hover {
		background-color: var(--el-fill-color-blank);
		border-color: var(--el-color-primary);
		transform: scale(1.05);
		box-shadow: 0 6px 16px rgba(0, 0, 0, 0.1);
	}

	.dark & {
		background-color: var(--el-bg-color);
		border-color: var(--el-border-color);

		&:hover {
			background-color: var(--el-fill-color);
			border-color: var(--el-color-primary);
		}
	}

	.variables-toggle-text {
		writing-mode: vertical-rl;
	}
}

.variables-panel-container {
	@apply flex-1 min-w-0 min-h-0 flex flex-col rounded-xl;
	background-color: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-light);

	.dark & {
		border-color: var(--el-border-color);
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

	:deep(.el-drawer__header) {
		margin-bottom: 0;
	}
}

.mode-selection-section {
	border-left: 4px solid var(--el-color-primary);
	background: var(--el-fill-color-blank);
	transition: all 0.3s ease;

	&:hover {
		background: var(--el-fill-color-lighter);
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	}

	.dark & {
		background: var(--el-fill-color);
		border-left-color: var(--el-color-primary-light-3);

		&:hover {
			background: var(--el-fill-color-light);
			box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
		}
	}
}
</style>
