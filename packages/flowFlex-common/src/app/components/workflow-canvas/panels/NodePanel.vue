<template>
	<el-drawer
		v-model="visible"
		:title="panelTitle"
		direction="rtl"
		:size="450"
		:close-on-click-modal="false"
		:destroy-on-close="false"
		class="node-panel stage-condition-editor"
	>
		<template #header>
			<div class="node-panel__header">
				<h3 class="node-panel__title">{{ panelTitle }}</h3>
				<p v-if="panelSubtitle" class="node-panel__subtitle">{{ panelSubtitle }}</p>
			</div>
		</template>

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
				:condition="conditionData"
				:stages="stages"
				:current-stage-index="currentStageIndex"
				:saving="saving"
				@save="handleSave"
				@cancel="handleCancel"
				@change="handleChange"
			/>

			<!-- 空状态 -->
			<div v-else class="node-panel__empty">
				<el-empty description="Select a node to view details" />
			</div>
		</div>
	</el-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
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
		return stageData.value?.name || 'Stage Details';
	}
	if (nodeType.value === 'condition') {
		return conditionData.value?.name || 'Condition Details';
	}
	return 'Node Details';
});

// 面板副标题
const panelSubtitle = computed((): string => {
	if (nodeType.value === 'stage') {
		return 'View stage information and manage conditions';
	}
	if (nodeType.value === 'condition') {
		return 'Configure condition rules and actions';
	}
	return '';
});

// 保存
const handleSave = (input: StageConditionInput) => {
	const stageId =
		nodeType.value === 'condition'
			? (props.selectedNode?.data as ConditionNodeData)?.stageId
			: stageData.value?.id;
	if (stageId) {
		emit('save', stageId, input);
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

.node-panel__content {
	height: 100%;
}

.node-panel__empty {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
}

.stage-condition-editor {
	.el-drawer__header {
		margin-bottom: 0px !important;
	}
}
</style>
