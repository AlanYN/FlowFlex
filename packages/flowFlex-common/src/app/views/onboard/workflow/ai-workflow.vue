<template>
	<div class="ai-workflow-page">
		<!-- Page Header -->
		<div class="page-header mb-6">
			<div class="flex items-center justify-between">
				<div>
					<h1 class="text-2xl font-bold text-gray-900">AI Workflow Generator</h1>
					<p class="text-gray-600 mt-1">
						Use artificial intelligence to quickly create and optimize workflows
					</p>
				</div>
				<div class="flex space-x-2">
					<el-button @click="showAIConfig" type="success">
						<el-icon class="mr-1">
							<Setting />
						</el-icon>
						AI Model Config
					</el-button>
				</div>
			</div>
		</div>

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
								class="workflow-item border border-gray-200 rounded-lg p-3 hover:bg-gray-50 transition-colors"
							>
								<div class="flex items-center justify-between mb-2">
									<h4 class="font-medium text-sm">{{ workflow.name }}</h4>
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
		>
			<div class="ai-config-container">
				<AIModelConfig />
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { List, Refresh, Setting } from '@element-plus/icons-vue';

// Components
import AIWorkflowGenerator from '../../../../components/ai/AIWorkflowGenerator.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
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

<style scoped>
.ai-workflow-page {
	padding: 24px;
	max-width: 1400px;
	margin: 0 auto;
}

.page-header {
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
	color: white;
	padding: 24px;
	border-radius: 12px;
	margin-bottom: 24px;
}

.page-header h1,
.page-header p {
	color: white;
}

.grid {
	display: grid;
}

.grid-cols-12 {
	grid-template-columns: repeat(12, minmax(0, 1fr));
}

.col-span-8 {
	grid-column: span 8 / span 8;
}

.col-span-4 {
	grid-column: span 4 / span 4;
}

.col-span-12 {
	grid-column: span 12 / span 12;
}

.gap-6 {
	gap: 1.5rem;
}

.space-y-3 > * + * {
	margin-top: 0.75rem;
}

.space-y-4 > * + * {
	margin-top: 1rem;
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
	background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
	border: 1px solid #e2e8f0;
}

.workflow-item:hover {
	background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
	border-color: #cbd5e1;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.stages-count {
	display: flex;
	align-items: center;
	color: #64748b;
	font-weight: 500;
}

.empty-state {
	transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
	transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
	transition-duration: 150ms;
	background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
	border-radius: 12px;
	padding: 2rem;
	margin: 1rem 0;
}

/* New styles for AI dialog */
.ai-workflow-dialog .el-dialog__header {
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	color: white;
	padding: 1.5rem;
	border-radius: 12px 12px 0 0;
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
	border-bottom: 1px solid #e5e7eb;
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
	background-color: #f3f4f6;
	color: #4f46e5;
	border-color: #d1d5db;
}

.add-stage-btn .el-button__inner:hover {
	background-color: #e5e7eb;
	border-color: #cbd5e1;
}

.workflow-info {
	margin-left: 1.5rem;
}

.workflow-title {
	font-size: 1.5rem;
	font-weight: 700;
	color: #374151;
	margin-bottom: 0.25rem;
}

.workflow-meta {
	display: flex;
	align-items: center;
	font-size: 0.875rem;
	color: #6b7280;
}

.meta-badge {
	background-color: #e0e7ff;
	color: #4f46e5;
	border: 1px solid #d1d5db;
	border-radius: 6px;
	padding: 0.25rem 0.75rem;
	font-weight: 600;
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
	background: linear-gradient(135deg, #f9fafb 0%, #f3f4f6 100%);
	border: 1px solid #e5e7eb;
	border-radius: 12px;
	padding: 1.25rem;
	transition: all 0.2s ease-in-out;
	height: fit-content;
}

.stage-card:hover {
	transform: translateY(-3px);
	box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
	border-color: #4f46e5;
}

.stage-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 1rem;
	gap: 0.75rem;
}

.stage-number-badge {
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	color: white;
	border-radius: 8px;
	padding: 0.375rem 0.75rem;
	font-weight: 600;
	font-size: 0.875rem;
	min-width: 2rem;
	text-align: center;
}

.stage-title-section {
	flex-grow: 1;
}

.stage-title-input .el-input__inner {
	border-radius: 8px;
	border-color: #d1d5db;
	font-weight: 600;
}

