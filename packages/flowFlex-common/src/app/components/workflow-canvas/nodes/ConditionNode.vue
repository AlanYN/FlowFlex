<template>
	<div class="condition-node-wrapper">
		<!-- 主节点内容 -->
		<div class="condition-node" :class="{ 'condition-node--selected': selected }">
			<!-- 左侧 Handle（输入） -->
			<Handle type="target" :position="Position.Left" id="left-in" class="condition-handle" />

			<!-- 菱形装饰 -->
			<div class="condition-node__diamond"></div>

			<!-- 节点内容 -->
			<div class="condition-node__content">
				<div class="condition-node__header">
					<el-icon class="condition-node__icon"><Share /></el-icon>
					<span class="condition-node__name">{{ data.condition.name }}</span>
				</div>
				<div class="condition-node__summary">
					{{ rulesCount }} {{ rulesCount > 1 ? 'rules' : 'rule' }}
					<span v-if="actionsCount > 0">
						· {{ actionsCount }} {{ actionsCount > 1 ? 'actions' : 'action' }}
					</span>
				</div>
			</div>

			<!-- 删除按钮 -->
			<el-button
				class="condition-node__delete"
				type="danger"
				size="small"
				circle
				@click.stop="handleDelete"
				:icon="Delete"
			/>

			<!-- 右侧 Handle（Fallback 输出） -->
			<Handle
				type="source"
				:position="Position.Right"
				id="fallback-out"
				class="condition-handle condition-handle--fallback"
			/>
		</div>

		<!-- 底部 Actions 区域（在节点外部） -->
		<div class="condition-node__actions-area">
			<div
				v-for="(action, index) in parsedActions"
				:key="index"
				class="condition-node__action-handle-wrapper"
				:style="{ left: getActionHandlePosition(index) }"
			>
				<Handle
					type="source"
					:position="Position.Bottom"
					:id="`action-${index}`"
					class="condition-handle condition-handle--action"
					:class="{
						'condition-handle--end': action.type === 'EndWorkflow',
						'condition-handle--goto': action.type === 'GoToStage',
						'condition-handle--skip': action.type === 'SkipStage',
					}"
				/>
				<div class="condition-node__action-label">
					{{ getActionLabel(action) }}
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Handle, Position } from '@vue-flow/core';
import { Share, Delete } from '@element-plus/icons-vue';
import type { ConditionNodeData } from '#/workflow-canvas';

interface Props {
	id: string;
	data: ConditionNodeData;
	selected?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	(e: 'delete'): void;
}>();

// 解析 actions
const parsedActions = computed(() => {
	const actionsJson = props.data.condition.actionsJson;
	if (!actionsJson) return [];
	if (Array.isArray(actionsJson)) return actionsJson;
	if (typeof actionsJson === 'string') {
		try {
			const parsed = JSON.parse(actionsJson);
			return Array.isArray(parsed) ? parsed : [];
		} catch {
			return [];
		}
	}
	return [];
});

// 计算每个 action handle 的水平位置
const getActionHandlePosition = (index: number): string => {
	const total = parsedActions.value.length;
	if (total === 1) return '50%';
	// 均匀分布，留出边距
	const spacing = 100 / (total + 1);
	return `${spacing * (index + 1)}%`;
};

const rulesCount = computed(() => {
	const rulesJson = props.data.condition.rulesJson;
	if (!rulesJson) return 0;

	if (typeof rulesJson === 'string') {
		try {
			const parsed = JSON.parse(rulesJson);
			return parsed.rules?.length || 0;
		} catch {
			return 0;
		}
	}

	return (rulesJson as any).rules?.length || 0;
});

const actionsCount = computed(() => {
	return parsedActions.value.length;
});

