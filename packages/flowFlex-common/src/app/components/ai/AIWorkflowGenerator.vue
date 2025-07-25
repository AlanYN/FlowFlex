<template>
	<div class="ai-workflow-generator">
		<!-- AI Header with Animated Background -->
		<div class="ai-header">
			<div class="ai-background-animation"></div>
			<div class="ai-header-content">
				<div class="ai-avatar">
					<div class="ai-brain">
						<div
							class="brain-wave"
							:class="{ active: generating || realTimeGenerating }"
						></div>
						<el-icon class="brain-icon">
							<Star />
						</el-icon>
					</div>
				</div>
				<div class="ai-title">
					<h2>AI Workflow Generator</h2>
					<p class="ai-subtitle">
						Powered by {{ aiStatus.provider }} •
						{{ aiStatus.isAvailable ? 'Online' : 'Offline' }}
					</p>
				</div>
				<div class="ai-status">
					<div class="status-indicator" :class="{ online: aiStatus.isAvailable }">
						<div class="pulse-ring"></div>
						<div class="pulse-dot"></div>
					</div>
				</div>
			</div>
		</div>

		<!-- Main Content Area -->
		<div class="ai-content">
			<!-- Mode Selection with Animation -->
			<div class="mode-selector">
				<div class="mode-title">Choose Your Action</div>
				<div class="mode-options">
					<div
						class="mode-card"
						:class="{ active: operationMode === 'create' }"
						@click="handleModeChange('create')"
					>
						<div class="mode-icon">
							<el-icon><Document /></el-icon>
						</div>
						<div class="mode-text">
							<div class="mode-name">Create New</div>
							<div class="mode-desc">Generate from scratch</div>
						</div>
						<div class="mode-glow"></div>
					</div>
					<div
						class="mode-card"
						:class="{ active: operationMode === 'modify' }"
						@click="handleModeChange('modify')"
					>
						<div class="mode-icon">
							<el-icon><Star /></el-icon>
						</div>
						<div class="mode-text">
							<div class="mode-name">Modify Existing</div>
							<div class="mode-desc">Enhance your workflow</div>
						</div>
						<div class="mode-glow"></div>
					</div>
				</div>
			</div>

			<!-- Workflow Selection (for modify mode) -->
			<transition name="slide-down" mode="out-in">
				<div v-if="operationMode === 'modify'" class="workflow-selector">
					<div class="selector-title">
						<el-icon class="mr-2"><Star /></el-icon>
						Select Workflow to Modify
					</div>
					<el-select
						v-model="selectedWorkflowId"
						placeholder="Choose an existing workflow..."
						class="workflow-select"
						@change="handleWorkflowSelect"
						:loading="loadingWorkflows"
						size="large"
					>
						<el-option
							v-for="workflow in availableWorkflows"
							:key="workflow.id"
							:label="workflow.name"
							:value="workflow.id"
							class="workflow-option"
						>
							<div class="workflow-option-content">
								<div class="workflow-name">{{ workflow.name }}</div>
								<div class="workflow-meta">
									<span class="stage-count">
										{{ getStageCount(workflow) }} stages
									</span>
									<span class="workflow-status">{{ workflow.status || 'active' }}</span>
								</div>
							</div>
						</el-option>
					</el-select>

					<!-- Current Workflow Preview -->
					<transition name="fade-in" mode="out-in">
						<div v-if="currentWorkflow" class="current-workflow-preview">
							<div class="preview-header">
								<div class="workflow-title">
									<h4>{{ currentWorkflow.name }}</h4>
									<div class="workflow-subtitle">Selected for AI Enhancement</div>
								</div>
								<div class="preview-meta">
									<el-tag size="small" type="info" class="stage-tag">
										<el-icon class="mr-1"><List /></el-icon>
										{{ getStageCount(currentWorkflow) }} stages
									</el-tag>
									<el-tag size="small" type="success" v-if="currentWorkflow.isActive" class="status-tag">
										<el-icon class="mr-1"><Check /></el-icon>
										Active
									</el-tag>
									<el-tag size="small" type="warning" v-if="currentWorkflow.isDefault" class="status-tag">
										<el-icon class="mr-1"><Star /></el-icon>
										Default
									</el-tag>
								</div>
							</div>
							<div class="preview-description">{{ currentWorkflow.description || 'No description available' }}</div>
							
							<!-- Enhanced Stages Display -->
							<div class="workflow-stages-container">
								<div v-if="currentWorkflow.stages && currentWorkflow.stages.length > 0" class="stages-display">
									<div class="stages-header">
										<div class="header-content">
											<el-icon class="mr-2"><Menu /></el-icon>
											<span>Current Workflow Stages</span>
										</div>
										<div class="stages-count">
											{{ currentWorkflow.stages.length }} Total
										</div>
									</div>
									<div class="stages-grid">
										<div
											v-for="(stage, index) in currentWorkflow.stages.slice(
												0,
												6
											)"
											:key="index"
											class="stage-card"
											:style="{ animationDelay: (index * 0.1) + 's' }"
										>
											<div class="stage-header">
												<div class="stage-number">{{ index + 1 }}</div>
												<div class="stage-status active"></div>
											</div>
											<div class="stage-body">
												<div class="stage-name">{{ stage.name }}</div>
												<div class="stage-description">{{ stage.description || 'No description' }}</div>
												<div class="stage-meta">
													<span class="stage-team">
														<el-icon class="mr-1"><User /></el-icon>
														{{ stage.assignedGroup || 'Unassigned' }}
													</span>
													<span class="stage-duration">
														<el-icon class="mr-1"><Clock /></el-icon>
														{{ stage.estimatedDuration || 1 }}d
													</span>
												</div>
											</div>
										</div>
									</div>
									<div v-if="currentWorkflow.stages.length > 6" class="more-stages-indicator">
										<div class="more-stages-card">
											<div class="more-icon">
												<el-icon><More /></el-icon>
											</div>
											<div class="more-text">
												+{{ currentWorkflow.stages.length - 6 }} more stages
											</div>
										</div>
									</div>
								</div>
								
								<!-- Enhanced No Stages Display -->
								<div v-else class="no-stages-display">
									<div class="no-stages-icon">
										<el-icon><DocumentAdd /></el-icon>
									</div>
									<div class="no-stages-content">
										<h5>No Stages Configured</h5>
										<p>This workflow doesn't have any stages yet. AI will help you create a complete workflow structure.</p>
										<div class="ai-suggestion">
											<el-icon class="mr-2"><Star /></el-icon>
											<span>Perfect candidate for AI enhancement!</span>
										</div>
									</div>
								</div>
							</div>
						</div>
					</transition>
				</div>
			</transition>

			<!-- AI Conversation Area -->
			<div class="ai-conversation-area" v-if="showConversation">
				<div class="conversation-header">
					<div class="conversation-title">
						<div class="ai-avatar-large">
							<el-icon><Avatar /></el-icon>
						</div>
						<div class="title-content">
							<h3>AI Workflow Assistant</h3>
							<p>Let's discuss your workflow requirements</p>
						</div>
					</div>
					<div class="conversation-subtitle">
						I'll ask you a few questions to better understand your needs
					</div>
				</div>

				<div class="conversation-container">
					<div class="conversation-messages" ref="conversationMessages">
						<div
							v-for="(message, index) in conversationHistory"
							:key="index"
							class="message-wrapper"
							:class="message.role"
						>
							<div class="message" :class="message.role">
								<div class="message-avatar">
									<el-icon v-if="message.role === 'assistant'">
										<Avatar />
									</el-icon>
									<el-icon v-else>
										<User />
									</el-icon>
								</div>
								<div class="message-bubble">
									<div class="message-content">
										<div class="message-text">{{ message.content }}</div>
									</div>
									<div class="message-time">{{ message.timestamp }}</div>
								</div>
							</div>
						</div>
						
						<!-- AI Typing Indicator -->
						<div v-if="aiTyping" class="message-wrapper assistant">
							<div class="message assistant">
								<div class="message-avatar">
									<el-icon><Avatar /></el-icon>
								</div>
								<div class="message-bubble">
									<div class="message-content">
										<div class="typing-indicator">
											<div class="typing-dots">
												<span></span>
												<span></span>
												<span></span>
											</div>
											<span class="typing-text">AI is thinking...</span>
										</div>
									</div>
								</div>
							</div>
						</div>
					</div>

					<div class="conversation-input-area">
						<div class="input-container">
							<el-input
								v-model="currentMessage"
								type="textarea"
								:rows="3"
								placeholder="Type your response here..."
								@keydown.enter.prevent="handleEnterKey"
								class="conversation-textarea"
								:disabled="aiTyping"
								resize="none"
							/>
							<div class="input-footer">
								<div class="input-hints">
									<span class="hint-text">Press Enter to send, Shift+Enter for new line</span>
								</div>
								<div class="input-actions">
									<el-button
										@click="resetConversation"
										class="reset-btn"
										size="small"
									>
										<el-icon><Refresh /></el-icon>
									</el-button>
									<el-button
										type="primary"
										@click="sendMessage"
										:loading="aiTyping"
										:disabled="!currentMessage.trim()"
										class="send-btn"
									>
										<el-icon v-if="!aiTyping"><Promotion /></el-icon>
										Send
									</el-button>
								</div>
							</div>
						</div>
					</div>

					<!-- Conversation Actions -->
					<div class="conversation-completion" v-if="conversationComplete">
						<div class="completion-card">
							<div class="completion-icon">
								<el-icon><Check /></el-icon>
							</div>
							<div class="completion-content">
								<h4>Perfect! I have all the information I need</h4>
								<p>Based on our conversation, I can now create a customized workflow for you.</p>
							</div>
						</div>
						<div class="completion-actions">
							<el-button @click="resetConversation" class="secondary-btn">
								<el-icon class="mr-1"><Refresh /></el-icon>
								Start Over
							</el-button>
							<el-button type="primary" @click="proceedToGeneration" class="primary-btn">
								<el-icon class="mr-1"><Setting /></el-icon>
								Generate My Workflow
							</el-button>
						</div>
					</div>
				</div>
			</div>

			<!-- AI Input Area -->
			<div class="ai-input-area" v-if="!showConversation">
				<div class="input-title">
					<el-icon class="mr-2"><Star /></el-icon>
					{{ operationMode === 'create' ? 'Describe Your Workflow' : 'Describe Your Modifications' }}
				</div>
				<div class="input-container">
					<el-input
						v-model="input.description"
						type="textarea"
						:placeholder="operationMode === 'create' 
							? 'Describe your desired workflow in natural language...\n\nExample: Create a customer onboarding process with document verification, training, and feedback collection stages.' 
							: 'Describe the modifications you want to make...\n\nExample: Add a trial period assessment stage with 30-day duration assigned to HR team.'"
						:rows="6"
						class="ai-textarea"
					/>
					<div class="input-footer">
						<div class="input-actions">
							<!-- Mode Selection -->
							<div class="generation-mode-toggle">
								<el-button
									@click="startConversation"
									class="conversation-mode-btn"
									size="large"
								>
									<el-icon class="mr-2"><ChatDotRound /></el-icon>
									Interactive Mode
								</el-button>
							</div>
							
							<!-- Direct Generation -->
							<div class="direct-generation">
								<el-button
									type="success"
									:loading="realTimeGenerating"
									@click="streamGenerateWorkflow"
									:disabled="!input.description.trim() || (operationMode === 'modify' && !selectedWorkflowId)"
									class="stream-btn ai-primary-btn"
									size="large"
								>
									<svg v-if="!realTimeGenerating" class="mr-2 w-5 h-5" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
										<path d="M12 8V4H8"></path>
										<rect width="16" height="12" x="4" y="8" rx="2"></rect>
										<path d="M2 14h2"></path>
										<path d="M20 14h2"></path>
										<path d="M15 13v2"></path>
										<path d="M9 13v2"></path>
									</svg>
									{{ realTimeGenerating 
										? 'AI is Creating...' 
										: (operationMode === 'create' ? 'Direct Generation' : 'Direct Enhancement')
									}}
								</el-button>
								
								<el-button
									@click="clearInput"
									class="clear-btn"
									size="large"
								>
									<el-icon><Refresh /></el-icon>
								</el-button>
							</div>
						</div>
					</div>
				</div>
			</div>

			<!-- Real-time Generation Display -->
			<transition name="slide-up" mode="out-in">
				<div v-if="realTimeGenerating && streamSteps.length > 0" class="realtime-display">
					<div class="realtime-header">
						<el-icon class="animate-spin"><Loading /></el-icon>
						<span>AI is generating your workflow in real-time...</span>
					</div>
					<div class="stream-steps">
						<div
							v-for="(step, index) in streamSteps"
							:key="index"
							class="stream-step"
							:class="step.type"
						>
							<div class="step-icon">
								<el-icon v-if="step.type === 'thinking'"><Loading class="animate-spin" /></el-icon>
								<el-icon v-else-if="step.type === 'generating'"><Star /></el-icon>
								<el-icon v-else-if="step.type === 'optimizing'"><Setting /></el-icon>
								<el-icon v-else-if="step.type === 'complete'"><Check /></el-icon>
								<el-icon v-else-if="step.type === 'error'"><Close /></el-icon>
							</div>
							<div class="step-content">
								<div class="step-title">{{ step.title }}</div>
								<div class="step-message">{{ step.message }}</div>
							</div>
						</div>
					</div>
				</div>
			</transition>

			<!-- Generation Result -->
			<transition name="slide-up" mode="out-in">
				<div v-if="result && result.generatedWorkflow" class="generation-result">
					<div class="result-header">
						<div class="result-title">
							<el-icon class="mr-2 text-green-500"><Check /></el-icon>
							<span>Generation Complete</span>
						</div>
						<div class="result-actions">
							<div class="confidence-score">
								<span class="confidence-label">Confidence:</span>
								<div class="confidence-bar">
									<div
										class="confidence-fill"
										:style="{ width: (result.confidence || 90) + '%' }"
									></div>
								</div>
								<span class="confidence-value">{{ result.confidence || 90 }}%</span>
							</div>
							<el-button
								type="primary"
								@click="applyWorkflow"
								class="apply-btn"
							>
								<el-icon class="mr-2"><Check /></el-icon>
								Apply Workflow
							</el-button>
						</div>
					</div>

					<!-- Workflow Preview -->
					<div class="workflow-preview">
						<div class="workflow-header">
							<h3 class="workflow-name">{{ result.generatedWorkflow.name }}</h3>
							<div class="workflow-meta">
								<el-tag size="small">{{ result.stages?.length || 0 }} stages</el-tag>
								<el-tag size="small" type="info">
									{{ result.stages?.reduce((sum, stage) => sum + (stage.estimatedDuration || 0), 0) || 0 }} days total
								</el-tag>
							</div>
						</div>
						<div class="workflow-description">{{ result.generatedWorkflow.description }}</div>
						
						<!-- Stages Visualization -->
						<div class="stages-visualization">
							<div class="stages-title">Workflow Stages</div>
							<div class="stages-flow">
								<div
									v-for="(stage, index) in result.stages"
									:key="index"
									class="stage-node"
									:style="{ animationDelay: (index * 0.1) + 's' }"
								>
									<div class="stage-number">{{ stage.order || (index + 1) }}</div>
									<div class="stage-content">
										<div class="stage-name">{{ stage.name }}</div>
										<div class="stage-details">
											<span class="stage-team">{{ stage.assignedGroup }}</span>
											<span class="stage-duration">{{ stage.estimatedDuration }}d</span>
										</div>
									</div>
									<div v-if="index < result.stages.length - 1" class="stage-connector">
										<div class="connector-line"></div>
										<div class="connector-arrow">→</div>
									</div>
								</div>
							</div>
						</div>

						<!-- AI Suggestions -->
						<div v-if="result.suggestions && result.suggestions.length > 0" class="ai-suggestions">
							<div class="suggestions-title">
								<el-icon class="mr-2"><Star /></el-icon>
								AI Suggestions
							</div>
							<div class="suggestions-list">
								<div
									v-for="(suggestion, index) in result.suggestions"
									:key="index"
									class="suggestion-item"
								>
									<el-icon class="suggestion-icon"><InfoFilled /></el-icon>
									<span>{{ suggestion }}</span>
								</div>
							</div>
						</div>
					</div>
				</div>
			</transition>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch, nextTick } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useRouter } from 'vue-router';
