<template>
	<el-drawer
		v-model="visible"
		:title="panelTitle"
		direction="rtl"
		:size="450"
		:close-on-click-modal="false"
		:destroy-on-close="false"
		class="node-panel"
	>
		<template #header>
			<div class="node-panel__header">
				<h3 class="node-panel__title">{{ panelTitle }}</h3>
				<p v-if="panelSubtitle" class="node-panel__subtitle">{{ panelSubtitle }}</p>
			</div>
		</template>

		<el-scrollbar class="node-panel__scrollbar">
			<div class="node-panel__content">
				<!-- Stage 面板 -->
				<StagePanelView
					v-if="nodeType === 'stage'"
					:stage="stageData"
					:has-condition="hasCondition"
					@add-condition="$emit('add-condition')"
				/>

				<!-- Condition 面板 -->
				<ConditionPanelView
					v-else-if="nodeType === 'condition'"
					ref="conditionPanelRef"
					:condition="conditionData"
					:stages="stages"
					:current-stage-index="currentStageIndex"
					@change="handleChange"
				/>

				<!-- 空状态 -->
				<div v-else class="node-panel__empty">
					<el-empty description="Select a node to view details" />
				</div>
			</div>
		</el-scrollbar>

		<!-- Condition 面板的 Footer -->
		<template v-if="nodeType === 'condition'" #footer>
			<div class="node-panel__footer">
				<el-button @click="handleCancel">Cancel</el-button>
				<el-button type="primary" :loading="saving" @click="handleSaveClick">
					{{ saving ? 'Saving...' : 'Save' }}
				</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Node } from '@vue-flow/core';
import type { Stage } from '#/onboard';
import type { StageCondition, StageConditionInput } from '#/condition';
import type { StageNodeData, ConditionNodeData, CanvasNodeData } from '#/workflow-canvas';
import StagePanelView from './StagePanelView.vue';
import ConditionPanelView from './ConditionPanelView.vue';

interface Props {
	modelValue: boolean;
	selectedNode: Node<CanvasNodeData> | null;
	stages: Stage[];
	saving?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	saving: false,
});

const emit = defineEmits<{
	(e: 'update:modelValue', value: boolean): void;
	(e: 'save', stageId: string, input: StageConditionInput): void;
	(e: 'cancel'): void;
	(e: 'add-condition'): void;
	(e: 'change'): void;
}>();

// Refs
const conditionPanelRef = ref<InstanceType<typeof ConditionPanelView> | null>(null);

// 双向绑定
const visible = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

// 节点类型
const nodeType = computed((): 'stage' | 'condition' | null => {
	return props.selectedNode?.data?.type || null;
});

// Stage 数据
const stageData = computed((): Stage | null => {
	if (nodeType.value !== 'stage') return null;
	return (props.selectedNode?.data as StageNodeData)?.stage || null;
});

// Condition 数据
const conditionData = computed((): StageCondition | null => {
	if (nodeType.value !== 'condition') return null;
	return (props.selectedNode?.data as ConditionNodeData)?.condition || null;
});

// 是否有 Condition
const hasCondition = computed((): boolean => {
	if (nodeType.value !== 'stage') return false;
	return (props.selectedNode?.data as StageNodeData)?.hasCondition || false;
});

// 当前 Stage 索引
const currentStageIndex = computed((): number => {
	if (nodeType.value === 'stage') {
		return (props.selectedNode?.data as StageNodeData)?.index || 0;
	}
	if (nodeType.value === 'condition') {
		const stageId = (props.selectedNode?.data as ConditionNodeData)?.stageId;
		return props.stages.findIndex((s) => s.id === stageId);
	}
	return 0;
});

// 面板标题
const panelTitle = computed((): string => {
	if (nodeType.value === 'stage') {
		return 'Stage Details';
	}
	if (nodeType.value === 'condition') {
		return 'Edit Condition';
	}
	return 'Node Details';
});

// 面板副标题
const panelSubtitle = computed((): string => {
	if (nodeType.value === 'stage') {
		return 'View stage information and manage conditions';
	}
	if (nodeType.value === 'condition') {
		return 'Set up conditions to create dynamic workflow paths based on stage results';
	}
	return '';
});

// 保存按钮点击
const handleSaveClick = async () => {
	if (conditionPanelRef.value) {
		const submitData = await conditionPanelRef.value.validateAndGetData();
		if (submitData) {
			const stageId = (props.selectedNode?.data as ConditionNodeData)?.stageId;
			if (stageId) {
				emit('save', stageId, submitData);
			}
		}
	}
};

// 取消
const handleCancel = () => {
	emit('cancel');
};

// 数据变更
const handleChange = () => {
	emit('change');
};
</script>

<style lang="scss">
.node-panel {
	.el-drawer__header {
		margin-bottom: 0 !important;
		align-items: start;
	}

	.el-drawer__body {
		display: flex;
		flex-direction: column;
		padding: 0;
		overflow: hidden;
	}

	.el-drawer__footer {
		border-top: 1px solid var(--el-border-color-lighter);
		padding: 16px 20px;
	}
}
</style>

<style scoped>
.node-panel__header {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.node-panel__title {
	font-size: 16px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 0;
}

.node-panel__subtitle {
	font-size: 13px;
	color: var(--el-text-color-secondary);
	margin: 0;
}

.node-panel__scrollbar {
	flex: 1;
	overflow: hidden;
}

.node-panel__content {
	padding: 16px 20px;
}

.node-panel__empty {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
}

.node-panel__footer {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
}
</style>
