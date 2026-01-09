<template>
	<div class="canvas-toolbar">
		<!-- 左侧：返回按钮和标题 -->
		<div class="canvas-toolbar__left">
			<el-button link @click="handleBack" :icon="ArrowLeft" />
			<el-divider direction="vertical" />
			<span class="canvas-toolbar__title">{{ title }}</span>
			<el-tag v-if="hasUnsavedChanges" type="warning" size="small" effect="plain">
				Unsaved
			</el-tag>
		</div>

		<!-- 右侧：缩放控制 -->
		<div class="canvas-toolbar__right">
			<el-button-group>
				<el-tooltip content="Zoom Out (25%)" placement="bottom">
					<el-button
						:disabled="zoomPercent <= 25"
						@click="$emit('zoom-out')"
						:icon="ZoomOut"
					/>
				</el-tooltip>

				<el-button class="canvas-toolbar__zoom-display" disabled>
					{{ zoomPercent }}%
				</el-button>

				<el-tooltip content="Zoom In (25%)" placement="bottom">
					<el-button
						:disabled="zoomPercent >= 200"
						@click="$emit('zoom-in')"
						:icon="ZoomIn"
					/>
				</el-tooltip>
			</el-button-group>

			<el-tooltip content="Fit View" placement="bottom">
				<el-button @click="$emit('fit-view')" :icon="FullScreen" />
			</el-tooltip>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ArrowLeft, ZoomIn, ZoomOut, FullScreen } from '@element-plus/icons-vue';

interface Props {
	title?: string;
	zoomPercent: number;
	hasUnsavedChanges?: boolean;
}

withDefaults(defineProps<Props>(), {
	title: 'Workflow Conditions',
	hasUnsavedChanges: false,
});

const emit = defineEmits<{
	(e: 'back'): void;
	(e: 'zoom-in'): void;
	(e: 'zoom-out'): void;
	(e: 'fit-view'): void;
}>();

const handleBack = () => {
	emit('back');
};
</script>

<style scoped>
.canvas-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 8px 16px;
	background: var(--el-bg-color);
	border-bottom: 1px solid var(--el-border-color-light);
}

.canvas-toolbar__left {
	display: flex;
	align-items: center;
	gap: 8px;
}

.canvas-toolbar__title {
	font-size: 16px;
	font-weight: 500;
	color: var(--el-text-color-primary);
}

.canvas-toolbar__right {
	display: flex;
	align-items: center;
	gap: 8px;
}

.canvas-toolbar__zoom-display {
	min-width: 60px;
	cursor: default !important;
}

/* 深色模式 */
html.dark .canvas-toolbar {
	background: var(--el-bg-color);
	border-color: var(--el-border-color-darker);
}
</style>