import { getTokenobj } from '@/utils/auth';
import { 
	generateAIWorkflow, 
	getAIWorkflowStatus, 
	getAvailableWorkflows,
	getWorkflowDetails,
	modifyAIWorkflow,
	sendAIChatMessage,
	type AIWorkflowModificationInput,
	type AIChatMessage,
	type AIChatInput
} from '@/apis/ai/workflow';
import {
	User,
	Star,
	Refresh,
	Loading,
	Document,
	Check,
	Close,
	InfoFilled,
	Setting,
	List,
	Warning,
	Clock,
	More,
	DocumentAdd,
	Menu,
	ChatDotRound,
	Avatar,
	Promotion
} from '@element-plus/icons-vue';

// Props & Emits
const emit = defineEmits(['workflowGenerated']);
const router = useRouter();

// Reactive Data
const operationMode = ref<'create' | 'modify'>('create');
const selectedWorkflowId = ref<number | null>(null);
const availableWorkflows = ref<any[]>([]);
const loadingWorkflows = ref(false);

const generating = ref(false);
const realTimeGenerating = ref(false);

// Conversation functionality
const showConversation = ref(false);
const conversationHistory = ref<AIChatMessage[]>([]);
const currentMessage = ref('');
const aiTyping = ref(false);
const conversationComplete = ref(false);
const conversationMessages = ref(null);
const conversationSessionId = ref('');

