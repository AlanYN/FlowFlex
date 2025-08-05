<template>
	<div class="ai-workflow-generator">
		<!-- AI Header with Animated Background -->

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
									<span class="workflow-status">
										{{ workflow.status || 'active' }}
									</span>
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
									<el-tag
										size="small"
										type="success"
										v-if="currentWorkflow.isActive"
										class="status-tag"
									>
										<el-icon class="mr-1"><Check /></el-icon>
										Active
									</el-tag>
									<el-tag
										size="small"
										type="warning"
										v-if="currentWorkflow.isDefault"
										class="status-tag"
									>
										<el-icon class="mr-1"><Star /></el-icon>
										Default
									</el-tag>
								</div>
							</div>
							<div class="preview-description">
								{{ currentWorkflow.description || 'No description available' }}
							</div>

							<!-- Enhanced Stages Display -->
							<div class="workflow-stages-container">
								<div
									v-if="
										currentWorkflow.stages && currentWorkflow.stages.length > 0
									"
									class="stages-display"
								>
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
											:style="{ animationDelay: index * 0.1 + 's' }"
										>
											<div class="stage-header">
												<div class="stage-number">{{ index + 1 }}</div>
												<div class="stage-status active"></div>
											</div>
											<div class="stage-body">
												<div class="stage-name">{{ stage.name }}</div>
												<div class="stage-description">
													{{ stage.description || 'No description' }}
												</div>
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
									<div
										v-if="currentWorkflow.stages.length > 6"
										class="more-stages-indicator"
									>
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
										<p>
											This workflow doesn't have any stages yet. AI will help
											you create a complete workflow structure.
										</p>
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
						<div class="title-content">
							<h3>AI Workflow Assistant</h3>
						</div>
						<!-- Current Model Display (moved to top right) -->
						<div v-if="currentModelInfo" class="current-model-display">
							<span class="current-model-icon">
								{{ getProviderIcon(currentModelInfo.provider) }}
							</span>
							<span class="current-model-text">{{ currentModelInfo.provider }}</span>
							<div class="ai-status-dot"></div>
						</div>
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

					<div class="p-4">
						<div class="flex items-center gap-2">
							<el-input
								v-model="currentMessage"
								type="textarea"
								:rows="3"
								placeholder="Type your response here..."
								@keydown.enter="handleEnterKey"
								:disabled="aiTyping"
								resize="none"
							/>
							<el-button
								type="primary"
								@click="sendMessage"
								:loading="aiTyping"
								:disabled="!currentMessage.trim()"
								:icon="Promotion"
							/>
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
								<p>
									Based on our conversation, I can now create a customized
									workflow for you.
								</p>
							</div>
						</div>
						<div class="completion-actions">
							<el-button @click="resetConversation" class="secondary-btn">
								<el-icon class="mr-1"><Refresh /></el-icon>
								Start Over
							</el-button>
							<el-button
								type="primary"
								@click="proceedToGeneration"
								class="primary-btn"
							>
								<el-icon class="mr-1"><Setting /></el-icon>
								Generate My Workflow
							</el-button>
						</div>
					</div>
				</div>

				<!-- AI Model Selector (moved to bottom) -->
				<div class="ai-model-selector-bottom">
					<div class="model-selector-label">Model:</div>
					<el-select
						v-model="selectedAIModel"
						:placeholder="
							loadingModels
								? 'Loading models...'
								: availableModels.length === 0
								? 'No models available'
								: '🧠 AI Model'
						"
						size="default"
						style="width: 220px"
						@change="onModelChange"
						:loading="loadingModels"
						:disabled="availableModels.length === 0"
					>
						<template #loading>
							<div style="display: flex; align-items: center; padding: 10px">
								<el-icon class="is-loading" style="margin-right: 8px">
									<Loading />
								</el-icon>
								Loading AI models...
							</div>
						</template>

						<!-- Show available models -->
						<el-option
							v-for="model in availableModels"
							:key="model.id"
							:label="`${model.provider} ${model.modelName}`"
							:value="String(model.id)"
							:disabled="!model.isAvailable"
						>
							<div class="flex items-center justify-between gap-2">
								<div class="flex items-center gap-2">
									{{ getProviderIcon(model.provider) }}
									<span>{{ model.provider }}</span>
									<span>{{ model.modelName }}</span>
								</div>
								<div
									class="w-2 h-2 rounded-full"
									:class="{
										'bg-green-500': model.isAvailable,
										'bg-red-500': !model.isAvailable,
									}"
								></div>
							</div>
						</el-option>
					</el-select>
				</div>
			</div>

			<!-- AI Input Area -->
			<div class="ai-input-area" v-if="!showConversation">
				<div class="input-title">
					<el-icon class="mr-2"><Star /></el-icon>
					{{
						operationMode === 'create'
							? 'Describe Your Workflow'
							: 'Describe Your Modifications'
					}}
				</div>
				<div class="input-container">
					<el-input
						v-model="input.description"
						type="textarea"
						:placeholder="
							operationMode === 'create'
								? 'Describe your desired workflow in natural language...\n\nExample: Create a customer onboarding process with document verification, training, and feedback collection stages.'
								: 'Describe the modifications you want to make...\n\nExample: Add a trial period assessment stage with 30-day duration assigned to HR team.'
						"
						:rows="6"
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

							<!-- Direct Generation - Hidden -->
							<div class="direct-generation" style="display: none">
								<el-button
									type="success"
									:loading="realTimeGenerating"
									@click="streamGenerateWorkflow"
									:disabled="
										!input.description.trim() ||
										(operationMode === 'modify' && !selectedWorkflowId)
									"
									class="stream-btn ai-primary-btn"
									size="large"
								>
									<svg
										v-if="!realTimeGenerating"
										class="mr-2 w-5 h-5"
										xmlns="http://www.w3.org/2000/svg"
										width="20"
										height="20"
										viewBox="0 0 24 24"
										fill="none"
										stroke="currentColor"
										stroke-width="2"
										stroke-linecap="round"
										stroke-linejoin="round"
									>
										<path d="M12 8V4H8" />
										<rect width="16" height="12" x="4" y="8" rx="2" />
										<path d="M2 14h2" />
										<path d="M20 14h2" />
										<path d="M15 13v2" />
										<path d="M9 13v2" />
									</svg>
									{{
										realTimeGenerating
											? 'AI is Creating...'
											: operationMode === 'create'
											? 'Direct Generation'
											: 'Direct Enhancement'
									}}
								</el-button>

								<el-button @click="clearInput" class="clear-btn" size="large">
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
								<el-icon v-if="step.type === 'thinking'">
									<Loading class="animate-spin" />
								</el-icon>
								<el-icon v-else-if="step.type === 'generating'"><Star /></el-icon>
								<el-icon v-else-if="step.type === 'optimizing'">
									<Setting />
								</el-icon>
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
							<el-button type="primary" @click="applyWorkflow" class="apply-btn">
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
								<el-tag size="small">
									{{ result.stages?.length || 0 }} stages
								</el-tag>
								<el-tag size="small" type="info">
									{{
										result.stages?.reduce(
											(sum, stage) => sum + (stage.estimatedDuration || 0),
											0
										) || 0
									}}
									days total
								</el-tag>
							</div>
						</div>
						<div class="workflow-description">
							{{ result.generatedWorkflow.description }}
						</div>

						<!-- Stages Visualization -->
						<div class="stages-visualization">
							<div class="stages-title">Workflow Stages</div>
							<div class="stages-flow">
								<div
									v-for="(stage, index) in result.stages"
									:key="index"
									class="stage-node"
									:style="{ animationDelay: index * 0.1 + 's' }"
								>
									<div class="stage-number">{{ stage.order || index + 1 }}</div>
									<div class="stage-content">
										<div class="stage-name">{{ stage.name }}</div>
										<div class="stage-details">
											<span class="stage-team">
												{{ stage.assignedGroup }}
											</span>
											<span class="stage-duration">
												{{ stage.estimatedDuration }}d
											</span>
										</div>
									</div>
									<div
										v-if="index < result.stages.length - 1"
										class="stage-connector"
									>
										<div class="connector-line"></div>
										<div class="connector-arrow">→</div>
									</div>
								</div>
							</div>
						</div>

						<!-- AI Suggestions -->
						<div
							v-if="result.suggestions && result.suggestions.length > 0"
							class="ai-suggestions"
						>
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
import { ref, reactive, onMounted, watch, nextTick } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
// API imports
import {
	generateAIWorkflow,
	getAIWorkflowStatus,
	getAvailableWorkflows,
	getWorkflowDetails,
	modifyAIWorkflow,
	sendAIChatMessage,
	type AIWorkflowModificationInput,
	type AIChatMessage,
	type AIChatInput,
} from '@/apis/ai/workflow';
import { getUserAIModels, getDefaultAIModel, type AIModelConfig } from '@/apis/ai/config';

