<template>
	<div class="ai-workflow-generator">
		<!-- AI Input Area -->
		<el-card class="mb-4" shadow="hover">
			<template #header>
				<div class="flex items-center justify-between">
					<div class="flex items-center">
						<el-icon class="mr-2 text-blue-500">
							<User />
						</el-icon>
						<span class="text-lg font-semibold">AI Workflow Generator</span>
					</div>
					<el-tag :type="aiStatus.isAvailable ? 'success' : 'danger'" size="small">
						{{ aiStatus.isAvailable ? 'Online' : 'Offline' }} - {{ aiStatus.provider }}
					</el-tag>
				</div>
			</template>

			<div class="space-y-4">
				<!-- Natural Language Input -->
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">
						Describe your desired workflow <span class="text-red-500">*</span>
					</label>
					<el-input
						v-model="input.description"
						type="textarea"
						:rows="4"
						placeholder="For example: I need an employee onboarding process, including document collection, training arrangement, equipment allocation and probation evaluation..."
						@input="handleInputChange"
						:disabled="generating"
					/>
				</div>

				<!-- Advanced Options -->
				<el-collapse v-model="showAdvanced">
					<el-collapse-item title="Advanced Options" name="advanced">
						<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
							<div>
								<label class="block text-sm font-medium text-gray-700 mb-2">
									Industry
								</label>
								<el-select 
									v-model="input.industry" 
									placeholder="Select Industry" 
									class="w-full"
								>
									<el-option label="Technology" value="Technology" />
									<el-option label="Manufacturing" value="Manufacturing" />
									<el-option label="Finance" value="Finance" />
									<el-option label="Healthcare" value="Healthcare" />
									<el-option label="Education" value="Education" />
									<el-option label="Retail" value="Retail" />
									<el-option label="Other" value="Other" />
								</el-select>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-2">
									Process Type
								</label>
								<el-select 
									v-model="input.processType" 
									placeholder="Select Process Type" 
									class="w-full"
								>
									<el-option label="Onboarding Process" value="Onboarding" />
									<el-option label="Approval Process" value="Approval" />
									<el-option label="Procurement Process" value="Procurement" />
									<el-option label="Project Management" value="Project Management" />
									<el-option label="Customer Service" value="Customer Service" />
									<el-option label="Other" value="Other" />
								</el-select>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-2">
									Context Information
								</label>
								<el-input
									v-model="input.context"
									type="textarea"
									:rows="2"
									placeholder="Provide additional context information..."
								/>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-2">
									Special Requirements
								</label>
								<el-input
									v-model="requirementsText"
									type="textarea"
									:rows="2"
									placeholder="One requirement per line..."
									@input="updateRequirements"
								/>
							</div>
						</div>

						<div class="mt-4 space-y-2">
							<el-checkbox v-model="input.includeApprovals">Include Approval Steps</el-checkbox>
							<el-checkbox v-model="input.includeNotifications">Include Notification Mechanism</el-checkbox>
						</div>
					</el-collapse-item>
				</el-collapse>

				<!-- Action Buttons -->
				<div class="flex justify-between items-center">
					<div class="flex space-x-2">
						<el-button
							type="primary"
							:loading="generating"
							@click="generateWorkflow"
							:disabled="!input.description.trim()"
						>
							<el-icon class="mr-1">
								<Star />
							</el-icon>
							{{ generating ? 'Generating...' : 'Generate Workflow' }}
						</el-button>

						<el-button
							v-if="!generating && input.description.trim()"
							type="success"
							@click="streamGenerateWorkflow"
							:disabled="generating"
						>
							<el-icon class="mr-1">
								<VideoPlay />
							</el-icon>
							Real-time Generation
						</el-button>
					</div>

					<el-button @click="clearInput" :disabled="generating">
						<el-icon class="mr-1">
							<Refresh />
						</el-icon>
						Clear
					</el-button>
				</div>
			</div>
		</el-card>

		<!-- Real-time Generation Progress -->
		<el-card v-if="streaming && streamSteps.length > 0" class="mb-4" shadow="hover">
			<template #header>
				<div class="flex items-center">
					<el-icon class="mr-2 animate-spin">
						<Loading />
					</el-icon>
					<span class="font-semibold">Generation Progress</span>
				</div>
			</template>

			<div class="space-y-2">
				<div
					v-for="(step, index) in streamSteps"
					:key="index"
					class="flex items-center space-x-2 p-2 rounded"
					:class="{
						'bg-blue-50': step.type === 'progress',
						'bg-green-50': step.type === 'complete',
						'bg-red-50': step.type === 'error'
					}"
				>
					<el-icon
						:class="{
							'text-blue-500': step.type === 'progress',
							'text-green-500': step.type === 'complete',
							'text-red-500': step.type === 'error'
						}"
					>
						<Loading v-if="step.type === 'progress'" />
						<SuccessFilled v-else-if="step.type === 'complete'" />
						<WarningFilled v-else-if="step.type === 'error'" />
						<InfoFilled v-else />
					</el-icon>
					<span>{{ step.message }}</span>
				</div>
			</div>
		</el-card>

		<!-- Generation Result -->
		<el-card v-if="result" class="mb-4" shadow="hover">
			<template #header>
				<div class="flex items-center justify-between">
					<div class="flex items-center">
						<el-icon class="mr-2 text-green-500">
							<SuccessFilled />
						</el-icon>
						<span class="font-semibold">Generation Result</span>
					</div>
					<div class="flex items-center space-x-2">
						<el-tag type="info" size="small">
							Confidence: {{ Math.round((result.confidenceScore || 0) * 100) }}%
						</el-tag>
						<el-button type="primary" size="small" @click="applyWorkflow">
							Apply Workflow
						</el-button>
					</div>
				</div>
			</template>

			<div class="space-y-4">
				<!-- Workflow Basic Info -->
				<div>
					<h3 class="text-lg font-semibold text-gray-800 mb-2">
						{{ result.generatedWorkflow?.name || 'AI Generated Workflow' }}
					</h3>
					<p class="text-gray-600 mb-4">{{ result.generatedWorkflow?.description || 'Generated by AI' }}</p>

					<div class="grid grid-cols-2 gap-4 text-sm text-gray-600">
						<div>Stages: {{ result.stages?.length || 0 }}</div>
						<div>Estimated Duration: {{ result.stages?.reduce((sum, stage) => sum + (stage.estimatedDuration || 0), 0) || 0 }} days</div>
					</div>
				</div>

				<!-- Workflow Stages -->
				<div>
					<h4 class="font-semibold text-gray-700 mb-2">Workflow Stages</h4>
					<div v-if="result.stages && result.stages.length > 0" class="space-y-2">
						<div
							v-for="(stage, index) in result.stages"
							:key="index"
							class="p-3 border rounded-lg bg-gray-50"
						>
							<div class="flex justify-between items-start">
								<div class="flex-1">
									<h5 class="font-medium text-gray-800">{{ stage.name }}</h5>
									<p class="text-sm text-gray-600 mt-1">{{ stage.description }}</p>
									<div class="flex items-center space-x-4 mt-2 text-xs text-gray-500">
										<span>Order: {{ stage.order }}</span>
										<span>Team: {{ stage.assignedGroup }}</span>
										<span>Duration: {{ stage.estimatedDuration }} days</span>
									</div>
								</div>
							</div>
						</div>
					</div>
					<div v-else class="text-gray-500 text-sm">No stages generated</div>
				</div>

				<!-- AI Suggestions -->
				<div v-if="result.suggestions && result.suggestions.length > 0">
					<h4 class="font-semibold text-gray-700 mb-2">AI Suggestions</h4>
					<ul class="space-y-1">
						<li
							v-for="(suggestion, index) in result.suggestions"
							:key="index"
							class="flex items-center space-x-2 text-sm text-blue-600"
						>
							<el-icon>
								<InfoFilled />
							</el-icon>
							<span>{{ suggestion }}</span>
						</li>
					</ul>
				</div>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getTokenobj } from '@/utils/auth';
