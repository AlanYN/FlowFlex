<template>
	<div class="stage-panel">
		<!-- Stage 信息 -->
		<div class="stage-panel__section">
			<h4 class="stage-panel__section-title">Stage Information</h4>

			<div class="stage-panel__info">
				<div class="stage-panel__info-item">
					<span class="stage-panel__label">Name</span>
					<span class="stage-panel__value">{{ stage?.name || '-' }}</span>
				</div>

				<div class="stage-panel__info-item">
					<span class="stage-panel__label">Description</span>
					<span class="stage-panel__value">{{ stage?.description || '-' }}</span>
				</div>

				<div class="stage-panel__info-item">
					<span class="stage-panel__label">Components</span>
					<span class="stage-panel__value">{{ componentCount }}</span>
				</div>

				<div class="stage-panel__info-item">
					<span class="stage-panel__label">Required</span>
					<el-tag v-if="stage?.required" type="warning" size="small">Yes</el-tag>
					<el-tag v-else type="info" size="small">No</el-tag>
				</div>
			</div>
		</div>

		<!-- Condition 状态 -->
		<div class="stage-panel__section">
			<h4 class="stage-panel__section-title">Condition</h4>

			<div v-if="hasCondition" class="stage-panel__condition-status">
				<el-icon class="text-green-500"><CircleCheck /></el-icon>
				<span>This stage has a condition configured</span>
			</div>

			<div v-else class="stage-panel__condition-empty">
				<p class="stage-panel__condition-hint">
					Add a condition to control workflow transitions based on this stage's data.
				</p>
				<el-button type="primary" @click="$emit('add-condition')">
					<el-icon class="mr-1"><Plus /></el-icon>
					Add Condition
				</el-button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { CircleCheck, Plus } from '@element-plus/icons-vue';
import type { Stage } from '#/onboard';

interface Props {
	stage: Stage | null;
	hasCondition?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	hasCondition: false,
});

defineEmits<{
	(e: 'add-condition'): void;
}>();

const componentCount = computed(() => {
	return props.stage?.components?.length || 0;
});
</script>

<style scoped>
.stage-panel {
	display: flex;
	flex-direction: column;
	gap: 24px;
}

.stage-panel__section {
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.stage-panel__section-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 0;
	padding-bottom: 8px;
	border-bottom: 1px solid var(--el-border-color-light);
}

.stage-panel__info {
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.stage-panel__info-item {
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.stage-panel__label {
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.stage-panel__value {
	font-size: 13px;
	color: var(--el-text-color-primary);
	text-align: right;
	max-width: 60%;
	word-break: break-word;
}

.stage-panel__condition-status {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 12px;
	background: var(--el-color-success-light-9);
	border-radius: 8px;
	color: var(--el-color-success);
	font-size: 13px;
}

.stage-panel__condition-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 12px;
	padding: 24px;
	background: var(--el-fill-color-light);
	border-radius: 8px;
	text-align: center;
}

.stage-panel__condition-hint {
	font-size: 13px;
	color: var(--el-text-color-secondary);
	margin: 0;
}

/* 深色模式 */
html.dark .stage-panel__condition-status {
	background: rgba(103, 194, 58, 0.1);
}

html.dark .stage-panel__condition-empty {
	background: var(--el-fill-color-darker);
}
</style>
