<template>
	<div class="stage-condition-card" :class="{ 'is-disabled': disabled }">
		<!-- 无 Condition 时显示添加按钮 -->
		<div v-if="!condition" class="condition-empty">
			<el-button
				type="primary"
				link
				:disabled="disabled"
				@click="handleAdd"
				class="add-condition-btn"
			>
				<el-icon class="mr-1"><Plus /></el-icon>
				Add Condition
			</el-button>
		</div>

		<!-- 有 Condition 时显示摘要卡片 -->
		<div v-else class="condition-summary">
			<div class="condition-header">
				<div class="condition-info">
					<el-icon class="condition-icon"><Connection /></el-icon>
					<span class="condition-name">{{ condition.name }}</span>
					<el-tag v-if="!condition.isActive" type="info" size="small" class="ml-2">
						Inactive
					</el-tag>
				</div>
				<div class="condition-actions">
					<el-tooltip content="Edit" placement="top">
						<el-button
							type="primary"
							link
							:disabled="disabled"
							@click="handleEdit"
							class="action-btn"
						>
							<el-icon><Edit /></el-icon>
						</el-button>
					</el-tooltip>
					<el-tooltip content="Delete" placement="top">
						<el-button
							type="danger"
							link
							:disabled="disabled"
							@click="handleDelete"
							class="action-btn"
						>
							<el-icon><Delete /></el-icon>
						</el-button>
					</el-tooltip>
				</div>
			</div>
			<div class="condition-details">
				<div class="condition-rules">
					<span class="detail-label">Rules:</span>
					<span class="detail-value">
						{{ rulesCount }} {{ rulesCount > 1 ? 'rules' : 'rule' }} ({{
							condition.rulesJson.logic
						}})
					</span>
				</div>
				<div class="condition-actions-summary">
					<span class="detail-label">Actions:</span>
					<span class="detail-value">
						{{ actionsCount }} {{ actionsCount > 1 ? 'actions' : 'action' }}
					</span>
				</div>
			</div>
			<div v-if="condition.description" class="condition-description">
				{{ condition.description }}
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Plus, Edit, Delete, Connection } from '@element-plus/icons-vue';
import type { StageCondition } from '#/condition';

// Props
const props = defineProps<{
	condition: StageCondition | null;
	stageId: string;
	stageName: string;
	isLastStage?: boolean;
	disabled?: boolean;
}>();

// Emits
const emit = defineEmits<{
	(e: 'add'): void;
	(e: 'edit', condition: StageCondition): void;
	(e: 'delete', conditionId: string): void;
}>();

// Computed
const rulesCount = computed(() => {
	return props.condition?.rulesJson?.rules?.length || 0;
});

const actionsCount = computed(() => {
	return props.condition?.actionsJson?.length || 0;
});

// Methods
const handleAdd = () => {
	emit('add');
};

const handleEdit = () => {
	if (props.condition) {
		emit('edit', props.condition);
	}
};

const handleDelete = () => {
	if (props.condition) {
		emit('delete', props.condition.id);
	}
};
</script>

<style lang="scss" scoped>
.stage-condition-card {
	@apply ml-12 mt-2 mb-2;

	&.is-disabled {
		opacity: 0.6;
		pointer-events: none;
	}
}

.condition-empty {
	@apply flex items-center;

	.add-condition-btn {
		@apply text-sm font-medium;
		color: var(--el-color-primary);

		&:hover {
			color: var(--el-color-primary-light-3);
		}
	}
}

.condition-summary {
	@apply p-3 rounded-lg border transition-all duration-200;
	background-color: var(--el-color-primary-light-9);
	border-color: var(--el-color-primary-light-7);

	&:hover {
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
	}
}

html.dark .condition-summary {
	background-color: var(--el-color-primary-dark-2);
	border-color: var(--el-color-primary-light-3);

	&:hover {
		border-color: var(--el-color-primary);
	}
}

.condition-header {
	@apply flex items-center justify-between;
}

.condition-info {
	@apply flex items-center gap-2;
}

.condition-icon {
	@apply text-base;
	color: var(--el-color-primary);
}

.condition-name {
	@apply font-medium text-sm;
	color: var(--el-color-primary);
}

html.dark .condition-name {
	color: var(--el-color-primary-light-5);
}

.condition-actions {
	@apply flex items-center gap-1;
}

.action-btn {
	@apply p-1;

	&:hover {
		background-color: var(--el-fill-color-light);
		border-radius: 4px;
	}
}

.condition-details {
	@apply flex items-center gap-4 mt-2 text-xs;
	color: var(--el-text-color-secondary);
}

.detail-label {
	color: var(--el-text-color-secondary);
}

.detail-value {
	@apply font-medium;
	color: var(--el-text-color-regular);
}

.condition-description {
	@apply mt-2 text-xs;
	color: var(--el-text-color-secondary);
	display: -webkit-box;
	-webkit-line-clamp: 2;
	line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
}
</style>