const input = reactive({
	description: '',
	context: '',
	requirements: [] as string[]
});

const result = ref(null);
const streamSteps = ref([]);
const currentWorkflow = ref(null);

// Workflow list for modification mode
const workflowList = ref([]);

// Helper function to get stage count safely
const getStageCount = (workflow) => {
	return workflow.stages?.length || 0;
};

// Computed
const aiStatus = computed(() => ({
	provider: 'ZhipuAI',
	isAvailable: true
}));

// Conversation Methods
const startConversation = () => {
	showConversation.value = true;
	conversationHistory.value = [];
	conversationComplete.value = false;
	conversationSessionId.value = `session_${Date.now()}`;
	
	// Start with AI greeting
	setTimeout(() => {
		addAIMessage("Hello! I'm your AI Workflow Assistant. I'm here to help you create the perfect workflow by understanding your specific needs and requirements.");
		
		setTimeout(() => {
			addAIMessage("To get started, could you tell me what type of process or workflow you're looking to create? For example, it could be employee onboarding, customer support, project approval, or any other business process you have in mind.");
		}, 1500);
	}, 500);
};

const addAIMessage = (content: string) => {
	const message: AIChatMessage = {
		role: 'assistant',
		content,
		timestamp: new Date().toLocaleTimeString()
	};
	conversationHistory.value.push(message);
	nextTick(() => {
		scrollToBottom();
	});
};

