<template>
	<div class="condition-editor-page">
		<!-- 工具栏 -->
		<CanvasToolbar
			:title="store.workflow?.name || 'Workflow Conditions'"
			:zoom-percent="store.getZoomPercent"
			:has-unsaved-changes="store.hasUnsavedChanges"
			@back="handleBack"
			@zoom-in="handleZoomIn"
			@zoom-out="handleZoomOut"
			@fit-view="handleFitView"
		/>

		<!-- 加载状态 -->
		<div v-if="store.loading" class="conditionLoading-container">
			<el-skeleton :rows="10" animated />
		</div>

		<!-- 错误状态 -->
		<div v-else-if="store.error" class="error-container">
			<el-result icon="error" :title="store.error">
				<template #extra>
					<el-button type="primary" @click="handleRetry">Retry</el-button>
					<el-button @click="handleBack">Back to Workflows</el-button>
				</template>
			</el-result>
		</div>

		<!-- 主内容 -->
		<template v-else>
			<!-- 画布区域 -->
			<WorkflowCanvas
				ref="canvasRef"
				:nodes="store.nodes"
				:edges="store.edges"
				@node-click="handleNodeClick"
				@pane-click="handlePaneClick"
				@viewport-change="handleViewportChange"
			/>

			<!-- 侧边面板 -->
			<NodePanel
				v-model="store.panelVisible"
				:selected-node="store.getSelectedNode"
				:stages="store.stages"
				:saving="store.saving"
				@save="handleSave"
				@cancel="handleCancel"
				@add-condition="handleAddCondition"
				@change="handleChange"
			/>

			<!-- 新建 Condition 编辑器 -->
			<StageConditionEditor
				ref="conditionEditorRef"
				:stages="store.stages"
				:workflow-id="workflowId"
				@save="handleConditionSaved"
			/>
		</template>
	</div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, useTemplateRef } from 'vue';
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router';
import { ElMessageBox } from 'element-plus';
import type { ViewportTransform, Node } from '@vue-flow/core';
import { useWorkflowCanvasStore } from '@/stores/modules/workflowCanvas';
import { WorkflowCanvas, NodePanel, CanvasToolbar } from '@/components/workflow-canvas';
import StageConditionEditor from './components/condition/StageConditionEditor.vue';
import type { StageConditionInput } from '#/condition';
import type { StageNodeData, ConditionNodeData, CanvasNodeData } from '#/workflow-canvas';

// Route
const route = useRoute();
const router = useRouter();
const workflowId = route.params.workflowId as string;

// Store
const store = useWorkflowCanvasStore();

// Refs
const conditionEditorRef = useTemplateRef('conditionEditorRef');
const canvasRef = useTemplateRef('canvasRef');

// 初始化
onMounted(async () => {
	if (workflowId) {
		await store.initCanvas(workflowId);
	}
});

// 清理
onBeforeUnmount(() => {
	store.resetState();
});

// 路由离开前检查
onBeforeRouteLeave(async (to, from, next) => {
	if (store.hasUnsavedChanges) {
		try {
			await ElMessageBox.confirm(
				'You have unsaved changes. Are you sure you want to leave?',
				'Unsaved Changes',
				{
					confirmButtonText: 'Leave',
					cancelButtonText: 'Stay',
					type: 'warning',
				}
			);
			next();
		} catch {
			next(false);
		}
	} else {
		next();
	}
});

// 节点点击
const handleNodeClick = (node: Node<CanvasNodeData>) => {
	store.selectNode(node.id);
};

// 画布空白区域点击
const handlePaneClick = () => {
	store.closePanel();
};

// 视口变化
const handleViewportChange = (viewport: ViewportTransform) => {
	store.setZoom(viewport.zoom);
};

// 放大
const handleZoomIn = () => {
	canvasRef.value?.zoomIn();
};

// 缩小
const handleZoomOut = () => {
	canvasRef.value?.zoomOut();
};

// 适配视图
const handleFitView = () => {
	canvasRef.value?.fitView();
};

// 返回
const handleBack = () => {
	router.push('/onboard/onboardWorkflow');
};

// 重试
const handleRetry = () => {
	if (workflowId) {
		store.initCanvas(workflowId);
	}
};

// 保存 Condition
const handleSave = async (stageId: string, input: StageConditionInput) => {
	const selectedNode = store.getSelectedNode;
	if (!selectedNode) return;

	if (selectedNode.data?.type === 'condition') {
		// 更新现有 Condition
		const conditionData = selectedNode.data as ConditionNodeData;
		const success = await store.updateConditionById(conditionData.condition.id, stageId, input);
		if (success) {
			store.closePanel();
		}
	}
};

// 取消
const handleCancel = () => {
	store.closePanel();
	store.setHasUnsavedChanges(false);
};

// 添加 Condition（从 Stage 面板）
const handleAddCondition = () => {
	const selectedNode = store.getSelectedNode;
	if (!selectedNode || selectedNode.data?.type !== 'stage') return;

	const stageData = selectedNode.data as StageNodeData;
	const stageIndex = stageData.index;

	// 打开 StageConditionEditor
	conditionEditorRef.value?.open(stageData.stage.id, stageData.stage.name, stageIndex, null);
};

// Condition 保存成功（从 StageConditionEditor）
const handleConditionSaved = async () => {
	// 重新加载 conditions 并重新生成节点和边
	await store.loadConditions();
	store.generateNodesAndEdges();
	// 关闭面板
	store.closePanel();
};

// 数据变更
const handleChange = () => {
	store.setHasUnsavedChanges(true);
};
</script>

<style lang="scss" scoped>
.condition-editor-page {
	position: relative;
	width: 100%;
	height: calc(100vh - 60px);
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

.conditionLoading-container,
.error-container {
	display: flex;
	align-items: center;
	justify-content: center;
	flex: 1;
	padding: 40px;
}
</style>
