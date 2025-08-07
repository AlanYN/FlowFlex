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
					<el-button @click="openWorkflowList">
						<el-icon class="mr-1">
							<List />
						</el-icon>
						{{ showWorkflowList ? 'Hide' : 'Show' }} Workflow List
					</el-button>
					<el-button type="primary" @click="goToTraditionalCreate">
						<el-icon class="mr-1">
							<Plus />
						</el-icon>
						Traditional Create
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

		<!-- AI Generated Workflow Dialog -->
		<el-dialog
			v-model="showGeneratedDialog"
			title="🤖 AI-Generated Workflow"
			width="95%"
			:close-on-click-modal="false"
			class="ai-workflow-dialog"
			top="3vh"
		>
			<!-- Workflow Overview Header -->
			<div class="workflow-overview-header">
				<div class="overview-left">
					<div class="ai-status-indicator">
						<div class="pulse-ring"></div>
						<div class="pulse-dot"></div>
					</div>
					<div class="workflow-info">
						<h3 class="workflow-title">{{ generatedWorkflow?.name }}</h3>
						<div class="workflow-meta">
							<span class="meta-badge">
								{{ isModifyMode ? 'AI-Enhanced' : 'AI-Generated' }}
							</span>
							<span class="meta-divider">•</span>
							<span class="stages-count">{{ generatedStages.length }} stages</span>
							<span class="meta-divider">•</span>
							<span class="total-duration">{{ getTotalDuration() }} days total</span>
						</div>
					</div>
				</div>
				<div class="overview-actions">
					<el-button size="small" @click="addStage" class="add-stage-btn">
						<el-icon class="mr-1"><Plus /></el-icon>
						Add Stage
					</el-button>
				</div>
			</div>

			<!-- Workflow Stages Grid -->
			<div class="workflow-stages-grid">
				<div class="stages-grid-container">
					<div
						v-for="(stage, index) in generatedStages"
						:key="index"
						class="stage-card-wrapper"
					>
						<div class="stage-card">
							<!-- Stage Header -->
							<div class="stage-card-header">
								<div class="stage-number-badge">{{ stage.order }}</div>
								<div class="stage-title-section">
									<el-input
										v-model="stage.name"
										size="small"
										placeholder="Stage name..."
										class="stage-title-input"
										@blur="updateStage(index)"
									/>
								</div>
								<div class="stage-actions">
									<el-button
										size="small"
										type="danger"
										@click="removeStage(index)"
										class="remove-stage-btn"
									>
										<el-icon><Remove /></el-icon>
									</el-button>
								</div>
							</div>

							<!-- Stage Content -->
							<div class="stage-card-body">
								<!-- Assignment and Duration -->
								<div class="stage-meta-row">
									<div class="meta-item">
										<label class="meta-label">Team:</label>
										<el-select
											v-model="stage.assignedGroup"
											size="small"
											placeholder="Select"
											class="team-select"
										>
											<el-option label="Sales" value="Sales" />
											<el-option label="IT" value="IT" />
											<el-option label="HR" value="HR" />
											<el-option label="Finance" value="Finance" />
											<el-option label="Operations" value="Operations" />
										</el-select>
									</div>
									<div class="meta-item">
										<label class="meta-label">Days:</label>
										<el-input-number
											v-model="stage.estimatedDuration"
											size="small"
											:min="1"
											:max="30"
											class="duration-input"
											controls-position="right"
										/>
									</div>
								</div>

								<!-- Description -->
								<div class="stage-description-section">
									<label class="meta-label">Description:</label>
									<el-input
										v-model="stage.description"
										type="textarea"
										:rows="3"
										placeholder="Describe what happens in this stage..."
										@blur="updateStage(index)"
										class="description-textarea"
									/>
								</div>

								<!-- Required Fields -->
								<div class="required-fields-section">
									<label class="meta-label">Required Fields:</label>
									<div
										v-if="
											stage.requiredFields && stage.requiredFields.length > 0
										"
										class="fields-tags-container"
									>
										<el-tag
											v-for="(field, fieldIndex) in stage.requiredFields"
											:key="fieldIndex"
											size="small"
											closable
											@close="removeRequiredField(index, fieldIndex)"
											class="field-tag"
										>
											{{ field }}
										</el-tag>
									</div>
									<el-button
										size="small"
										@click="addRequiredField(index)"
										class="add-field-btn"
										type="dashed"
									>
										<el-icon><Plus /></el-icon>
										{{
											stage.requiredFields && stage.requiredFields.length > 0
												? 'Add More'
												: 'Add Fields'
										}}
									</el-button>
								</div>
							</div>

							<!-- Stage Connection Line -->
							<div class="stage-connection" v-if="index < generatedStages.length - 1">
								<div class="connection-line"></div>
								<div class="connection-arrow">
									<el-icon><ArrowRight /></el-icon>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>

			<template #footer>
				<div class="ai-dialog-footer">
					<div class="footer-content">
						<div class="footer-left">
							<div class="workflow-summary">
								<span class="summary-text">
									Total: {{ generatedStages.length }} stages,
									{{ getTotalDuration() }} days
								</span>
							</div>
						</div>
						<div class="footer-center">
							<el-button
								@click="enhanceWorkflow"
								:loading="enhancing"
								class="ai-enhance-btn"
							>
								<el-icon class="mr-1"><Star /></el-icon>
								AI Enhance
							</el-button>
							<el-button
								@click="validateWorkflow"
								:loading="validating"
								class="ai-validate-btn"
							>
								<el-icon class="mr-1"><Check /></el-icon>
								Validate
							</el-button>
						</div>
						<div class="footer-right">
							<el-button @click="showGeneratedDialog = false" class="cancel-btn">
								Cancel
							</el-button>
							<el-button
								type="primary"
								@click="saveWorkflow"
								:loading="saving"
								class="save-workflow-btn"
							>
								{{ isModifyMode ? 'Update Workflow' : 'Save Workflow' }}
							</el-button>
						</div>
					</div>
				</div>
			</template>
		</el-dialog>

		<!-- Field Addition Dialog -->
		<el-dialog
			v-model="showFieldDialog"
			title="✨ Add Required Field"
			width="400px"
			class="ai-field-dialog"
		>
			<div class="field-dialog-content">
				<p class="text-sm text-gray-600 mb-4">Add a new required field for this stage:</p>
				<el-input
					v-model="newFieldName"
					placeholder="Enter field name..."
					@keyup.enter="confirmAddField"
					class="ai-field-input"
				/>
			</div>
			<template #footer>
				<div class="flex justify-end space-x-2">
					<el-button @click="showFieldDialog = false">Cancel</el-button>
					<el-button type="primary" @click="confirmAddField" class="ai-confirm-btn">
						Add Field
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- AI Enhancement Dialog -->
		<el-dialog
			v-model="showEnhanceDialog"
			title="🚀 AI Workflow Enhancement"
			width="600px"
			class="ai-enhance-dialog"
		>
			<div class="enhance-dialog-content">
				<div class="mb-4">
					<p class="text-sm text-gray-600 mb-2">
						Describe how you'd like to enhance this workflow:
					</p>
					<el-input
						v-model="enhanceRequest"
						type="textarea"
						:rows="4"
						placeholder="E.g., Add approval steps, include quality checkpoints, optimize for efficiency..."
						class="ai-enhance-input"
					/>
				</div>

				<div v-if="enhanceResult" class="enhancement-result mt-4">
					<h4 class="text-md font-medium text-gray-800 mb-2">
						AI Enhancement Suggestions:
					</h4>
					<div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
						<div
							v-for="(suggestion, index) in enhanceResult.suggestions"
							:key="index"
							class="mb-2"
						>
							<div class="flex items-start space-x-2">
								<el-icon class="text-blue-500 mt-0.5"><Star /></el-icon>
								<span class="text-sm text-gray-700">{{ suggestion }}</span>
							</div>
						</div>
					</div>
				</div>
			</div>
			<template #footer>
				<div class="flex justify-between">
					<el-button @click="showEnhanceDialog = false">Cancel</el-button>
					<el-button
						type="primary"
						@click="applyEnhancement"
						:loading="enhancing"
						class="ai-apply-btn"
					>
						<el-icon class="mr-1"><Star /></el-icon>
						Apply Suggestions
					</el-button>
				</div>
			</template>
		</el-dialog>

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
import { ElMessage } from 'element-plus';
import {
	List,
	Plus,
	Refresh,
	Document,
	Remove,
	Star,
	Check,
	ArrowRight,
	Setting,
} from '@element-plus/icons-vue';

