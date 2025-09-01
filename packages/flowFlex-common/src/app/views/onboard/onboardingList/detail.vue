<template>
	<div class="pb-6 bg-gray-50 dark:bg-black-400">
		<!-- 顶部导航栏 -->
		<div class="flex justify-between items-center mb-6">
			<div class="flex items-center">
				<el-button
					link
					size="small"
					@click="handleBack"
					class="mr-2 !p-1 hover:bg-gray-100 dark:hover:bg-black-200 rounded"
				>
					<el-icon class="text-lg">
						<ArrowLeft />
					</el-icon>
					Back
				</el-button>
				<h1 class="text-2xl font-bold text-gray-900 dark:text-white-100">
					Onboarding Details: {{ onboardingData?.leadId }} {{ onboardingData?.leadName }}
				</h1>
			</div>
			<div class="flex items-center space-x-2">
				<el-button
					type="primary"
					@click="saveQuestionnaireAndField"
					:loading="saveAllLoading"
					:icon="Document"
				>
					Save
				</el-button>
				<el-button type="primary" @click="handleCompleteStage" :loading="completing">
					<el-icon class="mr-1">
						<Check />
					</el-icon>
					Complete Stage
				</el-button>
				<el-button @click="handleCustomerOverview">Customer Overview</el-button>
				<el-button @click="portalAccessDialogVisible = true">
					<el-icon>
						<User />
					</el-icon>
					&nbsp;&nbsp;Portal Access Management
				</el-button>
				<!-- <el-button type="primary" @click="messageDialogVisible = true">
					<el-icon>
						<ChatDotSquare />
					</el-icon>
					&nbsp;&nbsp;Send Message
				</el-button> -->
			</div>
		</div>

		<!-- 主要内容区域 -->
		<div class="flex w-full gap-6">
			<!-- 左侧阶段详情 (2/3 宽度) -->
			<div class="flex-[2] min-w-0 overflow-hidden">
				<div class="rounded-md el-card is-always-shadow rounded-md el-card__header">
					<div
						class="bg-gradient-to-r from-blue-500 to-indigo-500 text-white -mx-5 -my-5 px-5 py-4 rounded-t-lg"
					>
						<h2 class="text-lg font-semibold">{{ currentStageTitle }}</h2>
					</div>
				</div>
				<el-scrollbar ref="leftScrollbarRef" class="h-full pr-4 w-full">
					<div class="space-y-6 mt-4">
						<!-- AI Summary 展示（当前阶段） -->
						<div
							v-if="showAISummarySection"
							class="ai-summary-container relative overflow-hidden ml-2"
						>
							<!-- AI装饰性背景元素 -->
							<div class="ai-bg-decoration"></div>
							<div class="ai-circuit-pattern"></div>

							<!-- 主要内容区域 -->
							<div
								class="relative z-10 bg-white dark:bg-gradient-to-br dark:from-slate-900 dark:to-slate-800 backdrop-blur-sm rounded-lg ai-gradient-border shadow-lg"
							>
								<!-- Header区域 -->
								<div
									class="ai-summary-header px-6 py-4 border-b border-blue-200/50 dark:border-blue-400/20"
								>
									<div class="flex items-center justify-between">
										<div class="flex items-center space-x-3">
											<!-- AI图标 -->
											<div class="ai-icon-container">
												<div class="ai-icon">
													<svg
														width="24"
														height="24"
														viewBox="0 0 24 24"
														fill="none"
														xmlns="http://www.w3.org/2000/svg"
													>
														<path
															d="M12 2L13.09 5.5L16 6L13.09 6.5L12 10L10.91 6.5L8 6L10.91 5.5L12 2Z"
															fill="currentColor"
														/>
														<path
															d="M18 8L18.82 10.5L21 11L18.82 11.5L18 14L17.18 11.5L15 11L17.18 10.5L18 8Z"
															fill="currentColor"
														/>
														<path
															d="M6 14L6.82 16.5L9 17L6.82 17.5L6 20L5.18 17.5L3 17L5.18 16.5L6 14Z"
															fill="currentColor"
														/>
													</svg>
												</div>
												<div class="ai-pulse-ring"></div>
											</div>

											<!-- 标题和状态 -->
											<div>
												<h3
													class="ai-title font-semibold text-transparent bg-clip-text bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 dark:from-blue-400 dark:via-purple-400 dark:to-indigo-400"
												>
													AI Summary
												</h3>
												<div class="flex items-center space-x-2 mt-1">
													<div
														v-if="aiSummaryLoading"
														class="ai-status-badge generating"
													>
														<div class="status-dot"></div>
														<span class="text-xs">Generating...</span>
													</div>
													<div
														v-else-if="currentAISummary"
														class="ai-status-badge ready"
													>
														<div class="status-dot"></div>
														<span class="text-xs">Ready</span>
													</div>
													<div v-else class="ai-status-badge idle">
														<div class="status-dot"></div>
														<span class="text-xs">Idle</span>
													</div>
													<!-- 时间戳移到Ready状态后面 -->
													<div
														v-if="
															currentAISummaryGeneratedAt &&
															currentAISummary
														"
														class="text-xs text-gray-400 dark:text-gray-500 ml-2"
													>
														Generated:
														{{
															timeZoneConvert(
																currentAISummaryGeneratedAt
															)
														}}
													</div>
												</div>
											</div>
										</div>

										<!-- 刷新按钮 -->
										<el-button
											:icon="Refresh"
											size="small"
											circle
											:loading="aiSummaryLoading"
											@click="refreshAISummary"
											title="Regenerate AI Summary"
											class="ai-refresh-btn"
										/>
									</div>
								</div>

								<!-- 内容区域 -->
								<div class="ai-summary-body px-6 py-4">
									<!-- AI Summary content (always visible if exists) -->
									<div v-if="currentAISummary" class="ai-summary-content">
										<div class="ai-content-wrapper">
											<p
												class="break-words word-wrap text-sm leading-7 text-gray-800 dark:text-gray-100 overflow-hidden"
												:class="{ 'ai-streaming': aiSummaryLoading }"
											>
												{{ currentAISummary }}
												<span
													v-if="aiSummaryLoading"
													class="ai-typing-cursor"
												>
													|
												</span>
											</p>
										</div>
									</div>

									<!-- Loading state (only when no content yet) -->
									<div v-else-if="aiSummaryLoading" class="ai-loading-state">
										<div class="ai-loading-animation">
											<div class="loading-brain">
												<div class="brain-wave"></div>
												<div class="brain-wave"></div>
												<div class="brain-wave"></div>
											</div>
										</div>
										<div class="ai-loading-text">
											<div
												class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1"
											>
												AI is analyzing your data
											</div>
											<div class="text-xs text-gray-500 dark:text-gray-400">
												{{ aiSummaryLoadingText }}
											</div>
										</div>
										<div class="ai-loading-progress">
											<div class="progress-bar"></div>
										</div>
									</div>

									<!-- Empty state -->
									<div
										v-else
										class="ai-empty-state cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 rounded-lg transition-colors"
										@click="refreshAISummary"
										title="Click to generate AI summary"
									>
										<div class="empty-icon">
											<svg
												width="48"
												height="48"
												viewBox="0 0 24 24"
												fill="none"
												xmlns="http://www.w3.org/2000/svg"
											>
												<path
													d="M12 2L13.09 5.5L16 6L13.09 6.5L12 10L10.91 6.5L8 6L10.91 5.5L12 2Z"
													fill="currentColor"
													opacity="0.3"
												/>
												<path
													d="M18 8L18.82 10.5L21 11L18.82 11.5L18 14L17.18 11.5L15 11L17.18 10.5L18 8Z"
													fill="currentColor"
													opacity="0.3"
												/>
												<path
													d="M6 14L6.82 16.5L9 17L6.82 17.5L6 20L5.18 17.5L3 17L5.18 16.5L6 14Z"
													fill="currentColor"
													opacity="0.3"
												/>
											</svg>
										</div>
										<div class="text-sm text-gray-500 dark:text-gray-400 mb-1">
											No AI insights available
										</div>
										<div class="text-xs text-gray-400 dark:text-gray-500 mb-2">
											Click here or the refresh button to generate intelligent
											summary
										</div>
										<div
											class="text-xs text-blue-600 dark:text-blue-400 font-medium"
										>
											🚀 Generate AI Summary
										</div>
									</div>

									<!-- Loading indicator when streaming content -->
									<div
										v-if="aiSummaryLoading && currentAISummary"
										class="ai-streaming-indicator"
									>
										<div class="streaming-dots">
											<div class="dot"></div>
											<div class="dot"></div>
											<div class="dot"></div>
										</div>
										<span class="text-xs text-blue-600 dark:text-blue-400 ml-2">
											{{ aiSummaryLoadingText }}
										</span>
									</div>
								</div>
							</div>
						</div>

						<!-- Stage Details 加载状态 -->
						<div
							v-if="stageDataLoading"
							class="bg-white dark:bg-black-300 rounded-md p-8"
						>
							<div class="flex flex-col items-center justify-center space-y-4">
								<el-icon class="is-loading text-4xl text-primary-500">
									<Loading />
								</el-icon>
								<p class="text-gray-500 dark:text-gray-400">
									Loading stage details...
								</p>
							</div>
						</div>

						<!-- 根据Stage Components动态渲染 -->
						<template v-if="!stageDataLoading && onboardingActiveStageInfo?.components">
							<div
								v-for="component in sortedComponents"
								:key="`${component.key}-${component.order}`"
								v-show="component.isEnabled"
							>
								<!-- 静态字段表单 -->
								<StaticForm
									v-if="
										component.key === 'fields' &&
										component?.staticFields &&
										component.staticFields?.length > 0
									"
									:ref="setStaticFormRef"
									:static-fields="component.staticFields"
									:onboarding-id="onboardingId"
									:stage-id="activeStage"
									@save-success="refreshChangeLog"
								/>

								<!-- 检查清单组件 -->
								<CheckList
									v-else-if="
										component.key === 'checklist' &&
										component?.checklistIds &&
										component.checklistIds?.length > 0
									"
									:loading="checkLoading"
									:stage-id="activeStage"
									:checklist-data="getChecklistDataForComponent(component)"
									:onboarding-id="onboardingId"
									@task-toggled="handleTaskToggled"
									@refresh-checklist="loadCheckListData"
								/>

								<!-- 问卷组件 -->
								<QuestionnaireDetails
									v-else-if="
										component.key === 'questionnaires' &&
										component?.questionnaireIds &&
										component.questionnaireIds?.length > 0
									"
									:ref="setQuestionnaireDetailsRef"
									:stage-id="activeStage"
									:lead-data="onboardingData"
									:workflow-stages="workflowStages"
									:questionnaire-data="
										getQuestionnaireDataForComponent(component)
									"
									:onboardingId="onboardingId"
									@stage-updated="handleStageUpdated"
									:questionnaire-answers="
										getQuestionnaireAnswersForComponent(component)
									"
								/>

								<!-- 文件组件 -->
								<Documents
									v-else-if="component.key === 'files'"
									ref="documentsRef"
									:onboarding-id="onboardingId"
									:stage-id="activeStage"
									:component="component"
									@document-uploaded="handleDocumentUploaded"
									@document-deleted="handleDocumentDeleted"
								/>
							</div>
						</template>

						<!-- 兜底的StageDetails组件 -->
					</div>
				</el-scrollbar>
			</div>

			<!-- 右侧进度和笔记 (1/3 宽度) -->
			<div class="flex-1 flex-shrink-0">
				<el-scrollbar ref="rightScrollbarRef" class="h-full pr-4">
					<div class="space-y-6">
						<!-- OnboardingProgress组件 -->
						<div class="rounded-md overflow-hidden">
							<OnboardingProgress
								v-if="onboardingData && onboardingId"
								:active-stage="activeStage"
								:onboarding-data="onboardingData"
								:workflow-stages="workflowStages"
								@set-active-stage="setActiveStage"
								@stage-completed="loadOnboardingDetail"
								class="bg-white dark:bg-black-300 rounded-md shadow-lg border border-gray-200 dark:border-gray-600"
							/>
						</div>

						<!-- 笔记区域 -->
						<div class="rounded-md overflow-hidden">
							<InternalNotes
								v-if="activeStage && onboardingId"
								:onboarding-id="onboardingId"
								:stage-id="activeStage"
								@note-added="handleNoteAdded"
							/>
						</div>
					</div>
				</el-scrollbar>
			</div>
		</div>

		<!-- 变更日志 -->
		<!-- ChangeLog 加载状态 -->
		<div class="mt-4">
			<ChangeLog
				v-if="onboardingId"
				ref="changeLogRef"
				:onboarding-id="onboardingId"
				:stage-id="activeStage"
			/>
		</div>

		<!-- 消息对话框 -->
		<MessageDialog v-model="messageDialogVisible" :onboarding-data="onboardingData" />
		<!-- Portal Access Management 对话框 -->
		<el-dialog
			v-model="portalAccessDialogVisible"
			title="Portal Access Management"
			width="1000px"
			:before-close="() => (portalAccessDialogVisible = false)"
		>
			<PortalAccessContent :onboarding-id="onboardingId" :onboarding-data="onboardingData" />
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick, onBeforeUpdate } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowLeft, Loading, User, Document, Refresh, Check } from '@element-plus/icons-vue';
import { getTokenobj } from '@/utils/auth';
import { getTimeZoneInfo, timeZoneConvert } from '@/hooks/time';
import { useGlobSetting } from '@/settings';
import {
	getOnboardingByLead,
	getStaticFieldValuesByOnboarding,
	saveCheckListTask,
	getCheckListIds,
	getCheckListIsCompleted,
	getQuestionIds,
	getQuestionnaireAnswer,
	completeCurrentStage,
	onboardingSave,
} from '@/apis/ow/onboarding';
import { OnboardingItem, StageInfo, ComponentData, SectionAnswer } from '#/onboard';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { useI18n } from 'vue-i18n';
import { defaultStr } from '@/settings/projectSetting';
import { useUserStore } from '@/stores/modules/user';
// 导入组件
import OnboardingProgress from './components/OnboardingProgress.vue';
import QuestionnaireDetails from './components/QuestionnaireDetails.vue';
import InternalNotes from './components/InternalNotes.vue';
import ChangeLog from './components/ChangeLog.vue';
import MessageDialog from './components/MessageDialog.vue';
import CheckList from './components/CheckList.vue';
import Documents from './components/Documents.vue';
import StaticForm from './components/StaticForm.vue';
import PortalAccessContent from './components/PortalAccessContent.vue';

