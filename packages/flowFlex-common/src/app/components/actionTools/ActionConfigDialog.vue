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
			:size="drawerSize"
			direction="rtl"
			@close="onCancel"
			@opened="opened"
			append-to-body
			:with-header="false"
		>
			<div class="font-bold mb-4">
				{{ dialogTitle }}
			</div>
			<div class="flex gap-4 w-full h-full min-h-0">
				<div v-if="leftPanelVisible" class="flex-1 min-w-0 min-h-0 flex flex-col">
					<el-scrollbar ref="scrollbarRefLeft" class="h-full">
						<VariablesPanel
							:stage-id="currentTriggerSourceId"
							:action-actionType="formData.actionType"
						/>
					</el-scrollbar>
				</div>

				<div
					class="action-config-container flex-1 min-w-0 min-h-0 flex flex-col"
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
										:disabled="!!currentActionId || isConfigModeDisabled"
									>
										<span class="text-sm">Use Tool</span>
									</el-radio>
									<el-radio
										:value="ToolsType.MyTool"
										:disabled="!!currentActionId || isConfigModeDisabled"
									>
										<span class="text-sm">My Action</span>
									</el-radio>
									<el-radio
										:value="ToolsType.NewTool"
										:disabled="!!currentActionId || isConfigModeDisabled"
									>
										<span class="text-sm">Create New Action</span>
									</el-radio>
									<el-radio
										:value="ToolsType.SystemTools"
										:disabled="!!currentActionId || isConfigModeDisabled"
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
										:disabled="!!currentActionId || shouldDisableFields"
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
									:id-editing="!!currentActionId"
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
									:id-editing="!!currentActionId"
									:disabled="shouldDisableFields"
								/>
							</div>

							<!-- Field Mapping Section -->
							<el-form-item label="Field Mapping" class="w-full">
								<div class="flex items-center gap-2 mb-4">
									<el-switch
										v-model="showFieldMapping"
										@change="handleShowFieldMappingChange"
										:disabled="shouldDisableFields"
									/>
									<span class="text-sm text-text-secondary">
										Enable Field Mapping
									</span>
								</div>
								<div class="space-y-4 w-full" v-if="showFieldMapping">
									<div class="flex justify-between items-center">
										<div>
											<h4 class="text-sm font-semibold text-text-primary m-0">
												Field Mapping
											</h4>
											<p class="text-xs text-text-secondary mt-1">
												Map external fields to WFE fields
											</p>
										</div>
										<el-button
											type="primary"
											@click="handleAddFieldMapping"
											:disabled="shouldDisableFields"
											:icon="Plus"
										>
											Add Field
										</el-button>
									</div>
									<el-table
										:data="fieldMappings"
										class="w-full"
										empty-text="No field mappings configured"
										:border="true"
									>
										<el-table-column label="External Field" min-width="200">
											<template #default="{ row }">
												<el-input
													v-model="row.externalFieldName"
													placeholder="Enter external field name"
													:disabled="shouldDisableFields"
												/>
											</template>
										</el-table-column>

										<el-table-column label="WFE Field" min-width="200">
											<template #default="{ row }">
												<el-select
													v-model="row.wfeFieldId"
													placeholder="Select WFE field"
													:disabled="shouldDisableFields"
													class="w-full"
												>
													<el-option
														v-for="field in wfeFieldOptions"
														:key="field.vIfKey || ''"
														:label="field.label"
														:value="String(field.vIfKey)"
													/>
												</el-select>
											</template>
										</el-table-column>

										<el-table-column label="Type" min-width="120">
											<template #default>
												<span class="text-sm">text</span>
											</template>
										</el-table-column>

										<el-table-column label="Direction" min-width="150">
											<template #default="{ row }">
												<el-select
													v-model="row.syncDirection"
													placeholder="Select direction"
													:disabled="shouldDisableFields"
													class="w-full"
												>
													<el-option
														:label="'View Only'"
														:value="SyncDirection.ViewOnly"
													/>
													<el-option
														:label="'Editable'"
														:value="SyncDirection.Editable"
													/>
												</el-select>
											</template>
										</el-table-column>

										<el-table-column label="Actions" width="100" align="center">
											<template #default="{ $index }">
												<el-button
													type="danger"
													link
													:icon="Delete"
													@click="handleRemoveFieldMapping($index)"
													:disabled="shouldDisableFields"
												/>
											</template>
										</el-table-column>
									</el-table>
								</div>
							</el-form-item>
						</el-form>
					</el-scrollbar>
				</div>
			</div>

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="onCancel">Cancel</el-button>
					<el-button type="primary" @click="onSave" :loading="saving">
						{{ !!currentActionId ? 'Update' : 'Add' }} Action
					</el-button>
				</div>
			</template>
		</el-drawer>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue';
