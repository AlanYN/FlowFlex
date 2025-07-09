<template>
	<div class="page-header rounded-md">
		<div class="header-content">
			<div class="header-left">
				<el-button
					type="primary"
					link
					class="back-button"
					@click="$emit('go-back')"
					size="small"
				>
					<el-icon class="back-icon"><Back /></el-icon>
				</el-button>
				<div class="header-info">
					<h1 class="page-title">
						{{ title }}
					</h1>
					<p class="page-description">
						{{ description }}
					</p>
				</div>
			</div>
			<div class="header-actions">
				<el-button
					type="default"
					class="preview-button"
					@click="$emit('toggle-preview')"
					:icon="currentTab === 'questions' ? View : Edit"
				>
					{{ currentTab === 'questions' ? 'Preview' : 'Edit' }}
				</el-button>
				<el-button
					type="primary"
					class="save-button"
					@click="$emit('save-questionnaire')"
					:loading="saving"
					:icon="Document"
				>
					{{ isEditMode ? 'Update Questionnaire' : 'Save Questionnaire' }}
				</el-button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { Back, View, Edit, Document } from '@element-plus/icons-vue';

interface Props {
	title: string;
	description: string;
	currentTab: string;
	saving: boolean;
	isEditMode: boolean;
}

defineProps<Props>();

defineEmits<{
	'go-back': [];
	'toggle-preview': [];
	'save-questionnaire': [];
}>();
</script>

<style scoped lang="scss">
.page-header {
	background: linear-gradient(135deg, var(--primary-50) 0%, var(--primary-100) 100%);
	border-bottom: 1px solid var(--primary-200);
	padding: 1.5rem 2rem;
}

.header-content {
	display: flex;
	justify-content: space-between;
	align-items: center;
	width: 100%;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.back-button {
	background-color: transparent;
	border: none;
	color: var(--primary-700);
	padding: 0.5rem;
	margin-right: 1rem;
	transition: all 0.2s;
	min-height: 2rem;
	display: flex;
	align-items: center;
	justify-content: center;
}

.back-button:hover {
	background-color: var(--primary-200);
	color: var(--primary-700);
}

.back-icon {
	font-size: 1.25rem;
	width: 1.25rem;
	height: 1.25rem;
}

.header-info {
	display: flex;
	flex-direction: column;
}

.page-title {
	font-size: 1.875rem;
	font-weight: 700;
	color: var(--primary-800);
	margin: 0;
}

.page-description {
	color: var(--primary-600);
	margin: 0.25rem 0 0 0;
}

.header-actions {
	display: flex;
	gap: 0.5rem;
}

.preview-button {
	border-color: var(--primary-300);
	color: var(--primary-700);
	background-color: transparent;
}

.preview-button:hover {
	background-color: var(--primary-100);
	border-color: var(--primary-400);
}

.save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.save-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-700);
}

/* 深色模式支持 */
.dark .page-header {
	background: linear-gradient(135deg, var(--primary-800) 0%, var(--primary-700) 100%);
	border-bottom-color: var(--primary-600);
}

.dark .page-title {
	color: var(--primary-100);
}

.dark .page-description {
	color: var(--primary-200);
}

.dark .back-button {
	background-color: transparent;
	color: var(--primary-200);
}

.dark .back-button:hover {
	background-color: var(--primary-600);
	color: var(--primary-200);
}

.dark .preview-button {
	border-color: var(--primary-600);
	color: var(--primary-200);
	background-color: transparent;
}

.dark .preview-button:hover {
	background-color: var(--primary-700);
	border-color: var(--primary-500);
}

.dark .save-button {
	background-color: var(--primary-600);
	border-color: var(--primary-600);
}

.dark .save-button:hover {
	background-color: var(--primary-500);
	border-color: var(--primary-500);
}
</style>