const { t } = useI18n();
const userStore = useUserStore();
const globSetting = useGlobSetting();

// 常量定义
const router = useRouter();
const route = useRoute();

// 响应式数据
const onboardingData = ref<OnboardingItem | null>(null);
const activeStage = ref<string>(''); // 初始为空，等待从服务器获取当前阶段
const workflowStages = ref<any[]>([]);
const messageDialogVisible = ref(false);
const portalAccessDialogVisible = ref(false);

// 存储批量查询到的数据
const checklistsData = ref<any[]>([]);
const questionnairesData = ref<any[]>([]);
// 问卷答案映射：questionnaireId -> responses[]
const questionnaireAnswersMap = ref<SectionAnswer[]>([]);

// Loading状态管理
const stageDataLoading = ref(false); // 初始加载和阶段完成后的数据加载状态
const initialLoading = ref(true); // 初始页面加载状态

// AI Summary相关状态
const aiSummaryLoading = ref(false);
const aiSummaryLoadingText = ref('Generating AI summary...');
const currentAISummary = ref('');
const currentAISummaryGeneratedAt = ref('');
const showAISummarySection = ref(true);
// 用于取消AI摘要请求的AbortController
let aiSummaryAbortController: AbortController | null = null;

// 使用自适应滚动条 hook
const { scrollbarRef: leftScrollbarRef } = useAdaptiveScrollbar(100);
const { scrollbarRef: rightScrollbarRef } = useAdaptiveScrollbar(100);