import { ElMessage, useZIndex } from 'element-plus';
import { Operation, Connection, Delete, Plus } from '@element-plus/icons-vue';
import PythonConfig from './PythonConfig.vue';
import HttpConfig from './HttpConfig.vue';
import VariablesPanel from './VariablesPanel.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { SyncDirection } from '@/enums/integration';

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
import staticFieldsData from './static-field.json';

const { scrollbarRef: scrollbarRefLeft, updateScrollbarHeight: updateScrollbarHeightLeft } =
	useAdaptiveScrollbar(80);

const { scrollbarRef: scrollbarRefRight, updateScrollbarHeight: updateScrollbarHeightRight } =
	useAdaptiveScrollbar(80);

// 使用Element Plus的z-index管理
const { nextZIndex } = useZIndex();

interface Props {
	triggerSourceId?: string;
	workflowId?: string;
	triggerType?: TriggerTypeEnum;
	mappingRequired?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	triggerSourceId: '',
	workflowId: '',
	mappingRequired: false,
});

const emit = defineEmits(['saveSuccess']);

// 内部状态
const visible = ref(false);
const loading = ref(false);
const currentActionId = ref<string>('');
const currentTriggerSourceId = ref<string>('');
const currentWorkflowId = ref<string>('');
const currentTriggerType = ref<TriggerTypeEnum | undefined>(undefined);
const forceEditable = ref(false);

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

const formData = ref<ActionItem & { fieldMappings?: IFieldMappingItem[] }>({
	id: '',
	name: '',
	actionType: ActionType.PYTHON_SCRIPT,
	description: '',
	condition: 'Stage Completed',
	isTools: false, // 新建时默认为 true（工具模式），允许用户选择
	actionConfig: {},
	fieldMappings: [],
});

// Computed
const dialogTitle = computed(() => {
	return currentActionId.value ? 'Edit Action' : 'Add New Action';
});