const addUserMessage = (content: string) => {
	const message: AIChatMessage = {
		role: 'user',
		content,
		timestamp: new Date().toLocaleTimeString()
	};
	conversationHistory.value.push(message);
	nextTick(() => {
		scrollToBottom();
	});
};

const scrollToBottom = () => {
	if (conversationMessages.value) {
		const element = conversationMessages.value as HTMLElement;
		element.scrollTop = element.scrollHeight;
	}
};

const sendMessage = async () => {
	if (!currentMessage.value.trim()) return;
	
	const userMessage = currentMessage.value.trim();
	addUserMessage(userMessage);
	currentMessage.value = '';
	
	aiTyping.value = true;
	
	try {
		// Call real AI API
		await callRealAI(userMessage);
	} catch (error) {
		console.error('AI conversation error:', error);
		addAIMessage("I apologize, but I'm having trouble processing your message right now. Could you please try again?");
	}
	
	aiTyping.value = false;
};

const callRealAI = async (userMessage: string) => {
	try {
		const chatInput: AIChatInput = {
			messages: conversationHistory.value,
			context: 'workflow_planning',
			sessionId: conversationSessionId.value,
			mode: 'workflow_planning'
		};

		const response = await sendAIChatMessage(chatInput);
		
		if (response.success && response.response) {
			addAIMessage(response.response.content);
			
			// Check if conversation is complete
			if (response.response.isComplete) {
				conversationComplete.value = true;
			}
			
			// Update session ID
			if (response.sessionId) {
				conversationSessionId.value = response.sessionId;
			}
		} else {
			throw new Error(response.message || 'AI response failed');
		}
	} catch (error) {
		console.error('Real AI call failed:', error);
		// Fallback to enhanced simulation
		await enhancedAISimulation(userMessage);
	}
};

const enhancedAISimulation = async (userMessage: string) => {
	// Enhanced simulation with more intelligent responses
	await new Promise(resolve => setTimeout(resolve, 1500));
	
	const messageCount = conversationHistory.value.filter(m => m.role === 'user').length;
	const lowerMessage = userMessage.toLowerCase();
	
	if (messageCount === 1) {
		// Analyze the first message and respond accordingly
		if (lowerMessage.includes('onboard') || lowerMessage.includes('employee')) {
			addAIMessage("Great! An employee onboarding workflow is essential for any organization. Now, who will be involved in this onboarding process? Please tell me about the teams, departments, or specific roles that will participate - for example, HR, IT, direct managers, or other stakeholders.");
		} else if (lowerMessage.includes('approval') || lowerMessage.includes('review')) {
			addAIMessage("Perfect! Approval workflows are crucial for maintaining control and quality. Could you tell me who will be involved in this approval process? What teams or roles need to participate, and are there different levels of approval required?");
		} else if (lowerMessage.includes('customer') || lowerMessage.includes('support')) {
			addAIMessage("Excellent! Customer support workflows help ensure consistent service quality. Who will be handling different parts of this process? Please describe the teams or roles involved - such as support agents, supervisors, technical teams, or escalation contacts.");
		} else {
			addAIMessage("That sounds like an important process to optimize! Now, could you tell me about the people and teams who will be involved? Who are the key stakeholders, and what roles or departments need to participate in this workflow?");
		}
	} else if (messageCount === 2) {
		// Ask about stages and timeline
		addAIMessage("Thank you for that information! Now I'd like to understand the structure and timing. How many main stages or steps do you envision for this workflow? And what's your target timeframe - should this be completed in days, weeks, or months?");
	} else if (messageCount === 3) {
		// Ask about requirements and specifics
		addAIMessage("Perfect! Now let's talk about the specific requirements. Are there any documents that need to be collected, approvals that must be obtained, or quality checkpoints that should be included? Also, are there any compliance requirements or company policies I should consider?");
	} else if (messageCount >= 4) {
		// Complete the conversation
		addAIMessage("Excellent! I now have a comprehensive understanding of your workflow requirements. Based on our conversation, I can create a detailed, customized workflow that addresses all your specific needs and includes the right people, processes, and timelines.");
		conversationComplete.value = true;
	}
};

const handleEnterKey = (event) => {
	if (event.shiftKey) {
		// Allow shift+enter for new lines
		return;
	}
	sendMessage();
};

const resetConversation = () => {
	conversationHistory.value = [];
	conversationComplete.value = false;
	currentMessage.value = '';
	startConversation();
};

const proceedToGeneration = () => {
	// Compile conversation into a comprehensive description
	const userMessages = conversationHistory.value
		.filter(m => m.role === 'user')
		.map(m => m.content);
	
	const aiMessages = conversationHistory.value
		.filter(m => m.role === 'assistant')
		.map(m => m.content);
	
	// Create a structured description based on the conversation
	let description = 'Based on our detailed conversation:\n\n';
	
	if (userMessages.length > 0) {
		description += `Workflow Type: ${userMessages[0]}\n`;
	}
	if (userMessages.length > 1) {
		description += `Teams/Roles Involved: ${userMessages[1]}\n`;
	}
	if (userMessages.length > 2) {
		description += `Structure & Timeline: ${userMessages[2]}\n`;
	}
	if (userMessages.length > 3) {
		description += `Requirements & Specifics: ${userMessages[3]}\n`;
	}
	
	// Add any additional context
	description += '\nAdditional Context:\n';
	description += `Session ID: ${conversationSessionId.value}\n`;
	description += `Total Messages: ${conversationHistory.value.length}\n`;
	description += 'This workflow was designed through an interactive AI conversation to ensure all requirements are captured.';
	
	input.description = description;
	input.context = `AI Conversation Session: ${conversationSessionId.value}`;
	
	showConversation.value = false;
	
	// Automatically start generation with enhanced context
	setTimeout(() => {
		streamGenerateWorkflow();
	}, 500);
};