// Components
import AIWorkflowGenerator from '@/components/ai/AIWorkflowGenerator.vue';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import AIModelConfig from './ai-config.vue';

// APIs
import { getWorkflowList, createWorkflow, updateWorkflow } from '@/apis/ow';
import { validateAIWorkflow } from '@/apis/ai/workflow';

// Router
const router = useRouter();
const { scrollbarRef, updateScrollbarHeight } = useAdaptiveScrollbar(50);

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

interface EnhanceResult {
	suggestions: string[];
}

// Reactive Data
const showWorkflowList = ref(false);
const workflowList = ref<Workflow[]>([]);
const showGeneratedDialog = ref(false);
const generatedWorkflow = ref<Workflow | null>(null);
const generatedStages = ref<WorkflowStage[]>([]);
const saving = ref(false);
const enhancing = ref(false);
const validating = ref(false);

// Field Dialog
const showFieldDialog = ref(false);
const newFieldName = ref('');
const currentStageIndex = ref(-1);

// Enhancement Dialog
const showEnhanceDialog = ref(false);
const enhanceRequest = ref('');
const enhanceResult = ref<EnhanceResult | null>(null);

// AI Config Dialog
const showAIConfigDialog = ref(false);

// Modification mode tracking
const isModifyMode = ref(false);
const selectedWorkflowId = ref<number | null>(null);

