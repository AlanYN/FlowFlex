<template>
	<div class="condition-card" :class="{ 'is-expanded': expanded, 'is-editing': expanded }">
		<!-- Collapsed Header -->
		<div class="card-header" @click="toggleExpand">
			<div class="header-left">
				<el-icon class="condition-icon" :size="20">
					<Connection />
				</el-icon>
				<div class="header-info">
					<span class="condition-name">{{ condition.name || 'Untitled condition' }}</span>
					<span class="condition-meta">
						{{ ruleCount }} rule{{ ruleCount !== 1 ? 's' : '' }} ·
						{{ actionCount }} action{{ actionCount !== 1 ? 's' : '' }}
					</span>
				</div>
			</div>
			<div class="header-actions" @click.stop>
				<el-tooltip
					content="Move up (higher priority)"
					placement="top"
					:show-after="500"
					:disabled="isFirst"
				>
					<el-button
						:icon="Top"
						size="small"
						text
						:disabled="isFirst"
						@click="$emit('moveUp')"
					/>
				</el-tooltip>
				<el-tooltip
					content="Move down (lower priority)"
					placement="top"
					:show-after="500"
					:disabled="isLast"
				>
					<el-button
						:icon="Bottom"
						size="small"
						text
						:disabled="isLast"
						@click="$emit('moveDown')"
					/>
				</el-tooltip>
				<el-icon
					class="expand-icon"
					:class="{ 'is-expanded': expanded }"
					@click="toggleExpand"
				>
					<ArrowRight />
				</el-icon>
				<el-tooltip content="Delete condition" placement="top" :show-after="500">
					<el-button
						:icon="Delete"
						size="small"
						text
						type="danger"
						@click="handleDelete"
					/>
				</el-tooltip>
			</div>
		</div>

		<!-- Expanded Content -->
		<el-collapse-transition>
			<div v-show="expanded" class="card-content">
				<el-form
					ref="formRef"
					:model="localData"
					:rules="formRules"
					label-position="top"
					@submit.prevent
				>
					<!-- Basic Information -->
					<el-form-item label="Condition Name" prop="name">
						<el-input
							v-model="localData.name"
							placeholder="Enter condition name"
							maxlength="100"
							show-word-limit
						/>
					</el-form-item>
					<el-form-item label="Description" prop="description">
						<el-input
							v-model="localData.description"
							type="textarea"
							placeholder="Enter description (optional)"
							:rows="2"
							maxlength="500"
							show-word-limit
						/>
					</el-form-item>

					<!-- Condition Rules -->
					<div class="section-title">Condition Rules</div>
					<ConditionRuleForm
						ref="ruleFormRef"
						v-model="localData.rules"
						v-model:logic="localData.logic"
						:stages="availableSourceStages"
						:current-stage-index="currentStageIndex"
					/>

					<!-- Actions -->
					<div class="section-title">Actions</div>
					<ConditionActionForm
						ref="actionFormRef"
						v-model="localData.actions"
						:stages="stages"
						:current-stage-index="currentStageIndex"
						:currentStageId="currentStageId"
					/>
				</el-form>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, reactive } from 'vue';
import { ElMessageBox, ElCollapseTransition } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { Top, Bottom, Delete, Connection, ArrowRight } from '@element-plus/icons-vue';
import type { Stage } from '#/onboard';
import type { StageCondition, RuleFormItem, ActionFormItem } from '#/condition';
import ConditionRuleForm from './ConditionRuleForm.vue';
import ConditionActionForm from './ConditionActionForm.vue';
import { getDefaultActionType } from '@/utils/actionTypeUtils';

const props = defineProps<{
	condition: StageCondition;
	stages: Stage[];
	currentStageId: string;
	currentStageIndex: number;
	isFirst: boolean;
	isLast: boolean;
	initialExpanded?: boolean;
}>();

const emit = defineEmits<{
	(
		e: 'update',
		data: {
			name: string;
			description: string;
			logic: string;
			rules: RuleFormItem[];
			actions: ActionFormItem[];
		}
	): void;
	(e: 'delete'): void;
	(e: 'moveUp'): void;
	(e: 'moveDown'): void;
}>();

// State
const expanded = ref(props.initialExpanded ?? false);
const formRef = ref<FormInstance>();
const ruleFormRef = ref<InstanceType<typeof ConditionRuleForm>>();
const actionFormRef = ref<InstanceType<typeof ConditionActionForm>>();

// Local editable data
const localData = reactive({
	name: '',
	description: '',
	logic: 'AND' as 'AND' | 'OR',
	rules: [] as RuleFormItem[],
	actions: [] as ActionFormItem[],
});

