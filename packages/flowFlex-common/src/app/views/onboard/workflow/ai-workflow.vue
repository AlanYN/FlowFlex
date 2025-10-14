<template>
	<div class="ai-workflow-page">
		<!-- Page Header -->

		<PageHeader
			title="AI Workflow Generator"
			description="Use artificial intelligence to quickly create and optimize workflows"
		>
			<template #actions>
				<el-button
					@click="showAIConfig"
					type="primary"
					size="default"
					class="page-header-btn page-header-btn-primary"
					:icon="Setting"
				>
					AI Model Config
				</el-button>
			</template>
		</PageHeader>

		<div class="grid grid-cols-12 gap-6">
			<!-- AI Generator -->
			<div :class="showWorkflowList ? 'col-span-8' : 'col-span-12'">
				<AIWorkflowGenerator @workflow-generated="handleWorkflowGenerated" />
			</div>

			<!-- Workflow List -->
			<div v-if="showWorkflowList" class="col-span-4">
				<el-card shadow="hover">
					<template #header>
						<div class="flex items-center justify-between">
							<span class="font-semibold">Active Workflows</span>
							<el-button size="small" @click="refreshWorkflowList">
								<el-icon>
									<Refresh />
								</el-icon>
							</el-button>
						</div>
					</template>

					<el-scrollbar ref="scrollbarRef">
						<div class="space-y-3">
							<div
								v-for="workflow in workflowList"
								:key="workflow.id"
								class="workflow-item border border-gray-200 rounded-xl p-3 hover:bg-gray-50 transition-colors"
							>
								<div class="flex items-center justify-between mb-2">
									<div class="flex items-center gap-2">
										<h4 class="font-medium text-sm">{{ workflow.name }}</h4>
										<el-tag
											v-if="workflow.isAIGenerated"
											type="primary"
											effect="light"
											size="small"
											class="ai-tag"
										>
											<div class="flex items-center gap-1">
												<span class="ai-sparkles">✨</span>
												AI
											</div>
										</el-tag>
									</div>
									<el-tag
										:type="workflow.isActive ? 'success' : 'info'"
										size="small"
									>
										{{ workflow.isActive ? 'Active' : 'Draft' }}
									</el-tag>
								</div>
								<p class="text-xs text-gray-600 mb-2">
									{{ workflow.description || 'No description available' }}
								</p>
								<div
									class="flex items-center justify-between text-xs text-gray-500"
								>
									<span class="stages-count">
										<el-icon class="mr-1"><List /></el-icon>
										{{ getStageCount(workflow) }} stages
									</span>
									<span>{{ formatDate(workflow.createdAt) }}</span>
								</div>
							</div>

							<div
								v-if="workflowList.length === 0"
								class="empty-state text-center py-8 text-gray-500"
							>
								<el-icon class="text-2xl mb-2">
									<Document />
								</el-icon>
								<p class="text-sm">No workflows available</p>
								<p class="text-xs mt-1">Create your first workflow with AI</p>
							</div>
						</div>
					</el-scrollbar>
				</el-card>
			</div>
		</div>

		<!-- AI Model Config Dialog -->
		<el-dialog
			v-model="showAIConfigDialog"
			title="AI Model Configuration"
			width="60%"
			:close-on-click-modal="false"
			top="5vh"
			class="ai-config-dialog"
			style="max-width: 1200px"
		>
			<div class="ai-config-container">
				<AIModelConfig />
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { List, Refresh, Setting } from '@element-plus/icons-vue';
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

// Components
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import AIWorkflowGenerator from '@/components/ai/AIWorkflowGenerator.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import AIModelConfig from './ai-config.vue';

// APIs
import { getWorkflowList } from '@/apis/ow';

// Router
const router = useRouter();
const { scrollbarRef } = useAdaptiveScrollbar(50);

// Types
interface WorkflowStage {
	name: string;
	description: string;
	order: number;
	assignedGroup: string;
	requiredFields: string[];
	estimatedDuration: number;
}

interface Workflow {
	id?: number;
	name: string;
	description: string;
	isActive: boolean;
	isAIGenerated?: boolean;
	stages?: WorkflowStage[];
	createdAt?: string;
}

interface AIWorkflowData {
	generatedWorkflow: Workflow;
	stages: WorkflowStage[];
	operationMode: string;
	selectedWorkflowId?: number;
}

// Reactive Data
const showWorkflowList = ref(false);
const workflowList = ref<Workflow[]>([]);

// AI Config Dialog
const showAIConfigDialog = ref(false);

