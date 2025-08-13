<template>
	<div class="ai-workflow-assistant">
		<el-card shadow="hover" class="assistant-card">
      <template #header>
        <div class="card-header">
										<div class="header-left">
						<span class="assistant-title">AI Workflow Assistant</span>
          <span class="status-indicator">
            <span class="pulse-dot"></span>
							{{ currentAIModel ? `${currentAIModel.provider.toLowerCase()} ${currentAIModel.modelName}` : 'Online' }}
          </span>
					</div>
					<div class="header-actions">
						<el-button size="small" @click="clearChat" type="text">
							<el-icon><Delete /></el-icon>
							Clear Chat
						</el-button>
					</div>
        </div>
      </template>

			<div class="assistant-container">
				<!-- Chat Area -->
				<div class="chat-area">
					<!-- Chat Messages -->
					<div class="chat-messages" ref="chatMessagesRef">
						<div v-for="(message, index) in chatMessages" :key="index" class="message-item">
							<!-- User Message -->
							<div v-if="message.type === 'user'" class="user-message">
								<div class="message-content">
									<div class="message-text">{{ message.content }}</div>
									<div class="message-time">{{ formatTime(message.timestamp) }}</div>
            </div>
								<div class="message-avatar">
									<el-icon><User /></el-icon>
          </div>
							</div>

							<!-- AI Message -->
							<div v-else-if="message.type === 'ai'" class="ai-message">
								<div class="message-avatar">
									<el-icon><Star /></el-icon>
								</div>
								<div class="message-content">
									<div class="message-text" v-html="formatAIMessage(message.content)"></div>
									<div class="message-time">{{ formatTime(message.timestamp) }}</div>
								</div>
							</div>

							<!-- System Message -->
							<div v-else-if="message.type === 'system'" class="system-message">
								<div class="system-content">
									<el-icon><InfoFilled /></el-icon>
									<div class="system-text">
										<div class="system-message-text">{{ message.content }}</div>
										<div class="system-actions" v-if="message.content.includes('Let\'s create your workflow!')">
											<el-button size="small" @click="clearChat" class="start-over-btn">
												<el-icon><Refresh /></el-icon>
												Start Over
											</el-button>
											<el-button 
												type="primary" 
												size="small" 
												@click="generateWorkflow"
												:loading="generating"
												class="generate-workflow-btn"
											>
												<el-icon><Star /></el-icon>
												Generate My Workflow
											</el-button>
            </div>
          </div>
        </div>
      </div>

							<!-- Generation Complete -->
							<div v-else-if="message.type === 'generation-complete'" class="generation-complete">
								<div class="complete-header">
									<el-icon class="success-icon"><CircleCheckFilled /></el-icon>
									<h4>Generation Complete</h4>
								</div>
								
								<!-- Workflow Preview -->
								<div class="workflow-preview">
									<div class="workflow-info">
										<h5>{{ message.data?.workflow?.name || 'Workflow' }}</h5>
										<p>{{ message.data?.workflow?.description || 'Description' }}</p>
										<div class="workflow-stats">
											<span class="stat-item">
												<el-icon><List /></el-icon>
												{{ message.data?.stages?.length || 0 }} stages
											</span>
											<span class="stat-item">
												<el-icon><Clock /></el-icon>
												{{ getTotalDuration(message.data?.stages || []) }} days
											</span>
										</div>
									</div>

																	<!-- Stages Grid -->
								<div class="stages-grid">
									<div 
										v-for="(stage, stageIndex) in message.data?.stages || []" 
										:key="stageIndex"
										class="stage-card"
									>
										<div class="stage-card-header">
											<div class="stage-badge">
												<span class="stage-number">{{ stage.order }}</span>
											</div>
											<el-button 
												size="small" 
												type="danger" 
												@click="removeStage(message.data!, stageIndex)"
												class="remove-stage-btn"
												circle
											>
												<el-icon><Close /></el-icon>
											</el-button>
      </div>

										<div class="stage-card-content">
        <el-input
												v-model="stage.name" 
												size="small" 
												class="stage-title-input"
												placeholder="Stage name..."
												@blur="onStageUpdated(message.data!, stageIndex)"
											/>
											
											<el-input
												v-model="stage.description" 
          type="textarea"
												:rows="2" 
												size="small"
												placeholder="Stage description..."
												@blur="onStageUpdated(message.data!, stageIndex)"
												class="stage-description"
											/>
											
											<div class="stage-meta-compact">
												<div class="meta-item">
													<span class="meta-label">Team:</span>
													<el-select 
														v-model="stage.assignedGroup" 
														size="small" 
														placeholder="Select"
														class="meta-select"
													>
														<el-option label="Sales" value="Sales" />
														<el-option label="IT" value="IT" />
														<el-option label="HR" value="HR" />
														<el-option label="Finance" value="Finance" />
														<el-option label="Operations" value="Operations" />
													</el-select>
												</div>
												<div class="meta-item">
													<span class="meta-label">Days:</span>
													<el-input-number 
														v-model="stage.estimatedDuration" 
														size="small" 
														:min="1" 
														:max="30"
														controls-position="right"
														class="meta-number"
													/>
        </div>
      </div>

											<!-- Required Fields -->
											<div class="required-fields-compact">
												<div class="fields-header">
													<span class="fields-label">Required Fields:</span>
													<el-button 
														size="small" 
														type="primary"
														@click="addRequiredField(message.data!, stageIndex)"
														class="add-field-btn"
														circle
													>
														<el-icon><Plus /></el-icon>
													</el-button>
												</div>
												<div class="fields-tags" v-if="stage.requiredFields && stage.requiredFields.length > 0">
													<el-tag 
														v-for="(field, fieldIndex) in stage.requiredFields"
														:key="fieldIndex"
														size="small"
														closable
														@close="removeRequiredField(message.data, stageIndex, fieldIndex)"
														class="field-tag"
													>
														{{ field }}
													</el-tag>
												</div>
											</div>
										</div>
									</div>
								</div>

									<!-- Add Stage Button -->
									<el-button 
										@click="addStage(message.data)" 
										type="default" 
										class="add-stage-btn"
									>
										<el-icon><Plus /></el-icon>
										Add Stage
									</el-button>

									<!-- Checklists & Questionnaires -->
									<div class="additional-components">
										<div class="component-section">
											<h6>Checklists</h6>
											<div class="component-list">
												<div 
													v-for="(checklist, clIndex) in message.data?.checklists || []"
													:key="clIndex"
													class="component-item"
												>
													<el-input v-model="checklist.name" size="small" />
													<el-button 
														size="small" 
														type="danger" 
														@click="removeChecklist(message.data, clIndex)"
													>
														<el-icon><Remove /></el-icon>
													</el-button>
												</div>
											</div>
											<el-button 
												size="small" 
												type="default" 
												@click="addChecklist(message.data)"
											>
												<el-icon><Plus /></el-icon>
												Add Checklist
											</el-button>
										</div>

										<div class="component-section">
											<h6>Questionnaires</h6>
											<div class="component-list">
												<div 
													v-for="(questionnaire, qIndex) in message.data?.questionnaires || []"
													:key="qIndex"
													class="component-item"
												>
													<el-input v-model="questionnaire.name" size="small" />
													<el-button 
														size="small" 
														type="danger" 
														@click="removeQuestionnaire(message.data, qIndex)"
													>
														<el-icon><Remove /></el-icon>
													</el-button>
												</div>
											</div>
											<el-button 
												size="small" 
												type="default" 
												@click="addQuestionnaire(message.data)"
											>
												<el-icon><Plus /></el-icon>
												Add Questionnaire
											</el-button>
										</div>
									</div>

									<!-- Apply Button -->
									<div class="apply-section">
        <el-button 
          type="primary" 
          size="large"
											@click="applyWorkflow(message.data!)"
											:loading="applying"
											class="apply-btn"
										>
											<el-icon class="mr-1"><Check /></el-icon>
											Apply Workflow
										</el-button>
									</div>
								</div>
							</div>
						</div>

						<!-- Streaming Message -->
						<div v-if="streamingMessage || isStreaming" class="ai-message streaming">
							<div class="message-avatar">
								<el-icon><Star /></el-icon>
							</div>
							<div class="message-content">
								<div class="message-text">
									{{ streamingMessage || 'Processing...' }}
									<span class="typing-indicator">|</span>
								</div>
							</div>
						</div>
					</div>

					<!-- Input Area -->
					<div class="input-area">
						<!-- Generate Button -->
						<div class="generate-section">
        <el-button 
          type="primary" 
          size="default"
          @click="generateWorkflow"
          :loading="generating"
								:disabled="!canGenerate"
								class="generate-btn"
        >
                     <el-icon class="mr-1"><Star /></el-icon>
								Generate My Workflow
        </el-button>
						</div>

						<!-- File Upload -->
						<div class="file-upload-section">
							<el-upload
								:show-file-list="false"
								:before-upload="handleFileUpload"
								accept=".txt,.doc,.docx,.pdf,.xlsx,.xls,.jpg,.jpeg,.png,.gif,.bmp,.webp,.svg"
								class="file-upload"
							>
								<el-button size="small" type="text">
									<el-icon><Paperclip /></el-icon>
									AI Analyze Files
								</el-button>
							</el-upload>
							
							<!-- Uploaded File Display -->
							<div v-if="uploadedFile" class="uploaded-file-display">
								<div class="file-info">
									<el-icon class="file-icon">
										<Picture v-if="isImageFile(uploadedFile)" />
										<Document v-else />
									</el-icon>
									<span class="file-name">{{ uploadedFile.name }}</span>
        <el-button 
										size="small" 
										type="text" 
										@click="removeUploadedFile"
										class="remove-file-btn"
									>
										<el-icon><Close /></el-icon>
        </el-button>
								</div>
								<!-- Image Preview -->
								<div v-if="isImageFile(uploadedFile)" class="image-preview">
									<img 
										:src="getFilePreviewUrl(uploadedFile)" 
										:alt="uploadedFile.name"
										class="preview-image"
									/>
								</div>
							</div>
						</div>

						<!-- Text Input -->
						<div class="text-input-section">
							<div class="input-with-button">
								<el-input
									v-model="currentInput"
									type="textarea"
									:rows="3"
									placeholder="Type your response here..."
									@keydown="handleKeydown"
									class="chat-input"
								/>
								<div class="input-actions">
									<el-button 
										type="primary" 
										@click="sendMessage"
										:disabled="!currentInput.trim() && !uploadedFile"
										size="default"
										class="send-button"
									>
										<el-icon><Position /></el-icon>
										Send
									</el-button>
								</div>
							</div>
						</div>

						<!-- Model Selection -->
						<div class="ai-model-selector-bottom">
							<div class="model-selector-label">Model:</div>
							<el-select
								v-model="currentAIModel"
								placeholder="Select AI Model"
								size="default"
								class="model-select"
								style="width: 220px"
								value-key="id"
								@change="handleModelChange"
							>
								<el-option
									v-for="model in availableModels"
									:key="model.id"
									:label="`${model.provider.toLowerCase()} ${model.modelName}`"
									:value="model"
									:disabled="!model.isAvailable"
								>
									<div class="model-option">
										<div class="model-info">
											<span class="model-display">{{ model.provider.toLowerCase() }} {{ model.modelName }}</span>
										</div>
										<div class="model-status">
											<span 
												class="status-dot" 
												:class="{ 'online': model.isAvailable, 'offline': !model.isAvailable }"
											></span>
										</div>
									</div>
								</el-option>
							</el-select>
						</div>
					</div>
				</div>

				<!-- Chat History Sidebar -->
				<div class="chat-history" :class="{ collapsed: isHistoryCollapsed }">
					<div class="history-header">
						<div class="header-content">
							<h4 v-if="!isHistoryCollapsed">Chat History</h4>
							<div class="header-actions">
								<el-button 
									v-if="!isHistoryCollapsed"
									size="small" 
									type="primary"
									@click="startNewChat"
									class="new-chat-btn"
								>
									<el-icon><Plus /></el-icon>
									New Chat
								</el-button>
								<el-button size="small" type="text" @click="toggleHistory" class="collapse-btn">
									<el-icon>
										<ArrowRight v-if="isHistoryCollapsed" />
										<ArrowLeft v-else />
									</el-icon>
								</el-button>
							</div>
						</div>
					</div>
					<div class="history-list" v-if="!isHistoryCollapsed">
						<div 
							v-for="(session, index) in chatHistory" 
							:key="index"
							:class="['history-item', { active: currentSessionId === session.id }]"
							@click="loadChatSession(session.id)"
						>
							<div class="history-title">{{ session.title }}</div>
							<div class="history-time">{{ formatDate(session.timestamp) }}</div>
						</div>
						<div v-if="chatHistory.length === 0" class="empty-history">
							<el-icon><ChatDotRound /></el-icon>
							<p>No chat history</p>
						</div>
					</div>
				</div>
      </div>
    </el-card>

		<!-- Field Addition Dialog -->
		<el-dialog
			v-model="showFieldDialog"
			title="Add Required Field"
			width="400px"
		>
			<el-input
				v-model="newFieldName"
				placeholder="Enter field name..."
				@keyup.enter="confirmAddField"
			/>
			<template #footer>
				<div class="dialog-footer">
					<el-button @click="showFieldDialog = false">Cancel</el-button>
					<el-button type="primary" @click="confirmAddField">Add Field</el-button>
				</div>
			</template>
		</el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