import { generateAIWorkflow, getAIWorkflowStatus } from '@/apis/ai/workflow';
import {
	User,
	Star,
	VideoPlay,
	Refresh,
	Loading,
	SuccessFilled,
	WarningFilled,
	InfoFilled
} from '@element-plus/icons-vue';

// Props and Emits
const emit = defineEmits<{
	workflowGenerated: [workflow: any];
}>();

// Reactive Data
const generating = ref(false);
const streaming = ref(false);
const showAdvanced = ref([]);
const streamSteps = ref<Array<{ type: string; message: string; data?: any }>>([]);

const input = reactive({
	description: '',
	context: '',
	requirements: [] as string[],
	industry: '',
	processType: '',
	includeApprovals: true,
	includeNotifications: true,
	estimatedDuration: 0
});

const requirementsText = ref('');
const result = ref<any>(null);

const aiStatus = reactive({
	isAvailable: true,
	provider: 'ZhipuAI',
	model: 'glm-4'
});

// Computed Properties
const totalEstimatedDuration = computed(() => {
	if (!result.value?.stages) return 0;
	return result.value.stages.reduce((total: number, stage: any) => total + stage.estimatedDuration, 0);
});

// Methods
const handleInputChange = () => {
	// 可以添加实时建议逻辑
};

const updateRequirements = () => {
	input.requirements = requirementsText.value
		.split('\n')
		.map(line => line.trim())
		.filter(line => line.length > 0);
};