// 计算属性
const onboardingId = computed(() => {
	const id = route.query.onboardingId;
	if (!id || typeof id !== 'string') {
		return '';
	}
	return id;
});

// 添加组件引用
const questionnaireDetailsRefs = ref<any[]>([]);
const staticFormRefs = ref<any[]>([]);
const onboardingActiveStageInfo = ref<StageInfo | null>(null);
const documentsRef = ref<any[]>([]);

// 在组件更新前重置 refs，避免多次渲染导致重复收集
onBeforeUpdate(() => {
	staticFormRefs.value = [];
	questionnaireDetailsRefs.value = [];
});

// 函数式ref，用于收集StaticForm组件实例（去重）
const setStaticFormRef = (el: any) => {
	if (el && !staticFormRefs.value.includes(el)) {
		staticFormRefs.value.push(el);
	}
};

// 函数式ref，用于收集QuestionnaireDetails组件实例（去重）
const setQuestionnaireDetailsRef = (el: any) => {
	if (el && !questionnaireDetailsRefs.value.includes(el)) {
		questionnaireDetailsRefs.value.push(el);
	}
};

// 清理StaticForm refs
const clearStaticFormRefs = () => {
	staticFormRefs.value = [];
};

// 清理QuestionnaireDetails refs
const clearQuestionnaireDetailsRefs = () => {
	questionnaireDetailsRefs.value = [];
};

// 辅助函数：根据组件的checklistIds获取对应的checklist数据
const getChecklistDataForComponent = (component: ComponentData) => {
	if (!component.checklistIds || component.checklistIds.length === 0) {
		return [];
	}
	return checklistsData.value.filter((checklist) =>
		component.checklistIds.includes(checklist.id)
	);
};

// 辅助函数：根据组件的questionnaireIds获取对应的questionnaire数据
const getQuestionnaireDataForComponent = (component: ComponentData) => {
	if (!component.questionnaireIds || component.questionnaireIds.length === 0) {
		return null;
	}

	// 检查questionnairesData是否包含当前组件需要的问卷
	for (const questionnaire of questionnairesData.value) {
		if (component.questionnaireIds.includes(questionnaire.id)) {
			return questionnaire;
		}
	}

	return null;
};

// 根据组件获取对应问卷答案数组
const getQuestionnaireAnswersForComponent = (component: ComponentData) => {
	if (!component.questionnaireIds || component.questionnaireIds.length === 0) {
		return [];
	}
	const qId = component.questionnaireIds[0];
	return questionnaireAnswersMap.value[qId] || [];
};

// 根据components数组排序，确保静态字段表单在前面
const sortedComponents = computed(() => {
	if (!onboardingActiveStageInfo.value?.components) {
		return [];
	}

	return [...onboardingActiveStageInfo.value.components].sort((a, b) => {
		return a.order - b.order; // 根据order排序
	});
});

// 处理onboarding数据的共同逻辑
const processOnboardingData = (responseData: any) => {
	onboardingData.value = responseData;

	workflowStages.value = responseData.stagesProgress;

	// 根据 workflowStages 返回第一个未完成的 stageId
	// 首先按 order 排序，然后找到第一个未完成的阶段
	const sortedStages = [...workflowStages.value].sort((a, b) => (a.order || 0) - (b.order || 0));
	const firstIncompleteStage = sortedStages.find((stage) => !stage.isCompleted);

	// 如果所有阶段都完成了，返回最后一个阶段
	const newStageId =
		firstIncompleteStage?.stageId || sortedStages[sortedStages.length - 1]?.stageId;

	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === newStageId
	);

	// 更新AI Summary显示
	updateAISummaryFromStageInfo();

	return newStageId;
};

