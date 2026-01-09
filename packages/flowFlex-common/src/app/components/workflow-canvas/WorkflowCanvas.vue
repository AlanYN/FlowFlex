<template>
	<div class="workflow-canvas">
		<VueFlow
			ref="vueFlowRef"
			v-model:nodes="nodes"
			v-model:edges="edges"
			:node-types="nodeTypes"
			:default-viewport="{ x: 0, y: 0, zoom: 1 }"
			:min-zoom="0.25"
			:max-zoom="2"
			:fit-view-on-init="true"
			:nodes-draggable="true"
			:nodes-connectable="false"
			:edges-updatable="false"
			class="workflow-canvas__flow"
			@node-click="handleNodeClick"
			@pane-click="handlePaneClick"
			@viewport-change="handleViewportChange"
		>
			<!-- 背景 -->
			<Background :gap="20" :size="1" />

			<!-- 小地图 -->
			<MiniMap
				v-if="showMinimap"
				:node-color="getMinimapNodeColor"
				:mask-color="minimapMaskColor"
				class="workflow-canvas__minimap"
			/>
		</VueFlow>

		<!-- 加载状态 -->
		<div v-if="loading" class="workflow-canvas__loading">
			<el-icon class="is-loading" :size="32"><Loading /></el-icon>
			<span>Loading workflow...</span>
		</div>

		<!-- 错误状态 -->
		<div v-if="error" class="workflow-canvas__error">
			<el-icon :size="48" color="var(--el-color-danger)"><WarningFilled /></el-icon>
			<p>{{ error }}</p>
			<el-button type="primary" @click="$emit('retry')">Retry</el-button>
		</div>

		<!-- 空状态 -->
		<div v-if="!loading && !error && nodes.length === 0" class="workflow-canvas__empty">
			<el-empty description="No stages in this workflow" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, markRaw } from 'vue';
import { VueFlow, useVueFlow } from '@vue-flow/core';
import { Background } from '@vue-flow/background';
import { MiniMap } from '@vue-flow/minimap';
import type { Node, Edge, ViewportTransform, NodeMouseEvent } from '@vue-flow/core';
import { Loading, WarningFilled } from '@element-plus/icons-vue';
import StageNode from './nodes/StageNode.vue';
import ConditionNode from './nodes/ConditionNode.vue';
import type { CanvasNodeData } from '#/workflow-canvas';

// 注册自定义节点类型
const nodeTypes: Record<string, any> = {
	stage: markRaw(StageNode),
	condition: markRaw(ConditionNode),
};

interface Props {
	nodes: Node<CanvasNodeData>[];
	edges: Edge[];
	loading?: boolean;
	error?: string | null;
	showMinimap?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	loading: false,
	error: null,
	showMinimap: true,
});

const emit = defineEmits<{
	(e: 'update:nodes', nodes: Node<CanvasNodeData>[]): void;
	(e: 'update:edges', edges: Edge[]): void;
	(e: 'node-click', node: Node<CanvasNodeData>): void;
	(e: 'pane-click'): void;
	(e: 'viewport-change', viewport: ViewportTransform): void;
	(e: 'retry'): void;
}>();

// Vue Flow 实例
const { zoomIn, zoomOut, fitView, setViewport, getViewport } = useVueFlow();

// 双向绑定 nodes
const nodes = computed({
	get: () => props.nodes,
	set: (val) => emit('update:nodes', val),
});

// 双向绑定 edges
const edges = computed({
	get: () => props.edges,
	set: (val) => emit('update:edges', val),
});

// 小地图遮罩颜色
const minimapMaskColor = computed(() => {
	return document.documentElement.classList.contains('dark')
		? 'rgba(0, 0, 0, 0.6)'
		: 'rgba(255, 255, 255, 0.6)';
});

// 获取小地图节点颜色
const getMinimapNodeColor = (node: Node): string => {
	if (node.type === 'stage') {
		const data = node.data as any;
		return data?.stage?.color || 'var(--el-color-primary)';
	}
	return 'var(--el-color-primary-light-5)';
};

// 节点点击
const handleNodeClick = (event: NodeMouseEvent) => {
	emit('node-click', event.node as Node<CanvasNodeData>);
};

// 画布空白区域点击
const handlePaneClick = () => {
	emit('pane-click');
};

// 视口变化
const handleViewportChange = (viewport: ViewportTransform) => {
	emit('viewport-change', viewport);
};

// 暴露方法给父组件
defineExpose({
	zoomIn,
	zoomOut,
	fitView,
	setViewport,
	getViewport,
});
</script>

<style scoped>
.workflow-canvas {
	position: relative;
	width: 100%;
	height: 100%;
	background: var(--el-bg-color-page);
}

.workflow-canvas__flow {
	width: 100%;
	height: 100%;
}

.workflow-canvas__minimap {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
}

.workflow-canvas__loading,
.workflow-canvas__error,
.workflow-canvas__empty {
	position: absolute;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 16px;
	color: var(--el-text-color-secondary);
}

.workflow-canvas__error p {
	margin: 0;
	font-size: 14px;
}

/* Vue Flow 样式覆盖 */
:deep(.vue-flow__node) {
	padding: 0;
	border: none;
	background: transparent;
}

:deep(.vue-flow__edge-path) {
	stroke-width: 2;
}

:deep(.vue-flow__edge-text) {
	font-size: 11px;
}

:deep(.vue-flow__background) {
	background: var(--el-bg-color-page);
}

/* 深色模式 */
html.dark .workflow-canvas {
	background: var(--el-bg-color-page);
}

html.dark .workflow-canvas__minimap {
	background: var(--el-bg-color);
	border-color: var(--el-border-color-darker);
}
</style>

<!-- 全局样式（Vue Flow 需要） -->
<style>
@import '@vue-flow/core/dist/style.css';
@import '@vue-flow/core/dist/theme-default.css';
/* @import '@vue-flow/background/dist/style.css'; */
@import '@vue-flow/minimap/dist/style.css';
@import '@vue-flow/controls/dist/style.css';
</style>
