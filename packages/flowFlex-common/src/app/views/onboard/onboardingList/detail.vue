<template>
	<div class="pb-6">
		<!-- 页面头部 -->
		<PageHeader
			:title="`${onboardingData?.caseCode || ''} - ${onboardingData?.caseName || ''}`"
			:show-back-button="true"
			@go-back="handleBack"
		>
			<template #description>
				<!-- 状态显示 -->
				<div class="flex items-center" v-if="onboardingData?.status">
					<GradientTag
						:type="statusTagType"
						:text="statusDisplayText"
						:pulse="statusShouldPulse"
						size="small"
					/>
				</div>
			</template>
			<template #actions>
				<el-button
					type="primary"
					@click="saveQuestionnaireAndField"
					:loading="saveAllLoading"
					:disabled="isSaveDisabled || stageCanCompleted || onboardingData?.isDisabled"
					:icon="Document"
					class="page-header-btn page-header-btn-primary"
					v-if="hasCasePermission(ProjectPermissionEnum.case.update) && !!activeStage"
				>
					Save
				</el-button>
				<el-tooltip
					v-if="hasCasePermission(ProjectPermissionEnum.case.update) && !!activeStage"
					:content="completeDisabledReason"
					:disabled="!completeDisabledReason"
					placement="top"
					effect="dark"
				>
					<el-button
						type="primary"
						@click="handleCompleteStage"
						:loading="completing"
						:disabled="
							isCompleteStageDisabled ||
							stageCanCompleted ||
							onboardingData?.isDisabled
						"
						class="page-header-btn page-header-btn-primary"
						:icon="Check"
					>
						Complete
					</el-button>
				</el-tooltip>
				<el-button
					@click="handleCustomerOverview"
					class="page-header-btn page-header-btn-secondary"
					:icon="View"
					v-if="functionPermission(ProjectPermissionEnum.case.read)"
				>
					Overview
				</el-button>
				<el-button
					@click="portalAccessDialogVisible = true"
					class="page-header-btn page-header-btn-secondary"
					:icon="User"
					v-if="hasCasePermission(ProjectPermissionEnum.case.update)"
				>
					Share
				</el-button>
			</template>
		</PageHeader>

		<!-- 主要内容区域 -->
		<div class="flex w-full gap-x-4">
			<!-- 左侧阶段详情 (2/3 宽度) -->
			<div class="flex-[2] min-w-0 overflow-hidden">
				<EditableStageHeader
					:current-stage="onboardingActiveStageInfo"
					:disabled="
						isAbortedReadonly ||
						onboardingStageStatus ||
						onboardingData?.isDisabled ||
						!hasCasePermission(ProjectPermissionEnum.case.update)
					"
					:onboardingId="onboardingId"
					@update:stage-data="handleStageDataUpdate"
				/>
				<el-scrollbar ref="leftScrollbarRef" class="h-full px-2 w-full">
					<div class="space-y-4 my-4">
						<!-- AI Summary 组件 -->
						<AISummary
							:show-a-i-summary-section="showAISummarySection"
							:loading="aiSummaryLoading"
							:loading-text="aiSummaryLoadingText"
							:current-a-i-summary="currentAISummary"
							:current-a-i-summary-generated-at="currentAISummaryGeneratedAt"
							@refresh="refreshAISummary"
						/>

						<!-- Stage Details 加载状态 -->
						<div
							v-if="stageDataLoading"
							class="bg-white dark:bg-black-300 rounded-xl p-8"
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
									:disabled="
										isAbortedReadonly ||
										stageCanCompleted ||
										onboardingData?.isDisabled ||
										!hasCasePermission(ProjectPermissionEnum.case.update)
									"
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
									:disabled="
										isAbortedReadonly ||
										stageCanCompleted ||
										onboardingData?.isDisabled ||
										!hasCasePermission(ProjectPermissionEnum.case.update)
									"
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
									:disabled="
										isAbortedReadonly ||
										stageCanCompleted ||
										onboardingData?.isDisabled ||
										!hasCasePermission(ProjectPermissionEnum.case.update)
									"
									:questionnaire-data="
										getQuestionnaireDataForComponent(component)
									"
									:currentstageCanCompleted="!!stageCanCompleted"
									:onboardingId="onboardingId"
									@stage-updated="handleStageUpdated"
									:loading="questionnaireLoading"
									@question-submitted="handleQuestionSubmitted"
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
									:document-is-required="documentIsRequired"
									:disabled="
										isAbortedReadonly ||
										stageCanCompleted ||
										onboardingData?.isDisabled ||
										!hasCasePermission(ProjectPermissionEnum.case.update)
									"
									:systemId="onboardingData?.systemId"
									:entityId="onboardingData?.entityId"
									@document-uploaded="handleDocumentUploaded"
									@document-deleted="handleDocumentDeleted"
								/>

								<QuickLink
									v-else-if="component.key === 'quickLink'"
									:component="component"
									:onboarding-id="onboardingId"
									:stage-id="activeStage"
								/>
							</div>
						</template>

						<!-- 兜底的StageDetails组件 -->
					</div>
				</el-scrollbar>
			</div>

			<!-- 右侧进度和笔记 (1/3 宽度) -->
			<div class="flex-1 flex-shrink-0 min-w-0">
				<el-scrollbar ref="rightScrollbarRef" class="h-full">
					<div class="space-y-4">
						<!-- OnboardingProgress组件 -->
						<OnboardingProgress
							v-if="onboardingData && onboardingId"
							:active-stage="activeStage"
							:onboarding-data="onboardingData"
							:workflow-stages="workflowStages"
							@set-active-stage="setActiveStage"
							@stage-completed="loadOnboardingDetail"
						/>

						<!-- 笔记区域 -->
						<InternalNotes
							v-if="activeStage && onboardingId"
							:onboarding-id="onboardingId"
							:stage-id="activeStage"
							:disabled="
								isAbortedReadonly ||
								onboardingData?.isDisabled ||
								!hasCasePermission(ProjectPermissionEnum.case.update)
							"
							@note-added="handleNoteAdded"
						/>
					</div>
				</el-scrollbar>
			</div>
		</div>

		<!-- 变更日志 -->
		<ChangeLog
			v-if="onboardingId"
			ref="changeLogRef"
			:onboarding-id="onboardingId"
			:stage-id="activeStage"
		/>

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
import { Loading, User, Document, Check, View } from '@element-plus/icons-vue';
import { getTokenobj } from '@/utils/auth';
import { getTimeZoneInfo } from '@/hooks/time';
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
	updateStageFields,
} from '@/apis/ow/onboarding';
import { OnboardingItem, SectionAnswer, Stage, StageComponentData } from '#/onboard';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { useI18n } from 'vue-i18n';
import { defaultStr } from '@/settings/projectSetting';
import { useUserStore } from '@/stores/modules/user';
// 导入组件
import PageHeader from '@/components/global/PageHeader/index.vue';
import GradientTag from '@/components/global/GradientTag/index.vue';
import OnboardingProgress from './components/OnboardingProgress.vue';
import QuestionnaireDetails from './components/QuestionnaireDetails.vue';
import InternalNotes from './components/InternalNotes.vue';
import ChangeLog from './components/ChangeLog.vue';
import CheckList from './components/CheckList.vue';
import Documents from './components/Documents.vue';
import StaticForm from './components/StaticForm.vue';
import PortalAccessContent from './components/PortalAccessContent.vue';
import AISummary from './components/AISummary.vue';
import EditableStageHeader from './components/EditableStageHeader.vue';
import QuickLink from './components/QuickLink.vue';
import { getAppCode } from '@/utils/threePartyLogin';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';
import { functionPermission } from '@/hooks';

