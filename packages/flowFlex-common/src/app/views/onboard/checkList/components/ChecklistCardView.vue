<template>
	<div>
		<!-- 加载状态 -->
		<div v-if="loading" class="flex justify-center items-center py-12">
			<el-icon class="animate-spin h-8 w-8 text-primary-500">
				<Loading />
			</el-icon>
			<span class="ml-2 text-primary-600">Loading checklists...</span>
		</div>

		<!-- 检查清单卡片网格 -->
		<div v-else-if="checklists.length > 0" class="checklist-grid">
			<ChecklistCard
				v-for="checklist in checklists"
				:key="checklist.id"
				:checklist="checklist"
				:workflows="workflows"
				:all-stages="allStages"
				:delete-loading="deleteLoading"
				:export-loading="exportLoading"
				:duplicate-loading="duplicateLoading"
				@command="handleCommand"
			/>
		</div>

		<!-- 空状态 -->
		<EmptyState v-else :description="emptyMessage" @create="$emit('new-checklist')" />
	</div>
</template>

<script setup>
import ChecklistCard from './ChecklistCard.vue';
import EmptyState from './EmptyState.vue';
import { Loading } from '@element-plus/icons-vue';

// Props
defineProps({
	checklists: {
		type: Array,
		default: () => [],
	},
	loading: {
		type: Boolean,
		default: false,
	},
	emptyMessage: {
		type: String,
		default: '',
	},
	workflows: {
		type: Array,
		default: () => [],
	},
	allStages: {
		type: Array,
		default: () => [],
	},
	deleteLoading: {
		type: Boolean,
		default: false,
	},
	exportLoading: {
		type: Boolean,
		default: false,
	},
	duplicateLoading: {
		type: Boolean,
		default: false,
	},
});

// Emits
const emit = defineEmits([
	'edit-checklist',
	'delete-checklist',
	'export-checklist',
	'duplicate-checklist',
	'view-tasks',
	'new-checklist',
]);

// Methods
const handleCommand = (command, checklist) => {
	switch (command) {
		case 'task':
			emit('view-tasks', checklist);
			break;
		case 'edit':
			emit('edit-checklist', checklist);
			break;
		case 'export':
			emit('export-checklist', checklist);
			break;
		case 'duplicate':
			emit('duplicate-checklist', checklist);
			break;
		case 'delete':
			emit('delete-checklist', checklist.id);
			break;
	}
};
</script>

<style scoped lang="scss">
/* 检查清单卡片网格布局 */
.checklist-grid {
	display: grid;
	gap: 24px;
	grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
	width: 100%;

	/* 响应式断点调整 */
	@media (max-width: 480px) {
		grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
		gap: 16px;
		padding: 0 8px;
	}

	@media (min-width: 481px) and (max-width: 768px) {
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 20px;
	}

	@media (min-width: 769px) and (max-width: 1024px) {
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 20px;
	}

	@media (min-width: 1025px) and (max-width: 1400px) {
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 24px;
	}

	@media (min-width: 1401px) and (max-width: 1920px) {
		grid-template-columns: repeat(auto-fill, minmax(420px, 1fr));
		gap: 28px;
	}

	@media (min-width: 1921px) and (max-width: 2560px) {
		grid-template-columns: repeat(auto-fill, minmax(440px, 1fr));
		gap: 32px;
	}

	@media (min-width: 2561px) {
		grid-template-columns: repeat(auto-fill, minmax(460px, 1fr));
		gap: 32px;
	}

	/* 限制单个卡片的最大宽度，防止过度拉伸 */
	& > :deep(.checklist-card) {
		max-width: 600px;
		width: 100%;
	}
}
</style>
