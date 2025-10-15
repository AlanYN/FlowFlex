<template>
	<div class="empty-state-container rounded-xl bg-white shadow-sm empty-state-border">
		<div class="empty-state-content">
			<svg
				xmlns="http://www.w3.org/2000/svg"
				width="64"
				height="64"
				viewBox="0 0 24 24"
				fill="none"
				stroke="currentColor"
				stroke-width="1.5"
				stroke-linecap="round"
				stroke-linejoin="round"
				class="empty-state-icon"
			>
				<rect x="8" y="2" width="8" height="4" rx="1" ry="1" />
				<path
					d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"
				/>
				<path d="M12 11h4" />
				<path d="M12 16h4" />
				<path d="M8 11h.01" />
				<path d="M8 16h.01" />
			</svg>
			<h2 class="empty-state-title">{{ title }}</h2>
			<p class="empty-state-desc">{{ description }}</p>
			<slot name="action">
				<el-button
					v-if="showCreateButton"
					type="primary"
					size="large"
					@click="$emit('create')"
					class="create-btn"
				>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						width="20"
						height="20"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
						class="mr-2"
					>
						<path d="M12 5v14" />
						<path d="M5 12h14" />
					</svg>
					<span>{{ createButtonText }}</span>
				</el-button>
			</slot>
		</div>
	</div>
</template>

<script setup>
defineProps({
	title: {
		type: String,
		default: 'No Checklists Found',
	},
	description: {
		type: String,
		default: 'Create your first checklist to help manage tasks during the onboarding process.',
	},
	showCreateButton: {
		type: Boolean,
		default: true,
	},
	createButtonText: {
		type: String,
		default: 'Create Checklist',
	},
});

defineEmits(['create']);
</script>

<style scoped lang="scss">
.empty-state-container {
	padding: 60px 20px;
	text-align: center;
	margin-bottom: 16px;
	@apply dark:bg-black-400 dark:border-black-200;
}

.empty-state-content {
	max-width: 500px;
	margin: 0 auto;
	display: flex;
	flex-direction: column;
	align-items: center;
}

.empty-state-icon {
	color: var(--primary-300);
	margin-bottom: 24px;
}

.empty-state-title {
	font-size: 24px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 0 0 16px 0;
}

html.dark .empty-state-title {
	color: var(--el-color-white);
}

.empty-state-desc {
	font-size: 16px;
	color: var(--el-text-color-regular);
	margin: 0 0 32px 0;
	line-height: 1.6;
}

.empty-state-border {
	border-color: var(--el-border-color-light);
}

.create-btn {
	padding: 12px 24px;
	font-size: 16px;
	display: flex;
	align-items: center;
	justify-content: center;
	background-color: var(--primary-500) !important;
	border-color: var(--primary-500) !important;
}

.create-btn:hover {
	background-color: var(--primary-600) !important;
	border-color: var(--primary-600) !important;
}
</style>