const { t } = useI18n();
const userStore = useUserStore();
const globSetting = useGlobSetting();

// 常量定义
const router = useRouter();
const route = useRoute();

// 响应式数据
const onboardingData = ref<OnboardingItem | null>(null);
const activeStage = ref<string>(''); // 初始为空，等待从服务器获取当前阶段
const workflowStages = ref<Stage[]>([]);
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

// 状态显示映射
const statusTagType = computed(() => {
	const status = onboardingData.value?.status;
	if (!status) return 'default';

	switch (status) {
		case 'Inactive':
			return 'info';
		case 'Active':
		case 'InProgress':
		case 'Started':
			return 'primary';
		case 'Completed':
			return 'success';
		case 'Paused':
			return 'warning';
		case 'Aborted':
		case 'Cancelled':
			return 'danger';
		default:
			return 'info';
	}
});

const statusDisplayText = computed(() => {
	const status = onboardingData.value?.status;
	if (!status) return defaultStr;

	switch (status) {
		case 'Active':
		case 'Started':
			return 'In progress';
		case 'Cancelled':
			return 'Aborted';
		default:
			return status;
	}
});

const statusShouldPulse = computed(() => {
	const status = onboardingData.value?.status;
	return ['Active', 'InProgress', 'Started', 'Paused'].includes(status || '');
});

