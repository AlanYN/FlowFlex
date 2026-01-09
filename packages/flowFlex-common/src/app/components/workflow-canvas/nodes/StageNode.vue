<template>
	<div class="stage-node" :class="{ 'stage-node--selected': selected }">
		<!-- 顶部 Handle -->
		<Handle
			v-if="data.index != 0"
			type="target"
			:position="Position.Top"
			id="top-in"
			class="stage-handle"
		/>

		<!-- 右侧 Handle（用于接收 condition 跳转连接，多个 handle 避免重叠） -->
		<Handle
			type="target"
			:position="Position.Right"
			id="right-in"
			class="stage-handle"
			:style="{ top: '30%' }"
		/>
		<Handle
			type="target"
			:position="Position.Right"
			id="right-in-2"
			class="stage-handle"
			:style="{ top: '50%' }"
		/>
		<Handle
			type="target"
			:position="Position.Right"
			id="right-in-3"
			class="stage-handle"
			:style="{ top: '70%' }"
		/>

		<!-- 节点内容 -->
		<div class="stage-node__content">
			<!-- 头部 -->
			<div class="stage-node__header">
				<div
					class="stage-node__order"
					:style="{ backgroundColor: data.stage.color || 'var(--el-color-primary)' }"
				>
					{{ data.index + 1 }}
				</div>
				<div class="stage-node__info">
					<div class="stage-node__name">{{ data.stage.name }}</div>
					<div class="stage-node__meta">
						{{ componentCount }} {{ componentCount > 1 ? 'Components' : 'Component' }}
					</div>
				</div>
			</div>

			<!-- 标签区域 -->
			<div class="stage-node__badges">
				<el-tag v-if="data.stage.required" type="warning" size="small" effect="plain">
					Required
				</el-tag>
				<el-tag v-if="data.hasCondition" type="primary" size="small" effect="plain">
					<el-icon class="mr-1"><Connection /></el-icon>
					Condition
				</el-tag>
			</div>
		</div>

		<!-- 底部 Handle -->
		<Handle type="source" :position="Position.Bottom" id="bottom-out" class="stage-handle" />

		<!-- 条件输出 Handle（右侧） -->
		<Handle
			type="source"
			:position="Position.Right"
			id="condition-out"
			class="stage-handle stage-handle--condition"
		/>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Handle, Position } from '@vue-flow/core';
import { Connection } from '@element-plus/icons-vue';
import type { StageNodeData } from '#/workflow-canvas';

interface Props {
	id: string;
	data: StageNodeData;
	selected?: boolean;
}

const props = defineProps<Props>();

const componentCount = computed(() => {
	return props.data.stage.components?.length || 0;
});
</script>

<style scoped>
.stage-node {
	width: 280px;
	padding: 12px 16px;
	border-radius: 8px;
	border: 2px solid var(--el-border-color);
	background: var(--el-bg-color);
	cursor: pointer;
	transition: all 0.2s ease;
}

.stage-node:hover {
	border-color: var(--el-color-primary-light-5);
	box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.stage-node--selected {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px var(--el-color-primary-light-8);
}

.stage-node__content {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.stage-node__header {
	display: flex;
	align-items: center;
	gap: 12px;
}

.stage-node__order {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	font-weight: 600;
	color: #fff;
	flex-shrink: 0;
}

.stage-node__info {
	flex: 1;
	min-width: 0;
}

.stage-node__name {
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.stage-node__meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-top: 2px;
}

.stage-node__badges {
	display: flex;
	gap: 6px;
	flex-wrap: wrap;
}

/* Handle 样式 */
.stage-handle {
	width: 10px;
	height: 10px;
	background: var(--el-color-primary);
	border: 2px solid var(--el-bg-color);
}

.stage-handle--condition {
	background: var(--el-color-primary);
}

/* 深色模式 */
html.dark .stage-node {
	background: var(--el-bg-color);
	border-color: var(--el-border-color-darker);
}

html.dark .stage-node:hover {
	border-color: var(--el-color-primary-light-3);
}

html.dark .stage-node--selected {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px var(--el-color-primary-dark-2);
}
</style>
