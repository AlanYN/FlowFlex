<template>
	<div class="ai-workflow-page">
		<!-- 页面头部 -->
		<div class="page-header mb-6">
			<div class="flex items-center justify-between">
				<div>
					<h1 class="text-2xl font-bold text-gray-900">AI Workflow Generator</h1>
					<p class="text-gray-600 mt-1">Use artificial intelligence to quickly create and optimize workflows</p>
				</div>
				<div class="flex space-x-2">
					<el-button @click="showWorkflowList = !showWorkflowList">
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
			<!-- AI生成器 -->
			<div :class="showWorkflowList ? 'col-span-8' : 'col-span-12'">
				<AIWorkflowGenerator @workflow-generated="handleWorkflowGenerated" />
			</div>

			<!-- 工作流列表 -->
			<div v-if="showWorkflowList" class="col-span-4">
				<el-card shadow="hover">
					<template #header>
						<div class="flex items-center justify-between">
							<span class="font-semibold">现有工作流</span>
							<el-button size="small" @click="refreshWorkflowList">
								<el-icon>
									<Refresh />
								</el-icon>
							</el-button>
						</div>
					</template>

					<div class="space-y-3">
						<div
							v-for="workflow in workflowList"
							:key="workflow.id"
							class="border border-gray-200 rounded-lg p-3 hover:bg-gray-50 cursor-pointer transition-colors"
							@click="selectWorkflow(workflow)"
						>
							<div class="flex items-center justify-between mb-2">
								<h4 class="font-medium text-sm">{{ workflow.name }}</h4>
								<el-tag :type="workflow.isActive ? 'success' : 'info'" size="small">
									{{ workflow.isActive ? '活跃' : '草稿' }}
								</el-tag>
							</div>
							<p class="text-xs text-gray-600 mb-2">{{ workflow.description }}</p>
							<div class="flex items-center justify-between text-xs text-gray-500">
								<span>{{ workflow.stageCount || 0 }} 个阶段</span>
								<span>{{ formatDate(workflow.createdAt) }}</span>
							</div>
						</div>

						<div v-if="workflowList.length === 0" class="text-center py-8 text-gray-500">
							<el-icon class="text-2xl mb-2">
								<Document />
							</el-icon>
							<p class="text-sm">暂无工作流</p>
						</div>
					</div>
				</el-card>
			</div>
		</div>

		<!-- 生成结果对话框 -->
		<el-dialog
			v-model="showGeneratedDialog"
			title="AI生成的工作流"
			width="80%"
			:close-on-click-modal="false"
		>
			<div v-if="generatedWorkflow" class="space-y-4">
				<!-- 工作流基本信息编辑 -->
				<el-card>
					<template #header>
						<span class="font-semibold">基本信息</span>
					</template>
					
					<el-form :model="generatedWorkflow" label-width="100px">
						<el-row :gutter="20">
							<el-col :span="12">
								<el-form-item label="工作流名称">
									<el-input v-model="generatedWorkflow.name" />
								</el-form-item>
							</el-col>
							<el-col :span="12">
								<el-form-item label="状态">
									<el-switch
										v-model="generatedWorkflow.isActive"
										active-text="启用"
										inactive-text="草稿"
									/>
								</el-form-item>
							</el-col>
						</el-row>
						<el-form-item label="描述">
							<el-input
								v-model="generatedWorkflow.description"
								type="textarea"
								:rows="3"
							/>
						</el-form-item>
					</el-form>
				</el-card>

				<!-- 阶段预览和编辑 -->
				<el-card>
					<template #header>
						<div class="flex items-center justify-between">
							<span class="font-semibold">工作流阶段</span>
							<el-button size="small" @click="addStage">
								<el-icon class="mr-1">
									<Plus />
								</el-icon>
								添加阶段
							</el-button>
						</div>
					</template>

					<div class="space-y-4">
						<div
							v-for="(stage, index) in generatedStages"
							:key="index"
							class="border border-gray-200 rounded-lg p-4"
						>
							<div class="flex items-center justify-between mb-3">
								<div class="flex items-center">
									<el-tag size="small" class="mr-2">{{ stage.order }}</el-tag>
									<el-input
										v-model="stage.name"
										size="small"
										class="w-48"
										@blur="updateStage(index)"
									/>
								</div>
								<div class="flex items-center space-x-2">
									<el-select
										v-model="stage.assignedGroup"
										size="small"
										placeholder="负责团队"
										class="w-32"
									>
										<el-option label="Sales" value="Sales" />
										<el-option label="IT" value="IT" />
										<el-option label="HR" value="HR" />
										<el-option label="Finance" value="Finance" />
										<el-option label="Operations" value="Operations" />
									</el-select>
									<el-input-number
										v-model="stage.estimatedDuration"
										size="small"
										:min="1"
										:max="30"
										class="w-24"
									/>
									<span class="text-sm text-gray-500">天</span>
									<el-button size="small" type="danger" @click="removeStage(index)">
										<el-icon>
											<Remove />
										</el-icon>
									</el-button>
								</div>
							</div>
							
							<el-input
								v-model="stage.description"
								type="textarea"
								:rows="2"
								placeholder="阶段描述..."
								@blur="updateStage(index)"
							/>

							<div v-if="stage.requiredFields && stage.requiredFields.length > 0" class="mt-2">
								<div class="text-xs text-gray-500 mb-1">必填字段:</div>
								<div class="flex flex-wrap gap-1">
									<el-tag
										v-for="(field, fieldIndex) in stage.requiredFields"
										:key="fieldIndex"
										size="small"
										closable
										@close="removeRequiredField(index, fieldIndex)"
									>
										{{ field }}
									</el-tag>
									<el-button size="small" @click="addRequiredField(index)">
										<el-icon>
											<Plus />
										</el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-card>
			</div>

			<template #footer>
				<div class="flex justify-between">
					<div>
						<el-button @click="enhanceWorkflow" :loading="enhancing">
							<el-icon class="mr-1">
								<Star />
							</el-icon>
							AI优化
						</el-button>
						<el-button @click="validateWorkflow" :loading="validating">
							<el-icon class="mr-1">
								<Check />
							</el-icon>
							验证工作流
						</el-button>
					</div>
					<div>
						<el-button @click="showGeneratedDialog = false">取消</el-button>
						<el-button type="primary" @click="saveWorkflow" :loading="saving">
							保存工作流
						</el-button>
					</div>
				</div>
			</template>
		</el-dialog>

		<!-- 字段添加对话框 -->
		<el-dialog v-model="showFieldDialog" title="添加必填字段" width="400px">
			<el-input
				v-model="newFieldName"
				placeholder="请输入字段名称"
				@keyup.enter="confirmAddField"
			/>
			<template #footer>
				<el-button @click="showFieldDialog = false">取消</el-button>
				<el-button type="primary" @click="confirmAddField">确定</el-button>
			</template>
		</el-dialog>

		<!-- AI增强对话框 -->
		<el-dialog v-model="showEnhanceDialog" title="AI工作流增强" width="600px">
			<div class="space-y-4">
				<div>
					<label class="block text-sm font-medium text-gray-700 mb-2">
						增强需求描述
					</label>
					<el-input
						v-model="enhanceRequest"
						type="textarea"
						:rows="4"
						placeholder="描述你希望如何改进这个工作流..."
					/>
				</div>

				<div v-if="enhanceResult" class="bg-gray-50 p-4 rounded-lg">
					<h4 class="font-semibold mb-2">AI增强建议</h4>
					<ul class="space-y-2">
						<li
							v-for="suggestion in enhanceResult.suggestions"
							:key="suggestion.description"
							class="flex items-start"
						>
							<el-icon class="mr-2 mt-1 text-blue-500">
								<InfoFilled />
							</el-icon>
							<div>
								<p class="text-sm">{{ suggestion.description }}</p>
								<div class="flex items-center mt-1">
									<el-tag size="small" :type="getPriorityType(suggestion.priority)">
										优先级: {{ Math.round(suggestion.priority * 100) }}%
									</el-tag>
								</div>
							</div>
						</li>
					</ul>
				</div>
			</div>

			<template #footer>
				<el-button @click="showEnhanceDialog = false">关闭</el-button>
				<el-button type="primary" @click="requestEnhancement" :loading="enhancing">
					获取建议
				</el-button>
				<el-button
					v-if="enhanceResult"
					type="success"
					@click="applyEnhancements"
				>
					应用建议
				</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	List,
	Plus,
	Refresh,
	Document,
	Remove,
	Star,
	Check,
	InfoFilled
} from '@element-plus/icons-vue';