// Methods
const handleModeChange = (mode: 'create' | 'modify') => {
	operationMode.value = mode;
	if (mode === 'create') {
		selectedWorkflowId.value = null;
		currentWorkflow.value = null;
	} else {
		loadAvailableWorkflows();
	}
};

const loadAvailableWorkflows = async () => {
	loadingWorkflows.value = true;
	try {
		const response = await getAvailableWorkflows();
		if (response.success) {
			availableWorkflows.value = response.data || [];
		}
	} catch (error) {
		console.error('Failed to load workflows:', error);
		ElMessage.warning('Failed to load available workflows');
	} finally {
		loadingWorkflows.value = false;
	}
};

const handleWorkflowSelect = async (workflowId: number) => {
	if (!workflowId) return;
	
	try {
		const response = await getWorkflowDetails(workflowId);
		if (response.success) {
			currentWorkflow.value = response.data;
		}
	} catch (error) {
		console.error('Failed to load workflow details:', error);
		currentWorkflow.value = availableWorkflows.value.find(w => w.id === workflowId);
	}
};

const generateWorkflow = async () => {
	console.log('🔥 generateWorkflow function called!');
	console.log('🔥 Button clicked - operationMode:', operationMode.value);
	
	if (!input.description.trim()) {
		ElMessage.warning(operationMode.value === 'create' ? 'Please describe your desired workflow' : 'Please describe the modifications you want to make');
		return;
	}

	if (operationMode.value === 'modify' && !selectedWorkflowId.value) {
		ElMessage.warning('Please select a workflow to modify');
		return;
	}

	generating.value = true;
	result.value = null;

	try {
		console.log('=== DEBUG: generateWorkflow called ===');
		console.log('Operation mode:', operationMode.value);
		console.log('Selected workflow ID:', selectedWorkflowId.value);
		console.log('Input description:', input.description);
		
		let response;
		
		if (operationMode.value === 'create') {
			console.log('Taking CREATE path');
			// 创建新工作流
			response = await generateAIWorkflow(input);
		} else {
			console.log('Taking MODIFY path');
			// 修改现有工作流
			const modificationParams: AIWorkflowModificationInput = {
				workflowId: selectedWorkflowId.value!,
				description: input.description,
				context: input.context,
				requirements: input.requirements,
				preserveExisting: true,
				modificationMode: 'modify'
			};
			console.log('Sending modification request:', modificationParams);
			console.log('Selected workflow ID:', selectedWorkflowId.value);
			console.log('Current workflow:', currentWorkflow.value);
			response = await modifyAIWorkflow(modificationParams);
		}

		if (response.success) {
			result.value = response.data;
			ElMessage.success(operationMode.value === 'create' 
				? 'Workflow generated successfully!' 
				: 'Workflow modified successfully!');
		} else {
			ElMessage.error(response.message || (operationMode.value === 'create' ? 'Generation failed' : 'Modification failed'));
		}
	} catch (error) {
		console.error('Generate workflow error:', error);
		ElMessage.error('Error during workflow generation');
	} finally {
		generating.value = false;
	}
};

const streamGenerateWorkflow = async () => {
	if (!input.description.trim()) {
		ElMessage.warning('Please describe your desired workflow');
		return;
	}

	if (operationMode.value === 'modify' && !selectedWorkflowId.value) {
		ElMessage.warning('Please select a workflow to modify');
		return;
	}

	realTimeGenerating.value = true;
	streamSteps.value = [];
	
	// Simulate real-time generation steps
	const steps = [
		{ type: 'thinking', title: 'Analyzing Requirements', message: 'Understanding your workflow description...' },
		{ type: 'generating', title: 'Generating Structure', message: 'Creating workflow stages and connections...' },
		{ type: 'optimizing', title: 'Optimizing Flow', message: 'Optimizing stage assignments and durations...' },
		{ type: 'complete', title: 'Generation Complete', message: 'Your workflow is ready!' }
	];

	for (let i = 0; i < steps.length; i++) {
		await new Promise(resolve => setTimeout(resolve, 1500));
		streamSteps.value.push(steps[i]);
		
		if (i === steps.length - 1) {
			// Trigger actual generation
			await generateWorkflow();
		}
	}

	realTimeGenerating.value = false;
};

const applyWorkflow = async () => {
	if (!result.value?.generatedWorkflow) return;

	try {
		const confirmed = await ElMessageBox.confirm(
			'Are you sure you want to apply this AI-generated workflow?',
			'Confirm Application',
			{
				confirmButtonText: 'Confirm',
				cancelButtonText: 'Cancel',
				type: 'warning'
			}
		);

		if (confirmed) {
			emit('workflowGenerated', {
				...result.value,
				operationMode: operationMode.value,
				selectedWorkflowId: selectedWorkflowId.value
			});
			ElMessage.success('Workflow applied');
		}
	} catch {
		// User cancelled
	}
};

const clearInput = () => {
	input.description = '';
	input.context = '';
	input.requirements = [];
	result.value = null;
	streamSteps.value = [];
};

// Lifecycle
onMounted(() => {
	// Initialize AI status check
	getAIWorkflowStatus().then(response => {
		if (response.success) {
			aiStatus.value = response.data;
		}
	}).catch(() => {
		aiStatus.value.isAvailable = false;
	});
});

// Watch for operation mode changes
watch(operationMode, (newMode) => {
	if (newMode === 'modify') {
		loadAvailableWorkflows();
	}
});
</script>

<style scoped>
.ai-workflow-generator {
	@apply max-w-6xl mx-auto p-6;
}

/* AI Header Styles */
.ai-header {
	@apply relative overflow-hidden rounded-2xl mb-8;
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
	min-height: 120px;
}

.ai-background-animation {
	@apply absolute inset-0 opacity-20;
	background: url('data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><defs><pattern id="grid" width="10" height="10" patternUnits="userSpaceOnUse"><path d="M 10 0 L 0 0 0 10" fill="none" stroke="white" stroke-width="0.5"/></pattern></defs><rect width="100" height="100" fill="url(%23grid)"/></svg>');
	animation: float 20s ease-in-out infinite;
}

.ai-header-content {
	@apply relative z-10 flex items-center justify-between p-6;
}

.ai-avatar {
	@apply flex-shrink-0;
}

.ai-brain {
	@apply relative w-16 h-16 rounded-full flex items-center justify-center;
	background: rgba(255, 255, 255, 0.1);
	backdrop-filter: blur(10px);
}