// 计算属性
const currentStageTitle = computed(() => {
	const currentStage = workflowStages.value.find((stage) => stage.stageId === activeStage.value);
	return currentStage?.stageName || defaultStr;
});

// API调用函数
const loadOnboardingDetail = async () => {
	if (!onboardingId.value) {
		ElMessage.error('Invalid onboarding ID');
		return;
	}

	try {
		// 通过 leadId 获取 onboarding 详情，包含stage进度信息
		const response = await getOnboardingByLead(onboardingId.value);
		if (response.code === '200') {
			const newStageId = processOnboardingData(response.data);

			// 设置activeStage
			if (newStageId) {
				activeStage.value = newStageId;
				// 设置 activeStage 后，加载当前阶段的基础数据
				await loadCurrentStageData();
				// 检查并自动生成AI Summary
				await checkAndGenerateAISummary();
			}
		}
	} finally {
		initialLoading.value = false;
		refreshChangeLog();
	}
};

// 批量加载检查清单数据
const loadCheckListData = async (onboardingId: string, stageId: string) => {
	if (!onboardingActiveStageInfo.value?.components) return;

	// 收集所有checklistIds
	const allChecklistIds: string[] = [];
	onboardingActiveStageInfo.value.components.forEach((component) => {
		if (component.key === 'checklist' && component.checklistIds?.length > 0) {
			allChecklistIds.push(...component.checklistIds);
		}
	});

	if (allChecklistIds.length === 0) return;

	try {
		// 并行调用两个接口
		const [checklistResponse, completionResponse] = await Promise.all([
			getCheckListIds(allChecklistIds),
			getCheckListIsCompleted(onboardingId, stageId),
		]);

		if (checklistResponse.code === '200') {
			// 获取已完成的任务信息，包含完成者和完成时间
			const completedTasksMap = new Map<string, any>();
			if (completionResponse.code === '200' && completionResponse.data) {
				// completionResponse.data 包含已完成的任务列表，包含 modifyBy 和 completedTime
				if (Array.isArray(completionResponse.data)) {
					completionResponse.data.forEach((completedTask: any) => {
						// 根据实际API返回的数据结构调整
						const taskId = completedTask.taskId || completedTask.id;
						if (taskId) {
							completedTasksMap.set(taskId, {
								isCompleted: completedTask.isCompleted,
								completedBy: completedTask.modifyBy || completedTask.createBy,
								completedTime:
									completedTask.completedTime || completedTask.modifyDate,
								filesJson: completedTask?.filesJson,
								assigneeName: completedTask?.assigneeName,
								filesCount: completedTask?.filesCount,
								notesCount: completedTask?.notesCount,
							});
						}
					});
				}
			}

			// 处理每个 checklist 的数据，合并完成状态信息
			const processedChecklists = (checklistResponse.data || []).map((checklist: any) => {
				// 确保 tasks 存在
				if (!checklist.tasks || !Array.isArray(checklist.tasks)) {
					checklist.tasks = [];
				}

				// 更新每个任务的完成状态和完成者信息
				checklist.tasks = checklist.tasks.map((task: any) => {
					const completionInfo = completedTasksMap.get(task.id);
					return {
						...task,
						isCompleted: completionInfo?.isCompleted || task.isCompleted || false,
						completedBy:
							completionInfo?.completedBy || task.assigneeName || task.createBy,
						completedDate: completionInfo?.completedTime || task.completedDate,
						filesJson: completionInfo?.filesJson,
						assigneeName: completionInfo?.assigneeName || task?.assigneeName,
						filesCount: completionInfo?.filesCount || task?.filesCount,
						notesCount: completionInfo?.notesCount || task?.notesCount,
					};
				});

				// 重新计算完成任务数和总任务数
				const completedTasks = checklist.tasks.filter(
					(task: any) => task.isCompleted
				).length;
				const totalTasks = checklist.tasks.length;

				// 重新计算完成率
				const completionRate =
					totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;

				// 更新 checklist 的统计信息
				return {
					...checklist,
					completedTasks,
					totalTasks,
					completionRate,
				};
			});

			checklistsData.value = processedChecklists;
		}
	} catch (error) {
		ElMessage.error('Failed to load checklists');
	}
};

// 批量加载问卷结构和答案
const loadQuestionnaireDataBatch = async (onboardingId: string, stageId: string) => {
	if (!onboardingActiveStageInfo.value?.components) return;

	// 收集所有questionnaireIds
	const allQuestionnaireIds: string[] = [];
	onboardingActiveStageInfo.value.components.forEach((component) => {
		if (component.key === 'questionnaires' && component.questionnaireIds?.length > 0) {
			allQuestionnaireIds.push(...component.questionnaireIds);
		}
	});

	if (allQuestionnaireIds.length === 0) return;

	try {
		// 并行请求：结构 + 答案
		const [structureRes, answerRes] = await Promise.all([
			getQuestionIds(allQuestionnaireIds),
			getQuestionnaireAnswer(onboardingId, stageId),
		]);

		// 处理结构
		if (structureRes.code === '200') {
			questionnairesData.value = structureRes.data || [];
		}
		await nextTick();
		// 处理答案
		if (answerRes.code === '200' && answerRes.data && Array.isArray(answerRes.data)) {
			const map: SectionAnswer[] = [];
			answerRes.data.forEach((item: any) => {
				if (item.questionnaireId && item.answerJson) {
					let parsed;
					try {
						parsed =
							typeof item.answerJson === 'string'
								? JSON.parse(item.answerJson)
								: item.answerJson;
					} catch {
						parsed = null;
					}
					if (parsed && Array.isArray(parsed.responses)) {
						map[item.questionnaireId] = {
							answer: parsed.responses,
							...item,
						};
					}
				}
			});
			questionnaireAnswersMap.value = map;
		}
	} catch (error) {
		ElMessage.error('Failed to load questionnaires');
	}
};

// 加载依赖stageId的数据（问卷、检查清单、变更日志）
const loadStageRelatedData = async (stageId: string) => {
	if (!stageId) return;

	try {
		// 设置加载状态
		stageDataLoading.value = true;

		// 清理之前的组件refs
		clearStaticFormRefs();
		clearQuestionnaireDetailsRefs();

		// 并行加载依赖stageId的数据
		await Promise.all([
			loadCheckListData(onboardingId.value, stageId),
			loadQuestionnaireDataBatch(onboardingId.value, stageId),
		]);
	} finally {
		stageDataLoading.value = false;
	}
};