// 计算是否应该禁用表单字段
const shouldDisableFields = computed(() => {
	// 如果强制允许编辑，直接返回false
	if (forceEditable.value) {
		return false;
	}

	if (disabledActionForMyTool.value) {
		return true;
	}

	// 编辑状态：根据 isTools 决定
	if (currentActionId.value && formData.value.isTools) {
		return true;
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
	return forceEditable.value;
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
// 字段映射相关状态和逻辑
interface IFieldMappingItem {
	externalFieldName: string;
	wfeFieldId: string;
	fieldType: number;
	syncDirection: number;
}

const showFieldMapping = ref(false);
const wfeFieldOptions = ref(staticFieldsData.formFields);

const fieldMappings = computed({
	get() {
		return formData.value.fieldMappings || [];
	},
	set(val: IFieldMappingItem[]) {
		formData.value.fieldMappings = val;
	},
});

/**
 * 处理显示字段映射开关变化
 */
function handleShowFieldMappingChange(value: string | number | boolean) {
	const boolValue = Boolean(value);
	showFieldMapping.value = boolValue;
	if (!boolValue) {
		// 关闭时清空字段映射
		fieldMappings.value = [];
	}
}

/**
 * 添加字段映射
 */
function handleAddFieldMapping() {
	const newMapping: IFieldMappingItem = {
		externalFieldName: '',
		wfeFieldId: '',
		fieldType: 1, // 默认 Text
		syncDirection: SyncDirection.ViewOnly,
	};
	fieldMappings.value = [...(fieldMappings.value || []), newMapping];
}

/**
 * 删除字段映射
 */
function handleRemoveFieldMapping(index: number) {
	const mappings = [...(fieldMappings.value || [])];
	mappings.splice(index, 1);
	fieldMappings.value = mappings;
}

const resetForm = (closeDialog = true) => {
	formData.value.id = '';
	formData.value.name = '';
	formData.value.actionType = ActionType.PYTHON_SCRIPT;
	formData.value.description = '';
	formData.value.isTools = false; // 新建时默认为工具模式
	formData.value.actionConfig = getDefaultConfig(ActionType.PYTHON_SCRIPT);
	formRef.value?.resetFields();
	if (closeDialog) {
		visible.value = false;
	}
	testResult.value = null;
	disabledActionForMyTool.value = false;

	// 重置选择已有工具的状态
	selectedToolId.value = '';
	existingToolsList.value = [];

	// 重置AI生成状态
	isAiGenerated.value = false;
	aiGeneratedConfig.value = null;

	// 重置字段映射状态
	showFieldMapping.value = false;
	fieldMappings.value = [];

	// 重置配置模式为默认值
	// 如果 forceEditable 为 true 且没有 actionId，设置为 NewTool 模式
	if (forceEditable.value && !currentActionId.value) {
		configMode.value = ToolsType.NewTool;
	} else {
		configMode.value = ToolsType.UseTool;
	}
};

const handleActionTypeChange = (actionType: ActionType) => {
	formData.value.actionConfig = getDefaultConfig(actionType);
};

// Update action name from HttpConfig component
const updateActionName = (actionName: string) => {
	if (actionName && typeof actionName === 'string' && actionName.trim()) {
		formData.value.name = actionName.trim();
	}
};

// Handle AI config applied from HttpConfig component
const handleAiConfigApplied = (config: any) => {
	isAiGenerated.value = true;
	aiGeneratedConfig.value = config;
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
	if (forceEditable.value) return;
	if (mode === ToolsType.UseTool) {
		// 使用已有工具：加载工具列表
		await loadExistingTools(true);
		formData.value.isTools = true;
		selectedToolId.value = formData.value.id;
	} else if (mode === ToolsType.MyTool) {
		await loadExistingTools(false);
		formData.value.isTools = false;
		// 检查当前 action 是否在加载的列表中
		if (
			formData.value.id &&
			existingToolsList.value.some((tool) => tool.id === formData.value.id)
		) {
			selectedToolId.value = formData.value.id;
			disabledActionForMyTool.value = false;
		} else {
			// 如果当前 action 不在列表中，清空选择但保留表单数据
			selectedToolId.value = '';
			disabledActionForMyTool.value = true;
		}
	} else if (mode === ToolsType.NewTool) {
		// 创建普通 Action：清空列表，设置为非工具模式
		existingToolsList.value = [];
		formData.value.isTools = false;
		selectedToolId.value = '';
	} else if (mode === ToolsType.SystemTools) {
		await loadExistingTools(false, true);
		formData.value.isTools = true;
		selectedToolId.value = formData.value.id;
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
						(item.triggerType && item.triggerType == currentTriggerType.value)
					);
				}) || [];
		} else {
			ElMessage.error('Failed to load existing tools');
			existingToolsList.value = [];
		}
	} catch (error) {
		existingToolsList.value = [];
	} finally {
		loadingExistingTools.value = false;
	}
};

/**
 * 加载 Action 详情
 */
