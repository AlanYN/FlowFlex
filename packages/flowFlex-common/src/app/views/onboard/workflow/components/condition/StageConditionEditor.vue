<template>
	<el-drawer
		v-model="visible"
		title="Edit Condition"
		direction="rtl"
		size="550px"
		:close-on-click-modal="false"
		:close-on-press-escape="false"
		class="stage-condition-editor"
		append-to-body
		:before-close="handleCancel"
		destroy-on-close
	>
		<template #header>
			<div class="drawer-header">
				<span class="drawer-title">Edit Condition</span>
				<span class="drawer-subtitle">
					Set up conditions to create dynamic workflow paths based on stage results
				</span>
			</div>
		</template>

		<div class="editor-body">
			<!-- Condition List -->
			<div class="conditions-list">
				<TransitionGroup name="condition-list" tag="div">
					<ConditionCard
						v-for="(condition, index) in localConditions"
						:key="condition.id"
						ref="cardRefs"
						:condition="condition"
						:stages="stages"
						:current-stage-id="currentStageId"
						:current-stage-index="currentStageIndex"
						:is-first="index === 0"
						:is-last="index === localConditions.length - 1"
						:initial-expanded="
							expandConditionId
								? condition.id === expandConditionId
								: condition.id.startsWith('temp-') || localConditions.length === 1
						"
						@update="(data) => handleConditionUpdate(index, data)"
						@delete="handleConditionDelete(index)"
						@move-up="handleMoveUp(index)"
						@move-down="handleMoveDown(index)"
					/>
				</TransitionGroup>
			</div>

			<!-- Add Condition Button -->
			<el-button
				class="add-condition-btn"
				:disabled="localConditions.length >= maxConditions"
				@click="handleAddCondition"
			>
				<el-icon><Plus /></el-icon>
				Add Condition
			</el-button>
			<div v-if="localConditions.length >= maxConditions" class="max-hint">
				Maximum {{ maxConditions }} conditions reached
			</div>

			<!-- Fallback Stage Section -->
			<div class="fallback-section">
				<div class="fallback-title">Fallback Stage</div>
				<div class="fallback-subtitle">
					Applies when none of the conditions above are met
				</div>

				<el-radio-group v-model="fallbackType" class="fallback-radio-group">
					<div
						class="fallback-option"
						:class="{ 'is-active': fallbackType === 'continue' }"
						@click="fallbackType = 'continue'"
					>
						<el-radio value="continue">
							<div class="option-content">
								<span class="option-title">Continue to next stage</span>
								<span class="option-desc">Proceed to the next stage in order</span>
							</div>
						</el-radio>
					</div>
					<div
						class="fallback-option"
						:class="{ 'is-active': fallbackType === 'jump' }"
						@click="fallbackType = 'jump'"
					>
						<el-radio value="jump">
							<div class="option-content">
								<span class="option-title">Jump to a chosen stage</span>
								<span class="option-desc">Go to a specific stage</span>
							</div>
						</el-radio>
						<el-select
							v-if="fallbackType === 'jump'"
							v-model="fallbackStageId"
							placeholder="Select stage"
							class="fallback-stage-select"
							@click.stop
						>
							<el-option
								v-for="stage in availableFallbackStages"
								:key="stage.id"
								:label="stage.name"
								:value="stage.id"
							/>
						</el-select>
					</div>
				</el-radio-group>
			</div>
		</div>

		<template #footer>
			<div class="editor-footer">
				<el-button @click="handleCancel">Cancel</el-button>
				<el-button type="primary" :loading="saving" @click="handleSave">
					{{ saving ? 'Saving...' : 'Save' }}
				</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus } from '@element-plus/icons-vue';
import type { Stage } from '#/onboard';
import type { StageCondition, RuleFormItem, ActionFormItem } from '#/condition';
import ConditionCard from './ConditionCard.vue';
import {
	getConditionsByStage,
	createCondition,
	updateCondition,
	deleteCondition,
	reorderConditions,
	updateConditionFallback,
} from '@/apis/ow';
import { getDefaultActionType } from '@/utils/actionTypeUtils';

// Props
const props = defineProps<{
	stages: Stage[];
	workflowId: string;
}>();

// Emits
const emit = defineEmits<{
	(e: 'save'): void;
	(e: 'cancel'): void;
}>();

// Constants
const maxConditions = 10;

// State
const visible = ref(false);
const saving = ref(false);
const currentStageId = ref('');
const currentStageIndex = ref(0);
const cardRefs = ref<InstanceType<typeof ConditionCard>[]>([]);
const expandConditionId = ref<string | null>(null); // which condition to auto-expand

// Condition list state
const localConditions = ref<StageCondition[]>([]);
const originalConditions = ref<StageCondition[]>([]); // snapshot for diff
const deletedConditionIds = ref<string[]>([]);