// Initialize local data from condition prop
const initLocalData = () => {
	localData.name = props.condition.name || '';
	localData.description = props.condition.description || '';

	// Parse rulesJson
	try {
		const rulesData =
			typeof props.condition.rulesJson === 'string'
				? JSON.parse(props.condition.rulesJson)
				: props.condition.rulesJson;
		localData.logic = rulesData?.logic || 'AND';
		localData.rules = (rulesData?.rules || []).map((rule: any) => ({ ...rule }));
	} catch {
		localData.logic = 'AND';
		localData.rules = [];
	}

	// Parse actionsJson
	let actionsData = props.condition.actionsJson;
	if (typeof actionsData === 'string') {
		try {
			actionsData = JSON.parse(actionsData);
		} catch {
			actionsData = [];
		}
	}
	localData.actions = (actionsData || []).map((action: any) => ({ ...action }));

	// Ensure at least one rule and action
	if (localData.rules.length === 0) {
		localData.rules = [
			{
				sourceStageId: props.currentStageId,
				componentType: '',
				fieldPath: '',
				operator: '==',
				value: '',
			},
		];
	}
	if (localData.actions.length === 0) {
		localData.actions = [
			{
				type: getDefaultActionType(props.currentStageIndex, props.stages.length, []),
				targetStageId: '',
				order: 0,
			},
		];
	}
};

initLocalData();

// Flag to prevent emit → prop update → re-init loop
let isUpdatingFromParent = false;

// Watch for external condition changes (e.g., after save refreshes data from API)
// Only re-init when the condition ID changes (meaning a fresh load), not on every prop mutation
watch(
	() => props.condition.id,
	(newId, oldId) => {
		if (newId !== oldId) {
			isUpdatingFromParent = true;
			initLocalData();
			isUpdatingFromParent = false;
		}
	}
);

// Emit updates when local data changes (but not when re-initializing from parent)
watch(
	localData,
	() => {
		if (!isUpdatingFromParent) {
			emit('update', {
				name: localData.name,
				description: localData.description,
				logic: localData.logic,
				rules: localData.rules,
				actions: localData.actions,
			});
		}
	},
	{ deep: true }
);

// Computed
const ruleCount = computed(() => localData.rules.length);
const actionCount = computed(() => localData.actions.length);

const availableSourceStages = computed(() => {
	return props.stages.slice(0, props.currentStageIndex + 1);
});

// Form rules
const formRules: FormRules = {
	name: [
		{ required: true, message: 'Please enter condition name', trigger: 'blur' },
		{ max: 100, message: 'Name must be less than 100 characters', trigger: 'blur' },
	],
};

// Methods
const toggleExpand = () => {
	expanded.value = !expanded.value;
};

const handleDelete = async () => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this condition? This action cannot be undone after saving.',
			'Delete Condition',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
				confirmButtonClass: 'el-button--danger',
			}
		);
		emit('delete');
	} catch {
		// cancelled
	}
};

// Validate this card's form
const validate = async (): Promise<{ valid: boolean; message: string }> => {
	if (!expanded.value) {
		// If collapsed, just check name exists
		if (!localData.name.trim()) {
			return {
				valid: false,
				message: `Condition "${localData.name || 'Untitled'}" has no name`,
			};
		}
		return { valid: true, message: '' };
	}

	const [formValid, ruleValidation, actionValidation] = await Promise.all([
		formRef.value
			?.validate()
			.then(() => true)
			.catch(() => false) ?? true,
		ruleFormRef.value?.validate() ?? Promise.resolve({ valid: true, message: '' }),
		actionFormRef.value?.validate() ?? Promise.resolve({ valid: true, message: '' }),
	]);

	if (!formValid)
		return {
			valid: false,
			message: `Condition "${localData.name || 'Untitled'}" has invalid fields`,
		};
	if (!ruleValidation.valid) return { valid: false, message: ruleValidation.message };
	if (!actionValidation.valid) return { valid: false, message: actionValidation.message };

	return { valid: true, message: '' };
};

defineExpose({ validate, expanded });
</script>

<style lang="scss" scoped>
.condition-card {
	border: 1px solid var(--el-border-color);
	border-radius: 8px;
	margin-bottom: 12px;
	transition: all 0.2s;
	background: var(--el-bg-color);
	width: 100%;

	&.is-expanded {
		border-color: var(--el-color-primary);
		box-shadow: 0 0 0 1px var(--el-color-primary-light-5);
	}

	&:hover {
		border-color: var(--el-color-primary-light-3);
	}
}

.card-header {
	display: flex;
	flex-direction: row;
	align-items: center;
	justify-content: space-between;
	padding: 12px 16px;
	cursor: pointer;
	user-select: none;
	width: 100%;
}

.header-left {
	display: flex;
	flex-direction: row;
	align-items: center;
	gap: 12px;
	text-align: left;
	flex: 1;
	min-width: 0;
}

.condition-icon {
	color: var(--el-color-primary);
	flex-shrink: 0;
}

.header-info {
	display: flex;
	flex-direction: column;
	gap: 2px;
	align-items: flex-start;
}

.condition-name {
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-primary);
	text-align: left;
}

.condition-meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.header-actions {
	display: flex;
	flex-direction: row;
	align-items: center;
	gap: 4px;
	flex-shrink: 0;
}

.expand-icon {
	transition: transform 0.2s;
	cursor: pointer;
	color: var(--el-text-color-secondary);
	font-size: 14px;
	padding: 4px;

	&.is-expanded {
		transform: rotate(90deg);
	}
}

.card-content {
	padding: 0 16px 16px;
	border-top: 1px solid var(--el-border-color-lighter);
}

.section-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 16px 0 8px;
}
</style>