// 加载当前阶段的基础数据（仅在初始加载时调用）
const loadCurrentStageData = async () => {
	if (!activeStage.value) return;

	await loadStageRelatedData(activeStage.value);
	await loadStaticFieldValues();
};

// 事件处理函数
const handleBack = () => {
	router.back();
};

const handleCustomerOverview = () => {
	// 跳转到客户概览页面，传递 leadId 参数
	router.push({
		name: 'CustomerOverview',
		params: {
			leadId: onboardingId.value,
		},
		query: {
			companyName: onboardingData.value?.leadName || '',
			from: 'onboardingDetail',
		},
	});
};

const handleStageUpdated = async () => {
	// 当stage内容更新后，重新加载当前stage的相关数据
	loadOnboardingDetail();
};

const loadStaticFieldValues = async () => {
	if (!onboardingId.value) return;

	try {
		const response = await getStaticFieldValuesByOnboarding(onboardingId.value);
		if (response.code === '200' && response.data && Array.isArray(response.data)) {
			// 接口返回的是数组格式的静态字段数据
			// 仅传递给 StaticForm 组件处理
			staticFormRefs.value.forEach((formRef) => {
				formRef.setFieldValues(response.data);
			});
		}
	} catch (error) {
		ElMessage.error('Failed to load static field values');
	}
};

const setActiveStage = async (stageId: string) => {
	// 如果切换到相同的阶段，不需要重新加载
	if (activeStage.value === stageId) {
		return;
	}

	// 取消当前正在进行的AI摘要生成（如果有）
	if (aiSummaryAbortController) {
		aiSummaryAbortController.abort();
		aiSummaryLoading.value = false;
		console.log('🚫 [Stage Switch] Cancelled AI summary generation due to stage change');
	}

	// 更新activeStage
	activeStage.value = stageId;
	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === stageId
	);

	// 更新AI Summary显示
	updateAISummaryFromStageInfo();

	// 重新加载依赖stageId的数据
	await loadStageRelatedData(stageId);
	await loadStaticFieldValues(); // 添加加载字段值的调用

	// 页面切换时自动检查并生成AI Summary
	console.log(
		'🔄 [Stage Switch] Stage switched to:',
		stageId,
		'AI Summary exists:',
		!!onboardingActiveStageInfo.value?.aiSummary
	);

	// 自动检查并生成AI Summary（如果不存在）
	await checkAndGenerateAISummary();
};

const handleNoteAdded = () => {
	// 笔记添加后的处理
	refreshChangeLog();
};

const handleDocumentUploaded = (document: any) => {
	// 文档上传后的处理
	refreshChangeLog();
};

const handleDocumentDeleted = (documentId: string) => {
	// 文档删除后的处理
	refreshChangeLog();
};

const checkLoading = ref(false);
const handleTaskToggled = async (task: any) => {
	// 处理任务状态切换
	try {
		checkLoading.value = true;
		const res = await saveCheckListTask({
			checklistId: task.checklistId,
			isCompleted: task.isCompleted,
			taskId: task.id,
			onboardingId: onboardingId.value,
			stageId: activeStage.value, // 添加当前阶段ID
		});
		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));

			// 更新本地 checklist 数据
			checklistsData.value.forEach((checklist) => {
				if (checklist.id === task.checklistId) {
					const taskToUpdate = checklist.tasks?.find((t: any) => t.id === task.id);
					if (taskToUpdate) {
						taskToUpdate.isCompleted = task.isCompleted;
						taskToUpdate.completedDate = task.isCompleted
							? new Date().toISOString()
							: null;
						// 更新完成者信息 - 从当前用户信息获取
						if (task.isCompleted) {
							taskToUpdate.completedBy =
								userStore.getUserInfo?.email || 'unknown@email.com';
						} else {
							taskToUpdate.completedBy = null;
						}

						// 更新 checklist 的完成统计
						const completedTasks =
							checklist.tasks?.filter((t: any) => t.isCompleted).length || 0;
						const totalTasks = checklist.tasks?.length || 0;
						checklist.completedTasks = completedTasks;
						checklist.completionRate =
							totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;
					}
				}
			});
			refreshChangeLog();
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} finally {
		checkLoading.value = false;
	}
};

const saveAllLoading = ref(false);
const saveAllForm = async (isValidate: boolean = true) => {
	try {
		saveAllLoading.value = true;
		const validationResults: Array<{ component: string; result: any }> = [];

		// 校验StaticForm组件
		if (staticFormRefs.value.length > 0) {
			for (let i = 0; i < staticFormRefs.value.length; i++) {
				const formRef = staticFormRefs.value[i];
				if (formRef && typeof formRef.handleSave === 'function') {
					try {
						const result = await formRef.handleSave(isValidate);
						validationResults.push({ component: `StaticForm-${i}`, result });
						if (!result) {
							return false;
						}
					} catch (error) {
						validationResults.push({ component: `StaticForm-${i}`, result: false });
						return false;
					}
				}
			}
		}

		// 校验QuestionnaireDetails组件
		if (questionnaireDetailsRefs.value.length > 0) {
			for (let i = 0; i < questionnaireDetailsRefs.value.length; i++) {
				const questRef = questionnaireDetailsRefs.value[i];
				if (questRef && typeof questRef.handleSave === 'function') {
					try {
						const result = await questRef.handleSave(false, isValidate);
						validationResults.push({ component: `QuestionnaireDetails-${i}`, result });
						if (!result) {
							return false;
						}
					} catch (error) {
						validationResults.push({
							component: `QuestionnaireDetails-${i}`,
							result: false,
						});
						return false;
					}
				}
			}
		}

		// 校验Documents组件
		if (documentsRef.value.length > 0 && isValidate) {
			for (let i = 0; i < documentsRef.value.length; i++) {
				const docRef = documentsRef.value[i];
				if (docRef && typeof docRef.vailComponent === 'function') {
					try {
						const result = docRef.vailComponent();
						validationResults.push({ component: `Documents-${i}`, result });
						if (!result) {
							return false;
						}
					} catch (error) {
						validationResults.push({ component: `Documents-${i}`, result: false });
						return false;
					}
				}
			}
		}

		return true;
	} catch (error) {
		return false;
	} finally {
		saveAllLoading.value = false;
	}
};