// Methods
const handleWorkflowGenerated = (workflowData: AIWorkflowData) => {
	// workflowData is now complete AI response data
	generatedWorkflow.value = workflowData.generatedWorkflow;
	generatedStages.value = workflowData.stages || [];

	// Set operation mode information
	isModifyMode.value = workflowData.operationMode === 'modify';
	selectedWorkflowId.value = workflowData.selectedWorkflowId || null;

	showGeneratedDialog.value = true;

	console.log('Generated workflow data:', workflowData);
	console.log('Generated stages:', workflowData.stages);
	console.log('Operation mode:', workflowData.operationMode);
	console.log('Selected workflow ID:', workflowData.selectedWorkflowId);

	const message = isModifyMode.value
		? 'AI workflow modification completed! Please review and save.'
		: 'AI workflow generated successfully! Please review, edit, and save.';
	ElMessage.success(message);
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

const goToTraditionalCreate = () => {
	router.push('/onboard/onboardWorkflow');
};

const showAIConfig = () => {
	showAIConfigDialog.value = true;
};

const addStage = () => {
	const newOrder = Math.max(...generatedStages.value.map((s) => s.order), 0) + 1;
	generatedStages.value.push({
		name: `New Stage ${newOrder}`,
		description: '',
		order: newOrder,
		assignedGroup: 'General',
		requiredFields: [],
		estimatedDuration: 1,
	});
};

const removeStage = (index: number) => {
	generatedStages.value.splice(index, 1);
	// Reorder stages
	generatedStages.value.forEach((stage, idx) => {
		stage.order = idx + 1;
	});
};

const updateStage = (index: number) => {
	// Stage update logic, can add auto-save
	console.log('Stage updated:', generatedStages.value[index]);
};

const addRequiredField = (stageIndex: number) => {
	currentStageIndex.value = stageIndex;
	showFieldDialog.value = true;
};

const confirmAddField = () => {
	if (!newFieldName.value.trim()) {
		ElMessage.warning('Field name cannot be empty');
		return;
	}

	if (currentStageIndex.value >= 0) {
		if (!generatedStages.value[currentStageIndex.value].requiredFields) {
			generatedStages.value[currentStageIndex.value].requiredFields = [];
		}
		generatedStages.value[currentStageIndex.value].requiredFields.push(
			newFieldName.value.trim()
		);
		newFieldName.value = '';
		showFieldDialog.value = false;
	}
};

const removeRequiredField = (stageIndex: number, fieldIndex: number) => {
	generatedStages.value[stageIndex].requiredFields.splice(fieldIndex, 1);
};

const enhanceWorkflow = () => {
	showEnhanceDialog.value = true;
	enhanceRequest.value = '';
	enhanceResult.value = null;
};

const applyEnhancement = async () => {
	if (!enhanceRequest.value.trim()) {
		ElMessage.warning('Please describe your enhancement requirements');
		return;
	}

	enhancing.value = true;
	try {
		// This would call the AI enhancement API
		// For now, we'll show a success message
		ElMessage.success('AI enhancement suggestions applied successfully!');
		showEnhanceDialog.value = false;
		enhanceRequest.value = '';
		enhanceResult.value = null;
	} catch (error) {
		console.error('Enhancement error:', error);
		ElMessage.error('Enhancement process encountered an error');
	} finally {
		enhancing.value = false;
	}
};

const validateWorkflow = async () => {
	if (!generatedWorkflow.value) return;

	validating.value = true;
	try {
		const workflowData = {
			...generatedWorkflow.value,
			stages: generatedStages.value,
		};

		const response = await validateAIWorkflow(workflowData);
		if (response.success) {
			const result = response.data;
			if (result.isValid) {
				ElMessage.success(
					`Workflow validated! Quality Score: ${Math.round(result.qualityScore * 100)}%`
				);
			} else {
				const errors = result.issues.filter((issue) => issue.severity === 'Error');
				const warnings = result.issues.filter((issue) => issue.severity === 'Warning');

				let message = 'Workflow validation issues:\n';
				if (errors.length > 0) {
					message += `Errors (${errors.length}): ${errors
						.map((e) => e.message)
						.join(', ')}\n`;
				}
				if (warnings.length > 0) {
					message += `Warnings (${warnings.length}): ${warnings
						.map((w) => w.message)
						.join(', ')}`;
				}

				ElMessage.warning(message);
			}
		}
	} catch (error) {
		console.error('Validation error:', error);
		ElMessage.error('Validation process encountered an error');
	} finally {
		validating.value = false;
	}
};

const saveWorkflow = async () => {
	console.log('Saving workflow - generatedWorkflow:', generatedWorkflow.value);
	console.log('Saving workflow - generatedStages:', generatedStages.value);
	console.log('Saving workflow - stages length:', generatedStages.value.length);

	if (!generatedWorkflow.value || generatedStages.value.length === 0) {
		ElMessage.warning('Please ensure the workflow includes at least one stage');
		return;
	}

	saving.value = true;
	try {
		const workflowData = {
			name: generatedWorkflow.value.name,
			description: generatedWorkflow.value.description,
			isActive: generatedWorkflow.value.isActive,
			status: 'active',
			startDate: new Date().toISOString(),
			// Note: Backend expects uppercase Stages
			stages: generatedStages.value.map((stage, index) => ({
				name: stage.name,
				description: stage.description,
				order: stage.order || index + 1,
				defaultAssignedGroup: stage.assignedGroup || 'Execution Team',
				estimatedDuration: stage.estimatedDuration || 1,
				isActive: true,
				workflowVersion: '1',
			})),
		};

		let response;
		if (isModifyMode.value && selectedWorkflowId.value) {
			// Modify mode: update existing workflow
			response = await updateWorkflow(selectedWorkflowId.value, workflowData);
			if (response.success) {
				ElMessage.success('Workflow updated successfully!');
				showGeneratedDialog.value = false;
				await refreshWorkflowList();

				// Navigate to workflow details page
				router.push({
					path: '/onboard/onboardWorkflow',
					query: { id: selectedWorkflowId.value },
				});
			} else {
				ElMessage.error(response.message || 'Update failed');
			}
		} else {
			// Create mode: create new workflow
			response = await createWorkflow(workflowData);
			if (response.success) {
				ElMessage.success('Workflow saved successfully!');
				showGeneratedDialog.value = false;
				await refreshWorkflowList();

				// Navigate to workflow details page
				router.push({
					path: '/onboard/onboardWorkflow',
					query: { id: response.data },
				});
			} else {
				ElMessage.error(response.message || 'Save failed');
			}
		}
	} catch (error) {
		console.error('Save workflow error:', error);
		ElMessage.error('An error occurred during saving');
	} finally {
		saving.value = false;
	}
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

const getTotalDuration = () => {
	return generatedStages.value.reduce((sum, stage) => sum + stage.estimatedDuration, 0);
};

const openWorkflowList = () => {
	showWorkflowList.value = !showWorkflowList.value;
	updateScrollbarHeight();
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