import { 
  Star, 
	User,
	InfoFilled,
	CircleCheckFilled,
	List,
	Clock,
	Remove,
	Plus,
	Check,
	Delete,
	ArrowLeft,
	ArrowRight,
	ChatDotRound,
	Paperclip,
	Position,
	Refresh,
	Document,
	Close,
	Picture
} from '@element-plus/icons-vue';
import { createWorkflow } from '@/apis/ow';
import { createChecklist } from '@/apis/ow/checklist';
import { createQuestionnaire } from '@/apis/ow/questionnaire';
import { sendAIChatMessage, streamAIChatMessageNative, type AIChatMessage } from '../../app/apis/ai/workflow';
import { getDefaultAIModel, getUserAIModels, type AIModelConfig } from '../../app/apis/ai/config';
import { useStreamAIWorkflow } from '../../hooks/useStreamAIWorkflow';

// Types
interface Workflow {
	id?: number;
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



interface ChecklistItem {
	name: string;
	description: string;
	stageId?: number;
}

interface QuestionnaireItem {
	name: string;
	description: string;
	stageId?: number;
}

interface ChatMessage {
	id: string;
	type: 'user' | 'ai' | 'system' | 'generation-complete';
	content: string;
	timestamp: Date;
	data?: {
		workflow: Workflow;
		stages: WorkflowStage[];
		checklists?: ChecklistItem[];
		questionnaires?: QuestionnaireItem[];
	};
}

interface ChatSession {
	id: string;
	title: string;
	timestamp: Date;
	messages: ChatMessage[];
}

// Emits
const emit = defineEmits<{
  workflowGenerated: [data: {
    generatedWorkflow: Workflow;
    stages: WorkflowStage[];
    operationMode: string;
    selectedWorkflowId?: number;
  }]
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

// UI State Management
const isHistoryCollapsed = ref(false);

// Stream AI Hook
const { isStreaming, startStreaming, streamFileAnalysis, stopStreaming } = useStreamAIWorkflow();

// Computed properties
const canGenerate = computed(() => {
	const hasInput = currentInput.value.trim();
	const hasFile = uploadedFile.value;
	const hasChatHistory = chatMessages.value.some(msg => msg.type === 'user');
	return hasInput || hasFile || hasChatHistory;
});

// Dialog states
const showFieldDialog = ref(false);
const newFieldName = ref('');
const currentStageIndex = ref(-1);
const currentMessageData = ref<any>(null);

// Refs
const chatMessagesRef = ref<HTMLElement>();

// Methods
const generateWorkflow = async () => {
	if (!canGenerate.value) {
		ElMessage.warning('Please describe your workflow or upload a file');
    return;
  }
	
	// 检查输入类型
	const hasInput = currentInput.value.trim();
	const hasFile = uploadedFile.value;
	const hasChatHistory = chatMessages.value.some(msg => msg.type === 'user');

  generating.value = true;
	
	// 性能提示：如果使用DeepSeek，建议切换到更快的模型
	if (currentAIModel.value?.provider?.toLowerCase() === 'deepseek') {
		ElMessage({
			message: '🚀 DeepSeek真实流式处理：您将看到实时进度更新（约20-30秒）',
			type: 'info',
			duration: 5000,
			showClose: true
		})
	}
	
	// 只有在有新输入时才添加用户消息
	if (hasInput || hasFile) {
		const userMessage: ChatMessage = {
			id: Date.now().toString(),
			type: 'user',
			content: currentInput.value || `Uploaded file "${uploadedFile.value?.name}" for workflow analysis`,
			timestamp: new Date()
		};
		chatMessages.value.push(userMessage);
	}

	// Add system message
	const systemMessage: ChatMessage = {
		id: (Date.now() + 1).toString(),
		type: 'system',
		content: 'AI is generating your workflow in real-time...',
		timestamp: new Date()
	};
	chatMessages.value.push(systemMessage);

	try {
		const onStreamChunk = (chunk: string) => {
			streamingMessage.value = chunk;
			scrollToBottom();
		};

		const onStreamComplete = (data: any) => {
			// Clear streaming message
			streamingMessage.value = '';
			
			console.log('🎯 onStreamComplete received data:', data);
			console.log('🎯 data.stages:', data.stages);
			
			const aiWorkflow = data.generatedWorkflow || data.GeneratedWorkflow || {
				name: 'AI Generated Workflow',
				description: 'Auto-created by AI',
				isActive: true,
			};
			
			// 处理workflow字段的大小写兼容性
			if (aiWorkflow && typeof aiWorkflow === 'object') {
				aiWorkflow.name = aiWorkflow.Name || aiWorkflow.name || 'AI Generated Workflow';
				aiWorkflow.description = aiWorkflow.Description || aiWorkflow.description || 'Auto-created by AI';
				aiWorkflow.isActive = aiWorkflow.IsActive !== undefined ? aiWorkflow.IsActive : (aiWorkflow.isActive !== undefined ? aiWorkflow.isActive : true);
			}

			const aiStages = (data.stages || []).map((s: any, idx: number) => {
				console.log(`🎯 Processing stage ${idx + 1}:`, s);
				return {
					name: s?.Name || s?.name || `Stage ${idx + 1}`,
					description: s?.Description || s?.description || '',
					order: Number.isFinite(Number(s?.Order || s?.order)) ? Math.trunc(Number(s?.Order || s?.order)) : idx + 1,
					assignedGroup: s?.AssignedGroup || s?.assignedGroup || 'General',
					requiredFields: Array.isArray(s?.RequiredFields || s?.requiredFields) ? (s?.RequiredFields || s?.requiredFields) : [],
					estimatedDuration: Number(s?.EstimatedDuration || s?.estimatedDuration) || 1,
				};
			});
			
			console.log('🎯 Processed aiStages:', aiStages);

			// Generate default checklists and questionnaires
			const checklists: ChecklistItem[] = aiStages.map((stage: WorkflowStage) => ({
				name: `${stage.name} Checklist`,
				description: `Checklist for ${stage.name} stage`
			}));

			const questionnaires: QuestionnaireItem[] = aiStages.map((stage: WorkflowStage) => ({
				name: `${stage.name} Questionnaire`,
				description: `Questionnaire for ${stage.name} stage`
			}));

			// Add generation complete message
			const completeMessage: ChatMessage = {
				id: (Date.now() + 2).toString(),
				type: 'generation-complete',
				content: 'Workflow generation completed successfully!',
				timestamp: new Date(),
				data: {
					workflow: aiWorkflow,
					stages: aiStages,
					checklists,
					questionnaires
				}
			};

			// Remove system message and add complete message
			chatMessages.value = chatMessages.value.filter(msg => msg.type !== 'system');
			chatMessages.value.push(completeMessage);

			// Save to history
			saveChatSession();
			scrollToBottom();
		};

		// Use stream response based on input type
		if (uploadedFile.value) {
			await streamFileAnalysis(uploadedFile.value, onStreamChunk, onStreamComplete, {
				id: currentAIModel.value?.id?.toString(),
				provider: currentAIModel.value?.provider,
				modelName: currentAIModel.value?.modelName
			});
		} else {
			// 如果没有当前输入，从聊天历史构建描述
			let workflowDescription = currentInput.value;
			if (!workflowDescription && hasChatHistory) {
				const userMessages = chatMessages.value
					.filter(msg => msg.type === 'user')
					.map(msg => msg.content)
					.join(' ');
				workflowDescription = userMessages;
			}
			await startStreaming(workflowDescription, onStreamChunk, onStreamComplete, {
				id: currentAIModel.value?.id?.toString(),
				provider: currentAIModel.value?.provider,
				modelName: currentAIModel.value?.modelName
			});
		}

  } catch (error) {
    console.error('Generation error:', error);
    ElMessage.error('Failed to generate workflow');
		// Remove system message on error
		chatMessages.value = chatMessages.value.filter(msg => msg.type !== 'system');
		streamingMessage.value = '';
  } finally {
    generating.value = false;
		currentInput.value = '';
		uploadedFile.value = null;
		await scrollToBottom();
	}
};



const sendMessage = async () => {
	if (!currentInput.value.trim() && !uploadedFile.value) return;

			const messageContent = currentInput.value || `Uploaded file "${uploadedFile.value?.name}" for workflow analysis`;
	
	const userMessage: ChatMessage = {
		id: Date.now().toString(),
		type: 'user',
		content: messageContent,
		timestamp: new Date()
	};
	chatMessages.value.push(userMessage);

	// Clear input immediately
	currentInput.value = '';
	uploadedFile.value = null;
	await scrollToBottom();

	// Add streaming AI message placeholder
	const aiMessageId = (Date.now() + 1).toString();
	const aiMessage: ChatMessage = {
		id: aiMessageId,
		type: 'ai',
		content: '',
		timestamp: new Date()
	};
	chatMessages.value.push(aiMessage);

	try {
		// Get current AI model configuration if not already loaded
		if (!currentAIModel.value) {
			try {
				const modelResponse = await getDefaultAIModel();
				if (modelResponse.success && modelResponse.data) {
					currentAIModel.value = modelResponse.data;
				}
			} catch (error) {
				console.warn('Failed to get default AI model, using default settings:', error);
			}
		}

		// Prepare chat messages for API with system prompt
		const apiMessages: AIChatMessage[] = [];
		
		// Add system prompt to establish context
		apiMessages.push({
			role: 'system',
			content: 'You are an AI Workflow Assistant specialized in helping users create business workflows. Your role is to understand their business processes and help them design structured workflows with clear stages, responsibilities, and requirements. Always focus on workflow planning, process optimization, and business automation. Ask relevant questions about process steps, stakeholders, timelines, and requirements.',
			timestamp: new Date().toISOString()
		});
		
		// Add existing chat history
		const historyMessages = chatMessages.value
			.filter(msg => msg.type === 'user' || msg.type === 'ai')
			.map(msg => ({
				role: (msg.type === 'user' ? 'user' : 'assistant') as 'user' | 'assistant',
				content: msg.content,
				timestamp: msg.timestamp.toISOString()
			}));
		apiMessages.push(...historyMessages);
		
		// Add current message
		apiMessages.push({
			role: 'user',
			content: messageContent,
			timestamp: new Date().toISOString()
		});

		// Prepare chat request with model configuration
		const chatRequest = {
			messages: apiMessages,
			context: 'workflow_planning',
			sessionId: conversationId.value || undefined,
			mode: 'workflow_planning' as const,
			// Add model configuration if available
			...(currentAIModel.value && {
				modelId: currentAIModel.value.id.toString(),
				modelProvider: currentAIModel.value.provider,
				modelName: currentAIModel.value.modelName
			})
		};

		// Try streaming chat first
		let streamSuccess = false;
		
		try {
			console.log('💬 Attempting to use native stream chat API');
			await streamAIChatMessageNative(
				chatRequest,
				(chunk: string) => {
					// Update the AI message content with streaming chunks
					const messageIndex = chatMessages.value.findIndex(msg => msg.id === aiMessageId);
					if (messageIndex !== -1) {
						chatMessages.value[messageIndex].content += chunk;
						scrollToBottom();
					}
				},
				(data: any) => {
					console.log('✅ Stream chat completed:', data);
					if (data?.sessionId) {
						conversationId.value = data.sessionId;
					}
					streamSuccess = true;
				},
				(error: any) => {
					console.warn('❌ Native stream chat failed:', error);
					throw error;
				}
			);
			
			if (streamSuccess) {
				// Check if we should show the workflow creation prompt
				const messageCount = chatMessages.value.filter(msg => msg.type === 'user').length;
				if (messageCount >= 2) {
					setTimeout(() => {
						const createMessage: ChatMessage = {
							id: (Date.now() + 2).toString(),
							type: 'system',
							content: `🔧 Let's create your workflow!\nI have the basics to start creating your workflow. Generate now for a quick start, or chat more for better customization.\n\nProgress: ${'▓'.repeat(Math.min(messageCount, 10))}${'░'.repeat(10 - Math.min(messageCount, 10))} ${Math.min(messageCount * 10, 100)}%`,
							timestamp: new Date()
						};
						chatMessages.value.push(createMessage);
						scrollToBottom();
					}, 1000);
				}
				return;
			}
		} catch (streamError) {
			console.warn('Stream chat failed, falling back to regular API:', streamError);
		}

		// Fallback to regular API if streaming fails
		const response = await sendAIChatMessage(chatRequest);
		console.log('📡 Frontend: Received response:', response);
		
		// Handle both wrapped and unwrapped response formats
		const actualResponse = (response as any).data || response;
		
		if (actualResponse.success && actualResponse.response) {
			if (actualResponse.sessionId) {
				conversationId.value = actualResponse.sessionId;
			}
			
			// Update AI message with complete response
			const messageIndex = chatMessages.value.findIndex(msg => msg.id === aiMessageId);
			if (messageIndex !== -1) {
				chatMessages.value[messageIndex].content = actualResponse.response.content;
			}
			console.log('📡 Frontend: Added AI message:', actualResponse.response.content);
			
			// Check if we should show the workflow creation prompt
			const messageCount = chatMessages.value.filter(msg => msg.type === 'user').length;
			if (messageCount >= 2) {
				setTimeout(() => {
					const createMessage: ChatMessage = {
						id: (Date.now() + 2).toString(),
						type: 'system',
						content: `🔧 Let's create your workflow!\nI have the basics to start creating your workflow. Generate now for a quick start, or chat more for better customization.\n\nProgress: ${'▓'.repeat(Math.min(messageCount, 10))}${'░'.repeat(10 - Math.min(messageCount, 10))} ${Math.min(messageCount * 10, 100)}%`,
						timestamp: new Date()
					};
					chatMessages.value.push(createMessage);
					scrollToBottom();
				}, 1000);
			}
		} else {
			throw new Error(actualResponse.message || response.message || 'AI response failed');
		}
	} catch (error) {
		console.error('Chat error:', error);
		// Update AI message with fallback response
		const messageIndex = chatMessages.value.findIndex(msg => msg.id === aiMessageId);
		if (messageIndex !== -1) {
			chatMessages.value[messageIndex].content = 'I understand you want to create a workflow. To help you design the most effective process, could you tell me more details about:\n\n1. What are the main steps involved in this process?\n2. Who are the key stakeholders or team members that need to be involved?\n3. What are the expected outcomes or deliverables?\n4. Are there any specific requirements or constraints I should consider?\n\nThis information will help me create a structured workflow tailored to your needs.';
		}
		
		ElMessage.warning('AI service temporarily unavailable, using fallback response');
	}
	
	await scrollToBottom();
};

const applyWorkflow = async (data: any) => {
	applying.value = true;
	try {
		// Create workflow
		const workflowPayload = {
			name: data.workflow.name,
			description: data.workflow.description,
			isActive: data.workflow.isActive,
			status: 'active',
			startDate: new Date().toISOString(),
			stages: data.stages.map((stage: WorkflowStage, index: number) => ({
				name: stage.name,
				description: stage.description,
				order: stage.order || index + 1,
				defaultAssignedGroup: stage.assignedGroup || 'General',
				estimatedDuration: stage.estimatedDuration || 1,
				isActive: true,
				workflowVersion: '1',
			})),
		};

		const response = await createWorkflow(workflowPayload);
		if (!response.success) {
			throw new Error(response.message || 'Create workflow failed');
		}

		const workflowId = response.data;

		// Create checklists and questionnaires
		if (data.checklists && data.checklists.length > 0) {
			for (const checklist of data.checklists) {
				try {
					await createChecklist({
						name: checklist.name,
						description: checklist.description,
						team: 'General',
						type: 'Instance',
						status: 'Active',
						isTemplate: false,
						estimatedHours: 0,
						isActive: true,
						assignments: [{ workflowId, stageId: null }],
					});
				} catch (e) {
					console.warn('Failed to create checklist:', e);
				}
			}
		}

		if (data.questionnaires && data.questionnaires.length > 0) {
			for (const questionnaire of data.questionnaires) {
				try {
					const structure = { title: questionnaire.name, sections: [] };
					await createQuestionnaire({
						name: questionnaire.name,
						description: questionnaire.description,
						status: 'Draft',
						structureJson: JSON.stringify(structure),
						version: 1,
						previewImageUrl: '',
						category: 'Onboarding',
						tagsJson: '[]',
						estimatedMinutes: 0,
						allowDraft: true,
						allowMultipleSubmissions: false,
						isActive: true,
						assignments: [{ workflowId, stageId: null }],
						sections: [],
					});
				} catch (e) {
					console.warn('Failed to create questionnaire:', e);
				}
			}
		}

		ElMessage.success('Workflow applied successfully!');
		
		// Emit for parent component navigation
		emit('workflowGenerated', {
			generatedWorkflow: data.workflow,
			stages: data.stages,
			operationMode: 'create'
		});

  } catch (error) {
		console.error('Apply workflow error:', error);
		ElMessage.error('Failed to apply workflow');
  } finally {
		applying.value = false;
	}
};

// Stage management
const addStage = (data: any) => {
	const newOrder = Math.max(...data.stages.map((s: WorkflowStage) => s.order), 0) + 1;
	data.stages.push({
		name: `New Stage ${newOrder}`,
		description: '',
		order: newOrder,
		assignedGroup: 'General',
		requiredFields: [],
		estimatedDuration: 1,
	});
};

const removeStage = (data: any, index: number) => {
	data.stages.splice(index, 1);
	// Reorder stages
	data.stages.forEach((stage: WorkflowStage, idx: number) => {
		stage.order = idx + 1;
	});
};

const onStageUpdated = (data: any, index: number) => {
	// Stage update logic
	console.log('Stage updated:', data.stages[index]);
};

// Required fields management
const addRequiredField = (data: any, stageIndex: number) => {
	currentStageIndex.value = stageIndex;
	currentMessageData.value = data;
	showFieldDialog.value = true;
};

const confirmAddField = () => {
	if (!newFieldName.value.trim()) {
		ElMessage.warning('Field name cannot be empty');
		return;
	}

	if (currentStageIndex.value >= 0 && currentMessageData.value) {
		if (!currentMessageData.value.stages[currentStageIndex.value].requiredFields) {
			currentMessageData.value.stages[currentStageIndex.value].requiredFields = [];
		}
		currentMessageData.value.stages[currentStageIndex.value].requiredFields.push(
			newFieldName.value.trim()
		);
		newFieldName.value = '';
		showFieldDialog.value = false;
	}
};

const removeRequiredField = (data: any, stageIndex: number, fieldIndex: number) => {
	data.stages[stageIndex].requiredFields.splice(fieldIndex, 1);
};

// Checklist management
const addChecklist = (data: any) => {
	if (!data.checklists) data.checklists = [];
	data.checklists.push({
		name: `New Checklist ${data.checklists.length + 1}`,
		description: 'New checklist description'
	});
};

const removeChecklist = (data: any, index: number) => {
	data.checklists.splice(index, 1);
};

// Questionnaire management
const addQuestionnaire = (data: any) => {
	if (!data.questionnaires) data.questionnaires = [];
	data.questionnaires.push({
		name: `New Questionnaire ${data.questionnaires.length + 1}`,
		description: 'New questionnaire description'
	});
};

const removeQuestionnaire = (data: any, index: number) => {
	data.questionnaires.splice(index, 1);
};

// File handling
const handleFileUpload = (file: File) => {
	uploadedFile.value = file;
	ElMessage.success(`File "${file.name}" uploaded successfully`);
	return false; // Prevent default upload
};

const removeUploadedFile = () => {
	// 如果是图片文件，清理URL对象以避免内存泄漏
	if (uploadedFile.value && isImageFile(uploadedFile.value)) {
		const previewUrl = getFilePreviewUrl(uploadedFile.value);
		if (previewUrl) {
			URL.revokeObjectURL(previewUrl);
		}
	}
	
	uploadedFile.value = null;
	ElMessage.info('File removed');
};

const isImageFile = (file: File) => {
	const imageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/bmp', 'image/webp', 'image/svg+xml'];
	return imageTypes.includes(file.type);
};

const getFilePreviewUrl = (file: File): string | undefined => {
	if (isImageFile(file)) {
		return URL.createObjectURL(file);
	}
	return undefined;
};



// Chat history management
const saveChatSession = () => {
	if (chatMessages.value.length === 0) return;

	const sessionId = currentSessionId.value || Date.now().toString();
	const title = chatMessages.value.find(msg => msg.type === 'user')?.content.slice(0, 50) + '...' || 'New Chat';
	
	const session: ChatSession = {
		id: sessionId,
		title,
		timestamp: new Date(),
		messages: [...chatMessages.value]
	};

	const existingIndex = chatHistory.value.findIndex(s => s.id === sessionId);
	if (existingIndex >= 0) {
		chatHistory.value[existingIndex] = session;
	} else {
		chatHistory.value.unshift(session);
	}

	currentSessionId.value = sessionId;
	
	// Save to localStorage
	localStorage.setItem('ai-workflow-chat-history', JSON.stringify(chatHistory.value));
};

const loadChatSession = (sessionId: string) => {
	const session = chatHistory.value.find(s => s.id === sessionId);
	if (session) {
		chatMessages.value = [...session.messages];
		currentSessionId.value = sessionId;
		scrollToBottom();
	}
};

const clearChat = () => {
	// Stop any ongoing streaming
	stopStreaming();

	chatMessages.value = [];
	currentSessionId.value = '';
	conversationId.value = '';
	currentInput.value = '';
	uploadedFile.value = null;
	streamingMessage.value = '';
    generating.value = false;
};

const toggleHistory = () => {
	isHistoryCollapsed.value = !isHistoryCollapsed.value;
};

const startNewChat = () => {
	// Clear current chat
	clearChat();
	// Collapse history sidebar
	isHistoryCollapsed.value = false;
};

// Utility functions
const formatTime = (timestamp: Date) => {
	return timestamp.toLocaleTimeString('en-US', { 
		hour: '2-digit', 
		minute: '2-digit' 
	});
};

const formatDate = (timestamp: Date) => {
	return timestamp.toLocaleDateString('en-US', { 
		month: 'short', 
		day: 'numeric' 
	});
};

const formatAIMessage = (content: string) => {
	return content.replace(/\n/g, '<br>');
};

const getTotalDuration = (stages: WorkflowStage[]) => {
	return stages.reduce((sum, stage) => sum + stage.estimatedDuration, 0);
};

const scrollToBottom = async () => {
	await nextTick();
	if (chatMessagesRef.value) {
		chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
	}
};

// Model management
const handleModelChange = (model: AIModelConfig) => {
	currentAIModel.value = model;
	ElMessage.success(`Switched to ${model.provider.toLowerCase()} ${model.modelName}`);
	console.log('Model changed to:', model);
};

// Keyboard event handling
const handleKeydown = (event: KeyboardEvent) => {
	if (event.key === 'Enter') {
		if (event.shiftKey) {
			// Shift+Enter: Allow default behavior (new line)
			return;
		} else {
			// Enter: Send message
			event.preventDefault();
			if ((currentInput.value.trim() || uploadedFile.value) && !generating.value) {
				sendMessage();
			}
		}
	}
};

// Lifecycle
onMounted(async () => {
	// Load available AI models
	try {
		const modelsResponse = await getUserAIModels();
		if (modelsResponse.success && modelsResponse.data) {
			availableModels.value = modelsResponse.data;
			console.log('Loaded available AI models:', modelsResponse.data.length);
		}
	} catch (error) {
		console.warn('Failed to load available AI models:', error);
	}

	// Load default AI model configuration
	try {
		const modelResponse = await getDefaultAIModel();
		if (modelResponse.success && modelResponse.data) {
			currentAIModel.value = modelResponse.data;
			console.log('Loaded default AI model:', modelResponse.data.modelName);
		}
	} catch (error) {
		console.warn('Failed to load default AI model:', error);
	}

	// Load chat history from localStorage
	const savedHistory = localStorage.getItem('ai-workflow-chat-history');
	if (savedHistory) {
		try {
			const parsed = JSON.parse(savedHistory);
			chatHistory.value = parsed.map((session: any) => ({
				...session,
				timestamp: new Date(session.timestamp),
				messages: session.messages.map((msg: any) => ({
					...msg,
					timestamp: new Date(msg.timestamp)
				}))
			}));
		} catch (e) {
			console.warn('Failed to load chat history:', e);
		}
	}

	// Add initial AI message if no messages exist
	if (chatMessages.value.length === 0) {
		const initialMessage: ChatMessage = {
			id: 'initial',
			type: 'ai',
			content: 'Hello! I\'m your AI Workflow Assistant. I\'m here to help you create the perfect workflow by understanding your specific business needs and requirements.\n\nTo get started, could you tell me what type of process or workflow you\'re looking to create? For example, it could be employee onboarding, customer support, project approval, or any other business process you have in mind.',
			timestamp: new Date()
		};
		chatMessages.value.push(initialMessage);
	}
});
</script>

<style scoped>
.ai-workflow-assistant {
  width: 100%;
}

.assistant-card {
	height: 80vh;
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
  0% { opacity: 0.5; }
  50% { opacity: 1; }
  100% { opacity: 0.5; }
}

.assistant-container {
	display: flex;
	height: 100%;
	gap: 1rem;
}

.chat-area {
	flex: 1;
	display: flex;
	flex-direction: column;
	height: 100%;
}

.generate-section {
	padding: 0.75rem 0;
	text-align: center;
	border-bottom: 1px solid #e5e7eb;
	margin-bottom: 0.75rem;
}

.generate-btn {
	min-width: 160px;
	height: 36px;
	font-size: 14px;
	font-weight: 600;
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	border: none;
  border-radius: 8px;
	transition: all 0.3s ease;
}

.generate-btn:hover {
	transform: translateY(-2px);
	box-shadow: 0 8px 25px rgba(79, 70, 229, 0.3);
}

.chat-messages {
	flex: 1;
	overflow-y: auto;
	padding: 1rem;
	display: flex;
	flex-direction: column;
	gap: 1rem;
	max-height: 500px;
	min-height: 400px;
}

/* 自定义滚动条样式 */
.chat-messages::-webkit-scrollbar {
	width: 8px;
}

.chat-messages::-webkit-scrollbar-track {
	background: #f1f5f9;
	border-radius: 4px;
}

.chat-messages::-webkit-scrollbar-thumb {
	background: #cbd5e1;
	border-radius: 4px;
	transition: background 0.2s ease;
}

.chat-messages::-webkit-scrollbar-thumb:hover {
	background: #94a3b8;
}

.message-item {
	animation: fadeInUp 0.3s ease-out;
}

@keyframes fadeInUp {
	from {
		opacity: 0;
		transform: translateY(20px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.user-message {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
}

.user-message .message-content {
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	color: white;
	padding: 0.75rem 1rem;
	border-radius: 18px 18px 4px 18px;
	max-width: 70%;
}

.user-message .message-avatar {
	width: 32px;
	height: 32px;
	background: #4f46e5;
	border-radius: 50%;
  display: flex;
  align-items: center;
	justify-content: center;
	color: white;
	font-size: 16px;
}

.ai-message {
	display: flex;
	gap: 0.5rem;
}

.ai-message .message-avatar {
	width: 32px;
	height: 32px;
	background: linear-gradient(135deg, #10b981 0%, #059669 100%);
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-size: 16px;
}

.ai-message .message-content {
	background: #f3f4f6;
	padding: 0.75rem 1rem;
	border-radius: 18px 18px 18px 4px;
	max-width: 70%;
}

.ai-message.streaming .message-content {
	background: linear-gradient(90deg, #f3f4f6 0%, #e5e7eb 50%, #f3f4f6 100%);
	background-size: 200% 100%;
	animation: shimmer 2s infinite;
}

@keyframes shimmer {
	0% { background-position: -200% 0; }
	100% { background-position: 200% 0; }
}

.typing-indicator {
	animation: blink 1s infinite;
}

@keyframes blink {
	0%, 50% { opacity: 1; }
	51%, 100% { opacity: 0; }
}

.system-message {
	display: flex;
	justify-content: center;
	margin: 1rem 0;
}

.system-content {
	background: linear-gradient(135deg, #e0f2fe 0%, #b3e5fc 100%);
	color: #0d47a1;
	padding: 1rem 1.5rem;
	border-radius: 16px;
	font-size: 14px;
	display: flex;
	align-items: flex-start;
	gap: 0.75rem;
	max-width: 80%;
	border: 1px solid #81d4fa;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.system-text {
	flex: 1;
}

.system-message-text {
	white-space: pre-line;
	line-height: 1.5;
	margin-bottom: 1rem;
}

.system-actions {
	display: flex;
	gap: 0.75rem;
	justify-content: flex-end;
	margin-top: 0.75rem;
}

.start-over-btn {
	background: white;
	border: 1px solid #90caf9;
	color: #1976d2;
}

.start-over-btn:hover {
	background: #f3f4f6;
	border-color: #64b5f6;
}

.generate-workflow-btn {
	background: linear-gradient(135deg, #ff9800 0%, #f57c00 100%);
	border: none;
	color: white;
	font-weight: 600;
}

.generate-workflow-btn:hover {
	background: linear-gradient(135deg, #f57c00 0%, #ef6c00 100%);
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(255, 152, 0, 0.3);
}

.message-text {
	margin-bottom: 0.25rem;
	line-height: 1.5;
}

.message-time {
	font-size: 12px;
	opacity: 0.7;
}

.generation-complete {
	background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%);
	border: 1px solid #bbf7d0;
	border-radius: 16px;
	padding: 1.5rem;
	margin: 1rem 0;
}

.complete-header {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	margin-bottom: 1rem;
}

.success-icon {
	color: #10b981;
  font-size: 24px;
}

.complete-header h4 {
	margin: 0;
	color: #065f46;
	font-size: 18px;
	font-weight: 600;
}

.workflow-preview {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.workflow-info h5 {
	margin: 0 0 0.5rem 0;
  color: #374151;
  font-size: 16px;
	font-weight: 600;
}

.workflow-info p {
	margin: 0 0 0.75rem 0;
  color: #6b7280;
  font-size: 14px;
}

.workflow-stats {
	display: flex;
	gap: 1rem;
}

.stat-item {
	display: flex;
	align-items: center;
	gap: 0.25rem;
	color: #6b7280;
	font-size: 14px;
}

.stages-grid {
  display: grid;
	grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
	gap: 1rem;
	margin-bottom: 1rem;
}

.stage-card {
	background: white;
	border: 1px solid #e5e7eb;
  border-radius: 12px;
	padding: 1rem;
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
  transition: all 0.2s ease;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.stage-card:hover {
	border-color: #4f46e5;
	box-shadow: 0 4px 12px rgba(79, 70, 229, 0.15);
}

.stage-card-header {
  display: flex;
  align-items: center;
	justify-content: space-between;
	margin-bottom: 0.5rem;
}

.stage-badge {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.stage-number {
	background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
	color: white;
	width: 28px;
	height: 28px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	font-weight: 700;
	box-shadow: 0 2px 4px rgba(79, 70, 229, 0.3);
}

.stage-card-content {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.stage-title-input {
	font-weight: 600;
}

.stage-description {
	font-size: 13px;
}

.remove-stage-btn {
	width: 28px;
	height: 28px;
	opacity: 0.6;
	transition: opacity 0.2s ease;
}

.remove-stage-btn:hover {
	opacity: 1;
}

.stage-meta-compact {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 0.75rem;
	padding: 0.75rem;
  background: #f8fafc;
	border-radius: 8px;
	border: 1px solid #e2e8f0;
}

.meta-item {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}

.meta-label {
	font-size: 12px;
	font-weight: 600;
	color: #64748b;
	text-transform: uppercase;
	letter-spacing: 0.5px;
}

.meta-select {
	width: 100%;
}

.meta-number {
	width: 100%;
}

.required-fields-compact {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.fields-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.fields-label {
	font-size: 12px;
	font-weight: 600;
	color: #64748b;
	text-transform: uppercase;
	letter-spacing: 0.5px;
}

.add-field-btn {
	width: 24px;
	height: 24px;
}

.fields-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
}

.field-tag {
	font-size: 12px;
}

.add-stage-btn {
	align-self: flex-start;
	border: 2px dashed #d1d5db;
	background: #f9fafb;
	color: #6b7280;
	border-radius: 8px;
	padding: 0.75rem 1rem;
	transition: all 0.2s ease;
}

.add-stage-btn:hover {
	border-color: #4f46e5;
  color: #4f46e5;
	background: #f8fafc;
}

.additional-components {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 1rem;
	margin-top: 1rem;
}

.component-section h6 {
	margin: 0 0 0.5rem 0;
  color: #374151;
	font-size: 14px;
	font-weight: 600;
}

.component-list {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	margin-bottom: 0.75rem;
}

.component-item {
	display: flex;
	gap: 0.5rem;
	align-items: center;
}

.component-item .el-input {
	flex: 1;
}

.apply-section {
	margin-top: 1.5rem;
	text-align: center;
}

.apply-btn {
	min-width: 200px;
	height: 48px;
  font-size: 16px;
	font-weight: 600;
	background: linear-gradient(135deg, #10b981 0%, #059669 100%);
	border: none;
	border-radius: 12px;
	transition: all 0.3s ease;
}

.apply-btn:hover {
	transform: translateY(-2px);
	box-shadow: 0 8px 25px rgba(16, 185, 129, 0.3);
}

.input-area {
	padding: 1rem;
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	background: #f9fafb;
	border-radius: 0 0 12px 12px;
}

.ai-model-selector-bottom {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	margin-top: 0.75rem;
	padding-top: 0.75rem;
	border-top: 1px solid #e5e7eb;
}

.model-selector-label {
	font-size: 0.875rem;
	font-weight: 500;
  color: #374151;
	min-width: 50px;
	flex-shrink: 0;
}

.model-option {
	display: flex;
	justify-content: space-between;
	align-items: center;
	width: 100%;
}

.model-info {
	display: flex;
	align-items: center;
}

.model-display {
	font-size: 0.875rem;
	color: #111827;
	font-weight: 400;
}

.model-status {
	display: flex;
	align-items: center;
}

.status-dot {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background-color: #ef4444;
}

.status-dot.online {
	background-color: #10b981;
}

.status-dot.offline {
	background-color: #ef4444;
}

.file-upload-section {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.file-upload .el-button {
  color: #6b7280;
	border: none;
	background: none;
	padding: 0;
}

.file-upload .el-button:hover {
	color: #4f46e5;
}

.uploaded-file-display {
	margin-top: 4px;
}

.file-info {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 8px 12px;
	background: #f0f9ff;
	border: 1px solid #bae6fd;
	border-radius: 6px;
  font-size: 12px;
	color: #0369a1;
}

.file-icon {
	color: #0284c7;
}

.file-name {
	flex: 1;
	font-weight: 500;
}

.remove-file-btn {
	color: #64748b;
	padding: 2px;
	min-height: auto;
}

.remove-file-btn:hover {
	color: #ef4444;
}

.image-preview {
	margin-top: 8px;
	border-radius: 6px;
	overflow: hidden;
	border: 1px solid #e5e7eb;
}

.preview-image {
	width: 100%;
	max-width: 200px;
	max-height: 150px;
	object-fit: cover;
	display: block;
}

.text-input-section {
  display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.input-with-button {
	display: flex;
	align-items: center;
	gap: 0.75rem;
}

.chat-input {
	flex: 1;
}

.input-actions {
	display: flex;
	align-items: center;
  justify-content: center;
}

.send-button {
	min-width: 80px;
	height: 36px;
	font-size: 0.875rem;
	font-weight: 500;
}

.chat-history {
	width: 300px;
	border-left: 1px solid #e5e7eb;
	display: flex;
	flex-direction: column;
	height: 100%;
	transition: width 0.3s ease;
	background: #f8fafc;
}

.chat-history.collapsed {
	width: 50px;
}

.history-header {
	padding: 1rem;
	border-bottom: 1px solid #e5e7eb;
}

.header-content {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 0.5rem;
}

.header-actions {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.new-chat-btn {
	font-size: 12px;
	padding: 4px 8px;
}

.collapse-btn {
	padding: 4px;
	min-width: 28px;
	height: 28px;
}

.history-header h4 {
	margin: 0;
  color: #374151;
  font-size: 16px;
	font-weight: 600;
}

.history-list {
	flex: 1;
	overflow-y: auto;
	padding: 0.5rem;
	max-height: 400px;
}

.history-item {
	padding: 0.75rem;
	border-radius: 8px;
	cursor: pointer;
	transition: background-color 0.2s ease;
	margin-bottom: 0.5rem;
}

.history-item:hover {
	background: #f3f4f6;
}

.history-item.active {
	background: #eff6ff;
	border: 1px solid #dbeafe;
}

.history-title {
	font-size: 14px;
	font-weight: 500;
  color: #374151;
	margin-bottom: 0.25rem;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.history-time {
  font-size: 12px;
	color: #6b7280;
}

.empty-history {
  display: flex;
	flex-direction: column;
	align-items: center;
  justify-content: center;
	padding: 2rem;
	color: #6b7280;
	text-align: center;
}

.empty-history .el-icon {
	font-size: 32px;
	margin-bottom: 0.5rem;
	opacity: 0.5;
}

.empty-history p {
	margin: 0;
	font-size: 14px;
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
}

.mr-1 {
  margin-right: 4px;
}

/* 聊天历史区域滚动条样式 */
.history-list::-webkit-scrollbar {
	width: 6px;
}

.history-list::-webkit-scrollbar-track {
	background: #f1f5f9;
	border-radius: 3px;
}

.history-list::-webkit-scrollbar-thumb {
	background: #cbd5e1;
	border-radius: 3px;
	transition: background 0.2s ease;
}

.history-list::-webkit-scrollbar-thumb:hover {
	background: #94a3b8;
}

/* Responsive Design */
@media (max-width: 768px) {
	.assistant-container {
		flex-direction: column;
	}
	
	.chat-history {
		width: 100%;
		border-left: none;
		border-top: 1px solid #e5e7eb;
		max-height: 200px;
	}
	
	.additional-components {
		grid-template-columns: 1fr;
	}
	
	.user-message .message-content,
	.ai-message .message-content {
		max-width: 85%;
	}
}
</style> 