.stage-title-input .el-input__inner:focus {
	border-color: #4f46e5;
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.stage-actions {
	display: flex;
	gap: 0.5rem;
}

.remove-stage-btn {
	width: 32px;
	height: 32px;
	border-radius: 6px;
	display: flex;
	align-items: center;
	justify-content: center;
}

.remove-stage-btn .el-button__inner {
	background-color: #fee2e2;
	color: #ef4444;
	border-color: #fecaca;
	padding: 0;
}

.remove-stage-btn .el-button__inner:hover {
	background-color: #fecaca;
	border-color: #f87171;
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
	color: #6b7280;
	text-transform: uppercase;
	letter-spacing: 0.05em;
}

.team-select {
	width: 100%;
}

.team-select .el-input__inner {
	border-radius: 6px;
	border-color: #d1d5db;
}

.team-select .el-input__inner:focus {
	border-color: #4f46e5;
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.duration-input {
	width: 100%;
}

.duration-input .el-input__inner {
	border-radius: 6px;
	border-color: #d1d5db;
}

.duration-input .el-input__inner:focus {
	border-color: #4f46e5;
	box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
}

.stage-description-section {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.description-textarea .el-textarea__inner {
	border-radius: 8px;
	border-color: #d1d5db;
	resize: vertical;
	min-height: 80px;
}

.description-textarea .el-textarea__inner:focus {
	border-color: #4f46e5;
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
	background-color: #e0e7ff;
	color: #4f46e5;
	border-color: #c7d2fe;
}

.field-tag:hover {
	background-color: #c7d2fe;
	border-color: #a5b4fc;
}

.add-field-btn {
	align-self: flex-start;
	border-radius: 6px;
	border: 1px dashed #d1d5db;
	background-color: #f9fafb;
	color: #6b7280;
	transition: all 0.2s ease-in-out;
}

.add-field-btn:hover {
	background-color: #f3f4f6;
	border-color: #4f46e5;
	color: #4f46e5;
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
	background: linear-gradient(90deg, #e5e7eb 0%, #4f46e5 100%);
}

.connection-arrow {
	position: absolute;
	right: -8px;
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	border-radius: 50%;
	width: 16px;
	height: 16px;
	display: flex;
	align-items: center;
	justify-content: center;
}

.connection-arrow .el-icon {
	font-size: 0.75rem;
	color: white;
}

.ai-dialog-footer {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding-top: 1.5rem;
	border-top: 1px solid #e5e7eb;
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
	color: #6b7280;
}

.footer-center {
	display: flex;
	gap: 0.5rem;
}

.ai-enhance-btn .el-button__inner {
	background-color: #f3f4f6;
	color: #4f46e5;
	border-color: #d1d5db;
}

.ai-enhance-btn .el-button__inner:hover {
	background-color: #e5e7eb;
	border-color: #cbd5e1;
}

.ai-validate-btn .el-button__inner {
	background-color: #f3f4f6;
	color: #4f46e5;
	border-color: #d1d5db;
}

.ai-validate-btn .el-button__inner:hover {
	background-color: #e5e7eb;
	border-color: #cbd5e1;
}

.save-workflow-btn .el-button__inner {
	background-color: #4f46e5;
	color: white;
	border-color: #4f46e5;
}

.save-workflow-btn .el-button__inner:hover {
	background-color: #4338ca;
	border-color: #4338ca;
}

.cancel-btn .el-button__inner {
	background-color: #f3f4f6;
	color: #6b7280;
	border-color: #d1d5db;
}

.cancel-btn .el-button__inner:hover {
	background-color: #e5e7eb;
	border-color: #cbd5e1;
}

/* New styles for Field Addition Dialog */
.ai-field-dialog .el-dialog__header {
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	color: white;
	padding: 1.5rem;
	border-radius: 12px 12px 0 0;
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
	border-radius: 8px;
	border-color: #d1d5db;
}

.ai-field-input .el-input__inner:focus {
	border-color: #4f46e5;
	box-shadow: 0 0 0 2px #4f46e5;
}

.ai-confirm-btn .el-button__inner {
	background-color: #4f46e5;
	color: white;
	border-color: #4f46e5;
}

.ai-confirm-btn .el-button__inner:hover {
	background-color: #4338ca;
	border-color: #4338ca;
}

/* New styles for Enhancement Dialog */
.ai-enhance-dialog .el-dialog__header {
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	color: white;
	padding: 1.5rem;
	border-radius: 12px 12px 0 0;
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
	border-radius: 8px;
	border-color: #d1d5db;
}

.ai-enhance-input .el-input__inner:focus {
	border-color: #4f46e5;
	box-shadow: 0 0 0 2px #4f46e5;
}

.enhancement-result {
	text-align: left;
}

.ai-apply-btn .el-button__inner {
	background-color: #4f46e5;
	color: white;
	border-color: #4f46e5;
}

.ai-apply-btn .el-button__inner:hover {
	background-color: #4338ca;
	border-color: #4338ca;
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
	border: 2px solid #4f46e5;
	border-radius: 50%;
	animation: pulse 1.5s infinite ease-in-out;
}

.pulse-dot {
	position: absolute;
	width: 10px;
	height: 10px;
	background-color: #4f46e5;
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
	background: linear-gradient(135deg, #10b981 0%, #059669 100%);
	color: white;
	padding: 1.5rem;
	border-radius: 12px 12px 0 0;
}

.ai-config-dialog .el-dialog__header .el-dialog__title {
	font-size: 1.5rem;
	font-weight: 700;
	color: white;
}

.ai-config-dialog .el-dialog__body {
	padding: 0;
	background: #f8fafc;
	max-height: 70vh;
	overflow-y: auto;
}

.ai-config-container {
	height: auto;
	min-height: fit-content;
}
</style>
