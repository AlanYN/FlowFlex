<template>
	<div class="ai-workflow-generator">
		<el-card shadow="hover">
			<template #header>
				<div class="card-header">
					<span>AI Workflow Generator</span>
					<span class="status-indicator">
						<span class="pulse-dot"></span>
						Online
					</span>
				</div>
			</template>

			<!-- Choose Action -->
			<div class="action-section">
				<h4>Choose Your Action</h4>
				<div class="action-cards">
					<div
						:class="['action-card', { active: selectedAction === 'create' }]"
						@click="selectedAction = 'create'"
					>
						<el-icon class="action-icon"><DocumentAdd /></el-icon>
						<div class="action-content">
							<h5>Create New</h5>
							<p>Generate from scratch</p>
						</div>
					</div>
					<div
						:class="['action-card', { active: selectedAction === 'modify' }]"
						@click="selectedAction = 'modify'"
					>
						<el-icon class="action-icon"><Star /></el-icon>
						<div class="action-content">
							<h5>Modify Existing</h5>
							<p>Enhance your workflow</p>
						</div>
					</div>
				</div>
			</div>

			<!-- Workflow Selection (for modify mode) -->
			<div v-if="selectedAction === 'modify'" class="workflow-select-section">
				<h4>Select Workflow to Modify</h4>
				<el-select
					v-model="selectedWorkflowId"
					placeholder="Choose a workflow"
					style="width: 100%"
				>
					<el-option
						v-for="workflow in workflowList"
						:key="workflow.id"
						:label="workflow.name"
						:value="workflow.id"
					/>
				</el-select>
			</div>

			<!-- Description Input -->
			<div class="description-section">
				<h4>Describe Your Workflow</h4>
				<el-input
					v-model="workflowDescription"
					type="textarea"
					:rows="4"
					placeholder="Describe your desired workflow in natural language..."
				/>
				<div class="example-text">
					Example: Create a customer onboarding process with document verification,
					training, and feedback collection stages.
				</div>
			</div>

			<!-- Generation Buttons -->
			<div class="generation-buttons">
				<el-button
					type="primary"
					size="large"
					@click="generateWorkflow"
					:loading="generating"
					:disabled="!workflowDescription.trim()"
				>
					<el-icon class="mr-1"><Star /></el-icon>
					{{ selectedAction === 'create' ? 'Generate Workflow' : 'Enhance Workflow' }}
				</el-button>
				<el-button
					size="large"
					@click="startInteractiveMode"
					:disabled="!workflowDescription.trim()"
				>
					<el-icon class="mr-1"><ChatDotRound /></el-icon>
					Interactive Mode
				</el-button>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { DocumentAdd, Star, ChatDotRound } from '@element-plus/icons-vue';
import { getWorkflowList } from '@/apis/ow';

// Types
interface Workflow {
	id: number;
	name: string;
	description: string;
	isActive: boolean;
}

interface WorkflowStage {
	name: string;
	description: string;
	order: number;
	assignedGroup: string;
	requiredFields: string[];
	estimatedDuration: number;
}

// Emits
const emit = defineEmits<{
	workflowGenerated: [
		data: {
			generatedWorkflow: Workflow;
			stages: WorkflowStage[];
			operationMode: string;
			selectedWorkflowId?: number;
		},
	];
}>();

// Reactive data
const selectedAction = ref<'create' | 'modify'>('create');
const selectedWorkflowId = ref<number | null>(null);
const workflowDescription = ref('');
const generating = ref(false);
const workflowList = ref<Workflow[]>([]);

// Methods
const loadWorkflowList = async () => {
	try {
		const response = await getWorkflowList();
		if (response.success) {
			workflowList.value = response.data || [];
		}
	} catch (error) {
		console.error('Failed to load workflow list:', error);
	}
};