const loadActionDetail = async (actionId: string) => {
	try {
		loading.value = true;
		const response = await getActionDetail(actionId);
		if (response.code === '200' && response.data) {
			const actionDetail = response.data;

			// 填充表单数据
			formData.value.id = actionDetail.id || '';
			formData.value.name = actionDetail.name || '';
			formData.value.description = actionDetail.description || '';
			formData.value.actionType = actionDetail.actionType;
			formData.value.actionConfig = JSON.parse(actionDetail.actionConfig || '{}');
			formData.value.isTools = actionDetail.isTools || false;
			formData.value.condition = actionDetail.condition || 'Stage Completed';
			formData.value.fieldMappings = actionDetail.fieldMappings || [];

			// 设置配置模式actionConfigDialogRef
			if (formData.value.actionType === ActionType.SYSTEM_TOOLS) {
				configMode.value = ToolsType.SystemTools;
			} else {
				configMode.value = formData.value.isTools ? ToolsType.UseTool : ToolsType.MyTool;
			}
			await changeConfigModeChange(configMode.value);

			// 初始化字段映射状态
			if (formData.value.fieldMappings && formData.value.fieldMappings.length > 0) {
				showFieldMapping.value = true;
			} else {
				showFieldMapping.value = false;
			}
		} else {
			ElMessage.error('Failed to load action details');
		}
	} catch (error) {
		console.error('Failed to load action details:', error);
		ElMessage.error('Failed to load action details');
	} finally {
		loading.value = false;
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
			formData.value.name = toolDetail.name || '';
			formData.value.description = toolDetail.description || '';
			formData.value.actionType = toolDetail.actionType;
			formData.value.actionConfig = JSON.parse(toolDetail.actionConfig || '{}');
			formData.value.id = toolDetail.id;
			formData.value.isTools = toolDetail.isTools || false;
			disabledActionForMyTool.value = false;
		} else {
			ElMessage.error('Failed to load tool details');
		}
	} catch (error) {
		console.error('Failed to load tool details:', error);
	}
};

// 重置表单数据
const resetFormData = () => {
	formData.value.id = '';
	formData.value.name = '';
	formData.value.description = '';
	formData.value.actionType = ActionType.PYTHON_SCRIPT;
	formData.value.actionConfig = getDefaultConfig(ActionType.PYTHON_SCRIPT);
	formRef.value?.clearValidate();
	disabledActionForMyTool.value = false;
};

const onTest = async () => {
	try {
		testing.value = true;
		testResult.value = null;
		// Execute test
		const testOutput = await testRunActionNoId({
			actionType: formData.value.actionType,
			actionConfig: JSON.stringify(formData.value.actionConfig),
		});

		if (testOutput.code == '200') {
			testResult.value = testOutput.data;
		}
	} finally {
		testing.value = false;
	}
};

/**
 * 创建 Action 映射关系
 */
const createActionMapping = async (actionDefinitionId: string) => {
	const mappingParams = {
		actionDefinitionId,
		triggerSourceId: props?.triggerSourceId || null,
		triggerType: props?.triggerType || null,
		workFlowId: props?.workflowId || null,
	};

	const mappingRes = await addMappingAction(mappingParams);
	if (mappingRes.code !== '200') {
		mappingRes?.msg && ElMessage.error(mappingRes?.msg);
		return false;
	}
	return true;
};