.brain-wave {
	@apply absolute inset-0 rounded-full border-2 border-white opacity-0;
	animation: pulse-wave 2s ease-out infinite;
}

.brain-wave.active {
	@apply opacity-100;
}

.brain-icon {
	@apply text-white text-2xl z-10;
}

.ai-title {
	@apply flex-1 ml-6;
}

.ai-title h2 {
	@apply text-2xl font-bold text-white mb-1;
}

.ai-subtitle {
	@apply text-white/80 text-sm;
}

.ai-status {
	@apply flex-shrink-0;
}

.status-indicator {
	@apply relative w-6 h-6;
}

.pulse-ring {
	@apply absolute inset-0 rounded-full border-2 border-red-400 opacity-75;
	animation: pulse-ring 1.5s ease-out infinite;
}

.pulse-dot {
	@apply absolute inset-2 rounded-full bg-red-400;
}

.status-indicator.online .pulse-ring {
	@apply border-green-400;
}

.status-indicator.online .pulse-dot {
	@apply bg-green-400;
}

/* Mode Selector Styles */
.mode-selector {
	@apply mb-8;
}

.mode-title {
	@apply text-xl font-semibold text-gray-800 mb-4;
}

.mode-options {
	@apply flex gap-4;
}

.mode-card {
	@apply relative flex-1 p-6 rounded-xl border-2 border-gray-200 cursor-pointer transition-all duration-300;
	background: linear-gradient(145deg, #ffffff, #f8fafc);
}

.mode-card:hover {
	@apply border-blue-300 shadow-lg transform -translate-y-1;
}

.mode-card.active {
	@apply border-blue-500 bg-blue-50;
}

.mode-card.active .mode-glow {
	@apply opacity-100;
}

.mode-icon {
	@apply text-3xl text-blue-500 mb-3;
}

.mode-name {
	@apply font-semibold text-gray-800 mb-1;
}

.mode-desc {
	@apply text-sm text-gray-600;
}

.mode-glow {
	@apply absolute inset-0 rounded-xl opacity-0 transition-opacity duration-300;
	background: linear-gradient(145deg, rgba(59, 130, 246, 0.1), rgba(147, 51, 234, 0.1));
}

/* Workflow Selector Styles */
.workflow-selector {
	@apply mb-8 p-6 rounded-xl bg-gradient-to-r from-purple-50 to-blue-50 border border-purple-200;
}

.selector-title {
	@apply flex items-center text-lg font-semibold text-gray-800 mb-4;
}

.workflow-select {
	@apply w-full mb-4;
}

.workflow-option-content {
	@apply py-2;
}

.workflow-name {
	@apply font-medium text-gray-800;
}

.workflow-meta {
	@apply flex items-center gap-3 text-sm text-gray-600 mt-1;
}

/* Enhanced Current Workflow Preview */
.current-workflow-preview {
	@apply p-6 rounded-xl bg-white border border-gray-200 shadow-sm;
	background: linear-gradient(135deg, #f8fafc 0%, #ffffff 100%);
}

.preview-header {
	@apply flex items-start justify-between mb-4;
}

.workflow-title h4 {
	@apply text-xl font-bold text-gray-800 mb-1;
}

.workflow-subtitle {
	@apply text-sm text-blue-600 font-medium;
}

.preview-meta {
	@apply flex items-center gap-2 flex-wrap;
}

.stage-tag, .status-tag {
	@apply flex items-center;
}

.preview-description {
	@apply text-gray-600 mb-6 leading-relaxed;
}

/* Enhanced Workflow Stages Container */
.workflow-stages-container {
	@apply mt-6;
}

.stages-display {
	@apply space-y-4;
}

.stages-header {
	@apply flex items-center justify-between p-4 bg-gradient-to-r from-blue-50 to-indigo-50 rounded-lg border border-blue-100;
}

.header-content {
	@apply flex items-center text-gray-800 font-semibold;
}

.stages-count {
	@apply text-sm text-blue-600 bg-blue-100 px-3 py-1 rounded-full;
}

.stages-grid {
	@apply grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4;
}

.stage-card {
	@apply p-4 rounded-lg border border-gray-200 bg-white shadow-sm hover:shadow-md transition-all duration-300;
	background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
	animation: fade-in-up 0.6s ease-out both;
}

.stage-header {
	@apply flex items-center justify-between mb-3;
}

.stage-number {
	@apply w-8 h-8 rounded-full bg-blue-500 text-white text-sm font-bold flex items-center justify-center;
}

.stage-status {
	@apply w-3 h-3 rounded-full;
}

.stage-status.active {
	@apply bg-green-400;
}

.stage-body {
	@apply space-y-2;
}

.stage-name {
	@apply font-semibold text-gray-800 text-sm;
}

.stage-description {
	@apply text-xs text-gray-600 leading-relaxed;
}

.stage-meta {
	@apply flex items-center justify-between text-xs;
}

.stage-team, .stage-duration {
	@apply flex items-center text-gray-500;
}

.more-stages-indicator {
	@apply mt-4;
}

.more-stages-card {
	@apply flex items-center justify-center p-4 rounded-lg border-2 border-dashed border-gray-300 bg-gray-50 text-gray-500;
}

.more-icon {
	@apply mr-2 text-xl;
}

.more-text {
	@apply font-medium;
}

/* Enhanced No Stages Display */
.no-stages-display {
	@apply flex flex-col items-center justify-center p-8 rounded-xl bg-gradient-to-br from-blue-50 to-indigo-100 border-2 border-dashed border-blue-200;
}

.no-stages-icon {
	@apply text-6xl text-blue-400 mb-4;
}

.no-stages-content {
	@apply text-center space-y-3;
}

.no-stages-content h5 {
	@apply text-lg font-semibold text-gray-800;
}

.no-stages-content p {
	@apply text-gray-600 max-w-md;
}

.ai-suggestion {
	@apply flex items-center justify-center text-blue-600 font-medium bg-blue-100 px-4 py-2 rounded-full;
}

/* AI Input Area Styles */
.ai-input-area {
	@apply mb-8;
}

.input-title {
	@apply flex items-center text-lg font-semibold text-gray-800 mb-4;
}

.input-container {
	@apply relative;
}

.ai-textarea {
	@apply transition-all duration-300;
}

.input-footer {
	@apply flex items-center justify-between mt-4;
}

.input-actions {
	@apply flex items-center gap-3;
}

.generate-btn, .stream-btn, .ai-primary-btn {
	@apply relative overflow-hidden;
}

.ai-primary-btn {
	@apply bg-gradient-to-r from-green-500 to-blue-500 hover:from-green-600 hover:to-blue-600;
	box-shadow: 0 4px 15px 0 rgba(31, 38, 135, 0.37);
}

.traditional-btn {
	@apply bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300;
}

.loading-spinner {
	@apply w-4 h-4;
}

.spinner-ring {
	@apply w-full h-full border-2 border-white/30 border-t-white rounded-full;
	animation: spin 1s linear infinite;
}

/* Real-time Display */
.realtime-display {
	@apply mb-8 p-6 rounded-xl bg-gradient-to-r from-green-50 to-blue-50 border border-green-200;
}

.realtime-header {
	@apply flex items-center gap-2 text-lg font-semibold text-gray-800 mb-4;
}

.stream-steps {
	@apply space-y-3;
}

.stream-step {
	@apply flex items-start gap-3 p-3 rounded-lg bg-white/50 backdrop-blur-sm;
	animation: slide-in-right 0.5s ease-out;
}

.step-icon {
	@apply w-8 h-8 rounded-full flex items-center justify-center text-white;
}

.stream-step.thinking .step-icon { @apply bg-blue-500; }
.stream-step.generating .step-icon { @apply bg-purple-500; }
.stream-step.optimizing .step-icon { @apply bg-orange-500; }
.stream-step.complete .step-icon { @apply bg-green-500; }
.stream-step.error .step-icon { @apply bg-red-500; }

.step-title {
	@apply font-semibold text-gray-800;
}

.step-message {
	@apply text-sm text-gray-600;
}

/* Generation Result */
.generation-result {
	@apply p-6 rounded-xl bg-white border border-gray-200 shadow-lg;
	animation: slide-up 0.5s ease-out;
}

.result-header {
	@apply flex items-center justify-between mb-6;
}

.result-title {
	@apply flex items-center text-xl font-semibold text-gray-800;
}

.result-actions {
	@apply flex items-center gap-4;
}

.confidence-score {
	@apply flex items-center gap-2 text-sm;
}

.confidence-bar {
	@apply w-20 h-2 bg-gray-200 rounded-full overflow-hidden;
}

.confidence-fill {
	@apply h-full bg-gradient-to-r from-green-400 to-blue-500 transition-all duration-1000;
}

.workflow-preview {
	@apply space-y-6;
}

.workflow-header {
	@apply flex items-center justify-between;
}

.workflow-name {
	@apply text-2xl font-bold text-gray-800;
}

.workflow-meta {
	@apply flex items-center gap-2;
}

.workflow-description {
	@apply text-gray-600 leading-relaxed;
}

/* Stages Visualization */
.stages-visualization {
	@apply space-y-4;
}

.stages-title {
	@apply text-lg font-semibold text-gray-800;
}

.stages-flow {
	@apply flex flex-wrap items-center gap-4;
}

.stage-node {
	@apply relative flex items-center gap-3 p-4 rounded-xl bg-gradient-to-r from-blue-50 to-purple-50 border border-blue-200;
	animation: fade-in-up 0.6s ease-out both;
}

.stage-number {
	@apply w-8 h-8 rounded-full bg-blue-500 text-white text-sm font-bold flex items-center justify-center;
}

.stage-content {
	@apply min-w-0;
}

.stage-name {
	@apply font-semibold text-gray-800 mb-1;
}

.stage-details {
	@apply flex items-center gap-2 text-xs text-gray-600;
}

.stage-team {
	@apply px-2 py-1 rounded bg-blue-100 text-blue-700;
}

.stage-duration {
	@apply px-2 py-1 rounded bg-green-100 text-green-700;
}

.stage-connector {
	@apply flex items-center text-gray-400;
}

.connector-line {
	@apply w-8 h-px bg-gray-300;
}

.connector-arrow {
	@apply text-lg;
}

/* AI Suggestions */
.ai-suggestions {
	@apply space-y-3;
}

.suggestions-title {
	@apply flex items-center text-lg font-semibold text-gray-800;
}

.suggestions-list {
	@apply space-y-2;
}

.suggestion-item {
	@apply flex items-start gap-2 p-3 rounded-lg bg-blue-50 text-blue-800;
}

.suggestion-icon {
	@apply text-blue-500 mt-0.5;
}

/* Animations */
@keyframes float {
	0%, 100% { transform: translateY(0px); }
	50% { transform: translateY(-10px); }
}

@keyframes pulse-wave {
	0% { transform: scale(1); opacity: 1; }
	100% { transform: scale(1.5); opacity: 0; }
}

@keyframes pulse-ring {
	0% { transform: scale(0.33); }
	80%, 100% { opacity: 0; }
}

@keyframes spin {
	from { transform: rotate(0deg); }
	to { transform: rotate(360deg); }
}

@keyframes slide-in-right {
	from { transform: translateX(100%); opacity: 0; }
	to { transform: translateX(0); opacity: 1; }
}

@keyframes slide-up {
	from { transform: translateY(20px); opacity: 0; }
	to { transform: translateY(0); opacity: 1; }
}

@keyframes fade-in-up {
	from { transform: translateY(10px); opacity: 0; }
	to { transform: translateY(0); opacity: 1; }
}

/* Transitions */
.slide-down-enter-active, .slide-down-leave-active {
	transition: all 0.3s ease;
}

.slide-down-enter-from {
	transform: translateY(-20px);
	opacity: 0;
}

.slide-down-leave-to {
	transform: translateY(-20px);
	opacity: 0;
}

.slide-up-enter-active, .slide-up-leave-active {
	transition: all 0.5s ease;
}

.slide-up-enter-from {
	transform: translateY(20px);
	opacity: 0;
}

.slide-up-leave-to {
	transform: translateY(20px);
	opacity: 0;
}

.fade-in-enter-active, .fade-in-leave-active {
	transition: all 0.3s ease;
}

.fade-in-enter-from, .fade-in-leave-to {
	opacity: 0;
}

/* Responsive Design */
@media (max-width: 768px) {
	.mode-options {
		@apply flex-col;
	}
	
	.stages-flow {
		@apply flex-col items-stretch;
	}
	
	.stage-connector {
		@apply rotate-90 my-2;
	}
	
	.result-header {
		@apply flex-col items-start gap-4;
	}
	
	.workflow-header {
		@apply flex-col items-start gap-2;
	}
	
	.input-actions {
		@apply flex-wrap;
	}
	
	.stages-grid {
		@apply grid-cols-1;
	}
}

/* Conversation Area Styles */
.ai-conversation-area {
	@apply bg-white rounded-xl border border-gray-200 p-6 mb-6;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
	max-height: 80vh;
	display: flex;
	flex-direction: column;
}

.conversation-header {
	@apply text-center mb-6 pb-4 border-b border-gray-100;
}

.conversation-title {
	@apply flex items-center justify-center mb-3;
}

.ai-avatar-large {
	@apply w-12 h-12 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center text-white text-lg mr-4;
	box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.title-content h3 {
	@apply text-xl font-bold text-gray-900 mb-1;
}

.title-content p {
	@apply text-gray-600 text-sm;
}

.conversation-subtitle {
	@apply text-sm text-gray-500 italic;
}

.conversation-container {
	@apply flex flex-col flex-1;
	min-height: 0;
}

.conversation-messages {
	@apply flex-1 overflow-y-auto mb-4 space-y-4 px-2;
	scroll-behavior: smooth;
	max-height: 400px;
}

.conversation-messages::-webkit-scrollbar {
	width: 6px;
}

.conversation-messages::-webkit-scrollbar-track {
	@apply bg-gray-100 rounded-full;
}

.conversation-messages::-webkit-scrollbar-thumb {
	@apply bg-gray-300 rounded-full;
}

.conversation-messages::-webkit-scrollbar-thumb:hover {
	@apply bg-gray-400;
}

.message-wrapper {
	@apply flex items-start space-x-3 animate-fade-in;
}

.message-wrapper.assistant {
	@apply justify-start;
}

.message-wrapper.user {
	@apply justify-end flex-row-reverse space-x-reverse;
}

.message {
	@apply flex items-start space-x-3 max-w-full;
}

.message-avatar {
	@apply w-8 h-8 rounded-full flex items-center justify-center text-white text-sm flex-shrink-0;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.message.assistant .message-avatar {
	@apply bg-gradient-to-br from-blue-500 to-blue-600;
}

.message.user .message-avatar {
	@apply bg-gradient-to-br from-green-500 to-green-600;
}

.message-bubble {
	@apply max-w-xs lg:max-w-md relative;
	animation: message-slide-in 0.3s ease-out;
}

.message.assistant .message-bubble {
	@apply bg-gray-50 border border-gray-200 rounded-2xl rounded-tl-md p-3;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.message.user .message-bubble {
	@apply bg-gradient-to-br from-blue-500 to-blue-600 text-white rounded-2xl rounded-tr-md p-3;
	box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

.message-content {
	@apply text-sm leading-relaxed;
}

.message-time {
	@apply text-xs opacity-70 mt-2;
}

.message.user .message-time {
	@apply text-right text-blue-100;
}

.typing-indicator {
	@apply flex items-center gap-2 py-2;
}

.typing-dots {
	@apply flex space-x-1;
}

.typing-dots span {
	@apply w-2 h-2 bg-blue-400 rounded-full;
	animation: typing-bounce 1.4s ease-in-out infinite both;
}

.typing-dots span:nth-child(1) { animation-delay: -0.32s; }
.typing-dots span:nth-child(2) { animation-delay: -0.16s; }
.typing-dots span:nth-child(3) { animation-delay: 0s; }

.typing-text {
	@apply text-gray-500 text-sm;
}

@keyframes typing-bounce {
	0%, 80%, 100% { 
		transform: scale(0.8);
		opacity: 0.5;
	}
	40% { 
		transform: scale(1);
		opacity: 1;
	}
}

@keyframes message-slide-in {
	from {
		opacity: 0;
		transform: translateY(10px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@keyframes fade-in {
	from { opacity: 0; }
	to { opacity: 1; }
}

.animate-fade-in {
	animation: fade-in 0.5s ease-out;
}

.conversation-input-area {
	@apply border-t border-gray-200 pt-4 mt-auto;
}

.input-container {
	@apply relative;
}

.conversation-textarea {
	@apply w-full;
}

.conversation-textarea .el-textarea__inner {
	@apply border-gray-300 rounded-xl resize-none transition-all duration-200;
	min-height: 80px;
}

.conversation-textarea .el-textarea__inner:focus {
	@apply border-blue-500;
	box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.input-footer {
	@apply flex justify-between items-center mt-3;
}

.input-hints {
	@apply flex-1;
}

.hint-text {
	@apply text-xs text-gray-400;
}

.input-actions {
	@apply flex items-center space-x-2;
}

.reset-btn {
	@apply w-8 h-8 rounded-lg flex items-center justify-center text-gray-500 hover:text-gray-700 hover:bg-gray-100 transition-colors;
}

.send-btn {
	@apply px-4 py-2 rounded-lg bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white font-medium transition-all duration-200;
	box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

.send-btn:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
}

.send-btn:disabled {
	@apply opacity-50 cursor-not-allowed;
	transform: none;
}

.conversation-completion {
	@apply mt-6 animate-fade-in;
}

.completion-card {
	@apply flex items-start p-4 rounded-xl bg-gradient-to-r from-green-50 to-emerald-50 border border-green-200 mb-4;
}

.completion-icon {
	@apply mr-4 mt-1 text-2xl text-green-600;
}

.completion-content h4 {
	@apply text-lg font-semibold mb-2 text-green-800;
}

.completion-content p {
	@apply text-sm text-green-700;
}

.completion-actions {
	@apply flex justify-center space-x-4;
}

.secondary-btn {
	@apply px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:border-gray-400 hover:bg-gray-50 transition-colors;
}

.primary-btn {
	@apply px-6 py-2 bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white rounded-lg font-medium transition-all duration-200;
	box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

.primary-btn:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
}

/* Input Area Mode Toggle */
.generation-mode-toggle {
	@apply mr-4;
}

.conversation-mode-btn {
	@apply bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white border-0 transition-all duration-200;
	box-shadow: 0 2px 8px rgba(139, 92, 246, 0.3);
}

.conversation-mode-btn:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(139, 92, 246, 0.4);
}

.direct-generation {
	@apply flex items-center space-x-3;
}
</style> 