// // 获取 action 的显示标签
const getActionLabel = (action: any): string => {
	if (!action || !action.type) return 'Action';

	switch (action.type) {
		case 'EndWorkflow':
			return 'End';
		case 'GoToStage':
			// 使用 stagesMap 查找目标 stage 名称
			if (action.targetStageId && props.data.stagesMap) {
				const stageName = props.data.stagesMap[action.targetStageId];
				return stageName ? `Go: ${stageName}` : 'Go To';
			}
			return 'Go To';
		case 'SkipStage':
			return 'Skip';
		case 'SendNotification':
			return 'Notify';
		case 'UpdateField':
			return 'Update';
		case 'TriggerAction':
			return 'Trigger';
		case 'AssignUser':
			return 'Assign';
		default:
			return action.type;
	}
};

const handleDelete = () => {
	emit('delete');
};
</script>

<style scoped>
/* 外层包装器 */
.condition-node-wrapper {
	display: flex;
	flex-direction: column;
	width: 200px;
}

.condition-node {
	position: relative;
	width: 100%;
	padding: 10px 14px;
	padding-left: 24px;
	border-radius: 8px;
	border: 2px solid var(--el-color-primary-light-5);
	background: var(--el-color-primary-light-9);
	cursor: pointer;
	transition: all 0.2s ease;
}

.condition-node:hover {
	border-color: var(--el-color-primary);
	box-shadow: 0 2px 12px rgba(64, 158, 255, 0.2);
}

.condition-node--selected {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px var(--el-color-primary-light-8);
}

/* 菱形装饰 */
.condition-node__diamond {
	position: absolute;
	left: -6px;
	top: 50%;
	transform: translateY(-50%) rotate(45deg);
	width: 12px;
	height: 12px;
	background: var(--el-color-primary);
	border-radius: 2px;
}

.condition-node__content {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.condition-node__header {
	display: flex;
	align-items: center;
	gap: 6px;
}

.condition-node__icon {
	color: var(--el-color-primary);
	font-size: 14px;
}

.condition-node__name {
	font-size: 13px;
	font-weight: 500;
	color: var(--el-color-primary-dark-2);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	flex: 1;
}

.condition-node__summary {
	font-size: 11px;
	color: var(--el-color-primary-light-3);
}

/* 底部 Actions 区域 */
.condition-node__actions-area {
	position: relative;
	width: 100%;
	height: 28px; /* 为 handles 和 labels 提供空间 */
}

.condition-node__action-handle-wrapper {
	position: absolute;
	top: 0;
	transform: translateX(-50%);
	display: flex;
	flex-direction: column;
	align-items: center;
}

.condition-node__action-label {
	position: absolute;
	top: 4px;
	left: 4px;
	font-size: 9px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	display: flex;
	align-items: center;
	gap: 2px;
}

.condition-node__action-label .el-icon {
	font-size: 10px;
}

/* 删除按钮 */
.condition-node__delete {
	position: absolute;
	top: -8px;
	right: -8px;
	width: 20px;
	height: 20px;
	padding: 0;
	opacity: 0;
	transition: opacity 0.2s ease;
}

.condition-node:hover .condition-node__delete {
	opacity: 1;
}

/* Handle 样式 */
.condition-handle {
	width: 8px;
	height: 8px;
	background: var(--el-color-primary);
	border: 2px solid var(--el-bg-color);
}

.condition-handle--fallback {
	background: var(--el-color-warning);
}

.condition-handle--action {
	background: var(--el-color-success);
}

.condition-handle--goto {
	background: var(--el-color-success);
}

.condition-handle--skip {
	background: var(--el-color-success);
}

.condition-handle--end {
	background: var(--el-color-danger);
}

/* 深色模式 */
html.dark .condition-node {
	background: rgba(64, 158, 255, 0.1);
	border-color: var(--el-color-primary-light-3);
}

html.dark .condition-node:hover {
	border-color: var(--el-color-primary);
}

html.dark .condition-node--selected {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px rgba(64, 158, 255, 0.3);
}

html.dark .condition-node__name {
	color: var(--el-color-primary-light-3);
}

html.dark .condition-node__summary {
	color: var(--el-color-primary-light-5);
}
</style>
