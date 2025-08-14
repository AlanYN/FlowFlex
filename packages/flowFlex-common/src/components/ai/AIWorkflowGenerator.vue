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

interface ChecklistTask {
	id: string;
	title: string;
	description: string;
	isRequired: boolean;
	completed?: boolean;
	estimatedMinutes?: number;
	category?: string;
}

interface ChecklistItem {
	name: string;
	description: string;
	stageId?: number;
	tasks: ChecklistTask[];
}

interface QuestionnaireQuestion {
	id: string;
	question: string;
	type: 'text' | 'select' | 'multiselect' | 'number' | 'date' | 'boolean';
	options?: string[];
	isRequired: boolean;
	answer?: any;
	category?: string;
	helpText?: string;
}

interface QuestionnaireItem {
	name: string;
	description: string;
	stageId?: number;
	questions: QuestionnaireQuestion[];
}

interface ChatMessage {
	id: string;
	type:
		| 'user'
		| 'ai'
		| 'system'
		| 'generation-complete'
		| 'workflow-modification'
		| 'workflow-selection';
	content: string;
	timestamp: Date;
	data?: {
		workflow?: Workflow;
		stages?: WorkflowStage[];
		checklists?: ChecklistItem[];
		questionnaires?: QuestionnaireItem[];
		workflows?: any[]; // For workflow selection
	};
}

interface ChatSession {
	id: string;
	title: string;
	timestamp: Date;
	messages: ChatMessage[];
	isPinned?: boolean;
	tags?: string[];
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
const currentInput = ref('');
const generating = ref(false);
const applying = ref(false);
const streamingMessage = ref('');
const chatMessages = ref<ChatMessage[]>([]);
const chatHistory = ref<ChatSession[]>([]);
const currentSessionId = ref<string>('');
const conversationId = ref<string>('');
const uploadedFile = ref<File | null>(null);
const currentAIModel = ref<AIModelConfig | null>(null);
const availableModels = ref<AIModelConfig[]>([]);

// Workflow modification data
const searchedWorkflows = ref<any[]>([]);
const selectedWorkflow = ref<any | null>(null);
const isSearchingWorkflows = ref(false);

// UI State Management
const isHistoryCollapsed = ref(false);
const historySearchQuery = ref('');
const filteredHistory = computed(() => {
	if (!historySearchQuery.value.trim()) {
		return unpinnedSessions.value;
	}

	const query = historySearchQuery.value.toLowerCase();
	return unpinnedSessions.value.filter((session) => {
		return (
			session.title.toLowerCase().includes(query) ||
			session.messages.some((msg) => msg.content.toLowerCase().includes(query))
		);
	});
});
const showRenameDialog = ref(false);
const renameSessionId = ref('');
const newSessionTitle = ref('');

// Collapse state management
const checklistsCollapsed = ref(false);
const questionnairesCollapsed = ref(false);
const collapsedChecklistTasks = ref<Set<number>>(new Set());
const collapsedQuestionnaireQuestions = ref<Set<number>>(new Set());

// Stream AI Hook
const { isStreaming, startStreaming, streamFileAnalysis, stopStreaming } = useStreamAIWorkflow();

// Computed properties
const canGenerate = computed(() => {
	const hasInput = currentInput.value.trim();
	const hasFile = uploadedFile.value;
	const hasChatHistory = chatMessages.value.some((msg) => msg.type === 'user');
	return hasInput || hasFile || hasChatHistory;
});

// Show Generate button only when there's chat history
const shouldShowGenerateButton = computed(() => {
	const hasChatHistory = chatMessages.value.some((msg) => msg.type === 'user');
	return hasChatHistory;
});

const pinnedSessions = computed(() => {
	return chatHistory.value.filter((session) => session.isPinned);
});

const unpinnedSessions = computed(() => {
	return chatHistory.value.filter((session) => !session.isPinned);
});

// Dialog states

// Refs
const chatMessagesRef = ref<HTMLElement>();

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

.assistant-card {
	display: flex;
	flex-direction: column;
}

.card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	font-weight: 600;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 12px;
}

.assistant-title {
	font-size: 18px;
	font-weight: 700;
	color: #374151;
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