// Icon imports
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
	Clock,
	More,
	DocumentAdd,
	Menu,
	ChatDotRound,
	Avatar,
	Promotion,
} from '@element-plus/icons-vue';

// Props & Emits
const emit = defineEmits(['workflowGenerated']);

// Reactive Data
const operationMode = ref<'create' | 'modify'>('create');
const selectedWorkflowId = ref<number | null>(null);
const availableWorkflows = ref<any[]>([]);
const loadingWorkflows = ref(false);

const generating = ref(false);
const realTimeGenerating = ref(false);

// Conversation functionality
const showConversation = ref(true);
const conversationHistory = ref<AIChatMessage[]>([]);
const currentMessage = ref('');
const aiTyping = ref(false);
const conversationComplete = ref(false);
const conversationMessages = ref(null);
const conversationSessionId = ref('');

const input = reactive({
	description: '',
	context: '',
	requirements: [] as string[],
});

const result = ref<any>(null);
const streamSteps = ref<any[]>([]);
const currentWorkflow = ref<any>(null);

// AI Model Management
const availableModels = ref<AIModelConfig[]>([]);
const loadingModels = ref(false);
const selectedAIModel = ref<string>('');
const currentModelInfo = ref<AIModelConfig | null>(null);

// Helper function to get stage count safely
const getStageCount = (workflow: any) => {
	return workflow?.stages?.length || 0;
};

// Computed
const aiStatus = ref({
	provider: 'ZhipuAI',
	isAvailable: true,
});

// Conversation Methods
const startConversation = () => {
	showConversation.value = true;
	conversationHistory.value = [];
	conversationComplete.value = false;
	conversationSessionId.value = `session_${Date.now()}`;

	// Start with AI greeting based on operation mode
	setTimeout(() => {
		if (operationMode.value === 'modify') {
			// Modify mode - check if workflow is selected
			if (currentWorkflow.value) {
				addAIMessage(
					`Hello! I'm your AI Workflow Assistant. I see you've selected the workflow "${currentWorkflow.value.name}" for modification.`
				);

				setTimeout(() => {
					const stageCount = getStageCount(currentWorkflow.value);
					const stageInfo =
						stageCount > 0
							? `This workflow currently has ${stageCount} stages. `
							: "This workflow doesn't have any stages yet. ";

					addAIMessage(
						`${stageInfo}I'm here to help you enhance and optimize it. What specific modifications would you like to make? For example:\n\n• Add new stages or steps\n• Modify existing stages\n• Change team assignments\n• Adjust timelines\n• Improve the overall flow\n• Add quality checkpoints\n\nPlease tell me what you'd like to change or improve about this workflow.`
					);
				}, 1500);
			} else {
				addAIMessage(
					"Hello! I'm your AI Workflow Assistant. I notice you're in modification mode, but no workflow has been selected yet. Please select a workflow above that you'd like to modify, and I'll help you enhance it!"
				);
			}
		} else {
			// Create mode - original logic
			addAIMessage(
				"Hello! I'm your AI Workflow Assistant. I'm here to help you create the perfect workflow by understanding your specific needs and requirements."
			);

			setTimeout(() => {
				addAIMessage(
					"To get started, could you tell me what type of process or workflow you're looking to create? For example, it could be employee onboarding, customer support, project approval, or any other business process you have in mind."
				);
			}, 1500);
		}
	}, 500);
};