// Methods
const handleWorkflowGenerated = (workflowData: AIWorkflowData) => {
	// Navigate to workflow details page after creation
	router.push({
		path: '/onboard/onboardWorkflow',
		query: { id: workflowData.generatedWorkflow.id },
	});
};

const refreshWorkflowList = async () => {
	try {
		const response = await getWorkflowList();
		if (response.success) {
			workflowList.value = response.data || [];
		}
	} catch (error) {
		console.error('Failed to load workflow list:', error);
	}
};

const showAIConfig = () => {
	showAIConfigDialog.value = true;
};

const formatDate = (dateString?: string) => {
	if (!dateString) return '';
	try {
		const date = new Date(dateString);
		if (isNaN(date.getTime())) {
			return String(dateString);
		}
		// Format as MM/dd/yyyy (US format)
		return date.toLocaleDateString('en-US', {
			month: '2-digit',
			day: '2-digit',
			year: 'numeric',
		});
	} catch {
		return String(dateString);
	}
};

const getStageCount = (workflow: Workflow) => {
	if (!workflow || !workflow.stages || workflow.stages.length === 0) {
		return 0;
	}
	return workflow.stages.length;
};

// Lifecycle
onMounted(() => {
	refreshWorkflowList();
});
</script>

<style scoped lang="scss">
.ai-workflow-page {
	max-width: 1400px;
	margin: 0 auto;
}

.transition-colors {
	transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
	transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
	transition-duration: 150ms;
}

.workflow-item {
	transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
	transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
	transition-duration: 150ms;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
}

.workflow-item:hover {
	background: var(--el-fill-color-lighter);
	border-color: var(--el-border-color);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.stages-count {
	display: flex;
	align-items: center;
	color: var(--el-text-color-secondary);
	font-weight: 500;
}

.empty-state {
	transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
	transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
	transition-duration: 150ms;
	background: var(--el-fill-color-lighter);
	padding: 2rem;
	margin: 1rem 0;
	@apply rounded-xl;
}

/* New styles for AI dialog */
.ai-workflow-dialog .el-dialog__header {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	padding: 1.5rem;
	@apply rounded-xl;
}

.ai-workflow-dialog .el-dialog__header .el-dialog__title {
	font-size: 1.5rem;
	font-weight: 700;
}

.ai-workflow-dialog .el-dialog__body {
	padding: 1.5rem;
}

.workflow-overview-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1.5rem;
	padding-bottom: 1.5rem;
	border-bottom: 1px solid var(--el-border-color-light);
}

.overview-left {
	display: flex;
	align-items: center;
}

.overview-actions {
	display: flex;
	gap: 0.5rem;
}

.add-stage-btn .el-button__inner {
	background-color: var(--el-fill-color-light);
	color: var(--el-color-primary);
	border-color: var(--el-border-color);
}

.add-stage-btn .el-button__inner:hover {
	background-color: var(--el-fill-color);
	border-color: var(--el-border-color-light);
}

.workflow-info {
	margin-left: 1.5rem;
}

.workflow-title {
	font-size: 1.5rem;
	font-weight: 700;
	color: var(--el-text-color-regular);
	margin-bottom: 0.25rem;
}

.workflow-meta {
	display: flex;
	align-items: center;
	font-size: 0.875rem;
	color: var(--el-text-color-secondary);
}

.meta-badge {
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
	border: 1px solid var(--el-border-color);
	padding: 0.25rem 0.75rem;
	font-weight: 600;
	@apply rounded-xl;
}

.meta-divider {
	margin: 0 0.5rem;
}

.workflow-stages-grid {
	max-height: 70vh;
	overflow-y: auto;
	padding-right: 0.5rem;
}

.stages-grid-container {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
	gap: 1.5rem;
	padding: 1rem 0;
}

.stage-card-wrapper {
	position: relative;
}

.stage-card {
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-light);
	padding: 1.25rem;
	transition: all 0.2s ease-in-out;
	height: fit-content;
	@apply rounded-xl;
}

.stage-card:hover {
	transform: translateY(-3px);
	box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
	border-color: var(--el-color-primary);
}

.stage-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 1rem;
	gap: 0.75rem;
}

.stage-number-badge {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	padding: 0.375rem 0.75rem;
	font-weight: 600;
	font-size: 0.875rem;
	min-width: 2rem;
	text-align: center;
	@apply rounded-xl;
}

.stage-title-section {
	flex-grow: 1;
}

.stage-title-input .el-input__inner {
	border-color: var(--el-border-color);
	font-weight: 600;
	@apply rounded-xl;
}

.stage-title-input .el-input__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.stage-actions {
	display: flex;
	gap: 0.5rem;
}