// 计算是否禁用保存按钮
const isSaveDisabled = computed(() => {
	const status = onboardingData.value?.status;
	if (!status) return false;

	// 对于已中止、已取消、暂停或强制完成的状态，禁用保存
	return ['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status);
});

// 计算是否禁用完成阶段按钮
const isCompleteStageDisabled = computed(() => {
	// 检查当前阶段之前是否有未完成的必填阶段
	const currentStageIndex = workflowStages.value.findIndex(
		(stage) => stage.stageId === activeStage.value
	);
	if (currentStageIndex > 0) {
		const previousStages = workflowStages.value.slice(0, currentStageIndex);
		const hasIncompleteRequiredStage = previousStages.some(
			(stage) => stage.required && !stage.isCompleted && stage.status !== 'Skipped'
		);
		if (hasIncompleteRequiredStage) {
			return true;
		}
	}

	const status = onboardingData.value?.status;
	if (!status) return false;

	// 对于已中止、已取消或暂停的状态，禁用完成阶段
	if (['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status)) {
		return true;
	}

	return false;
});

// 获取 Complete 按钮禁用的原因提示
const completeDisabledReason = computed(() => {
	if (onboardingData.value?.isDisabled) {
		return 'This case is disabled';
	}

	if (stageCanCompleted.value) {
		return 'This stage has already been completed';
	}

	// 检查前置必填阶段
	const currentStageIndex = workflowStages.value.findIndex(
		(stage) => stage.stageId === activeStage.value
	);
	if (currentStageIndex > 0) {
		const previousStages = workflowStages.value.slice(0, currentStageIndex);
		const hasIncompleteRequiredStage = previousStages.some(
			(stage) => stage.required && !stage.isCompleted && stage.status !== 'Skipped'
		);
		if (hasIncompleteRequiredStage) {
			return 'There are incomplete required stages. Please complete them first.';
		}
	}

	const status = onboardingData.value?.status;
	if (status && ['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status)) {
		return `Cannot complete stage when case status is ${status}`;
	}

	return '';
});

// 计算是否因为Aborted状态而禁用组件（类似于Viewable only逻辑）
const isAbortedReadonly = computed(() => {
	const status = onboardingData.value?.status;
	return !!status && ['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status);
});

const stageCanCompleted = computed(() => {
	const currentStage = workflowStages.value.find((stage) => stage.stageId === activeStage.value);
	return currentStage?.isCompleted;
});

const documentIsRequired = computed(() => {
	const currentStage = workflowStages.value.find((stage) => stage.stageId === activeStage.value);
	return currentStage?.attachmentManagementNeeded;
});

const onboardingStageStatus = computed(() => {
	const onboardingActiveStage = workflowStages.value.find(
		(stage) => stage?.stageId === activeStage?.value
	);
	return !!onboardingActiveStage?.completedBy;
});

// 添加组件引用
const questionnaireDetailsRefs = ref<any[]>([]);
const staticFormRefs = ref<any[]>([]);
const onboardingActiveStageInfo = ref<Stage | null>(null);
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
const getChecklistDataForComponent = (component: StageComponentData) => {
	if (!component.checklistIds || component.checklistIds.length === 0) {
		return [];
	}
	return checklistsData.value.filter((checklist) =>
		component.checklistIds.includes(checklist.id)
	);
};

// 辅助函数：根据组件的questionnaireIds获取对应的questionnaire数据
const getQuestionnaireDataForComponent = (component: StageComponentData) => {
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
const getQuestionnaireAnswersForComponent = (component: StageComponentData) => {
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
const processOnboardingData = (responseData: OnboardingItem) => {
	onboardingData.value = responseData;

	workflowStages.value =
		(responseData.stagesProgress.filter(
			(stage) => stage?.permission?.canView
		) as any as Stage[]) || [];

	// 根据 workflowStages 返回第一个未完成的 stageId
	// 首先按 order 排序，然后找到第一个未完成的阶段
	const sortedStages = [...workflowStages.value].sort((a, b) => (a.order || 0) - (b.order || 0));
	const firstIncompleteStage = sortedStages.find(
		(stage) => !stage.isCompleted && stage.status != 'Skipped'
	);

	// 如果所有阶段都完成了，返回最后一个阶段
	const newStageId =
		responseData.currentStageId ||
		firstIncompleteStage?.stageId ||
		sortedStages[sortedStages.length - 1]?.stageId;

	onboardingActiveStageInfo.value =
		workflowStages.value.find((stage) => stage.stageId === newStageId) || null;

	// 更新AI Summary显示
	updateAISummaryFromStageInfo();

	return newStageId;
};

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
			// 将新加载的答案合并到现有的 map 中，保留其他 stage 的答案
			Object.assign(questionnaireAnswersMap.value, map);
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

// 检查是否有权限（功能权限 && 数据权限）
const hasCasePermission = (functionalPermission: string) => {
	// if (onboardingData.value && onboardingData.value.permission) {
	// 	return (
	// 		functionPermission(functionalPermission) && onboardingData.value.permission.canOperate
	// 	);
	// }
	const currentStage = workflowStages.value?.find((stage) => stage.stageId === activeStage.value);
	return (
		functionPermission(functionalPermission) &&
		!!currentStage?.permission?.canOperate &&
		!!onboardingData.value?.permission?.canOperate
	);
};

// 事件处理函数
const handleBack = () => {
	router.push({
		path: '/onboard/onboardList',
	});
};

const handleCustomerOverview = () => {
	// 跳转到客户概览页面，传递 leadId 参数
	router.push({
		name: 'CustomerOverview',
		params: {
			leadId: onboardingId.value,
		},
		query: {
			companyName: onboardingData.value?.caseName || '',
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
			nextTick(() => {
				staticFormRefs.value.forEach((formRef) => {
					formRef.setFieldValues(response.data);
				});
			});
		} else {
			response.msg && ElMessage.error(response.msg);
		}
	} catch {
		//deep
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
	}

	// 更新activeStage
	activeStage.value = stageId;
	onboardingActiveStageInfo.value =
		workflowStages.value.find((stage) => stage.stageId === stageId) || null;

	// 更新AI Summary显示
	updateAISummaryFromStageInfo();

	// 重新加载依赖stageId的数据
	await loadStageRelatedData(stageId);
	await loadStaticFieldValues(); // 添加加载字段值的调用

	refreshChangeLog();
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
					if (task.actionId) {
						loadOnboardingDetail();
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
			confirmButtonText: 'Complete',
			cancelButtonText: 'Cancel',
			distinguishCancelAndClose: true,
			showCancelButton: true,
			showConfirmButton: true,
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					// 显示loading状态
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = 'Complete';

					completing.value = true;
					try {
						const res = await saveAllForm();
						if (!res) {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Complete';
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
						instance.confirmButtonText = 'Complete';
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

// 处理阶段数据更新
const handleStageDataUpdate = async (updateData: {
	stageId: string;
	customEstimatedDays: number;
	customEndTime: string;
	assignee: string[];
	coAssignees: string[];
}) => {
	try {
		// 这里应该调用API来更新阶段数据
		// 暂时先更新本地数据，实际项目中需要调用相应的API
		const res = await updateStageFields(onboardingId.value, updateData);
		if (res.code === '200') {
			ElMessage.success('Stage data updated successfully');
			loadOnboardingDetail();
		} else {
			ElMessage.error(res.msg || 'Failed to update stage data');
		}
	} catch (error) {
		console.error('Error updating stage data:', error);
		ElMessage.error('Failed to update stage data');
	}
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
	}

	// 创建新的AbortController
	aiSummaryAbortController = new AbortController();
	const currentStageId = activeStage.value; // 保存当前阶段ID，用于验证

	// 重置状态，开始流式生成
	aiSummaryLoading.value = true;
	aiSummaryLoadingText.value = 'Starting AI summary generation...';
	currentAISummary.value = ''; // 清空现有内容，准备流式显示

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
		headers['X-App-Code'] = getAppCode();
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
				aiSummaryLoading.value = false;
				return;
			}

			const chunk = decoder.decode(value, { stream: true });

			// 检查是否是错误信息
			if (chunk.startsWith('Error:')) {
				aiSummaryLoading.value = false;
				return;
			}

			// 直接将文本内容添加到AI Summary中
			if (chunk.trim()) {
				currentAISummary.value += chunk;
			}
		}

		// 最终验证阶段是否仍然是开始时的阶段
		if (activeStage.value !== currentStageId) {
			aiSummaryLoading.value = false;
			return;
		}

		// 流结束，设置状态
		currentAISummaryGeneratedAt.value = new Date().toISOString();
		aiSummaryLoading.value = false;
		//ElMessage.success('AI Summary generated successfully');

		// 更新本地stage信息 - 再次验证阶段
		if (onboardingActiveStageInfo.value && activeStage.value === currentStageId) {
			onboardingActiveStageInfo.value.aiSummary = currentAISummary.value;
			onboardingActiveStageInfo.value.aiSummaryGeneratedAt =
				currentAISummaryGeneratedAt.value;
		}
	} catch (error: any) {
		// 检查是否是用户取消的请求
		if (error.name === 'AbortError') {
			aiSummaryLoading.value = false;
			return;
		}

		aiSummaryLoading.value = false;
		// 不显示错误弹窗，只记录日志
	} finally {
		// 清理AbortController引用
		aiSummaryAbortController = null;
	}
};

const checkAndGenerateAISummary = () => {
	// 检查当前阶段是否有AI Summary，如果没有则自动生成
	// 只有在stagesProgress中确实没有aiSummary时才自动生成
	if (
		!onboardingActiveStageInfo.value?.aiSummary &&
		!aiSummaryLoading.value &&
		onboardingActiveStageInfo.value &&
		activeStage.value
	) {
		refreshAISummary();
	}
};

const questionnaireLoading = ref(false);
const handleQuestionSubmitted = async (
	onboardingId: string,
	stageId: string,
	questionnaireId: string
) => {
	try {
		questionnaireLoading.value = true;
		// 重新获取问卷答案数据
		await refreshQuestionnaireAnswers(onboardingId, stageId, questionnaireId);
	} finally {
		questionnaireLoading.value = false;
	}
};

// 重新获取问卷答案数据（仅答案，不刷新结构）
const refreshQuestionnaireAnswers = async (
	onboardingId: string,
	stageId: string,
	questionnaireId?: string
) => {
	const answerRes = await getQuestionnaireAnswer(onboardingId, stageId);

	if (answerRes.code === '200' && answerRes.data && Array.isArray(answerRes.data)) {
		const map: SectionAnswer[] = [];
		answerRes.data.forEach((item: any) => {
			if (questionnaireId && item.questionnaireId === questionnaireId && item.answerJson) {
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
		// 将新获取的答案合并到现有的 map 中，而不是完全替换
		Object.assign(questionnaireAnswersMap.value, map);
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
</style>