const generateWorkflow = async () => {
	if (!input.description.trim()) {
		ElMessage.warning('Please describe your desired workflow');
		return;
	}

	generating.value = true;
	result.value = null;

	try {
		// 调用后端API生成工作流
		const response = await generateAIWorkflow(input);

		if (response.success) {
			result.value = response.data;
			ElMessage.success('Workflow generated successfully!');
		} else {
			ElMessage.error(response.message || 'Generation failed');
		}
	} catch (error) {
		console.error('Generate workflow error:', error);
		ElMessage.error('Error during generation');
	} finally {
		generating.value = false;
	}
};

const streamGenerateWorkflow = async () => {
	if (!input.description.trim()) {
		ElMessage.warning('Please describe your desired workflow');
		return;
	}

	streaming.value = true;
	streamSteps.value = [];
	result.value = null;

	try {
		// 暂时使用普通生成代替流式生成，因为流式需要特殊处理
		streamSteps.value.push({
			type: 'start',
			message: 'Starting workflow generation...',
			data: null
		});

		streamSteps.value.push({
			type: 'progress',
			message: 'Analyzing requirements...',
			data: null
		});

		const response = await generateAIWorkflow(input);

		streamSteps.value.push({
			type: 'progress',
			message: 'Generating workflow structure...',
			data: null
		});

		if (response.success) {
			streamSteps.value.push({
				type: 'complete',
				message: 'Workflow generation complete!',
				data: response.data
			});
			
			result.value = response.data;
			ElMessage.success('Workflow generated successfully!');
		} else {
			streamSteps.value.push({
				type: 'error',
				message: response.message || 'Generation failed',
				data: null
			});
			ElMessage.error(response.message || 'Generation failed');
		}

		ElMessage.success('Real-time generation complete!');
	} catch (error) {
		console.error('Stream generate error:', error);
		ElMessage.error('Error during real-time generation');
		streamSteps.value.push({
			type: 'error',
			message: 'Error during generation'
		});
	} finally {
		streaming.value = false;
	}
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
			emit('workflowGenerated', result.value);
			ElMessage.success('Workflow applied');
		}
	} catch {
		// User cancelled
	}
};

const clearInput = () => {
	Object.assign(input, {
		description: '',
		context: '',
		requirements: [],
		industry: '',
		processType: '',
		includeApprovals: true,
		includeNotifications: true,
		estimatedDuration: 0
	});
	requirementsText.value = '';
	result.value = null;
	streamSteps.value = [];
};

const getToken = () => {
	// 使用项目标准的token获取方式
	const tokenObj = getTokenobj();
	return tokenObj?.accessToken?.token || '';
};

// Lifecycle
onMounted(() => {
	// 检查AI服务状态
	checkAIStatus();
});

const checkAIStatus = async () => {
	try {
		const response = await getAIWorkflowStatus();
		
		if (response.success) {
			Object.assign(aiStatus, response.data);
		}
	} catch (error) {
		console.error('Check AI status error:', error);
		aiStatus.isAvailable = false;
	}
};
</script>

<style scoped>
.ai-workflow-generator {
	max-width: 1200px;
	margin: 0 auto;
}

.animate-spin {
	animation: spin 1s linear infinite;
}

@keyframes spin {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}
</style> 