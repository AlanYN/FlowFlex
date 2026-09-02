<template>
	<div class="trigger-editor">
		<!-- ===== 顶栏（统一 PageHeader 组件） ===== -->
		<PageHeader
			title="Workflow Triggers"
			description="Add workflows to the canvas, connect them, then set each trigger's condition and data mapping"
			:show-back-button="true"
			@go-back="handleBack"
		>
			<template #actions>
				<el-button
					v-if="editor.hasUnsavedChanges.value"
					class="save-status unsaved"
					link
					@click="handleRevert"
				>
					<Icon icon="ph:dot-bold" class="w-3 h-3" />
					Unsaved changes · Click to revert
				</el-button>
				<el-button
					type="primary"
					:loading="editor.saving.value"
					:disabled="!editor.hasUnsavedChanges.value || editor.saving.value"
					@click="handleSave"
				>
					{{ editor.saving.value ? 'Saving...' : 'Save' }}
				</el-button>
			</template>
		</PageHeader>

		<!-- ===== 主体 ===== -->
		<div class="flex flex-1 overflow-hidden">
			<!-- 左侧列表 -->
			<TriggerSidebar
				:workflows="editor.allWorkflows.value as WorkflowItem[]"
				:canvas-workflow-ids="editor.canvasWorkflowIds.value"
				:on-canvas-count="editor.onCanvasCount.value"
				:connection-count="editor.connectionCount.value"
				:current-workflow-id="workflowId"
				v-model="sidebarModel"
				@add="editor.addToCanvas"
				@remove="editor.removeFromCanvas"
			/>

			<!-- 画布区域 -->
			<div class="flex-1 relative overflow-hidden">
				<!-- 加载 -->
				<div v-if="editor.loading.value" class="canvas-overlay">
					<Icon icon="ep:loading" class="canvas-spinner is-loading" />
				</div>

				<!-- 错误 -->
				<div v-else-if="editor.error.value" class="canvas-overlay">
					<el-result icon="error" :title="editor.error.value">
						<template #extra>
							<el-button type="primary" @click="() => editor.init()">Retry</el-button>
						</template>
					</el-result>
				</div>

				<!-- 画布 -->
				<TriggerCanvas
					v-else
					:cards="editor.cards.value"
					:connections="editor.connections.value"
					:current-workflow-id="workflowId"
					:selected-connection-id="editor.selectedConnectionId.value"
					:connecting-from="editor.connectingFrom.value"
					:zoom-level="zoomPercent / 100"
					@update-position="editor.updateCardPosition"
					@add-connection="editor.addConnection"
					@remove-card="editor.removeFromCanvas"
					@select-connection="editor.selectConnection"
					@deselect="editor.selectConnection(null)"
					@connecting-start="editor.startConnecting"
					@connecting-end="editor.finishConnecting"
					@connecting-cancel="editor.cancelConnecting"
					@zoom="(delta: number) => zoom(delta)"
				/>

				<!-- 缩放工具栏 -->
				<div
					class="canvas-toolbar absolute top-3 right-3 flex items-center gap-0.5 px-1.5 py-1 rounded-lg z-20"
				>
					<button
						class="zoom-btn flex items-center justify-center w-[26px] h-[26px] rounded-[5px] border-none bg-transparent cursor-pointer text-xs transition-all duration-150"
						title="Zoom out"
						@click="zoom(-10)"
					>
						<Icon icon="ep:minus" class="w-3.5 h-3.5" />
					</button>
					<span class="zoom-label text-xs w-[38px] text-center tabular-nums">
						{{ zoomPercent }}%
					</span>
					<button
						class="zoom-btn flex items-center justify-center w-[26px] h-[26px] rounded-[5px] border-none bg-transparent cursor-pointer text-xs transition-all duration-150"
						title="Zoom in"
						@click="zoom(10)"
					>
						<Icon icon="ep:plus" class="w-3.5 h-3.5" />
					</button>
					<button
						class="zoom-btn reset-btn flex items-center justify-center w-auto px-2 h-[26px] rounded-[5px] border-none bg-transparent cursor-pointer text-[11px] font-medium transition-all duration-150"
						@click="resetZoom"
					>
						Reset
					</button>
				</div>
			</div>

			<!-- 右侧连线配置面板 -->
			<ConnectionPanel
				:connection="editor.selectedConnection.value"
				:all-workflows="editor.allWorkflows.value"
				:saving="editor.saving.value"
				@close="editor.selectConnection(null)"
				@save="handlePanelSave"
				@delete="handlePanelDelete"
			/>
		</div>

		<!-- OW-729: Trigger History — FAB + slide-up panel (same pattern as ChangeLog) -->
		<TriggerHistory v-if="workflowId" :workflow-id="workflowId" />
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue';
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router';
import { ElMessageBox } from 'element-plus';
import { useTriggerEditor } from '@/hooks/useTriggerEditor';
import type { WorkflowItem } from '@/hooks/useTriggerEditor';
import TriggerSidebar from './components/triggers/TriggerSidebar.vue';
import TriggerCanvas from './components/triggers/TriggerCanvas.vue';
import ConnectionPanel from './components/triggers/ConnectionPanel.vue';
import TriggerHistory from './components/triggers/TriggerHistory.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';