const addAIMessage = (content: string) => {
	const message: AIChatMessage = {
		role: 'assistant',
		content,
		timestamp: new Date().toLocaleTimeString(),
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
		timestamp: new Date().toLocaleTimeString(),
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
		addAIMessage(
			"I apologize, but I'm having trouble processing your message right now. Could you please try again?"
		);
	}

	aiTyping.value = false;
};

const callRealAI = async (userMessage: string) => {
	try {
		const chatInput: AIChatInput = {
			messages: conversationHistory.value,
			context: 'workflow_planning',
			sessionId: conversationSessionId.value,
			mode: 'workflow_planning',
			// 添加当前选中的模型信息
			modelId: currentModelInfo.value?.id ? String(currentModelInfo.value.id) : undefined,
			modelProvider: currentModelInfo.value?.provider,
			modelName: currentModelInfo.value?.modelName,
		};

		const response = await sendAIChatMessage(chatInput);

		// 处理后端返回的标准API响应格式
		// response 应该直接是 AIChatResponse，但如果有data包装则解包
		const aiResponse = (response as any).data || response;

		if (aiResponse.success && aiResponse.response) {
			addAIMessage(aiResponse.response.content);

			// Check if conversation is complete
			if (aiResponse.response.isComplete) {
				conversationComplete.value = true;
			}

			// Update session ID
			if (aiResponse.sessionId) {
				conversationSessionId.value = aiResponse.sessionId;
			}
		} else {
			throw new Error(aiResponse.message || 'AI response failed');
		}
	} catch (error) {
		// Fallback to enhanced simulation
		await enhancedAISimulation(userMessage);
	}
};

const enhancedAISimulation = async (userMessage: string) => {
	// Enhanced simulation with more intelligent responses
	await new Promise((resolve) => setTimeout(resolve, 1500));

	const messageCount = conversationHistory.value.filter((m) => m.role === 'user').length;
	const lowerMessage = userMessage.toLowerCase();

	if (operationMode.value === 'modify') {
		// Modify mode responses
		if (messageCount === 1) {
			// First response in modify mode
			const stageCount = currentWorkflow.value ? getStageCount(currentWorkflow.value) : 0;

			if (lowerMessage.includes('add') || lowerMessage.includes('new')) {
				addAIMessage(
					`I understand you want to add new elements to the workflow. ${
						stageCount > 0 ? `Currently there are ${stageCount} stages.` : ''
					} What specifically would you like to add? For example:\n\n• New stages before/after existing ones\n• Additional steps within current stages\n• New team members or roles\n• Extra approval checkpoints\n\nPlease describe what you'd like to add and where it should fit in the process.`
				);
			} else if (lowerMessage.includes('remove') || lowerMessage.includes('delete')) {
				addAIMessage(
					`I see you want to remove something from the workflow. ${
						stageCount > 0 ? `Looking at the current ${stageCount} stages,` : ''
					} what would you like to remove or simplify? Please specify which stages, steps, or requirements you think are unnecessary.`
				);
			} else if (
				lowerMessage.includes('change') ||
				lowerMessage.includes('modify') ||
				lowerMessage.includes('update')
			) {
				addAIMessage(
					`Perfect! You want to modify existing elements. ${
						stageCount > 0 ? `With ${stageCount} current stages,` : ''
					} what specific changes do you have in mind? For example:\n\n• Change team assignments\n• Adjust stage durations\n• Modify stage names or descriptions\n• Update approval requirements\n\nWhich stages or aspects would you like to change?`
				);
			} else {
				addAIMessage(
					`Thank you for sharing your modification ideas! ${
						stageCount > 0
							? `I can see the workflow currently has ${stageCount} stages.`
							: ''
					} To better help you enhance this workflow, could you be more specific about what changes you'd like to make? Are you looking to add, remove, or modify certain aspects?`
				);
			}
		} else if (messageCount === 2) {
			// Follow-up questions for modify mode
			addAIMessage(
				'Excellent! Now, are there any specific requirements or constraints I should consider for these modifications? For example:\n\n• Team availability or preferences\n• Timeline constraints\n• Compliance requirements\n• Integration with other processes\n\nThis will help me suggest the most practical improvements.'
			);
		} else if (messageCount >= 3) {
			// Complete the modify conversation
			addAIMessage(
				'Perfect! I now have a clear understanding of the modifications you want to make to this workflow. Based on our discussion, I can enhance the existing workflow with your specific improvements while maintaining its core structure and effectiveness.'
			);
			conversationComplete.value = true;
		}
	} else {
		// Create mode responses (original logic)
		if (messageCount === 1) {
			// Analyze the first message and respond accordingly
			if (lowerMessage.includes('onboard') || lowerMessage.includes('employee')) {
				addAIMessage(
					'Great! An employee onboarding workflow is essential for any organization. Now, who will be involved in this onboarding process? Please tell me about the teams, departments, or specific roles that will participate - for example, HR, IT, direct managers, or other stakeholders.'
				);
			} else if (lowerMessage.includes('approval') || lowerMessage.includes('review')) {
				addAIMessage(
					'Perfect! Approval workflows are crucial for maintaining control and quality. Could you tell me who will be involved in this approval process? What teams or roles need to participate, and are there different levels of approval required?'
				);
			} else if (lowerMessage.includes('customer') || lowerMessage.includes('support')) {
				addAIMessage(
					'Excellent! Customer support workflows help ensure consistent service quality. Who will be handling different parts of this process? Please describe the teams or roles involved - such as support agents, supervisors, technical teams, or escalation contacts.'
				);
			} else {
				addAIMessage(
					'That sounds like an important process to optimize! Now, could you tell me about the people and teams who will be involved? Who are the key stakeholders, and what roles or departments need to participate in this workflow?'
				);
			}
		} else if (messageCount === 2) {
			// Ask about stages and timeline
			addAIMessage(
				"Thank you for that information! Now I'd like to understand the structure and timing. How many main stages or steps do you envision for this workflow? And what's your target timeframe - should this be completed in days, weeks, or months?"
			);
		} else if (messageCount === 3) {
			// Ask about requirements and specifics
			addAIMessage(
				"Perfect! Now let's talk about the specific requirements. Are there any documents that need to be collected, approvals that must be obtained, or quality checkpoints that should be included? Also, are there any compliance requirements or company policies I should consider?"
			);
		} else if (messageCount >= 4) {
			// Complete the conversation
			addAIMessage(
				'Excellent! I now have a comprehensive understanding of your workflow requirements. Based on our conversation, I can create a detailed, customized workflow that addresses all your specific needs and includes the right people, processes, and timelines.'
			);
			conversationComplete.value = true;
		}
	}
};

