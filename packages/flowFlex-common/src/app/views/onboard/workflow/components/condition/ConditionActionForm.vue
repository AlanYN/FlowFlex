<template>
	<div class="condition-action-form">
		<!-- Loading 骨架屏 -->
		<template v-if="isLoading">
			<div class="actions-list">
				<div v-for="(_, index) in modelValue" :key="index" class="action-item">
					<div class="action-header">
						<el-skeleton :rows="0" animated style="width: 80px">
							<template #template>
								<el-skeleton-item variant="text" style="width: 80px" />
							</template>
						</el-skeleton>
					</div>
					<el-skeleton :rows="2" animated />
				</div>
			</div>
		</template>

		<!-- 实际内容 -->
		<template v-else>
			<!-- 动作列表 -->
			<div class="actions-list">
				<div v-for="(action, index) in modelValue" :key="index" class="action-item">
					<div class="action-header">
						<span class="action-number">Action {{ index + 1 }}</span>
						<el-button
							type="danger"
							link
							:icon="Delete"
							@click="handleRemoveAction(index)"
						/>
					</div>

					<!-- Action Type -->
					<el-form-item label="Action Type" class="action-field">
						<el-select
							v-model="action.type"
							placeholder="Select action type"
							@change="() => handleActionTypeReset(action)"
						>
							<el-option
								v-for="type in actionTypes"
								:key="type.value"
								:label="type.label"
								:value="type.value"
							>
								<div class="action-type-option">
									<span>{{ type.label }}</span>
									<span class="action-type-desc">{{ type.description }}</span>
								</div>
							</el-option>
						</el-select>
					</el-form-item>

					<!-- GoToStage: Target Stage -->
					<el-form-item v-if="action.type === 'GoToStage'" label="Target Stage">
						<el-select v-model="action.targetStageId" placeholder="Select target stage">
							<el-option
								v-for="stage in stages"
								:key="stage.id"
								:label="stage.name"
								:value="stage.id"
							/>
						</el-select>
						<!-- 循环警告 -->
						<div v-if="isLoopWarning(action.targetStageId)" class="loop-warning">
							<el-icon><Warning /></el-icon>
							<span>Warning: This may cause a loop in the workflow</span>
						</div>
					</el-form-item>

					<!-- TriggerAction: Action Definition -->
					<el-form-item
						v-if="action.type === 'TriggerAction'"
						label="Action"
						class="action-field"
					>
						<el-select v-model="action.actionDefinitionId" placeholder="Select action">
							<el-option-group
								v-for="(actions, groupName) in groupedActions"
								:key="groupName"
								:label="groupName"
							>
								<el-option
									v-for="act in actions"
									:key="act.id"
									:label="act.name"
									:value="act.id"
								/>
							</el-option-group>
						</el-select>
					</el-form-item>

					<!-- SendNotification: Recipient -->
					<template v-if="action.type === 'SendNotification'">
						<el-form-item label="Recipient Type" class="action-field">
							<el-select
								v-model="getActionParams(action).recipientType"
								placeholder="Select recipient type"
								@change="() => handleRecipientTypeChange(action)"
							>
								<el-option value="user" label="User" />
								<el-option value="team" label="Team" />
								<el-option value="email" label="Email" />
							</el-select>
						</el-form-item>
						<!-- User/Team 选择器 -->
						<el-form-item
							v-if="getActionParams(action).recipientType === 'user'"
							label="Select User"
							class="action-field"
						>
							<FlowflexUserSelector
								v-model="getActionParams(action).recipientId"
								selection-type="user"
								placeholder="Select user"
								@change="(val) => handleUserChange(action, val)"
							/>
						</el-form-item>
						<el-form-item
							v-if="getActionParams(action).recipientType === 'team'"
							label="Select Team"
							class="action-field"
						>
							<FlowflexUserSelector
								v-model="getActionParams(action).recipientId"
								selection-type="team"
								placeholder="Select team"
								@change="(val) => handleUserChange(action, val)"
							/>
						</el-form-item>
						<!-- Email 输入框 -->
						<el-form-item
							v-if="getActionParams(action).recipientType === 'email'"
							label="Email Address"
							class="action-field"
						>
							<el-input
								v-model="getActionParams(action).recipientEmail"
								placeholder="Enter email address"
								type="email"
							/>
						</el-form-item>
					</template>

					<!-- UpdateField: Field and Value -->
					<template v-if="action.type === 'UpdateField'">
						<el-form-item label="Target Field to Update" class="action-field">
							<el-select
								v-model="getActionParams(action).fieldPath"
								placeholder="Select field"
								@change="(val: string) => handleFieldSelect(action, val)"
							>
								<el-option-group
									v-for="group in groupedFieldOptions"
									:key="group.stageId"
									:label="group.stageName"
								>
									<el-option
										v-for="field in group.fields"
										:key="field.key"
										:value="field.key"
										:label="field.name"
									/>
								</el-option-group>
							</el-select>
						</el-form-item>
						<el-form-item
							v-if="getActionParams(action).fieldPath"
							:label="`Value for &quot;${
								getActionParams(action).fieldName || ''
							}&quot;`"
							class="action-field"
						>
							<!-- 有固定选项的字段类型使用下拉选择 -->
							<el-select
								v-if="hasFieldFixedOptions(action)"
								v-model="getActionParams(action).fieldValue"
								placeholder="Select value"
							>
								<el-option
									v-for="opt in getFieldValueOptions(action)"
									:key="opt.value"
									:label="opt.label"
									:value="opt.value"
								/>
							</el-select>
							<!-- 其他类型使用输入框 -->
							<el-input
								v-else
								v-model="getActionParams(action).fieldValue"
								placeholder="Enter new value"
							/>
						</el-form-item>
					</template>

					<!-- AssignUser: Assignee -->
					<template v-if="action.type === 'AssignUser'">
						<el-form-item label="Assignee Type" class="action-field">
							<el-select
								v-model="getActionParams(action).assigneeType"
								placeholder="Select assignee type"
								@change="() => handleAssigneeTypeChange(action)"
							>
								<el-option value="user" label="User" />
								<el-option value="team" label="Team" />
							</el-select>
						</el-form-item>
						<!-- User 选择器 (多选) -->
						<el-form-item
							v-if="getActionParams(action).assigneeType === 'user'"
							label="Select Users"
							class="action-field"
						>
							<FlowflexUserSelector
								v-model="getActionParams(action).assigneeIds"
								selection-type="user"
								placeholder="Select users"
								@change="(val) => handleAssigneeChange(action, val)"
							/>
						</el-form-item>
						<!-- Team 选择器 (多选) -->
						<el-form-item
							v-if="getActionParams(action).assigneeType === 'team'"
							label="Select Teams"
							class="action-field"
						>
							<FlowflexUserSelector
								v-model="getActionParams(action).assigneeIds"
								selection-type="team"
								placeholder="Select teams"
								@change="(val) => handleAssigneeChange(action, val)"
							/>
						</el-form-item>
					</template>
				</div>
			</div>
		</template>

		<!-- 添加动作按钮 -->
		<el-button type="primary" link @click="handleAddAction">
			<el-icon class="mr-1"><Plus /></el-icon>
			Add Action
		</el-button>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { Plus, Delete, Warning } from '@element-plus/icons-vue';
import type { ActionFormItem } from '#/condition';
import type { Stage } from '#/onboard';
import type { DynamicList, DynamicDropdownItem } from '#/dynamic';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { conditionAction } from '@/apis/ow';
import { batchIdsDynamicFields } from '@/apis/global/dyanmicField';
import { ToolsType, propertyTypeEnum } from '@/enums/appEnum';

// Props
const props = defineProps<{
	modelValue: ActionFormItem[];
	stages: Stage[];
	currentStageIndex: number;
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:modelValue', value: ActionFormItem[]): void;
}>();

// ToolsType 标签映射
const TOOLS_TYPE_LABELS: Record<number, string> = {
	[ToolsType.UseTool]: 'Public Tools',
	[ToolsType.MyTool]: 'My Tools',
	[ToolsType.SystemTools]: 'System Tools',
};

// 可用的 Action 定义
interface ActionOption {
	id: string;
	name: string;
	toolsType: ToolsType;
}
const availableActions = ref<ActionOption[]>([]);

// Loading 状态
const loadingActions = ref(false);
const loadingFields = ref(false);

// 整体 loading 状态
const isLoading = computed(() => loadingActions.value || loadingFields.value);

// 按 ToolsType 分组的 Actions
const groupedActions = computed(() => {
	const groups: Record<string, ActionOption[]> = {};

	availableActions.value.forEach((action) => {
		const typeName = TOOLS_TYPE_LABELS[action.toolsType] || 'Other';
		if (!groups[typeName]) {
			groups[typeName] = [];
		}
		groups[typeName].push(action);
	});

	return groups;
});

// 动作类型选项
const actionTypes = [
	{ value: 'GoToStage', label: 'Go To Stage', description: 'Jump to a specific stage' },
	{ value: 'SkipStage', label: 'Skip Stage', description: 'Skip the next stage' },
	{
		value: 'EndWorkflow',
		label: 'End Workflow',
		description: 'Complete the workflow immediately',
	},
	{
		value: 'SendNotification',
		label: 'Send Notification',
		description: 'Send email/SMS notification',
	},
	{
		value: 'UpdateField',
		label: 'Update Field',
		description: 'Automatically update a field value',
	},
	{ value: 'TriggerAction', label: 'Trigger Action', description: 'Execute a predefined action' },
	{ value: 'AssignUser', label: 'Assign User', description: 'Reassign to user/team' },
];

// 检查是否会产生循环
const isLoopWarning = (targetStageId?: string) => {
	if (!targetStageId) return false;
	const targetIndex = props.stages.findIndex((s) => s.id === targetStageId);
	return targetIndex !== -1 && targetIndex <= props.currentStageIndex;
};

// 获取 action 的 parameters，确保存在
const getActionParams = (action: ActionFormItem) => {
	if (!action.parameters) {
		action.parameters = {};
	}
	return action.parameters;
};

// 处理动作类型变化时重置相关字段
const handleActionTypeReset = (action: ActionFormItem) => {
	action.targetStageId = undefined;
	action.actionDefinitionId = undefined;
	action.parameters = {};
};

// 处理 SendNotification 的 recipientType 变化
const handleRecipientTypeChange = (action: ActionFormItem) => {
	const params = getActionParams(action);
	params.recipientId = undefined;
	params.recipientEmail = undefined;
};

// 处理 User/Team 选择变化
const handleUserChange = (action: ActionFormItem, value: string | string[] | undefined) => {
	const params = getActionParams(action);
	params.recipientId = Array.isArray(value) ? value[0] : value;
};

// 处理 AssignUser 的 assigneeType 变化
const handleAssigneeTypeChange = (action: ActionFormItem) => {
	const params = getActionParams(action);
	params.assigneeIds = [];
};

// 处理 Assignee 选择变化
const handleAssigneeChange = (action: ActionFormItem, value: string | string[] | undefined) => {
	const params = getActionParams(action);
	params.assigneeIds = Array.isArray(value) ? value : value ? [value] : [];
};

// 添加动作
const handleAddAction = () => {
	const newAction: ActionFormItem = {
		type: 'GoToStage',
		order: props.modelValue.length,
		parameters: {},
	};
	emit('update:modelValue', [...props.modelValue, newAction]);
};

// 删除动作
const handleRemoveAction = (index: number) => {
	const newActions = props.modelValue.filter((_, i) => i !== index);
	emit('update:modelValue', newActions);
};

// 加载 Actions
const loadAvailableActions = async () => {
	loadingActions.value = true;
	try {
		const res = await conditionAction();
		if (res.code === '200' && res.data) {
			availableActions.value = res.data.map((item: any) => {
				// 根据 isSystemTools 和 isTools 判断 ToolsType
				let toolsType: ToolsType;
				if (item.isSystemTools) {
					toolsType = ToolsType.SystemTools;
				} else if (item.isTools) {
					toolsType = ToolsType.UseTool;
				} else {
					toolsType = ToolsType.MyTool;
				}

				return {
					id: item.id,
					name: item.name || item.actionCode || 'Unnamed Action',
					toolsType,
				};
			});
		}
	} catch (error) {
		console.error('Failed to load available actions:', error);
		availableActions.value = [];
	} finally {
		loadingActions.value = false;
	}
};

// 静态字段映射缓存
const staticFieldsMap = ref<Map<string, DynamicList>>(new Map());

// 字段选项接口
interface FieldOption {
	key: string; // stageId_fieldId 组合键
	id: string;
	name: string;
	stageId: string;
	stageName: string;
	dataType: number;
	dropdownItems?: DynamicDropdownItem[];
	additionalInfo?: DynamicList['additionalInfo'];
}

// 字段分组接口
interface FieldOptionGroup {
	stageName: string;
	stageId: string;
	fields: FieldOption[];
}

// Value 选项接口
interface ValueOption {
	label: string;
	value: string;
}

// 按 Stage 分组的字段选项（只包含当前 stage 之后的 stage）
const groupedFieldOptions = computed<FieldOptionGroup[]>(() => {
	const groups: FieldOptionGroup[] = [];

	// 只遍历当前 stage 之后的 stages（不包含当前 stage）
	props.stages.slice(props.currentStageIndex + 1).forEach((stage) => {
		const fieldsComponent = stage.components?.find((c) => c.key === 'fields');
		if (!fieldsComponent?.staticFields?.length) return;

		const fields: FieldOption[] = [];
		fieldsComponent.staticFields.forEach((field) => {
			const fieldInfo = staticFieldsMap.value.get(field.id);
			if (fieldInfo) {
				fields.push({
					key: `${stage.id}_${field.id}`,
					id: field.id,
					name: fieldInfo.displayName || field.id,
					stageId: stage.id,
					stageName: stage.name,
					dataType: fieldInfo.dataType,
					dropdownItems: fieldInfo.dropdownItems,
					additionalInfo: fieldInfo.additionalInfo,
				});
			}
		});

		if (fields.length > 0) {
			groups.push({
				stageName: stage.name,
				stageId: stage.id,
				fields,
			});
		}
	});

	return groups;
});

// 加载所有 Stage 的静态字段映射
const loadStaticFieldsMapping = async () => {
	// 收集所有 stage 的字段 ID
	const allFieldIds: string[] = [];
	props.stages.forEach((stage) => {
		const fieldsComponent = stage.components?.find((c) => c.key === 'fields');
		if (fieldsComponent?.staticFields?.length) {
			fieldsComponent.staticFields.forEach((f) => {
				if (!allFieldIds.includes(f.id)) {
					allFieldIds.push(f.id);
				}
			});
		}
	});

	if (allFieldIds.length === 0) return;

	// 过滤掉已经缓存的ID
	const uncachedIds = allFieldIds.filter((id) => !staticFieldsMap.value.has(id));

	if (uncachedIds.length === 0) return;

	loadingFields.value = true;
	try {
		const res: any = await batchIdsDynamicFields({ ids: uncachedIds });
		if (res.code === '200' && res.data) {
			res.data.forEach((field: DynamicList) => {
				staticFieldsMap.value.set(field.id, field);
			});
		}
	} catch (error) {
		console.error('Failed to load static fields mapping:', error);
	} finally {
		loadingFields.value = false;
	}
};

// 处理字段选择变化
const handleFieldSelect = (action: ActionFormItem, fieldKey: string) => {
	const params = getActionParams(action);

	// 从所有分组中查找选中的字段
	let selectedField: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		selectedField = group.fields.find((f) => f.key === fieldKey);
		if (selectedField) break;
	}

	if (selectedField) {
		params.stageId = selectedField.stageId;
		params.fieldId = selectedField.id;
		params.fieldName = selectedField.name;
		params.fieldPath = fieldKey;
		// 清空之前的值
		params.fieldValue = '';
	}
};

// 判断字段是否有固定选项
const hasFieldFixedOptions = (action: ActionFormItem): boolean => {
	const params = getActionParams(action);
	const fieldKey = params.fieldPath;
	if (!fieldKey) return false;

	// 从所有分组中查找字段
	let fieldInfo: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		fieldInfo = group.fields.find((f) => f.key === fieldKey);
		if (fieldInfo) break;
	}

	if (!fieldInfo) return false;

	return (
		fieldInfo.dataType === propertyTypeEnum.DropdownSelect ||
		fieldInfo.dataType === propertyTypeEnum.Switch
	);
};

// 获取字段的值选项
const getFieldValueOptions = (action: ActionFormItem): ValueOption[] => {
	const params = getActionParams(action);
	const fieldKey = params.fieldPath;
	if (!fieldKey) return [];

	// 从所有分组中查找字段
	let fieldInfo: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		fieldInfo = group.fields.find((f) => f.key === fieldKey);
		if (fieldInfo) break;
	}

	if (!fieldInfo) return [];

	const options: ValueOption[] = [];

	if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
		if (fieldInfo.dropdownItems?.length) {
			fieldInfo.dropdownItems.forEach((item) => {
				options.push({
					label: item.value,
					value: item.value,
				});
			});
		}
	} else if (fieldInfo.dataType === propertyTypeEnum.Switch) {
		const trueLabel = fieldInfo.additionalInfo?.trueLabel || 'Yes';
		const falseLabel = fieldInfo.additionalInfo?.falseLabel || 'No';
		options.push({ label: trueLabel, value: 'true' }, { label: falseLabel, value: 'false' });
	}

	return options;
};

// 加载可用的 Action 定义
onMounted(async () => {
	await loadAvailableActions();
	await loadStaticFieldsMapping();
});

// 监听 stage 变化重新构建字段选项
watch(
	() => props.currentStageIndex,
	() => {
		loadStaticFieldsMapping();
	}
);
</script>

<style lang="scss" scoped>
.condition-action-form {
	@apply flex flex-col gap-4;
}

.actions-list {
	@apply flex flex-col gap-4;
}

.action-item {
	@apply p-4 rounded-lg border;
	background-color: var(--el-fill-color-lighter);
}

.action-header {
	@apply flex items-center justify-between mb-3;
}

.action-number {
	@apply text-sm font-medium text-primary;
}

.action-type-option {
	@apply flex flex-col;
}

.action-type-desc {
	@apply text-xs;
	color: var(--el-text-color-secondary);
}

.loop-warning {
	@apply flex items-center gap-1 mt-2 text-xs;
	color: var(--el-color-warning);
}
</style>