const route = useRoute();
const router = useRouter();
const workflowId = route.params.workflowId as string;

const editor = useTriggerEditor(workflowId);
const sidebarModel = ref({ keyword: '', filter: 'all' });
const zoomPercent = ref(100);

const zoom = (delta: number) => {
	zoomPercent.value = Math.min(200, Math.max(25, zoomPercent.value + delta));
};
const resetZoom = () => {
	zoomPercent.value = 100;
};

onMounted(async () => {
	if (workflowId) await editor.init();
});

onBeforeRouteLeave(async (to, from, next) => {
	if (editor.hasUnsavedChanges.value) {
		try {
			await ElMessageBox.confirm(
				'You have unsaved changes. Are you sure you want to leave?',
				'Unsaved Changes',
				{ confirmButtonText: 'Leave', cancelButtonText: 'Stay', type: 'warning' }
			);
			ElMessageBox.close();
			// 关闭 ConnectionPanel 抽屉，避免 append-to-body 的浮层残留
			editor.selectConnection(null);
			next();
		} catch {
			next(false);
		}
	} else {
		next();
	}
});

// 组件销毁前关闭所有浮层
onBeforeUnmount(() => {
	ElMessageBox.close();
	editor.selectConnection(null);
});

const handleBack = async () => {
	if (editor.hasUnsavedChanges.value) {
		try {
			await ElMessageBox.confirm(
				'You have unsaved changes. Are you sure you want to leave?',
				'Unsaved Changes',
				{ confirmButtonText: 'Leave', cancelButtonText: 'Stay', type: 'warning' }
			);
		} catch {
			return;
		}
	}
	router.push('/onboard/onboardWorkflow');
};

const handleSave = async () => {
	await editor.save();
};

const handleRevert = async () => {
	try {
		await ElMessageBox.confirm(
			'Discard all unsaved changes and revert to the last saved state?',
			'Revert Changes',
			{ confirmButtonText: 'Revert', cancelButtonText: 'Cancel', type: 'warning' }
		);
		editor.selectConnection(null);
		await editor.revert();
	} catch {
		/* cancelled */
	}
};

// ========================= ConnectionPanel 回调 =========================

const handlePanelSave = (patch: {
	conditionSummary: string;
	configJson: string;
	ruleName: string;
}) => {
	const connId = editor.selectedConnectionId.value;
	if (!connId) return;
	editor.updateConnectionConfig(connId, patch.conditionSummary, patch.configJson, patch.ruleName);
};

const handlePanelDelete = (connectionId: string) => {
	editor.removeConnection(connectionId);
};
</script>

<style scoped lang="scss">
.trigger-editor {
	display: flex;
	flex-direction: column;
	height: calc(100vh - 64px - 32px);
	overflow: hidden;
	background: var(--el-fill-color-light);
	border-radius: 8px;

	/* PageHeader flex-shrink：不参与高度伸缩 */
	:deep(.relative.rounded-xl) {
		flex-shrink: 0;
		margin-bottom: 0;
	}
}

.save-status.unsaved {
	color: var(--el-color-warning);
	font-weight: 500;
}

.canvas-overlay {
	position: absolute;
	inset: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	background: var(--el-fill-color-lighter);
	z-index: 10;
}
.canvas-spinner {
	width: 32px;
	height: 32px;
	color: var(--el-color-primary-light-3);
}

/* 缩放工具栏（颜色 + :hover/:active 伪类依赖 CSS 变量，无法用 tailwind） */
.canvas-toolbar {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}
.zoom-btn {
	color: var(--el-text-color-secondary);
	&:hover {
		background: var(--el-fill-color);
		color: var(--el-text-color-primary);
	}
	&:active {
		transform: scale(0.92);
	}
}
.zoom-label {
	color: var(--el-text-color-secondary);
}
</style>