const completing = ref(false);
const handleCompleteStage = async () => {
	ElMessageBox.confirm(
		`Are you sure you want to mark this stage as complete? This action will record your name and the current time as the completion signature.`,
		'⚠️ Confirm Stage Completion',
		{
			confirmButtonText: 'Complete Stage',
			cancelButtonText: 'Cancel',
			distinguishCancelAndClose: true,
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					// 显示loading状态
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Deactivating...';

					completing.value = true;
					try {
						const res = await saveAllForm();
						if (!res) {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Complete Stage';
						} else {
							const res = await completeCurrentStage(onboardingId.value, {
								currentStageId: activeStage.value,
							});
							if (res.code === '200') {
								ElMessage.success(t('sys.api.operationSuccess'));
								loadOnboardingDetail();
							} else {
								ElMessage.error(res.msg || t('sys.api.operationFailed'));
							}
						}
						done();
					} finally {
						instance.confirmButtonLoading = false;
						instance.confirmButtonText = 'Complete Stage';
						completing.value = false;
					}
				} else {
					done();
				}
			},
		}
	);
};

const saveQuestionnaireAndField = async () => {
	const res = await saveAllForm(false);
	if (res) {
		ElMessage.success(t('sys.api.operationSuccess'));
		await onboardingSave(onboardingId.value, {
			onboardingId: onboardingId.value,
			stageId: activeStage.value,
		});
		loadOnboardingDetail();
	} else {
		ElMessage.error(t('sys.api.operationFailed'));
	}
};

const changeLogRef = ref<InstanceType<typeof ChangeLog>>();
const refreshChangeLog = () => {
	if (!changeLogRef.value) return;
	changeLogRef.value.loadChangeLogs();
};

// AI Summary相关方法
const updateAISummaryFromStageInfo = () => {
	if (onboardingActiveStageInfo.value?.aiSummary) {
		currentAISummary.value = onboardingActiveStageInfo.value.aiSummary;
		currentAISummaryGeneratedAt.value =
			onboardingActiveStageInfo.value.aiSummaryGeneratedAt || '';
	} else {
		currentAISummary.value = '';
		currentAISummaryGeneratedAt.value = '';
	}
};

const refreshAISummary = async () => {
	if (!activeStage.value) {
		ElMessage.error('No active stage selected');
		return;
	}

	// 取消之前的请求（如果存在）
	if (aiSummaryAbortController) {
		aiSummaryAbortController.abort();
		console.log('🚫 [AI Summary] Cancelled previous request');
	}

	// 创建新的AbortController
	aiSummaryAbortController = new AbortController();
	const currentStageId = activeStage.value; // 保存当前阶段ID，用于验证

	// 重置状态，开始流式生成
	aiSummaryLoading.value = true;
	aiSummaryLoadingText.value = 'Starting AI summary generation...';
	currentAISummary.value = ''; // 清空现有内容，准备流式显示
	console.log('🔄 [AI Summary] Starting generation for stage:', currentStageId);

	try {
		// 获取认证信息
		const tokenObj = getTokenobj();
		const userInfo = userStore.getUserInfo;

		// 构建请求头
		const headers: Record<string, string> = {
			'Content-Type': 'application/json',
			Accept: 'text/plain',
			'Time-Zone': getTimeZoneInfo().timeZone,
			'Application-code': globSetting?.ssoCode || '',
		};

		// 添加认证头
		if (tokenObj?.accessToken?.token) {
			const token = tokenObj.accessToken.token;
			const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
			headers.Authorization = `${tokenType} ${token}`;
		}

		// 添加用户相关头信息
		if (userInfo?.appCode) {
			headers['X-App-Code'] = String(userInfo.appCode);
		}
		if (userInfo?.tenantId) {
			headers['X-Tenant-Id'] = String(userInfo.tenantId);
		}

		// 使用fetch进行POST流式请求
		const url = `/api/ow/stages/v1/${currentStageId}/ai-summary/stream?onboardingId=${onboardingId.value}`;
		const response = await fetch(url, {
			method: 'POST',
			headers,
			signal: aiSummaryAbortController.signal,
		});

		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}

		const reader = response.body?.getReader();
		const decoder = new TextDecoder();

		if (!reader) {
			throw new Error('Response body is not readable');
		}

		// 直接处理纯文本流式响应
		for (let done = false; !done; ) {
			const { value, done: isDone } = await reader.read();
			done = isDone;
			if (done) break;

			// 检查当前阶段是否已经改变
			if (activeStage.value !== currentStageId) {
				console.log(
					'🚫 [AI Summary] Stage changed during generation, stopping stream processing'
				);
				aiSummaryLoading.value = false;
				return;
			}

			const chunk = decoder.decode(value, { stream: true });

			// 检查是否是错误信息
			if (chunk.startsWith('Error:')) {
				console.error('❌ [AI Summary] Server error:', chunk);
				ElMessage.error(chunk.replace('Error: ', '') || 'Failed to generate AI summary');
				aiSummaryLoading.value = false;
				return;
			}

			// 直接将文本内容添加到AI Summary中
			if (chunk.trim()) {
				currentAISummary.value += chunk;
				console.log('📝 [AI Summary] Text chunk received:', chunk.length, 'chars');
			}
		}

		// 最终验证阶段是否仍然是开始时的阶段
		if (activeStage.value !== currentStageId) {
			console.log(
				'🚫 [AI Summary] Stage changed after generation completed, discarding result'
			);
			aiSummaryLoading.value = false;
			return;
		}

		// 流结束，设置状态
		console.log('✅ [AI Summary] Stream completed for stage:', currentStageId);
		currentAISummaryGeneratedAt.value = new Date().toISOString();
		aiSummaryLoading.value = false;
		ElMessage.success('AI Summary generated successfully');

		// 更新本地stage信息 - 再次验证阶段
		if (onboardingActiveStageInfo.value && activeStage.value === currentStageId) {
			onboardingActiveStageInfo.value.aiSummary = currentAISummary.value;
			onboardingActiveStageInfo.value.aiSummaryGeneratedAt =
				currentAISummaryGeneratedAt.value;
			console.log('📝 [AI Summary] Updated stage info for stage:', currentStageId);
		} else {
			console.log('⚠️ [AI Summary] Skipped updating stage info due to stage change');
		}
	} catch (error: any) {
		// 检查是否是用户取消的请求
		if (error.name === 'AbortError') {
			console.log('🚫 [AI Summary] Request was cancelled');
			aiSummaryLoading.value = false;
			return;
		}

		console.error('Error generating AI summary:', error);
		aiSummaryLoading.value = false;
		ElMessage.error('Failed to generate AI summary');
	} finally {
		// 清理AbortController引用
		aiSummaryAbortController = null;
	}
};