const generateWorkflow = async () => {
	if (!workflowDescription.value.trim()) {
		ElMessage.warning('Please describe your workflow');
		return;
	}

	generating.value = true;
	try {
		// Mock AI generation for demo
		const mockWorkflow: Workflow = {
			id: Date.now(),
			name: `AI Generated Workflow - ${new Date().toLocaleDateString()}`,
			description: workflowDescription.value,
			isActive: true,
		};

		const mockStages: WorkflowStage[] = [
			{
				name: 'Initial Setup',
				description: 'Set up basic requirements and gather initial information',
				order: 1,
				assignedGroup: 'Admin',
				requiredFields: ['name', 'email', 'department'],
				estimatedDuration: 2,
			},
			{
				name: 'Processing',
				description: 'Process and validate the submitted information',
				order: 2,
				assignedGroup: 'Operations',
				requiredFields: ['validation_status', 'processed_date'],
				estimatedDuration: 3,
			},
			{
				name: 'Review & Approval',
				description: 'Review all information and provide final approval',
				order: 3,
				assignedGroup: 'Management',
				requiredFields: ['approval_status', 'comments'],
				estimatedDuration: 1,
			},
		];

		// Simulate API delay
		await new Promise((resolve) => setTimeout(resolve, 2000));

		const result = {
			generatedWorkflow: mockWorkflow,
			stages: mockStages,
			operationMode: selectedAction.value,
			selectedWorkflowId: selectedWorkflowId.value || undefined,
		};

		emit('workflowGenerated', result);
		ElMessage.success('Workflow generated successfully!');
	} catch (error) {
		console.error('Generation error:', error);
		ElMessage.error('Failed to generate workflow');
	} finally {
		generating.value = false;
	}
};

const startInteractiveMode = () => {
	ElMessage.info('Interactive mode is coming soon!');
};

// Lifecycle
onMounted(() => {
	loadWorkflowList();
});
</script>

<style scoped>
.ai-workflow-generator {
	width: 100%;
}

.card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	font-weight: 600;
}

.status-indicator {
	display: flex;
	align-items: center;
	gap: 8px;
	color: #10b981;
	font-size: 14px;
}

.pulse-dot {
	width: 8px;
	height: 8px;
	background: #10b981;
	border-radius: 50%;
	animation: pulse 2s infinite;
}

@keyframes pulse {
	0% {
		opacity: 0.5;
	}
	50% {
		opacity: 1;
	}
	100% {
		opacity: 0.5;
	}
}

.action-section {
	margin-bottom: 24px;
}

.action-section h4 {
	margin: 0 0 16px 0;
	color: #374151;
	font-size: 16px;
}

.action-cards {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 16px;
}

.action-card {
	border: 2px solid #e5e7eb;
	border-radius: 12px;
	padding: 20px;
	cursor: pointer;
	transition: all 0.2s ease;
	display: flex;
	align-items: center;
	gap: 12px;
}

.action-card:hover {
	border-color: #4f46e5;
	background: #f8fafc;
}

.action-card.active {
	border-color: #4f46e5;
	background: #eff6ff;
}

.action-icon {
	font-size: 24px;
	color: #4f46e5;
}

.action-content h5 {
	margin: 0 0 4px 0;
	color: #374151;
	font-size: 16px;
}

.action-content p {
	margin: 0;
	color: #6b7280;
	font-size: 14px;
}

.workflow-select-section {
	margin-bottom: 24px;
}

.workflow-select-section h4 {
	margin: 0 0 12px 0;
	color: #374151;
	font-size: 16px;
}

.description-section {
	margin-bottom: 24px;
}

.description-section h4 {
	margin: 0 0 12px 0;
	color: #374151;
	font-size: 16px;
}

.example-text {
	margin-top: 8px;
	color: #6b7280;
	font-size: 12px;
	font-style: italic;
}

.generation-buttons {
	display: flex;
	gap: 12px;
	justify-content: center;
}

.mr-1 {
	margin-right: 4px;
}
</style>