// Fallback state
const fallbackType = ref<'continue' | 'jump'>('continue');
const fallbackStageId = ref<string | undefined>(undefined);
const originalFallbackStageId = ref<string | null>(null);

// Computed
const availableFallbackStages = computed(() => {
	return props.stages.filter((s: any) => s.id !== currentStageId.value);
});

// Open drawer
const open = async (
	stageId: string,
	stageName: string,
	stageIndex: number,
	conditionId?: string
) => {
	currentStageId.value = stageId;
	currentStageIndex.value = stageIndex;
	deletedConditionIds.value = [];
	expandConditionId.value = conditionId || null;

	// Load existing conditions
	try {
		const res = await getConditionsByStage(stageId);
		const conditions = (res as any)?.data || res || [];
		localConditions.value = Array.isArray(conditions) ? [...conditions] : [];
		originalConditions.value = JSON.parse(JSON.stringify(localConditions.value));
	} catch {
		localConditions.value = [];
		originalConditions.value = [];
	}

	// Load stage fallback (from stage data)
	const currentStage = props.stages.find((s: any) => String(s.id) === String(stageId));
	const stageFallback = (currentStage as any)?.conditionFallbackStageId || null;
	originalFallbackStageId.value = stageFallback;
	if (stageFallback) {
		fallbackType.value = 'jump';
		fallbackStageId.value = String(stageFallback);
	} else {
		fallbackType.value = 'continue';
		fallbackStageId.value = undefined;
	}

	visible.value = true;
};

const close = () => {
	visible.value = false;
};