const onSave = async () => {
	try {
		// 表单验证
		if (configMode.value !== ToolsType.SystemTools) {
			if (!formRef.value) return;
			await formRef.value.validate();
		}

		// 业务验证
		if (
			formData.value.actionType === ActionType.PYTHON_SCRIPT &&
			!formData.value.actionConfig.sourceCode
		) {
			ElMessage.error('Please enter Python script code');
			return;
		}

		if (formData.value.actionType === ActionType.HTTP_API && !formData.value.actionConfig.url) {
			ElMessage.error('Please enter HTTP API URL');
			return;
		}

		saving.value = true;

		// 准备 actionConfig
		let cleanActionConfig: any = {};
		if (formData.value.actionType === ActionType.PYTHON_SCRIPT) {
			cleanActionConfig = {
				sourceCode: formData.value.actionConfig.sourceCode,
			};
		} else if (formData.value.actionType === ActionType.HTTP_API) {
			// 确保不包含 fieldMappings（fieldMappings 现在是同级别字段）
			const httpConfig = { ...formData.value.actionConfig };
			delete (httpConfig as any).fieldMappings;
			cleanActionConfig = {
				...httpConfig,
				url: formData.value.actionConfig.url || '',
				method: formData.value.actionConfig.method || 'GET',
				headers: formData.value.actionConfig.headers || {},
				params: formData.value.actionConfig.params || {},
				body: formData.value.actionConfig.body || '',
				timeout: formData.value.actionConfig.timeout || 30,
				followRedirects: formData.value.actionConfig.followRedirects !== false,
			};
		}

		// 准备保存参数
		const actionParams = {
			...formData.value,
			actionConfig: JSON.stringify(cleanActionConfig),
			fieldMappings: fieldMappings.value || [],
			workflowId: currentWorkflowId.value || null,
			actionType: formData.value.actionType,
			triggerSourceId: currentTriggerSourceId.value || null,
			triggerType: currentTriggerType.value || null,
			isAIGenerated: isAiGenerated.value,
			aiGeneratedConfig: aiGeneratedConfig.value
				? JSON.stringify(aiGeneratedConfig.value)
				: null,
		};

		// 先创建或更新 Action
		const actionRes: any = formData.value.id
			? await updateAction(formData.value.id, actionParams)
			: await addAction(actionParams);

		if (actionRes.code == '200') {
			const savedAction = actionRes.data;
			const actionId = savedAction.id || formData.value.id;

			// 根据条件判断是否需要创建映射关系
			const needMapping =
				(currentTriggerSourceId.value || currentTriggerType.value) &&
				!formData.value.id && // 新建时才需要创建映射
				configMode.value !== ToolsType.SystemTools; // 系统工具不需要映射

			if (needMapping || props?.mappingRequired) {
				await createActionMapping(actionId);
			}

			emit('saveSuccess', savedAction);
			visible.value = false;
		} else {
			actionRes?.msg && ElMessage.error(actionRes?.msg);
		}
	} finally {
		saving.value = false;
	}
};

const onCancel = () => {
	visible.value = false;
	resetForm();
};

const opened = () => {
	nextTick(() => {
		if (!currentActionId.value) {
			changeConfigModeChange(configMode.value);
		}
	});
};

/**
 * 打开对话框
 * @param options 打开选项
 */
const open = async (options?: {
	actionId?: string;
	triggerSourceId?: string;
	workflowId?: string;
	triggerType?: TriggerTypeEnum;
	forceEditable?: boolean;
}) => {
	// 重置状态
	currentActionId.value = options?.actionId || '';
	currentTriggerSourceId.value = options?.triggerSourceId || props.triggerSourceId || '';
	currentWorkflowId.value = options?.workflowId || props.workflowId || '';
	currentTriggerType.value = options?.triggerType || props.triggerType;
	forceEditable.value = options?.forceEditable || false;

	// 重置表单（不关闭对话框）
	resetForm(false);

	// 打开对话框
	visible.value = true;

	// 如果有 actionId，加载详情
	if (options?.actionId) {
		await loadActionDetail(options.actionId);
	} else {
		// 新建模式，初始化配置模式
		nextTick(() => {
			changeConfigModeChange(configMode.value);
		});
	}
};

const resetScrollbarHeight = () => {
	nextTick(() => {
		updateScrollbarHeightLeft();
		updateScrollbarHeightRight();
	});
};

defineExpose({
	resetScrollbarHeight,
	open,
});
</script>

<style scoped lang="scss">
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