// Components
import AIWorkflowGenerator from '@/components/ai/AIWorkflowGenerator.vue';

// APIs
import { getWorkflowList, createWorkflow, updateWorkflow } from '@/apis/ow';
import { enhanceAIWorkflow, validateAIWorkflow } from '@/apis/ai/workflow';

// Router
const router = useRouter();

// Reactive Data
const showWorkflowList = ref(true);
const workflowList = ref([]);
const showGeneratedDialog = ref(false);
const generatedWorkflow = ref(null);
const generatedStages = ref([]);
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
const enhanceResult = ref(null);

// Modification mode tracking
const isModifyMode = ref(false);
const selectedWorkflowId = ref(null);

// Methods
const handleWorkflowGenerated = (workflowData) => {
	// workflowData 现在是完整的AI响应数据
	generatedWorkflow.value = workflowData.generatedWorkflow;
	generatedStages.value = workflowData.stages || [];
	
	// 设置操作模式信息
	isModifyMode.value = workflowData.operationMode === 'modify';
	selectedWorkflowId.value = workflowData.selectedWorkflowId;
	
	showGeneratedDialog.value = true;
	
	console.log('Generated workflow data:', workflowData);
	console.log('Generated stages:', workflowData.stages);
	console.log('Operation mode:', workflowData.operationMode);
	console.log('Selected workflow ID:', workflowData.selectedWorkflowId);
	
	const message = isModifyMode.value ? 'AI工作流修改完成！请检查并保存。' : 'AI工作流生成完成！请检查并编辑后保存。';
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

const selectWorkflow = (workflow) => {
	router.push({
		path: '/onboard/workflow',
		query: { id: workflow.id }
	});
};

const goToTraditionalCreate = () => {
	router.push('/onboard/workflow');
};

const addStage = () => {
	const newOrder = Math.max(...generatedStages.value.map(s => s.order), 0) + 1;
	generatedStages.value.push({
		name: `新阶段 ${newOrder}`,
		description: '',
		order: newOrder,
		assignedGroup: 'General',
		requiredFields: [],
		estimatedDuration: 1
	});
};

const removeStage = (index) => {
	generatedStages.value.splice(index, 1);
	// 重新排序
	generatedStages.value.forEach((stage, idx) => {
		stage.order = idx + 1;
	});
};

const updateStage = (index) => {
	// 阶段更新逻辑，可以添加自动保存
	console.log('Stage updated:', generatedStages.value[index]);
};

const addRequiredField = (stageIndex) => {
	currentStageIndex.value = stageIndex;
	showFieldDialog.value = true;
};

const confirmAddField = () => {
	if (newFieldName.value.trim() && currentStageIndex.value >= 0) {
		if (!generatedStages.value[currentStageIndex.value].requiredFields) {
			generatedStages.value[currentStageIndex.value].requiredFields = [];
		}
		generatedStages.value[currentStageIndex.value].requiredFields.push(newFieldName.value.trim());
		newFieldName.value = '';
		showFieldDialog.value = false;
	}
};

const removeRequiredField = (stageIndex, fieldIndex) => {
	generatedStages.value[stageIndex].requiredFields.splice(fieldIndex, 1);
};

const enhanceWorkflow = () => {
	showEnhanceDialog.value = true;
	enhanceRequest.value = '';
	enhanceResult.value = null;
};

const requestEnhancement = async () => {
	if (!enhanceRequest.value.trim()) {
		ElMessage.warning('请描述增强需求');
		return;
	}

	enhancing.value = true;
	try {
		// 假设有工作流ID，实际应用中需要先保存工作流获取ID
		const response = await enhanceAIWorkflow(1, enhanceRequest.value);
		if (response.success) {
			enhanceResult.value = response.data;
		} else {
			ElMessage.error('获取增强建议失败');
		}
	} catch (error) {
		console.error('Enhancement error:', error);
		ElMessage.error('增强过程中出现错误');
	} finally {
		enhancing.value = false;
	}
};

const applyEnhancements = () => {
	// 应用AI增强建议的逻辑
	ElMessage.success('增强建议已应用');
	showEnhanceDialog.value = false;
};

const validateWorkflow = async () => {
	if (!generatedWorkflow.value) return;

	validating.value = true;
	try {
		const workflowData = {
			...generatedWorkflow.value,
			stages: generatedStages.value
		};

		const response = await validateAIWorkflow(workflowData);
		if (response.success) {
			const result = response.data;
			if (result.isValid) {
				ElMessage.success(`工作流验证通过！质量评分: ${Math.round(result.qualityScore * 100)}%`);
			} else {
				const errors = result.issues.filter(issue => issue.severity === 'Error');
				const warnings = result.issues.filter(issue => issue.severity === 'Warning');
				
				let message = '工作流验证发现问题:\n';
				if (errors.length > 0) {
					message += `错误 (${errors.length}): ${errors.map(e => e.message).join(', ')}\n`;
				}
				if (warnings.length > 0) {
					message += `警告 (${warnings.length}): ${warnings.map(w => w.message).join(', ')}`;
				}
				
				ElMessage.warning(message);
			}
		}
	} catch (error) {
		console.error('Validation error:', error);
		ElMessage.error('验证过程中出现错误');
	} finally {
		validating.value = false;
	}
};

const saveWorkflow = async () => {
	console.log('Saving workflow - generatedWorkflow:', generatedWorkflow.value);
	console.log('Saving workflow - generatedStages:', generatedStages.value);
	console.log('Saving workflow - stages length:', generatedStages.value.length);
	
	if (!generatedWorkflow.value || generatedStages.value.length === 0) {
		ElMessage.warning('请确保工作流包含至少一个阶段');
		return;
	}

	saving.value = true;
	try {
		const workflowData = {
			name: generatedWorkflow.value.name,
			description: generatedWorkflow.value.description,
			isActive: generatedWorkflow.value.isActive,
			status: "active",
			startDate: new Date().toISOString(),
			// 注意：后端期望的是大写的Stages
			stages: generatedStages.value.map((stage, index) => ({
				name: stage.name,
				description: stage.description,
				order: stage.order || (index + 1),
				defaultAssignedGroup: stage.assignedGroup || "执行团队",
				estimatedDuration: stage.estimatedDuration || 1,
				isActive: true,
				workflowVersion: "1"
			}))
		};

		let response;
		if (isModifyMode.value && selectedWorkflowId.value) {
			// 修改模式：更新现有workflow
			response = await updateWorkflow(selectedWorkflowId.value, workflowData);
			if (response.success) {
				ElMessage.success('工作流修改成功！');
				showGeneratedDialog.value = false;
				await refreshWorkflowList();
				
				// 跳转到工作流详情页
				router.push({
					path: '/onboard/onboardWorkflow',
					query: { id: selectedWorkflowId.value }
				});
			} else {
				ElMessage.error(response.message || '修改失败');
			}
		} else {
			// 创建模式：创建新workflow
			response = await createWorkflow(workflowData);
			if (response.success) {
				ElMessage.success('工作流保存成功！');
				showGeneratedDialog.value = false;
				await refreshWorkflowList();
				
				// 跳转到工作流详情页
				router.push({
					path: '/onboard/onboardWorkflow',
					query: { id: response.data }
				});
			} else {
				ElMessage.error(response.message || '保存失败');
			}
		}
	} catch (error) {
		console.error('Save workflow error:', error);
		ElMessage.error('保存过程中出现错误');
	} finally {
		saving.value = false;
	}
};

const formatDate = (dateString) => {
	if (!dateString) return '';
	return new Date(dateString).toLocaleDateString();
};

const getPriorityType = (priority) => {
	if (priority >= 0.8) return 'danger';
	if (priority >= 0.6) return 'warning';
	return 'info';
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
</style> 