const handleEnterKey = (event) => {
	if (event.shiftKey) {
		// Allow shift+enter for new lines - don't prevent default
		return;
	}
	// Prevent default for regular Enter (send message)
	event.preventDefault();
	sendMessage();
};

const resetConversation = () => {
	conversationHistory.value = [];
	conversationComplete.value = false;
	currentMessage.value = '';
	startConversation();
};

const proceedToGeneration = () => {
	// Compile the COMPLETE conversation into a comprehensive description
	let description = 'Based on our detailed conversation:\n\n';

	// Extract user messages for structured summary
	const userMessages = conversationHistory.value
		.filter((m) => m.role === 'user')
		.map((m) => m.content);

	// Create structured summary from user input
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

	// Add COMPLETE conversation history
	description += '\n=== COMPLETE CONVERSATION HISTORY ===\n\n';

	conversationHistory.value.forEach((message, index) => {
		const role = message.role === 'user' ? '👤 User' : '🤖 AI Assistant';
		const timestamp = message.timestamp || '';

		description += `${role} [${timestamp}]:\n`;
		description += `${message.content}\n\n`;

		// Add separator between messages
		if (index < conversationHistory.value.length - 1) {
			description += '---\n\n';
		}
	});

	// Add session context
	description += '\n=== SESSION INFORMATION ===\n';
	description += `Session ID: ${conversationSessionId.value}\n`;
	description += `Total Messages: ${conversationHistory.value.length}\n`;
	description += `AI Model Used: ${currentModelInfo.value?.provider || 'Unknown'} ${
		currentModelInfo.value?.modelName || ''
	}\n`;
	description +=
		'This workflow was designed through an interactive AI conversation to ensure all requirements are captured.\n';

	// Also extract the latest AI response if it contains detailed recommendations
	const latestAIMessage = conversationHistory.value.filter((m) => m.role === 'assistant').pop();

	if (latestAIMessage && latestAIMessage.content.length > 100) {
		description += '\n=== AI DETAILED RECOMMENDATIONS ===\n';
		description += latestAIMessage.content + '\n';
	}

	input.description = description;
	input.context = `AI Conversation Session: ${conversationSessionId.value} | Complete conversation with ${conversationHistory.value.length} messages`;

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
		// Restart conversation for create mode
		if (showConversation.value) {
			setTimeout(() => {
				startConversation();
			}, 300);
		}
	} else {
		loadAvailableWorkflows();
		// Restart conversation for modify mode (will guide user to select workflow)
		if (showConversation.value) {
			setTimeout(() => {
				startConversation();
			}, 300);
		}
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
		ElMessage.warning('Failed to load available workflows');
	} finally {
		loadingWorkflows.value = false;
	}
};

const handleWorkflowSelect = async (workflowId: number) => {
	if (!workflowId) {
		currentWorkflow.value = null;
		return;
	}

	try {
		const response = await getWorkflowDetails(workflowId);

		if (response.success) {
			currentWorkflow.value = response.data;
		} else {
			currentWorkflow.value = availableWorkflows.value.find((w) => w.id === workflowId);
		}
	} catch (error) {
		// Use fallback from available workflows list
		currentWorkflow.value = availableWorkflows.value.find((w) => w.id === workflowId);
	}

	// Restart conversation when workflow is selected in modify mode
	if (operationMode.value === 'modify' && showConversation.value && currentWorkflow.value) {
		setTimeout(() => {
			startConversation();
		}, 300);
	}
};