const checkAndGenerateAISummary = async () => {
	// 检查当前阶段是否有AI Summary，如果没有则自动生成
	// 只有在stagesProgress中确实没有aiSummary时才自动生成
	if (
		!onboardingActiveStageInfo.value?.aiSummary &&
		!aiSummaryLoading.value &&
		onboardingActiveStageInfo.value &&
		activeStage.value
	) {
		console.log(
			'🤖 [AI Summary] Auto-generating for stage without existing summary:',
			activeStage.value
		);
		await refreshAISummary();
	} else if (onboardingActiveStageInfo.value?.aiSummary) {
		console.log('✅ [AI Summary] Stage already has AI summary, skipping auto-generation');
	} else {
		console.log('⏸️ [AI Summary] Skipping auto-generation:', {
			hasAiSummary: !!onboardingActiveStageInfo.value?.aiSummary,
			isLoading: aiSummaryLoading.value,
			hasStageInfo: !!onboardingActiveStageInfo.value,
			hasActiveStage: !!activeStage.value,
		});
	}
};

// 生命周期
onMounted(async () => {
	// 检查是否有有效的 onboarding ID
	if (!onboardingId.value) {
		ElMessage.error('Invalid onboarding ID from route parameters');
		router.push('/onboard/onboardList'); // 重定向到列表页
		return;
	}

	// 加载入职详情，这会获取 workflowId，然后加载对应的 stages，设置 activeStage 并加载基于 stage 的数据
	await loadOnboardingDetail();
});
</script>

<style scoped lang="scss">
/* AI Summary 容器样式 */
.ai-summary-container {
	position: relative;
	margin-bottom: 1.5rem;
}