// Add new condition
const handleAddCondition = () => {
	const tempId = `temp-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
	const newCondition: StageCondition = {
		id: tempId, // temp ID for stable key, treated as "new" if starts with "temp-"
		stageId: currentStageId.value,
		name: '',
		description: '',
		order: localConditions.value.length,
		rulesJson: { logic: 'AND', rules: [] } as any,
		actionsJson: [] as any,
		isActive: true,
	};
	localConditions.value.push(newCondition);
};

// Handle condition update from card
const handleConditionUpdate = (
	index: number,
	data: {
		name: string;
		description: string;
		logic: string;
		rules: RuleFormItem[];
		actions: ActionFormItem[];
	}
) => {
	const condition = localConditions.value[index];
	if (condition) {
		condition.name = data.name;
		condition.description = data.description;
		condition.rulesJson = { logic: data.logic, rules: data.rules } as any;
		condition.actionsJson = data.actions as any;
	}
};

// Handle delete (local removal + track for API call)
const handleConditionDelete = (index: number) => {
	const condition = localConditions.value[index];
	if (condition.id && !condition.id.startsWith('temp-')) {
		deletedConditionIds.value.push(condition.id);
	}
	localConditions.value.splice(index, 1);
};

// Move condition up
const handleMoveUp = (index: number) => {
	if (index <= 0) return;
	const temp = localConditions.value[index];
	localConditions.value[index] = localConditions.value[index - 1];
	localConditions.value[index - 1] = temp;
};

// Move condition down
const handleMoveDown = (index: number) => {
	if (index >= localConditions.value.length - 1) return;
	const temp = localConditions.value[index];
	localConditions.value[index] = localConditions.value[index + 1];
	localConditions.value[index + 1] = temp;
};

// Build submit data for a single condition
const buildConditionPayload = (condition: StageCondition) => {
	const rulesJson =
		typeof condition.rulesJson === 'string'
			? condition.rulesJson
			: JSON.stringify(condition.rulesJson);
	const actionsJson =
		typeof condition.actionsJson === 'string'
			? condition.actionsJson
			: JSON.stringify(
					(condition.actionsJson || []).map((a: any, i: number) => ({ ...a, order: i }))
			  );

	return {
		stageId: currentStageId.value,
		workflowId: props.workflowId,
		name: condition.name,
		description: condition.description || undefined,
		rulesJson,
		actionsJson,
		isActive: condition.isActive ?? true,
	};
};

// Save all changes
const handleSave = async () => {
	// Validate all cards
	if (cardRefs.value && cardRefs.value.length > 0) {
		for (const card of cardRefs.value) {
			if (card) {
				const validation = await card.validate();
				if (!validation.valid) {
					ElMessage.error(validation.message);
					return;
				}
			}
		}
	}

	saving.value = true;

	try {
		// Phase 1: Delete removed conditions
		const deletePromises: Promise<any>[] = [];
		for (const id of deletedConditionIds.value) {
			deletePromises.push(deleteCondition(id));
		}
		if (deletePromises.length > 0) {
			await Promise.allSettled(deletePromises);
		}

		// Phase 2: Create new conditions / Update existing (need to be sequential for creates to get IDs back)
		const createUpdatePromises: Promise<any>[] = [];
		const newConditionIndices: number[] = []; // track which indices are new

		for (let i = 0; i < localConditions.value.length; i++) {
			const condition = localConditions.value[i];
			const payload = buildConditionPayload(condition);
			if (!condition.id || condition.id.startsWith('temp-')) {
				// New condition — create and store the returned ID
				newConditionIndices.push(i);
				createUpdatePromises.push(
					createCondition(payload).then((res: any) => {
						// Store the new ID back so we can reorder it
						if (res?.code === '200' && res?.data?.id) {
							localConditions.value[i].id = String(res.data.id);
						}
					})
				);
			} else {
				// Check if changed
				const original = originalConditions.value.find((c) => c.id === condition.id);
				if (
					original &&
					JSON.stringify(payload) !== JSON.stringify(buildConditionPayload(original))
				) {
					createUpdatePromises.push(updateCondition(condition.id, payload));
				}
			}
		}

		if (createUpdatePromises.length > 0) {
			const results = await Promise.allSettled(createUpdatePromises);
			const failed = results.filter((r) => r.status === 'rejected');
			if (failed.length > 0) {
				ElMessage.warning(`${failed.length} save operation(s) failed. Please try again.`);
				// Refresh and return
				const res = await getConditionsByStage(currentStageId.value);
				const conditions = (res as any)?.data || res || [];
				localConditions.value = Array.isArray(conditions) ? [...conditions] : [];
				originalConditions.value = JSON.parse(JSON.stringify(localConditions.value));
				deletedConditionIds.value = [];
				return;
			}
		}

		// Phase 3: Reorder — now all conditions have real IDs (temp- replaced after create)
		const conditionsWithIds = localConditions.value.filter(
			(c) => !!c.id && !c.id.startsWith('temp-')
		);
		if (conditionsWithIds.length > 1) {
			const reorderItems = conditionsWithIds.map((c, i) => ({ id: c.id, order: i }));
			await reorderConditions(currentStageId.value, reorderItems);
		}

		// Phase 4: Update fallback if changed
		const newFallback = fallbackType.value === 'jump' ? fallbackStageId.value || null : null;
		if (newFallback !== originalFallbackStageId.value) {
			await updateConditionFallback(currentStageId.value, newFallback);
		}

		ElMessage.success('Conditions saved successfully');
		emit('save');
		close();
	} catch (err: any) {
		ElMessage.error(err?.message || 'Failed to save conditions');
	} finally {
		saving.value = false;
	}
};

// Cancel
const handleCancel = () => {
	emit('cancel');
	close();
};

defineExpose({ open, close });
</script>

<style lang="scss">
.stage-condition-editor {
	.el-drawer__header {
		margin-bottom: 0px !important;
		align-items: start;
	}

	.el-drawer__body {
		text-align: left;
	}
}
</style>

<style lang="scss" scoped>
.drawer-header {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.drawer-title {
	font-size: 16px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.drawer-subtitle {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.editor-body {
	padding: 0 4px;
}

.conditions-list {
	min-height: 60px;
	display: flex;
	flex-direction: column;
	align-items: stretch;
}

/* FLIP animation for condition reorder */
.condition-list-move {
	transition: transform 0.35s cubic-bezier(0.22, 1, 0.36, 1);
}

.condition-list-enter-active {
	transition: all 0.3s ease-out;
}

.condition-list-leave-active {
	transition: all 0.2s ease-in;
	position: absolute;
	width: 100%;
}

.condition-list-enter-from {
	opacity: 0;
	transform: translateY(-10px);
}

.condition-list-leave-to {
	opacity: 0;
	transform: translateX(20px);
}

.add-condition-btn {
	width: 100%;
	margin: 8px 0 24px;
	border-style: dashed;
}

.max-hint {
	text-align: center;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-top: -16px;
	margin-bottom: 24px;
}

.fallback-section {
	border-top: 1px solid var(--el-border-color-lighter);
	padding-top: 20px;
}

.fallback-title {
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 4px;
}

.fallback-subtitle {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-bottom: 16px;
}

.fallback-radio-group {
	display: flex;
	flex-direction: column;
	gap: 12px;
	width: 100%;
	align-items: flex-start;
}

.fallback-option {
	border: 1px solid var(--el-border-color);
	border-radius: 8px;
	padding: 12px 16px;
	cursor: pointer;
	transition: all 0.2s;
	width: 100%;

	&.is-active {
		border-color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
	}

	&:hover {
		border-color: var(--el-color-primary-light-3);
	}
}

.option-content {
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.option-title {
	font-size: 14px;
	font-weight: 500;
}

.option-desc {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.fallback-stage-select {
	margin-top: 8px;
	margin-left: 24px;
	width: calc(100% - 24px);
}

.editor-footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding: 16px 20px;
}
</style>