.remove-stage-btn {
	width: 32px;
	height: 32px;
	display: flex;
	align-items: center;
	justify-content: center;
	@apply rounded-xl;
}

.remove-stage-btn .el-button__inner {
	background-color: var(--el-color-danger-light-9);
	color: var(--el-color-danger);
	border-color: var(--el-color-danger-light-7);
	padding: 0;
}

.remove-stage-btn .el-button__inner:hover {
	background-color: var(--el-color-danger-light-7);
	border-color: var(--el-color-danger-light-5);
}

.stage-card-body {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.stage-meta-row {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 1rem;
}

.meta-item {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.meta-label {
	font-size: 0.75rem;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	text-transform: uppercase;
	letter-spacing: 0.05em;
}

.team-select {
	width: 100%;
}

.team-select .el-input__inner {
	border-color: var(--el-border-color);
	@apply rounded-xl;
}

.team-select .el-input__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.duration-input {
	width: 100%;
}

.duration-input .el-input__inner {
	border-color: var(--el-border-color);
	@apply rounded-xl;
}

.duration-input .el-input__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.stage-description-section {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.description-textarea .el-textarea__inner {
	border-color: var(--el-border-color);
	resize: vertical;
	min-height: 80px;
	@apply rounded-xl;
}

.description-textarea .el-textarea__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.required-fields-section {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.fields-tags-container {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
	margin-bottom: 0.5rem;
}

.field-tag {
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
	border: 1px dashed var(--el-color-primary);
}

.field-tag:hover {
	background-color: var(--el-color-primary-light-8);
	border: 1px dashed var(--el-color-primary);
}

.add-field-btn {
	align-self: flex-start;
	border: 1px dashed var(--el-border-color);
	background-color: var(--el-fill-color-blank);
	color: var(--el-text-color-secondary);
	transition: all 0.2s ease-in-out;
	@apply rounded-xl;
}

.add-field-btn:hover {
	background-color: var(--el-fill-color-light);
	border-color: var(--el-color-primary);
	color: var(--el-color-primary);
}

.stage-connection {
	position: absolute;
	right: -1.75rem;
	top: 50%;
	transform: translateY(-50%);
	width: 1.5rem;
	height: 2px;
	display: flex;
	align-items: center;
	z-index: 10;
}

.connection-line {
	width: 100%;
	height: 2px;
	background: var(--el-color-primary);
}

.connection-arrow {
	position: absolute;
	right: -8px;
	background: var(--el-color-primary);
	border-radius: 50%;
	width: 16px;
	height: 16px;
	display: flex;
	align-items: center;
	justify-content: center;
}

.connection-arrow .el-icon {
	font-size: 0.75rem;
	color: var(--el-color-white);
}

.ai-dialog-footer {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding-top: 1.5rem;
	border-top: 1px solid var(--el-border-color-light);
}

.footer-content {
	display: flex;
	justify-content: space-between;
	align-items: center;
	width: 100%;
}

.footer-left {
	display: flex;
	align-items: center;
}

.workflow-summary {
	font-size: 0.875rem;
	color: var(--el-text-color-secondary);
}

.footer-center {
	display: flex;
	gap: 0.5rem;
}

.ai-enhance-btn .el-button__inner {
	background-color: var(--el-fill-color-light);
	color: var(--el-color-primary);
	border-color: var(--el-border-color);
}

.ai-enhance-btn .el-button__inner:hover {
	background-color: var(--el-fill-color);
	border-color: var(--el-border-color-light);
}

.ai-validate-btn .el-button__inner {
	background-color: var(--el-fill-color-light);
	color: var(--el-color-primary);
	border-color: var(--el-border-color);
}

.ai-validate-btn .el-button__inner:hover {
	background-color: var(--el-fill-color);
	border-color: var(--el-border-color-light);
}

.save-workflow-btn .el-button__inner {
	background-color: var(--el-color-primary);
	color: var(--el-color-white);
	border-color: var(--el-color-primary);
}

.save-workflow-btn .el-button__inner:hover {
	background-color: var(--el-color-primary-dark-2);
	border-color: var(--el-color-primary-dark-2);
}

.cancel-btn .el-button__inner {
	background-color: var(--el-fill-color-light);
	color: var(--el-text-color-secondary);
	border-color: var(--el-border-color);
}

.cancel-btn .el-button__inner:hover {
	background-color: var(--el-fill-color);
	border-color: var(--el-border-color-light);
}

/* New styles for Field Addition Dialog */
.ai-field-dialog .el-dialog__header {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	padding: 1.5rem;
	@apply rounded-xl;
}

.ai-field-dialog .el-dialog__header .el-dialog__title {
	font-size: 1.5rem;
	font-weight: 700;
}

.ai-field-dialog .el-dialog__body {
	padding: 1.5rem;
}

.field-dialog-content {
	text-align: center;
}

.ai-field-input .el-input__inner {
	border-color: var(--el-border-color);
	@apply rounded-xl;
}

.ai-field-input .el-input__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px var(--el-color-primary);
}

.ai-confirm-btn .el-button__inner {
	background-color: var(--el-color-primary);
	color: var(--el-color-white);
	border-color: var(--el-color-primary);
}

.ai-confirm-btn .el-button__inner:hover {
	background-color: var(--el-color-primary-dark-2);
	border-color: var(--el-color-primary-dark-2);
}

/* New styles for Enhancement Dialog */
.ai-enhance-dialog .el-dialog__header {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	padding: 1.5rem;
	@apply rounded-xl;
}

.ai-enhance-dialog .el-dialog__header .el-dialog__title {
	font-size: 1.5rem;
	font-weight: 700;
}

.ai-enhance-dialog .el-dialog__body {
	padding: 1.5rem;
}

.enhance-dialog-content {
	text-align: center;
}

.ai-enhance-input .el-input__inner {
	border-color: var(--el-border-color);
	@apply rounded-xl;
}

.ai-enhance-input .el-input__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px var(--el-color-primary);
}

.enhancement-result {
	text-align: left;
}

.ai-apply-btn .el-button__inner {
	background-color: var(--el-color-primary);
	color: var(--el-color-white);
	border-color: var(--el-color-primary);
}

.ai-apply-btn .el-button__inner:hover {
	background-color: var(--el-color-primary-dark-2);
	border-color: var(--el-color-primary-dark-2);
}

/* AI Status Indicator */
.ai-status-indicator {
	position: relative;
	width: 40px;
	height: 40px;
}

.pulse-ring {
	position: absolute;
	width: 100%;
	height: 100%;
	border: 2px solid var(--el-color-primary);
	border-radius: 50%;
	animation: pulse 1.5s infinite ease-in-out;
}

.pulse-dot {
	position: absolute;
	width: 10px;
	height: 10px;
	background-color: var(--el-color-primary);
	border-radius: 50%;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
}

@keyframes pulse {
	0% {
		transform: scale(0.9);
		opacity: 0.7;
	}
	70% {
		transform: scale(1.1);
		opacity: 0;
	}
	100% {
		transform: scale(0.9);
		opacity: 0;
	}
}

/* AI Model Config Dialog Styles */
.ai-config-dialog {
	max-height: 85vh;
}

.ai-config-dialog .el-dialog__header {
	background: var(--el-color-success);
	color: var(--el-color-white);
	padding: 1.5rem;
	@apply rounded-xl;
}

.ai-config-dialog .el-dialog__header .el-dialog__title {
	font-size: 1.5rem;
	font-weight: 700;
	color: var(--el-color-white);
}

.ai-config-dialog .el-dialog__body {
	padding: 0;
	background: var(--el-fill-color-blank);
	max-height: 70vh;
	overflow-y: auto;
}

.ai-config-container {
	height: auto;
	min-height: fit-content;
}

/* AI Tag Styles */
.ai-tag {
	background: var(--el-color-primary);
	background-color: var(--el-color-primary);
	color: var(--el-color-white);
	border-color: transparent;
	padding: 2px 6px;
	font-size: 10px;
	display: inline-flex;
	align-items: center;
}

/* Increase specificity to override Element Plus tag presets */
.ai-tag.el-tag,
.ai-tag.el-tag--primary,
.ai-tag.is-light,
.ai-tag.el-tag.el-tag--primary,
.el-tag.ai-tag,
.el-tag--primary.ai-tag,
.el-tag--primary.is-light.ai-tag {
	background: var(--el-color-primary) !important;
	background-color: var(--el-color-primary) !important;
	background-image: none !important;
	color: var(--el-color-white) !important;
	border-color: transparent !important;
	--el-tag-text-color: var(--el-color-white) !important;
}

.ai-sparkles {
	font-size: 10px;
	animation: sparkle 2s ease-in-out infinite;
	display: inline-block;
}

@keyframes sparkle {
	0%,
	100% {
		transform: scale(1) rotate(0deg);
		opacity: 1;
	}
	25% {
		transform: scale(1.1) rotate(5deg);
		opacity: 0.9;
	}
	50% {
		transform: scale(1.2) rotate(-5deg);
		opacity: 0.8;
	}
	75% {
		transform: scale(1.1) rotate(3deg);
		opacity: 0.9;
	}
}
</style>