/* AI渐变边框 */
.ai-gradient-border {
	position: relative;
	border: 2px solid transparent;
	background:
		linear-gradient(white, white) padding-box,
		linear-gradient(135deg, #3b82f6 0%, #8b5cf6 25%, #06b6d4 50%, #10b981 75%, #3b82f6 100%)
			border-box;
	animation: ai-border-flow 4s ease-in-out infinite;
	box-shadow:
		0 0 20px rgba(59, 130, 246, 0.15),
		0 0 40px rgba(139, 92, 246, 0.1),
		0 4px 24px rgba(0, 0, 0, 0.1);
	transition: all 0.3s ease;
}

.ai-gradient-border:hover {
	box-shadow:
		0 0 30px rgba(59, 130, 246, 0.25),
		0 0 60px rgba(139, 92, 246, 0.15),
		0 8px 32px rgba(0, 0, 0, 0.15);
	transform: translateY(-1px);
}

.dark .ai-gradient-border {
	background:
		linear-gradient(135deg, rgb(51, 65, 85), rgb(30, 41, 59)) padding-box,
		linear-gradient(135deg, #60a5fa 0%, #a78bfa 25%, #22d3ee 50%, #34d399 75%, #60a5fa 100%)
			border-box;
	box-shadow:
		0 0 25px rgba(96, 165, 250, 0.2),
		0 0 50px rgba(167, 139, 250, 0.12),
		0 4px 28px rgba(0, 0, 0, 0.3);
}

.dark .ai-gradient-border:hover {
	box-shadow:
		0 0 35px rgba(96, 165, 250, 0.3),
		0 0 70px rgba(167, 139, 250, 0.18),
		0 8px 36px rgba(0, 0, 0, 0.4);
}

/* AI装饰性背景 */
.ai-bg-decoration {
	position: absolute;
	top: -10px;
	right: -10px;
	width: 100px;
	height: 100px;
	background: radial-gradient(circle, rgba(59, 130, 246, 0.1) 0%, transparent 70%);
	border-radius: 50%;
	pointer-events: none;
	animation: pulse-glow 3s ease-in-out infinite;
}

.ai-circuit-pattern {
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background-image: linear-gradient(90deg, rgba(59, 130, 246, 0.03) 1px, transparent 1px),
		linear-gradient(rgba(59, 130, 246, 0.03) 1px, transparent 1px);
	background-size: 20px 20px;
	pointer-events: none;
	opacity: 0.5;
}

/* AI图标容器 */
.ai-icon-container {
	position: relative;
	display: flex;
	align-items: center;
	justify-content: center;
}

.ai-icon {
	width: 32px;
	height: 32px;
	background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #6366f1 100%);
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	position: relative;
	z-index: 2;
	box-shadow: 0 4px 20px rgba(59, 130, 246, 0.3);
	animation: float 3s ease-in-out infinite;
}

.ai-pulse-ring {
	position: absolute;
	width: 40px;
	height: 40px;
	border: 2px solid rgba(59, 130, 246, 0.4);
	border-radius: 50%;
	animation: pulse-ring 2s linear infinite;
}

/* AI标题 */
.ai-title {
	font-size: 16px;
	letter-spacing: 0.5px;
}

/* 状态徽章 */
.ai-status-badge {
	display: flex;
	align-items: center;
	gap: 4px;
	padding: 4px 8px;
	border-radius: 12px;
	font-weight: 500;

	.status-dot {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		margin-right: 4px;
		animation: status-pulse 2s ease-in-out infinite;
	}

	&.generating {
		background: rgba(245, 158, 11, 0.1);
		color: #f59e0b;

		.status-dot {
			background: #f59e0b;
		}
	}

	&.ready {
		background: rgba(34, 197, 94, 0.1);
		color: #22c55e;

		.status-dot {
			background: #22c55e;
		}
	}

	&.idle {
		background: rgba(107, 114, 128, 0.1);
		color: #6b7280;

		.status-dot {
			background: #6b7280;
		}
	}
}

/* 刷新按钮 */
.ai-refresh-btn {
	background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
	border: none;
	color: white;
	transition: all 0.3s ease;

	&:hover {
		transform: translateY(-1px);
		box-shadow: 0 6px 20px rgba(59, 130, 246, 0.4);
	}
}

/* AI内容样式 */
.ai-content-wrapper {
	position: relative;
	border-radius: 8px;
	width: 100%;
	max-width: 100%;
	overflow-wrap: break-word;
	word-break: break-word;
}

.ai-streaming {
	background: linear-gradient(
		90deg,
		transparent 0%,
		rgba(59, 130, 246, 0.08) 50%,
		transparent 100%
	);
	background-size: 200% 100%;
	animation: ai-shimmer 2s infinite;
	border-radius: 6px;
	padding: 8px;
}

.ai-typing-cursor {
	color: #3b82f6;
	font-weight: bold;
	animation: typing-blink 1s infinite;
}

/* 加载状态 */
.ai-loading-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 2rem;
	text-align: center;
}

.ai-loading-animation {
	margin-bottom: 1rem;
}

.loading-brain {
	width: 48px;
	height: 48px;
	position: relative;

	.brain-wave {
		position: absolute;
		width: 100%;
		height: 4px;
		background: linear-gradient(90deg, #3b82f6 0%, #8b5cf6 50%, #6366f1 100%);
		border-radius: 2px;
		animation: brain-wave-animation 1.5s ease-in-out infinite;

		&:nth-child(1) {
			top: 12px;
			animation-delay: 0s;
		}

		&:nth-child(2) {
			top: 22px;
			animation-delay: 0.3s;
		}

		&:nth-child(3) {
			top: 32px;
			animation-delay: 0.6s;
		}
	}
}

.ai-loading-progress {
	width: 100%;
	max-width: 200px;
	height: 3px;
	background: rgba(59, 130, 246, 0.1);
	border-radius: 2px;
	overflow: hidden;
	margin-top: 1rem;

	.progress-bar {
		height: 100%;
		background: linear-gradient(90deg, #3b82f6 0%, #8b5cf6 100%);
		border-radius: 2px;
		animation: progress-flow 2s ease-in-out infinite;
	}
}

/* 空状态 */
.ai-empty-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 2rem;
	text-align: center;

	.empty-icon {
		margin-bottom: 1rem;
		color: #9ca3af;
		opacity: 0.7;
		animation: float 4s ease-in-out infinite;
	}
}

/* 流式指示器 */
.ai-streaming-indicator {
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 8px;
	margin-top: 8px;
	background: rgba(59, 130, 246, 0.05);
	border-radius: 6px;
}

.streaming-dots {
	display: flex;
	gap: 4px;

	.dot {
		width: 4px;
		height: 4px;
		background: #3b82f6;
		border-radius: 50%;
		animation: dot-bounce 1.4s ease-in-out infinite both;

		&:nth-child(1) {
			animation-delay: -0.32s;
		}
		&:nth-child(2) {
			animation-delay: -0.16s;
		}
		&:nth-child(3) {
			animation-delay: 0s;
		}
	}
}

/* Powered by AI徽章 */
.ai-powered-badge {
	padding: 4px 8px;
	background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
	color: white;
	border-radius: 8px;
	font-size: 10px;
	text-transform: uppercase;
	letter-spacing: 0.5px;
	opacity: 0.8;
}

/* 动画定义 */
@keyframes pulse-glow {
	0%,
	100% {
		opacity: 0.5;
		transform: scale(1);
	}
	50% {
		opacity: 0.8;
		transform: scale(1.1);
	}
}

@keyframes pulse-ring {
	0% {
		transform: scale(0.8);
		opacity: 1;
	}
	100% {
		transform: scale(1.4);
		opacity: 0;
	}
}

@keyframes float {
	0%,
	100% {
		transform: translateY(0px);
	}
	50% {
		transform: translateY(-4px);
	}
}

@keyframes status-pulse {
	0%,
	100% {
		opacity: 1;
	}
	50% {
		opacity: 0.6;
	}
}

@keyframes ai-shimmer {
	0% {
		background-position: -200% 0;
	}
	100% {
		background-position: 200% 0;
	}
}

@keyframes typing-blink {
	0%,
	50% {
		opacity: 1;
	}
	51%,
	100% {
		opacity: 0;
	}
}

@keyframes brain-wave-animation {
	0%,
	100% {
		transform: scaleX(0.5);
		opacity: 0.5;
	}
	50% {
		transform: scaleX(1);
		opacity: 1;
	}
}

@keyframes progress-flow {
	0% {
		transform: translateX(-100%);
	}
	100% {
		transform: translateX(200%);
	}
}

@keyframes dot-bounce {
	0%,
	80%,
	100% {
		transform: scale(0);
	}
	40% {
		transform: scale(1);
	}
}

@keyframes ai-border-flow {
	0% {
		background:
			linear-gradient(white, white) padding-box,
			linear-gradient(135deg, #3b82f6 0%, #8b5cf6 25%, #06b6d4 50%, #10b981 75%, #3b82f6 100%)
				border-box;
	}
	25% {
		background:
			linear-gradient(white, white) padding-box,
			linear-gradient(135deg, #10b981 0%, #3b82f6 25%, #8b5cf6 50%, #06b6d4 75%, #10b981 100%)
				border-box;
	}
	50% {
		background:
			linear-gradient(white, white) padding-box,
			linear-gradient(135deg, #06b6d4 0%, #10b981 25%, #3b82f6 50%, #8b5cf6 75%, #06b6d4 100%)
				border-box;
	}
	75% {
		background:
			linear-gradient(white, white) padding-box,
			linear-gradient(135deg, #8b5cf6 0%, #06b6d4 25%, #10b981 50%, #3b82f6 75%, #8b5cf6 100%)
				border-box;
	}
	100% {
		background:
			linear-gradient(white, white) padding-box,
			linear-gradient(135deg, #3b82f6 0%, #8b5cf6 25%, #06b6d4 50%, #10b981 75%, #3b82f6 100%)
				border-box;
	}
}

/* 滚动条样式 */
:deep(.el-scrollbar__view) {
	padding: 0;
}

:deep(.el-scrollbar__bar) {
	opacity: 0.3;
	transition: opacity 0.3s;
}

:deep(.el-scrollbar:hover .el-scrollbar__bar) {
	opacity: 1;
}

/* 文字溢出处理 */
.word-wrap {
	word-wrap: break-word;
	-webkit-hyphens: auto;
	-moz-hyphens: auto;
	hyphens: auto;
}

/* 响应式设计 */
@media (max-width: 1024px) {
	/* 在小屏幕设备上的样式调整 */
}

/* 暗色主题样式 */
html.dark {
	.bg-gray-50 {
		@apply bg-black-400 !important;
	}

	.text-gray-900 {
		@apply text-white-100 !important;
	}

	.text-gray-600,
	.text-gray-500 {
		@apply text-gray-300 !important;
	}

	:deep(.el-scrollbar__thumb) {
		background-color: rgba(255, 255, 255, 0.2);
	}

	:deep(.el-scrollbar__track) {
		background-color: rgba(0, 0, 0, 0.1);
	}
}
</style>
