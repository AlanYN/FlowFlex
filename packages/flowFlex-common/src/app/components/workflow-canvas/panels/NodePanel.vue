<template>
	<el-drawer
		v-model="visible"
		:title="panelTitle"
		direction="rtl"
		:size="450"
		:close-on-click-modal="false"
		:destroy-on-close="false"
		class="node-panel"
		append-to-body
		:before-close="handleCancel"
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

				<!-- 空状态 -->
				<div v-else class="node-panel__empty">
					<el-empty description="Select a node to view details" />
				</div>
			</div>
		</el-scrollbar>
	</el-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Node } from '@vue-flow/core';
import type { Stage } from '#/onboard';
import type { StageNodeData, CanvasNodeData } from '#/workflow-canvas';
import StagePanelView from './StagePanelView.vue';

interface Props {
	modelValue: boolean;
	selectedNode: Node<CanvasNodeData> | null;
	stages: Stage[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	(e: 'update:modelValue', value: boolean): void;
	(e: 'cancel'): void;
	(e: 'add-condition'): void;
}>();

// 双向绑定
const visible = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

// 节点类型
const nodeType = computed((): 'stage' | null => {
	const type = props.selectedNode?.data?.type;
	return type === 'stage' ? 'stage' : null;
});

// Stage 数据
const stageData = computed((): Stage | null => {
	if (nodeType.value !== 'stage') return null;
	return (props.selectedNode?.data as StageNodeData)?.stage || null;
});

// 是否有 Condition
const hasCondition = computed((): boolean => {
	if (nodeType.value !== 'stage') return false;
	return (props.selectedNode?.data as StageNodeData)?.hasCondition || false;
});

// 面板标题
const panelTitle = computed((): string => {
	if (nodeType.value === 'stage') {
		return 'Stage Details';
	}
	return 'Node Details';
});

// 面板副标题
const panelSubtitle = computed((): string => {
	if (nodeType.value === 'stage') {
		return 'View stage information and manage conditions';
	}
	return '';
});

// 取消
const handleCancel = () => {
	emit('cancel');
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
</style>
