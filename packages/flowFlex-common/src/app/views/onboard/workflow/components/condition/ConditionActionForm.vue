<template>
	<div class="condition-action-form">
		<!-- 动作列表 -->
		<div class="actions-list">
			<div v-for="(action, index) in modelValue" :key="action._id" class="action-item">
				<div class="action-header">
					<span class="action-number">Action {{ index + 1 }}</span>
					<el-button type="danger" link size="small" @click="handleRemoveAction(index)">
						<el-icon><Delete /></el-icon>
					</el-button>
				</div>

				<div class="action-fields">
					<!-- Action Type -->
					<el-form-item label="Action Type" class="action-field">
						<el-select
							:model-value="action.type"
							placeholder="Select action type"
							@update:model-value="(val) => handleActionTypeChange(index, val)"
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
					<el-form-item
						v-if="action.type === 'GoToStage'"
						label="Target Stage"
						class="action-field"
					>
						<el-select
							:model-value="action.targetStageId"
							placeholder="Select target stage"
							@update:model-value="(val) => updateAction(index, 'targetStageId', val)"
						>
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
						<el-select
							:model-value="action.actionDefinitionId"
							placeholder="Select action"
							@update:model-value="
								(val) => updateAction(index, 'actionDefinitionId', val)
							"
						>
							<el-option
								v-for="act in availableActions"
								:key="act.id"
								:label="act.name"
								:value="act.id"
							/>
						</el-select>
					</el-form-item>

					<!-- SendNotification: Recipient -->
					<template v-if="action.type === 'SendNotification'">
						<el-form-item label="Recipient Type" class="action-field">
							<el-select
								:model-value="action.parameters?.recipientType || 'user'"
								placeholder="Select recipient type"
								@update:model-value="
									(val) => updateActionParam(index, 'recipientType', val)
								"
							>
								<el-option value="user" label="User" />
								<el-option value="team" label="Team" />
								<el-option value="email" label="Email" />
							</el-select>
						</el-form-item>
					</template>

					<!-- UpdateField: Field and Value -->
					<template v-if="action.type === 'UpdateField'">
						<el-form-item label="Field Path" class="action-field">
							<el-input
								:model-value="action.parameters?.fieldPath || ''"
								placeholder="Enter field path"
								@update:model-value="
									(val) => updateActionParam(index, 'fieldPath', val)
								"
							/>
						</el-form-item>
						<el-form-item label="New Value" class="action-field">
							<el-input
								:model-value="action.parameters?.newValue || ''"
								placeholder="Enter new value"
								@update:model-value="
									(val) => updateActionParam(index, 'newValue', val)
								"
							/>
						</el-form-item>
					</template>

					<!-- AssignUser: Assignee -->
					<template v-if="action.type === 'AssignUser'">
						<el-form-item label="Assignee Type" class="action-field">
							<el-select
								:model-value="action.parameters?.assigneeType || 'user'"
								placeholder="Select assignee type"
								@update:model-value="
									(val) => updateActionParam(index, 'assigneeType', val)
								"
							>
								<el-option value="user" label="User" />
								<el-option value="team" label="Team" />
							</el-select>
						</el-form-item>
					</template>
				</div>
			</div>
		</div>

		<!-- 添加动作按钮 -->
		<el-button type="primary" link @click="handleAddAction" class="add-action-btn">
			<el-icon class="mr-1"><Plus /></el-icon>
			Add Action
		</el-button>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Plus, Delete, Warning } from '@element-plus/icons-vue';
import type { ActionFormItem, ConditionActionType } from '#/condition';
import type { Stage } from '#/onboard';
import { getAvailableActions } from '@/apis/ow';

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

// 可用的 Action 定义
const availableActions = ref<Array<{ id: string; name: string }>>([]);

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

// 生成唯一 ID
const generateId = () => `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

// 检查是否会产生循环
const isLoopWarning = (targetStageId?: string) => {
	if (!targetStageId) return false;
	const targetIndex = props.stages.findIndex((s) => s.id === targetStageId);
	return targetIndex !== -1 && targetIndex <= props.currentStageIndex;
};

// 更新动作字段
const updateAction = (index: number, field: keyof ActionFormItem, value: any) => {
	const newActions = [...props.modelValue];
	newActions[index] = { ...newActions[index], [field]: value };
	emit('update:modelValue', newActions);
};

// 更新动作参数
const updateActionParam = (index: number, paramKey: string, value: any) => {
	const newActions = [...props.modelValue];
	newActions[index] = {
		...newActions[index],
		parameters: {
			...newActions[index].parameters,
			[paramKey]: value,
		},
	};
	emit('update:modelValue', newActions);
};

// 处理动作类型变化
const handleActionTypeChange = (index: number, val: ConditionActionType) => {
	const newActions = [...props.modelValue];
	newActions[index] = {
		...newActions[index],
		type: val,
		targetStageId: undefined,
		actionDefinitionId: undefined,
		parameters: {},
	};
	emit('update:modelValue', newActions);
};

// 添加动作
const handleAddAction = () => {
	const newAction: ActionFormItem = {
		_id: generateId(),
		type: 'GoToStage',
		order: props.modelValue.length,
	};
	emit('update:modelValue', [...props.modelValue, newAction]);
};

// 删除动作
const handleRemoveAction = (index: number) => {
	const newActions = props.modelValue.filter((_, i) => i !== index);
	emit('update:modelValue', newActions);
};

// 加载可用的 Action 定义
onMounted(async () => {
	try {
		const actions = await getAvailableActions();
		availableActions.value = actions || [];
	} catch (error) {
		console.error('Failed to load available actions:', error);
	}
});
</script>

<style lang="scss" scoped>
.condition-action-form {
	@apply flex flex-col gap-4;
}

.actions-list {
	@apply flex flex-col gap-4;
}

.action-item {
	@apply p-4 rounded-lg border bg-black-400;
}

.action-header {
	@apply flex items-center justify-between mb-3;
}

.action-number {
	@apply text-sm font-medium;
	color: var(--el-text-color-primary);
}

.action-fields {
	@apply flex flex-col gap-3;
}

.action-field {
	@apply mb-0;

	:deep(.el-form-item__label) {
		@apply text-xs mb-1;
		color: var(--el-text-color-secondary);
	}
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

.add-action-btn {
	@apply self-start;
}
</style>