const generateWorkflow = async () => {
	if (!input.description.trim()) {
		ElMessage.warning(
			operationMode.value === 'create'
				? 'Please describe your desired workflow'
				: 'Please describe the modifications you want to make'
		);
		return;
	}

	if (operationMode.value === 'modify' && !selectedWorkflowId.value) {
		ElMessage.warning('Please select a workflow to modify');
		return;
	}

	generating.value = true;
	result.value = null;

	try {
		let response;

		if (operationMode.value === 'create') {
			// 创建新工作流 - 包含完整的AI模型和对话信息
			const workflowInput = {
				...input,
				// AI模型信息
				modelId: currentModelInfo.value?.id ? String(currentModelInfo.value.id) : undefined,
				modelProvider: currentModelInfo.value?.provider,
				modelName: currentModelInfo.value?.modelName,
				// 对话历史信息
				conversationHistory: conversationHistory.value,
				sessionId: conversationSessionId.value,
				// 元数据
				conversationMetadata: {
					totalMessages: conversationHistory.value.length,
					conversationStartTime: conversationHistory.value[0]?.timestamp,
					conversationEndTime:
						conversationHistory.value[conversationHistory.value.length - 1]?.timestamp,
					conversationMode: 'interactive_planning',
				},
			};

			response = await generateAIWorkflow(workflowInput);
		} else {
			// 修改现有工作流
			const modificationParams: AIWorkflowModificationInput = {
				workflowId: selectedWorkflowId.value!,
				description: input.description,
				context: input.context,
				requirements: input.requirements,
				preserveExisting: true,
				modificationMode: 'modify',
			};
			response = await modifyAIWorkflow(modificationParams);
		}

		if (response.success) {
			result.value = response.data;
			ElMessage.success(
				operationMode.value === 'create'
					? 'Workflow generated successfully!'
					: 'Workflow modified successfully!'
			);
		} else {
			ElMessage.error(
				response.message ||
					(operationMode.value === 'create' ? 'Generation failed' : 'Modification failed')
			);
		}
	} catch (error) {
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
		{
			type: 'thinking',
			title: 'Analyzing Requirements',
			message: 'Understanding your workflow description...',
		},
		{
			type: 'generating',
			title: 'Generating Structure',
			message: 'Creating workflow stages and connections...',
		},
		{
			type: 'optimizing',
			title: 'Optimizing Flow',
			message: 'Optimizing stage assignments and durations...',
		},
		{ type: 'complete', title: 'Generation Complete', message: 'Your workflow is ready!' },
	];

	for (let i = 0; i < steps.length; i++) {
		await new Promise((resolve) => setTimeout(resolve, 1500));
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
				type: 'warning',
			}
		);

		if (confirmed) {
			emit('workflowGenerated', {
				...result.value,
				operationMode: operationMode.value,
				selectedWorkflowId: selectedWorkflowId.value,
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

// AI Model Management Methods
const loadAvailableAIModels = async () => {
	loadingModels.value = true;

	try {
		const response = await getUserAIModels();

		if (response.success && String(response.code) === '200') {
			const models = response.data || [];
			availableModels.value = models;

			if (models.length === 0) {
				ElMessage.warning(
					'No AI models configured. Please configure at least one AI model in settings.'
				);
				return;
			}

			// Set default model if available
			try {
				const defaultResponse = await getDefaultAIModel();

				if (defaultResponse.success && String(defaultResponse.code) === '200') {
					const defaultModel = defaultResponse.data;

					if (defaultModel && defaultModel.id) {
						selectedAIModel.value = String(defaultModel.id);
						currentModelInfo.value = defaultModel;
					}
				} else {
					// If no default, select the first available model
					const firstModel = models.find((m) => m.isAvailable);
					if (firstModel) {
						selectedAIModel.value = String(firstModel.id);
						currentModelInfo.value = firstModel;
					}
				}
			} catch (defaultError) {
				// If default model fetch fails, select first available
				const firstModel = models.find((m) => m.isAvailable);
				if (firstModel) {
					selectedAIModel.value = String(firstModel.id);
					currentModelInfo.value = firstModel;
				}
			}
		} else {
			ElMessage.error('Failed to load AI models. Please check your configuration.');
		}
	} catch (error) {
		ElMessage.error('Failed to load available AI models. Please try again later.');
	} finally {
		loadingModels.value = false;
	}
};

const onModelChange = async (modelId: string) => {
	const selectedModel = availableModels.value.find((m) => String(m.id) === modelId);
	if (selectedModel) {
		selectedAIModel.value = modelId;
		currentModelInfo.value = selectedModel;
		ElMessage.success(`Switched to ${selectedModel.provider} ${selectedModel.modelName}`);

		// 通知后端切换模型
		try {
			// 重置会话ID，让后端使用新的模型
			conversationSessionId.value = `session_${Date.now()}_${modelId}`;

			// 添加系统消息提示用户模型已切换
			addAIMessage(
				`🔄 Switched to ${selectedModel.provider} ${selectedModel.modelName}. How can I help you today?`
			);
		} catch (error) {
			ElMessage.error('Failed to switch AI model');
		}
	} else {
		selectedAIModel.value = '';
		currentModelInfo.value = null;
		ElMessage.warning('Selected AI model not found.');
	}
};

// Helper function to get provider icon
const getProviderIcon = (provider: string) => {
	switch (provider.toLowerCase()) {
		case 'zhipuai':
			return '🧠';
		case 'openai':
			return '🤖';
		case 'anthropic':
			return '🔮';
		case 'claude':
			return '💎';
		case 'deepseek':
			return '🚀';
		default:
			return '⚡';
	}
};

// Lifecycle
onMounted(() => {
	// Initialize AI status check
	getAIWorkflowStatus()
		.then((response) => {
			if (response.success) {
				aiStatus.value = response.data;
			}
		})
		.catch(() => {
			aiStatus.value.isAvailable = false;
		});

	// Load available AI models
	loadAvailableAIModels();

	// Auto-start conversation mode
	setTimeout(() => {
		startConversation();
	}, 500);
});

// Watch for operation mode changes
watch(operationMode, (newMode) => {
	if (newMode === 'modify') {
		loadAvailableWorkflows();
	}
});
</script>

<style scoped lang="scss">
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

.workflow-select :deep(.el-input__inner) {
	font-size: 13px !important;
	padding: 8px 12px !important;
	line-height: 1.4 !important;
}

.workflow-select :deep(.el-select__placeholder) {
	font-size: 13px !important;
}

.workflow-option-content {
	@apply py-2;
}

.workflow-name {
	@apply font-medium text-gray-800;
	font-size: 13px !important;
}

.workflow-meta {
	@apply flex items-center gap-3 text-gray-600 mt-1;
	font-size: 11px !important;
}

/* Dropdown option styling */
:deep(.el-select-dropdown .el-select-dropdown__item) {
	padding: 8px 12px !important;
	line-height: 1.3 !important;
	min-height: auto !important;
}

:deep(.workflow-option .workflow-name) {
	font-size: 13px !important;
	font-weight: 500 !important;
	line-height: 1.4 !important;
}

:deep(.workflow-option .workflow-meta) {
	font-size: 11px !important;
	margin-top: 3px !important;
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

.stage-tag,
.status-tag {
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

.stage-team,
.stage-duration {
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

.input-footer {
	@apply flex items-center justify-between mt-4;
}

.input-actions {
	@apply flex items-center gap-3;
}

.generate-btn,
.stream-btn,
.ai-primary-btn {
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

.stream-step.thinking .step-icon {
	@apply bg-blue-500;
}
.stream-step.generating .step-icon {
	@apply bg-purple-500;
}
.stream-step.optimizing .step-icon {
	@apply bg-orange-500;
}
.stream-step.complete .step-icon {
	@apply bg-green-500;
}
.stream-step.error .step-icon {
	@apply bg-red-500;
}

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
	0%,
	100% {
		transform: translateY(0px);
	}
	50% {
		transform: translateY(-10px);
	}
}

@keyframes pulse-wave {
	0% {
		transform: scale(1);
		opacity: 1;
	}
	100% {
		transform: scale(1.5);
		opacity: 0;
	}
}

@keyframes pulse-ring {
	0% {
		transform: scale(0.33);
	}
	80%,
	100% {
		opacity: 0;
	}
}

@keyframes spin {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

@keyframes slide-in-right {
	from {
		transform: translateX(100%);
		opacity: 0;
	}
	to {
		transform: translateX(0);
		opacity: 1;
	}
}

@keyframes slide-up {
	from {
		transform: translateY(20px);
		opacity: 0;
	}
	to {
		transform: translateY(0);
		opacity: 1;
	}
}

@keyframes fade-in-up {
	from {
		transform: translateY(10px);
		opacity: 0;
	}
	to {
		transform: translateY(0);
		opacity: 1;
	}
}

/* Transitions */
.slide-down-enter-active,
.slide-down-leave-active {
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

.slide-up-enter-active,
.slide-up-leave-active {
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

.fade-in-enter-active,
.fade-in-leave-active {
	transition: all 0.3s ease;
}

.fade-in-enter-from,
.fade-in-leave-to {
	opacity: 0;
}

/* Conversation Completion Styles */
.conversation-completion {
	@apply mt-6 p-6 rounded-xl;
	background: linear-gradient(135deg, #f0f9ff 0%, #e0f7fa 50%, #f3e5f5 100%);
	border: 1px solid #e3f2fd;
	box-shadow:
		0 8px 32px rgba(59, 130, 246, 0.12),
		0 4px 16px rgba(139, 92, 246, 0.08);
	position: relative;
	overflow: hidden;
	animation: completion-appear 0.6s ease-out;
}

.conversation-completion::before {
	content: '';
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background: radial-gradient(circle at 10% 20%, rgba(59, 130, 246, 0.08) 0%, transparent 50%),
		radial-gradient(circle at 90% 80%, rgba(139, 92, 246, 0.08) 0%, transparent 50%);
	pointer-events: none;
}

.completion-card {
	@apply relative z-10 mb-6;
	display: flex;
	align-items: flex-start;
	gap: 16px;
}

.completion-icon {
	@apply w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0;
	background: linear-gradient(135deg, #10b981 0%, #059669 100%);
	box-shadow:
		0 4px 12px rgba(16, 185, 129, 0.3),
		0 0 20px rgba(16, 185, 129, 0.1);
	animation: completion-pulse 2s ease-in-out infinite;
}

.completion-icon .el-icon {
	@apply text-white text-xl;
}

.completion-content {
	@apply flex-1;
}

.completion-content h4 {
	@apply text-xl font-semibold text-gray-800 mb-2;
	background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
	background-clip: text;
}

.completion-content p {
	@apply text-gray-600 leading-relaxed;
	font-size: 15px;
}

.completion-actions {
	@apply relative z-10 flex items-center justify-end gap-3;
}

.completion-actions .secondary-btn {
	@apply px-6 py-3 rounded-lg font-medium transition-all duration-300;
	background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
	color: #64748b;
	border: 1px solid #cbd5e1;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.completion-actions .secondary-btn:hover {
	@apply transform -translate-y-0.5;
	background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e1 100%);
	color: #475569;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.completion-actions .primary-btn {
	@apply px-8 py-3 rounded-lg font-semibold text-white transition-all duration-300;
	background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 50%, #7c3aed 100%);
	border: none;
	box-shadow:
		0 4px 15px rgba(59, 130, 246, 0.4),
		0 0 30px rgba(59, 130, 246, 0.2);
	position: relative;
	overflow: hidden;
}

.completion-actions .primary-btn::before {
	content: '';
	position: absolute;
	top: 0;
	left: -100%;
	width: 100%;
	height: 100%;
	background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
	transition: left 0.5s;
}

.completion-actions .primary-btn:hover {
	@apply transform -translate-y-1;
	background: linear-gradient(135deg, #2563eb 0%, #1e40af 50%, #6d28d9 100%);
	box-shadow:
		0 8px 25px rgba(59, 130, 246, 0.5),
		0 0 40px rgba(59, 130, 246, 0.3);
}

.completion-actions .primary-btn:hover::before {
	left: 100%;
}

.completion-actions .primary-btn:active {
	@apply transform -translate-y-0;
	box-shadow:
		0 4px 15px rgba(59, 130, 246, 0.4),
		0 0 30px rgba(59, 130, 246, 0.2);
}

.completion-actions .el-icon {
	@apply mr-2;
}

/* Animations */
@keyframes completion-appear {
	0% {
		opacity: 0;
		transform: translateY(20px) scale(0.95);
	}
	100% {
		opacity: 1;
		transform: translateY(0) scale(1);
	}
}

@keyframes completion-pulse {
	0%,
	100% {
		transform: scale(1);
		box-shadow:
			0 4px 12px rgba(16, 185, 129, 0.3),
			0 0 20px rgba(16, 185, 129, 0.1);
	}
	50% {
		transform: scale(1.05);
		box-shadow:
			0 6px 20px rgba(16, 185, 129, 0.4),
			0 0 30px rgba(16, 185, 129, 0.2);
	}
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

	/* Completion responsive design */
	.completion-card {
		@apply flex-col items-center text-center gap-4;
	}

	.completion-actions {
		@apply flex-col w-full gap-3;
	}

	.completion-actions .secondary-btn,
	.completion-actions .primary-btn {
		@apply w-full justify-center py-4;
	}

	.completion-content h4 {
		@apply text-lg;
	}

	.completion-content p {
		@apply text-sm;
	}

	/* Input area responsive design */
	.input-footer {
		@apply flex-col items-stretch gap-3;
	}

	.input-actions {
		@apply w-full justify-between;
	}
}

/* Conversation Area Styles - Enhanced Modern Design */
.ai-conversation-area {
	@apply bg-white rounded-2xl border-0 p-0 mb-6;
	box-shadow:
		0 25px 50px -12px rgba(0, 0, 0, 0.12),
		0 8px 32px -8px rgba(0, 0, 0, 0.08),
		0 0 0 1px rgba(0, 0, 0, 0.05),
		inset 0 1px 0 rgba(255, 255, 255, 0.9);
	max-height: 85vh;
	display: flex;
	flex-direction: column;
	position: relative;
	overflow: hidden;
	background: linear-gradient(135deg, #ffffff 0%, #f8fafc 40%, #f1f5f9 100%);
}

.ai-conversation-area::before {
	content: '';
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background: radial-gradient(circle at 15% 15%, rgba(59, 130, 246, 0.08) 0%, transparent 50%),
		radial-gradient(circle at 85% 85%, rgba(139, 92, 246, 0.08) 0%, transparent 50%),
		radial-gradient(circle at 50% 50%, rgba(16, 185, 129, 0.04) 0%, transparent 70%);
	pointer-events: none;
	z-index: 0;
}

.conversation-header {
	padding: 28px 32px;
	border-bottom: 1px solid rgba(226, 232, 240, 0.4);
	background: linear-gradient(
		135deg,
		rgba(255, 255, 255, 0.95) 0%,
		rgba(248, 250, 252, 0.92) 40%,
		rgba(241, 245, 249, 0.88) 100%
	);
	position: relative;
	overflow: hidden;
	z-index: 2;
	border-radius: 24px 24px 0 0;
	backdrop-filter: blur(20px);
	box-shadow:
		0 4px 6px -1px rgba(0, 0, 0, 0.05),
		inset 0 1px 0 rgba(255, 255, 255, 0.9);
}

.conversation-header::before {
	content: '';
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background: radial-gradient(circle at 25% 25%, rgba(59, 130, 246, 0.12) 0%, transparent 50%),
		radial-gradient(circle at 75% 75%, rgba(139, 92, 246, 0.12) 0%, transparent 50%),
		linear-gradient(135deg, rgba(16, 185, 129, 0.05) 0%, transparent 100%);
	pointer-events: none;
	z-index: -1;
}

.conversation-title {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 8px;
}

.conversation-title > div:first-child {
	display: flex;
	align-items: center;
}

.ai-avatar-large {
	width: 56px;
	height: 56px;
	background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 50%, #8b5cf6 100%);
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	margin-right: 20px;
	box-shadow:
		0 8px 25px rgba(59, 130, 246, 0.4),
		0 4px 12px rgba(139, 92, 246, 0.2),
		inset 0 1px 0 rgba(255, 255, 255, 0.3);
	position: relative;
	z-index: 1;
	border: 2px solid rgba(255, 255, 255, 0.8);
}

.ai-avatar-large::before {
	content: '';
	position: absolute;
	inset: -3px;
	border-radius: 50%;
	background: linear-gradient(45deg, #3b82f6, #8b5cf6, #06b6d4, #10b981);
	z-index: -1;
	animation: rotate 4s linear infinite;
	opacity: 0.8;
	filter: blur(0.5px);
}

@keyframes rotate {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

.ai-avatar-large .el-icon {
	color: white;
	font-size: 24px;
}

.title-content h3 {
	margin: 0 0 6px 0;
	background: linear-gradient(135deg, #1e293b 0%, #0f172a 50%, #334155 100%);
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
	background-clip: text;
	font-size: 22px;
	font-weight: 700;
	letter-spacing: -0.025em;
	text-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.title-content p {
	margin: 0;
	color: #64748b;
	font-size: 15px;
	font-weight: 500;
	opacity: 0.8;
}

/* Current Model Display in top right - Enhanced */
.current-model-display {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 12px 18px;
	background: linear-gradient(135deg, rgba(59, 130, 246, 0.12) 0%, rgba(139, 92, 246, 0.08) 100%);
	border-radius: 24px;
	font-size: 14px;
	color: #3b82f6;
	font-weight: 600;
	border: 1px solid rgba(59, 130, 246, 0.25);
	box-shadow:
		0 4px 12px rgba(59, 130, 246, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.7);
	backdrop-filter: blur(10px);
	position: relative;
	overflow: hidden;
}

.current-model-display .current-model-icon {
	font-size: 16px;
}

.current-model-display .current-model-text {
	line-height: 1;
}

.current-model-display .ai-status-dot {
	width: 8px;
	height: 8px;
}

/* AI Model Selector at bottom - Enhanced */
.ai-model-selector-bottom {
	display: flex;
	align-items: center;
	gap: 16px;
	padding: 20px 32px;
	border-top: 1px solid rgba(226, 232, 240, 0.4);
	background: linear-gradient(
		135deg,
		rgba(248, 250, 252, 0.95) 0%,
		rgba(241, 245, 249, 0.9) 100%
	);
	position: relative;
	z-index: 1199;
	backdrop-filter: blur(20px);
	border-radius: 0 0 24px 24px;
	box-shadow:
		inset 0 1px 0 rgba(255, 255, 255, 0.8),
		0 -2px 8px rgba(0, 0, 0, 0.02);
}

.model-selector-label {
	font-size: 15px;
	font-weight: 700;
	color: #475569;
	white-space: nowrap;
	letter-spacing: -0.025em;
	text-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.ai-model-selector-bottom .el-select {
	position: relative;
	z-index: 1199;
}

.conversation-input {
	@apply p-6 border-t-0;
	background: linear-gradient(
		135deg,
		rgba(255, 255, 255, 0.95) 0%,
		rgba(248, 250, 252, 0.9) 100%
	);
	position: relative;
	backdrop-filter: blur(20px);
	border-top: 1px solid rgba(226, 232, 240, 0.4);
	border-radius: 0 0 24px 24px;
	z-index: 2;
}

.input-container {
	@apply relative;
	margin: 0 auto;
}

.input-footer {
	@apply flex items-center justify-between mt-4;
	padding: 0 4px;
}

.input-actions {
	@apply flex items-center gap-2;
}

/* Message and conversation styles */
.conversation-container {
	@apply flex flex-col flex-1;
	min-height: 0;
	position: relative;
	z-index: 1;
	height: 100%;
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
	@apply w-10 h-10 rounded-full flex items-center justify-center text-white text-sm flex-shrink-0;
	box-shadow:
		0 4px 12px rgba(0, 0, 0, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.3);
	border: 2px solid rgba(255, 255, 255, 0.8);
}

.message.assistant .message-avatar {
	@apply bg-gradient-to-br from-blue-500 via-blue-600 to-purple-600;
}

.message.user .message-avatar {
	@apply bg-gradient-to-br from-green-500 via-emerald-500 to-teal-500;
}

.message-bubble {
	@apply max-w-xs lg:max-w-md relative;
	animation: message-slide-in 0.3s ease-out;
}

.message.assistant .message-bubble {
	@apply bg-white border-0 rounded-2xl rounded-tl-sm p-4;
	background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
	box-shadow:
		0 4px 12px rgba(0, 0, 0, 0.08),
		0 0 0 1px rgba(0, 0, 0, 0.05),
		inset 0 1px 0 rgba(255, 255, 255, 0.9);
	border: 1px solid rgba(226, 232, 240, 0.5);
}

.message.user .message-bubble {
	@apply text-white rounded-2xl rounded-tr-sm p-4;
	background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 50%, #8b5cf6 100%);
	box-shadow:
		0 4px 15px rgba(59, 130, 246, 0.3),
		0 0 30px rgba(59, 130, 246, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.3);
	border: 1px solid rgba(255, 255, 255, 0.2);
}

.message-content {
	@apply text-sm leading-relaxed;
	font-weight: 500;
	line-height: 1.6;
}

.message-time {
	@apply text-xs opacity-60 mt-3;
	font-weight: 500;
	letter-spacing: 0.025em;
}

.message.user .message-time {
	@apply text-right;
	color: rgba(255, 255, 255, 0.8);
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

.typing-dots span:nth-child(1) {
	animation-delay: -0.32s;
}
.typing-dots span:nth-child(2) {
	animation-delay: -0.16s;
}
.typing-dots span:nth-child(3) {
	animation-delay: 0s;
}

.typing-text {
	@apply text-gray-500 text-sm;
}

@keyframes typing-bounce {
	0%,
	80%,
	100% {
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
	from {
		opacity: 0;
	}
	to {
		opacity: 1;
	}
}

.animate-fade-in {
	animation: fade-in 0.5s ease-out;
}

.ai-dot {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background: linear-gradient(45deg, #10b981, #059669);
	margin-right: 6px;
	animation: pulse-simple 2s infinite;
}

.ai-dot.loading {
	background: linear-gradient(45deg, #3b82f6, #1d4ed8);
	animation: spin 1s linear infinite;
}

.ai-dot.error {
	background: linear-gradient(45deg, #ef4444, #dc2626);
	animation: pulse-error 2s infinite;
}

@keyframes pulse-simple {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.6;
		transform: scale(1.2);
	}
}

@keyframes spin {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

@keyframes pulse-error {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.7;
		transform: scale(1.1);
	}
}

/* Fix selection functionality - ensure text is clickable */
:deep(.ai-model-popper-simple .el-select-dropdown__item) {
	padding: 0 !important;
	margin: 0 !important;
	background: transparent !important;
	line-height: normal !important;
	height: auto !important;
	cursor: pointer !important;
	pointer-events: auto !important;
	position: relative !important;
}

:deep(.ai-model-popper-simple .el-select-dropdown__item:hover) {
	background: transparent !important;
}

:deep(.ai-model-popper-simple .el-select-dropdown__item.selected) {
	background: transparent !important;
}

.ai-status-dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: #10b981;
	animation: pulse-simple 2s infinite;
}

/* Global z-index and container styles */
:deep(.ai-model-popper-simple) {
	background: white !important;
	border: 1px solid #e2e8f0 !important;
	border-radius: 8px !important;
	box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15) !important;
	padding: 6px !important;
	overflow: visible !important;
	min-width: 320px !important;
	width: auto !important;
	max-width: none !important;
	z-index: 1200 !important;
	position: fixed !important;
}

:deep(.el-popper__arrow) {
	display: none !important;
}

:deep(.el-popper) {
	z-index: 1200 !important;
	position: relative !important;
}

:deep(.el-select-dropdown) {
	z-index: 1200 !important;
	position: relative !important;
}

:deep(.el-select__popper) {
	z-index: 1200 !important;
	position: relative !important;
}
</style>
