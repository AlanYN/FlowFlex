<template>
	<div class="min-h-screen bg-siderbarGray dark:bg-black">
		<!-- Mobile sidebar -->
		<div :class="['fixed inset-0 z-50 lg:hidden', sidebarOpen ? 'block' : 'hidden']">
			<div
				class="fixed inset-0 bg-siderbarGray dark:bg-black"
				@click="sidebarOpen = false"
			></div>
			<div class="fixed inset-y-0 left-0 flex w-64 flex-col bg-siderbarGray dark:bg-black">
				<div class="flex h-16 items-center justify-between px-4 border-b">
					<h1 class="text-xl font-bold text-primary">Customer Portal</h1>
					<button @click="sidebarOpen = false" class="p-1 rounded-xl portal-btn-hover">
						<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M6 18L18 6M6 6l12 12"
							/>
						</svg>
					</button>
				</div>
				<nav class="flex-1 space-y-1 px-2 py-4">
					<div
						v-for="item in navigation"
						:key="item.name"
						:class="[
							'group flex items-center px-2 py-2 text-sm font-medium rounded-xl cursor-pointer portal-nav-item',
							currentView === item.view ? 'portal-nav-active' : '',
						]"
						@click="handleNavigation(item)"
					>
						<component :is="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-xl border bg-siderbarGray dark:bg-black p-4 shadow-sm">
						<div class="flex items-center space-x-3">
							<div class="portal-company-icon">
								<svg
									class="h-5 w-5"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
									/>
								</svg>
							</div>
							<div class="flex-1 min-w-0">
								<p class="text-sm font-medium portal-text-primary truncate">
									{{ customerData.companyName }}
								</p>
								<p class="text-xs portal-text-secondary truncate">
									{{ customerData.contactName }}
								</p>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- Desktop sidebar -->
		<div class="hidden lg:fixed lg:inset-y-0 lg:flex lg:w-64 lg:flex-col">
			<div
				class="flex flex-col flex-grow bg-siderbarGray dark:bg-black portal-sidebar-border"
			>
				<div class="flex h-16 items-center px-4 border-b">
					<h1 class="text-xl font-bold text-primary">Customer Portal</h1>
				</div>
				<nav class="flex-1 space-y-1 px-2 py-4">
					<div
						v-for="item in navigation"
						:key="item.name"
						:class="[
							'group flex items-center px-2 py-2 text-sm font-medium rounded-xl cursor-pointer portal-nav-item',
							currentView === item.view ? 'portal-nav-active' : '',
						]"
						@click="handleNavigation(item)"
					>
						<component :is="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-xl border bg-siderbarGray dark:bg-black p-4 shadow-sm">
						<div class="flex items-center space-x-3 mb-3">
							<div class="portal-company-icon">
								<svg
									class="h-5 w-5"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
									/>
								</svg>
							</div>
							<div class="flex-1 min-w-0">
								<p class="text-sm font-medium portal-text-primary truncate">
									{{ customerData.companyName }}
								</p>
								<p class="text-xs portal-text-secondary truncate">
									{{ customerData.contactName }}
								</p>
							</div>
						</div>
						<div class="space-y-1">
							<div class="flex items-center text-xs portal-text-secondary">
								<svg
									class="h-3 w-3 mr-1"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
									/>
								</svg>
								Account Manager: {{ customerData.accountManager }}
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- Main content -->
		<div class="lg:pl-64">
			<!-- Mobile header -->
			<div class="flex h-16 items-center justify-between border-b bg-white px-4 lg:hidden">
				<button @click="sidebarOpen = true" class="p-1 rounded-xl portal-btn-hover">
					<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M4 6h16M4 12h16M4 18h16"
						/>
					</svg>
				</button>
				<h1 class="text-lg font-semibold">Customer Portal</h1>
				<div></div>
			</div>

			<!-- Page content -->
			<main class="flex-1 p-6" style="background: var(--el-bg-color-page)">
				<!-- Onboarding Detail View -->
				<div class="pb-6">
					<!-- 统一页面头部 -->
					<PageHeader
						:title="`${onboardingData?.leadId || ''} ${onboardingData?.leadName || ''}`"
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
								:disabled="isSaveDisabled || stageCanCompleted"
								:icon="Document"
								class="page-header-btn page-header-btn-primary"
							>
								Save
							</el-button>
							<el-button
								type="primary"
								@click="handleCompleteStage"
								:loading="completing"
								:disabled="isCompleteStageDisabled || stageCanCompleted"
								:icon="Check"
								class="page-header-btn page-header-btn-primary"
							>
								Complete Stage
							</el-button>
						</template>
					</PageHeader>

					<!-- 主要内容区域 -->
					<div class="flex w-full gap-x-4">
						<!-- 左侧阶段详情 (2/3 宽度) -->
						<div class="flex-[2] min-w-0 overflow-hidden">
							<div
								class="rounded-xl el-card is-always-shadow rounded-xl el-card__header"
							>
								<div
									class="editable-header-card text-white -mx-5 -my-5 px-5 py-4 rounded-t-lg"
								>
									<h2 class="text-lg font-semibold">{{ currentStageTitle }}</h2>
								</div>
							</div>
							<el-scrollbar ref="leftScrollbarRef" class="h-full px-2 w-full">
								<div class="space-y-4 my-4">
									<!-- AI Summary 组件 -->
									<AISummary
										:show-a-i-summary-section="showAISummarySection"
										:loading="aiSummaryLoading"
										:loading-text="aiSummaryLoadingText"
										:current-a-i-summary="currentAISummary"
										:current-a-i-summary-generated-at="
											currentAISummaryGeneratedAt
										"
										@refresh="refreshAISummary"
									/>
									<!-- Stage Details 加载状态 -->
									<div
										v-if="stageDataLoading"
										class="bg-white dark:bg-black-300 rounded-xl p-8"
									>
										<div
											class="flex flex-col items-center justify-center space-y-4"
										>
											<el-icon class="is-loading text-4xl text-primary-500">
												<Loading />
											</el-icon>
											<p class="portal-loading-text">
												Loading stage details...
											</p>
										</div>
									</div>

									<!-- 根据Stage Components动态渲染 -->
									<template
										v-if="
											!stageDataLoading &&
											onboardingActiveStageInfo?.components
										"
									>
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
													(onboardingActiveStageInfo.visibleInPortal &&
														stagePortalPermission) ||
													component.customerPortalAccess ===
														StageComponentPortal.Viewable
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
												:checklist-data="
													getChecklistDataForComponent(component)
												"
												:onboarding-id="onboardingId"
												:disabled="
													isAbortedReadonly ||
													(onboardingActiveStageInfo.visibleInPortal &&
														stagePortalPermission) ||
													component.customerPortalAccess ===
														StageComponentPortal.Viewable
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
													(onboardingActiveStageInfo.visibleInPortal &&
														stagePortalPermission) ||
													component.customerPortalAccess ===
														StageComponentPortal.Viewable
												"
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
												:disabled="
													isAbortedReadonly ||
													(onboardingActiveStageInfo.visibleInPortal &&
														stagePortalPermission) ||
													component.customerPortalAccess ===
														StageComponentPortal.Viewable
												"
												@document-uploaded="handleDocumentUploaded"
												@document-deleted="handleDocumentDeleted"
											/>
										</div>
									</template>
								</div>
							</el-scrollbar>
						</div>

						<!-- 右侧进度和笔记 (1/3 宽度) -->
						<div class="flex-1">
							<el-scrollbar ref="rightScrollbarRef" class="h-full">
								<div class="space-y-4">
									<!-- OnboardingProgress组件 -->
									<OnboardingProgress
										v-if="onboardingData && onboardingId"
										:active-stage="activeStage"
										:onboarding-data="onboardingData"
										:workflow-stages="workflowStages"
										@set-active-stage="setActiveStageWithData"
										@stage-completed="loadOnboardingDetail"
									/>
								</div>
							</el-scrollbar>
						</div>
					</div>

					<!-- 编辑对话框 -->
					<el-dialog
						v-model="editDialogVisible"
						title="Edit Case"
						width="500px"
						:before-close="handleEditDialogClose"
					>
						<el-form :model="editForm" label-width="100px" @submit.prevent>
							<el-form-item label="Priority">
								<el-select
									v-model="editForm.priority"
									placeholder="Select Priority"
									class="w-full"
								>
									<el-option label="High" value="High" />
									<el-option label="Medium" value="Medium" />
									<el-option label="Low" value="Low" />
								</el-select>
							</el-form-item>
							<el-form-item label="Assignee">
								<el-input
									v-model="editForm.assignee"
									placeholder="Enter assignee name"
								/>
							</el-form-item>
							<el-form-item label="Notes">
								<el-input
									v-model="editForm.notes"
									type="textarea"
									:rows="3"
									placeholder="Enter notes"
								/>
							</el-form-item>
						</el-form>

						<template #footer>
							<div class="flex justify-end space-x-2">
								<el-button @click="editDialogVisible = false">Cancel</el-button>
								<el-button type="primary" @click="handleSaveEdit" :loading="saving">
									Save
								</el-button>
							</div>
						</template>
					</el-dialog>
				</div>
			</main>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick, watch, onBeforeUpdate } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Loading, Check, Document } from '@element-plus/icons-vue';
import {
	getOnboardingByLead,
	getStaticFieldValuesByOnboarding,
	saveCheckListTask,
	getCheckListIds,
	getCheckListIsCompleted,
	getQuestionIds,
	getQuestionnaireAnswer,
	completeCurrentStage,
	getOnboardingFilesByStage,
	onboardingSave,
} from '@/apis/ow/onboarding';
import { OnboardingItem, StageInfo, ComponentData, SectionAnswer } from '#/onboard';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { useI18n } from 'vue-i18n';
import { defaultStr } from '@/settings/projectSetting';
import { getTokenobj } from '@/utils/auth';
import { getTimeZoneInfo } from '@/hooks/time';
import { useGlobSetting } from '@/settings';
import { useUserStore } from '@/stores/modules/user';
// 导入组件
import OnboardingProgress from '../onboardingList/components/OnboardingProgress.vue';
import QuestionnaireDetails from '../onboardingList/components/QuestionnaireDetails.vue';
import CheckList from '../onboardingList/components/CheckList.vue';
import Documents from '../onboardingList/components/Documents.vue';
import StaticForm from '../onboardingList/components/StaticForm.vue';
import AISummary from '../onboardingList/components/AISummary.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { StageComponentPortal } from '@/enums/appEnum';
import GradientTag from '@/components/global/GradientTag/index.vue';
import { PortalPermissionEnum } from '@/enums/portalPermissionEnum';
import { getAppCode } from '@/utils/threePartyLogin';

// 图标组件
const HomeIcon = {
	template: `
		<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
		</svg>
	`,
};

const DetailsIcon = {
	template: `
		<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
		</svg>
	`,
};

const { t } = useI18n();
const userStore = useUserStore();
const globSetting = useGlobSetting();

// 常量定义
const router = useRouter();
const route = useRoute();

// 响应式数据
const onboardingData = ref<OnboardingItem | null>(null);
const activeStage = ref<string>('');
const workflowStages = ref<any[]>([]);
const editDialogVisible = ref(false);
const saving = ref(false);

// 侧边栏相关数据
const sidebarOpen = ref(false);
const currentView = ref('onboarding');

// 客户数据
const customerData = computed(() => {
	if (!onboardingData.value) {
		return {
			id: 'CUST-001',
			companyName: 'Loading...',
			contactName: 'Loading...',
			email: '',
			phone: '',
			accountManager: '',
		};
	}

	const data = onboardingData.value;
	return {
		id: data.leadId,
		companyName: data.leadName,
		contactName: data.contactPerson,
		email: data.contactEmail,
		phone: '',
		accountManager: data.createBy || 'N/A',
	};
});

// 导航菜单
const navigation = ref([
	{
		name: 'Case Progress',
		view: 'progress',
		icon: HomeIcon,
	},
	{
		name: 'Case Detail',
		view: 'onboarding',
		icon: DetailsIcon,
	},
	// {
	// 	name: 'Message Center',
	// 	view: 'messages',
	// 	icon: MessageSquareIcon,
	// },
	// {
	// 	name: 'Document Center',
	// 	view: 'documents',
	// 	icon: FileTextIcon,
	// },
	// {
	// 	name: 'Contact Us',
	// 	view: 'contact',
	// 	icon: PhoneIcon,
	// },
]);

// 存储批量查询到的数据
const checklistsData = ref<any[]>([]);
const questionnairesData = ref<any[]>([]);
const questionnaireAnswersMap = ref<Record<string, SectionAnswer>>({});

// Loading状态管理
const stageDataLoading = ref(false);
const initialLoading = ref(true);

// 使用自适应滚动条 hook
const { scrollbarRef: leftScrollbarRef } = useAdaptiveScrollbar(100);
const { scrollbarRef: rightScrollbarRef } = useAdaptiveScrollbar(100);

// 编辑表单
const editForm = reactive({
	priority: '',
	assignee: '',
	notes: '',
});

// 计算属性
const onboardingId = computed(() => {
	const id = route.query.onboardingId;
	if (!id || typeof id !== 'string') {
		console.error('Invalid onboarding ID from route:', id);
		return '';
	}
	return id;
});

const stageIdFromRoute = computed(() => {
	const id = route.query.stageId;
	if (id && typeof id === 'string') {
		return id;
	}
	return '';
});

// 添加组件引用
const questionnaireDetailsRefs = ref<any[]>([]);
const staticFormRefs = ref<any[]>([]);
const documentsRef = ref<any[]>([]);
const onboardingActiveStageInfo = ref<StageInfo | null>(null);

// 在组件更新前重置 refs，避免多次渲染导致重复收集
onBeforeUpdate(() => {
	staticFormRefs.value = [];
	questionnaireDetailsRefs.value = [];
});

// 计算属性
const currentStageTitle = computed(() => {
	const currentStage = workflowStages.value.find((stage) => stage.stageId === activeStage.value);
	return currentStage?.stageName || defaultStr;
});

// 计算是否禁用保存按钮 - 与detail.vue保持一致
const isSaveDisabled = computed(() => {
	const status = onboardingData.value?.status;
	if (!status) return false;

	// 对于已中止、已取消、暂停或强制完成的状态，禁用保存
	return ['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status);
});

// 计算是否禁用完成阶段按钮 - 与detail.vue保持一致
const isCompleteStageDisabled = computed(() => {
	const status = onboardingData.value?.status;
	if (!status) return false;

	// 对于已中止、已取消或暂停的状态，禁用完成阶段
	return ['Aborted', 'Cancelled', 'Paused'].includes(status);
});

// 计算当前阶段是否已完成 - 与detail.vue保持一致
const stageCanCompleted = computed(() => {
	const currentStage = workflowStages.value.find((stage) => stage.stageId === activeStage.value);
	return currentStage?.isCompleted;
});

const stagePortalPermission = computed(() => {
	const status = onboardingData.value?.status;
	const currentStage = workflowStages.value.find((stage) => stage.stageId === activeStage.value);

	// 如果是Paused或Force Completed状态，直接禁用
	if (status && ['Paused', 'Force Completed'].includes(status)) {
		return true;
	}

	return currentStage?.portalPermission == PortalPermissionEnum.Viewable ||
		currentStage?.isCompleted
		? true
		: false;
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
		case 'Force Completed':
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
		case 'Force Completed':
			return 'Force Completed';
		default:
			return status;
	}
});

const statusShouldPulse = computed(() => {
	const status = onboardingData.value?.status;
	return ['Active', 'InProgress', 'Started', 'Paused'].includes(status || '');
});

// 计算是否因为Aborted状态而禁用组件（类似于Viewable only逻辑）
const isAbortedReadonly = computed(() => {
	const status = onboardingData.value?.status;
	return !!status && ['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status);
});

const sortedComponents = computed(() => {
	if (!onboardingActiveStageInfo.value?.components) {
		return [];
	}
	const validComponents = onboardingActiveStageInfo.value?.components.filter(
		(component) =>
			onboardingActiveStageInfo.value?.visibleInPortal &&
			(component?.customerPortalAccess == undefined ||
				(component?.customerPortalAccess != undefined &&
					component?.customerPortalAccess != StageComponentPortal.Hidden))
	);
	return [...validComponents].sort((a, b) => {
		return a.order - b.order;
	});
});

// 获取所有启用的组件（包括隐藏组件），用于隐藏组件校验
const allComponents = computed(() => {
	if (!onboardingActiveStageInfo.value?.components) {
		return [];
	}
	return [...onboardingActiveStageInfo.value.components]
		.filter((component) => component.isEnabled)
		.sort((a, b) => a.order - b.order);
});

// AI Summary相关状态
const aiSummaryLoading = ref(false);
const aiSummaryLoadingText = ref('Generating AI summary...');
const currentAISummary = ref('');
const currentAISummaryGeneratedAt = ref('');
const showAISummarySection = ref(true);
// 用于取消AI摘要请求的AbortController
let aiSummaryAbortController: AbortController | null = null;

const updateAISummaryFromStageInfo = () => {
	if (onboardingActiveStageInfo.value?.aiSummary) {
		currentAISummary.value = (onboardingActiveStageInfo.value as any).aiSummary;
		currentAISummaryGeneratedAt.value =
			(onboardingActiveStageInfo.value as any).aiSummaryGeneratedAt || '';
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
			(onboardingActiveStageInfo.value as any).aiSummary = currentAISummary.value;
			(onboardingActiveStageInfo.value as any).aiSummaryGeneratedAt =
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

// 隐藏组件校验函数
// 静态字段数据校验函数
const validateStaticFieldsData = async (
	component: any
): Promise<{ isValid: boolean; errors: string[] }> => {
	if (component.key !== 'fields' || !component.staticFields?.length) {
		return { isValid: true, errors: [] };
	}

	const errors: string[] = [];
	try {
		// 获取当前静态字段值
		const response = await getStaticFieldValuesByOnboarding(onboardingId.value);
		const fieldValues = response.code === '200' ? response.data : null;

		// 必填字段定义（复用StaticForm中的定义）
		const requiredFields = new Set([
			'leadId',
			'customerName',
			'priority',
			'requestedCreditLimit',
			'approvedCreditLimit',
			'salesApprovedCreditLimit',
			'salesApprovalNotes',
			'paymentTerm',
			'status',
			'accountHolderCategory',
			'assignee',
			'assigneeLocation',
		]);

		// 字段标签映射（用于友好的错误提示）
		const fieldLabelMap: Record<string, string> = {
			LEADID: 'Lead ID',
			CUSTOMERNAME: 'Customer Name',
			PRIORITY: 'Priority',
			REQUESTEDCREDITLIMIT: 'Requested Credit Limit',
			APPROVEDCREDITLIMIT: 'Approved Credit Limit',
			SALESAPPROVEDCREDITLIMIT: 'Sales Approved Credit Limit',
			SALESAPPROVALNOTES: 'Sales Approval Notes',
			PAYMENTTERM: 'Payment Term',
			STATUS: 'Status',
			ACCOUNTHOLDERCATEGORY: "Account Holder's Category",
			ASSIGNEE: 'Assignee',
			ASSIGNEELOCATION: "Assignee's Responsible Location",
		};

		// 表单字段到API字段的映射
		const formToApiFieldsMap: Record<string, string> = {
			leadId: 'LEADID',
			customerName: 'CUSTOMERNAME',
			priority: 'PRIORITY',
			requestedCreditLimit: 'REQUESTEDCREDITLIMIT',
			approvedCreditLimit: 'APPROVEDCREDITLIMIT',
			salesApprovedCreditLimit: 'SALESAPPROVEDCREDITLIMIT',
			salesApprovalNotes: 'SALESAPPROVALNOTES',
			paymentTerm: 'PAYMENTTERM',
			status: 'STATUS',
			accountHolderCategory: 'ACCOUNTHOLDERCATEGORY',
			assignee: 'ASSIGNEE',
			assigneeLocation: 'ASSIGNEELOCATION',
		};

		// 检查每个静态字段的必填项
		component.staticFields.forEach((apiFieldKey: string) => {
			// 找到对应的表单字段名
			const formFieldName = Object.keys(formToApiFieldsMap).find(
				(key) => formToApiFieldsMap[key] === apiFieldKey
			);

			if (formFieldName && requiredFields.has(formFieldName)) {
				// 检查字段值是否存在且不为空
				let fieldValue: any = null;
				if (Array.isArray(fieldValues)) {
					// 新格式：数组格式
					const field = fieldValues.find((f: any) => f.fieldName === apiFieldKey);
					if (field && field.fieldValueJson !== undefined) {
						try {
							// fieldValueJson 是JSON字符串，需要解析
							fieldValue = JSON.parse(field.fieldValueJson);
						} catch {
							// 解析失败时使用原值
							fieldValue = field.fieldValueJson;
						}
					}
				} else if (fieldValues && typeof fieldValues === 'object') {
					// 旧格式：对象格式
					fieldValue = fieldValues[apiFieldKey];
					// 如果是JSON字符串，尝试解析
					if (typeof fieldValue === 'string') {
						try {
							fieldValue = JSON.parse(fieldValue);
						} catch {
							// 保持原值
						}
					}
				}

				// 验证字段值 - 特别处理不同类型的字段
				let isEmpty = false;
				if (Array.isArray(fieldValue)) {
					// 数组类型字段（如assignee）
					isEmpty = fieldValue.length === 0;
				} else if (typeof fieldValue === 'string') {
					// 字符串类型字段
					isEmpty =
						fieldValue.trim() === '' ||
						fieldValue === 'null' ||
						fieldValue === 'undefined';
				} else {
					// 其他类型字段
					isEmpty = fieldValue === null || fieldValue === undefined;
				}

				if (isEmpty) {
					const fieldLabel = fieldLabelMap[apiFieldKey] || apiFieldKey;
					errors.push(`${fieldLabel} is required`);
				}
			}
		});

		return { isValid: errors.length === 0, errors };
	} catch (error) {
		console.error('Error validating static fields:', error);
		return { isValid: true, errors: [] }; // 出错时不阻止操作
	}
};

// 问卷数据校验函数
const validateQuestionnaireData = (component: any): { isValid: boolean; errors: string[] } => {
	if (component.key !== 'questionnaires' || !component.questionnaireIds?.length) {
		return { isValid: true, errors: [] };
	}

	const errors: string[] = [];

	// 遍历所有问卷进行校验
	component.questionnaireIds.forEach((questionnaireId: string) => {
		const questionnaire = questionnairesData.value.find((q) => q.id === questionnaireId);
		const answers = questionnaireAnswersMap.value[questionnaireId];

		if (!questionnaire) {
			console.warn(`Questionnaire not found: ${questionnaireId}`);
			return;
		}

		// 从structureJson解析问卷结构
		let structure: any = {};
		try {
			if (questionnaire.structureJson) {
				structure = JSON.parse(questionnaire.structureJson);
			}
		} catch (error) {
			console.error(
				`Error parsing structureJson for questionnaire ${questionnaireId}:`,
				error
			);
			return;
		}

		if (!structure?.sections || !Array.isArray(structure.sections)) {
			console.warn(`Questionnaire has no valid sections: ${questionnaireId}`);
			return;
		}

		structure.sections.forEach((section: any, sIndex: number) => {
			section.questions?.forEach((question: any, qIndex: number) => {
				if (!question.required) return;

				// 查找问题的答案
				const answerData = answers?.answer?.find(
					(ans: any) => ans.questionId === question.id
				);
				const value = answerData?.answer;

				// 根据问题类型检查答案是否为空
				let isEmpty = false;

				switch (question.type) {
					case 'short_answer':
					case 'paragraph':
						isEmpty = !value || (typeof value === 'string' && value.trim() === '');
						break;

					case 'multiple_choice':
						isEmpty = !value || value === '';
						break;

					case 'checkboxes':
						isEmpty = !Array.isArray(value) || value.length === 0;
						break;

					case 'number':
						isEmpty = value === null || value === undefined || value === '';
						break;

					case 'date':
					case 'time':
						isEmpty = value === null || value === undefined;
						break;

					case 'rating':
						isEmpty = !value || (typeof value === 'number' && value < 1);
						break;

					case 'linear_scale':
						isEmpty =
							value === null ||
							value === undefined ||
							(typeof value === 'number' && value <= (question.min || 0));
						break;

					case 'slider':
						isEmpty = value === null || value === undefined;
						break;

					case 'file':
					case 'file_upload':
						isEmpty = !Array.isArray(value) || value.length === 0;
						break;

					case 'multiple_choice_grid':
						// 多选网格：检查每一行是否都有选择
						if (question.rows && question.rows.length > 0) {
							let allRowsCompleted = true;
							question.rows.forEach((row: any, rowIndex: number) => {
								// 查找该行的答案
								const gridAnswerData = answers?.answer?.find(
									(ans: any) =>
										ans.questionId === `${question.id}_${row.id || rowIndex}`
								);
								const gridValue = gridAnswerData?.answer;
								if (!Array.isArray(gridValue) || gridValue.length === 0) {
									allRowsCompleted = false;
								}
							});
							isEmpty = !allRowsCompleted;
						} else {
							isEmpty = !Array.isArray(value) || value.length === 0;
						}
						break;

					case 'checkbox_grid':
						// 单选网格：检查每一行是否都有选择
						if (question.rows && question.rows.length > 0) {
							let allRowsCompleted = true;
							question.rows.forEach((row: any, rowIndex: number) => {
								// 查找该行的答案
								const gridAnswerData = answers?.answer?.find(
									(ans: any) =>
										ans.questionId === `${question.id}_${row.id || rowIndex}`
								);
								const gridValue = gridAnswerData?.answer;
								if (!gridValue || gridValue === '') {
									allRowsCompleted = false;
								}
							});
							isEmpty = !allRowsCompleted;
						}
						break;

					case 'short_answer_grid':
						// 短答案网格：检查每一行是否至少有一个单元格有内容
						if (question.rows && question.columns && question.columns.length > 0) {
							let allRowsCompleted = true;
							question.rows.forEach((row: any, rowIndex: number) => {
								let rowHasValue = false;
								question.columns.forEach((column: any, columnIndex: number) => {
									// 查找该单元格的答案
									const cellAnswerData = answers?.answer?.find(
										(ans: any) =>
											ans.questionId ===
											`${question.id}_${column.id}_${row.id}`
									);
									const cellValue = cellAnswerData?.answer;
									if (
										cellValue &&
										typeof cellValue === 'string' &&
										cellValue.trim() !== ''
									) {
										rowHasValue = true;
									}
								});
								if (!rowHasValue) {
									allRowsCompleted = false;
								}
							});
							isEmpty = !allRowsCompleted;
						}
						break;

					case 'divider':
					case 'description':
					case 'image':
					case 'video':
						// 这些类型不需要校验
						isEmpty = false;
						break;

					default:
						// 其他类型的通用校验
						isEmpty =
							value === null ||
							value === undefined ||
							value === '' ||
							(typeof value === 'string' && value.trim() === '') ||
							(Array.isArray(value) && value.length === 0);
				}

				if (isEmpty) {
					const questionnaireTitle =
						questionnaire.name || structure.name || `Questionnaire ${questionnaireId}`;
					const sectionTitle = section.name || section.title || `Section ${sIndex + 1}`;
					const questionTitle =
						question.title || question.question || `Question ${qIndex + 1}`;
					errors.push(`${questionnaireTitle} - ${sectionTitle} - ${questionTitle}`);
				}
			});
		});
	});

	return { isValid: errors.length === 0, errors };
};

// 检查清单数据校验函数
const validateChecklistData = (component: any): { isValid: boolean; errors: string[] } => {
	if (component.key !== 'checklist' || !component.checklistIds?.length) {
		return { isValid: true, errors: [] };
	}

	const errors: string[] = [];

	component.checklistIds.forEach((checklistId: string) => {
		const checklist = checklistsData.value.find((c) => c.id === checklistId);
		if (!checklist?.tasks) return;

		// 查找必填且未完成的任务
		const incompleteRequiredTasks = checklist.tasks.filter(
			(task: any) => task.isRequired !== false && !task.isCompleted
		);

		if (incompleteRequiredTasks.length > 0) {
			const taskNames = incompleteRequiredTasks
				.map((task: any) => task.name || `Task ${task.id}`)
				.join(', ');
			errors.push(
				`${checklist.name}: ${incompleteRequiredTasks.length} required tasks not completed (${taskNames})`
			);
		}
	});

	return { isValid: errors.length === 0, errors };
};

// 文件组件数据校验函数
const validateDocumentsData = async (
	component: any
): Promise<{ isValid: boolean; errors: string[] }> => {
	if (component.key !== 'files') {
		return { isValid: true, errors: [] };
	}

	try {
		// 对于隐藏的文件组件，需要调用接口获取文件列表
		// 因为隐藏组件不会渲染Documents组件，无法自动获取文件数据
		const response = await getOnboardingFilesByStage(
			onboardingId.value,
			onboardingActiveStageInfo.value?.stageId || ''
		);
		const documents = response.code === '200' ? response.data || [] : [];

		// 复用Documents.vue中的vailComponent逻辑
		if (component.isEnabled && documents.length <= 0) {
			return {
				isValid: false,
				errors: ['At least one document is required'],
			};
		}

		return { isValid: true, errors: [] };
	} catch (error) {
		console.error('Error validating documents:', error);
		// 出现错误时返回通过状态，不阻止其他校验
		return { isValid: true, errors: [] };
	}
};

// 隐藏组件校验错误接口
interface HiddenValidationError {
	componentType: string;
	componentName: string;
	errors: string[];
}

// 隐藏组件校验主函数
const validateHiddenComponents = async (): Promise<{
	isValid: boolean;
	hiddenValidationErrors: HiddenValidationError[];
}> => {
	// 筛选出被隐藏的组件
	const hiddenComponents = allComponents.value.filter(
		(component) => component.customerPortalAccess === StageComponentPortal.Hidden
	);

	if (hiddenComponents.length === 0) {
		return { isValid: true, hiddenValidationErrors: [] };
	}

	const allErrors: HiddenValidationError[] = [];

	// 遍历所有隐藏组件进行校验
	for (const component of hiddenComponents) {
		let validationResult: { isValid: boolean; errors: string[] } = {
			isValid: true,
			errors: [],
		};
		let componentTypeName = '';

		switch (component.key) {
			case 'fields':
				validationResult = await validateStaticFieldsData(component);
				componentTypeName = 'Static Fields';
				break;
			case 'questionnaires':
				validationResult = validateQuestionnaireData(component);
				componentTypeName = 'Questionnaire';
				break;
			case 'checklist':
				validationResult = validateChecklistData(component);
				componentTypeName = 'Checklist';
				break;
			case 'files':
				validationResult = await validateDocumentsData(component);
				componentTypeName = 'Documents';
				break;
			default:
				// 未知组件类型，跳过校验
				continue;
		}

		if (!validationResult.isValid && validationResult.errors.length > 0) {
			allErrors.push({
				componentType: componentTypeName,
				componentName: component.name || componentTypeName,
				errors: validationResult.errors,
			});
		}
	}

	return {
		isValid: allErrors.length === 0,
		hiddenValidationErrors: allErrors,
	};
};

// 友好错误提示函数
const showHiddenComponentErrors = (errors: HiddenValidationError[]) => {
	if (errors.length === 0) return;

	const errorGroups = '';
	// errors
	// 	.map(
	// 		(error) =>
	// 			`<div class="mb-2">
	//     <strong class="text-red-600">${error.componentType}:</strong>
	//     <ul class="ml-4 mt-1">
	//       ${error.errors.map((err) => `<li>• ${err}</li>`).join('')}
	//     </ul>
	//   </div>`
	// 	)
	// 	.join('');

	const fullMessage = `
    <div class="text-sm">
      <p class="mb-3" style="color: var(--el-text-color-regular);">
        Some hidden required fields need to be completed by administrators before this stage can be finished:
      </p>
      ${errorGroups}
      <p class="mt-3 text-xs" style="color: var(--el-text-color-secondary);">
        Please contact your administrator to complete these required items.
      </p>
    </div>
  `;

	ElMessageBox.alert(fullMessage, 'Hidden Required Fields Not Completed', {
		type: 'warning',
		dangerouslyUseHTMLString: true,
		customClass: 'hidden-validation-notification',
		confirmButtonText: 'Confirm',
		showCancelButton: false,
		closeOnClickModal: false,
		closeOnPressEscape: true,
	});
};

// 事件处理函数
const handleNavigation = (item: any) => {
	if (item.view === 'progress') {
		router.push({
			path: '/customer-portal',
			query: {
				onboardingId: onboardingId.value,
			},
		});
	} else {
		currentView.value = item.view;
		sidebarOpen.value = false;
	}
};

const handleBack = () => {
	router.push({
		path: '/customer-portal',
		query: {
			onboardingId: onboardingId.value,
		},
	});
};

const completing = ref(false);
const checkLoading = ref(false);

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

// 其他必要的函数（简化版本）
const processOnboardingData = (responseData: any) => {
	onboardingData.value = responseData;
	// 只显示在Portal中可见的阶段
	workflowStages.value = responseData.stagesProgress.filter(
		(stage: any) => stage.visibleInPortal !== false
	);

	let newStageId = '';

	// 优先使用路由中的 stageId
	if (stageIdFromRoute.value) {
		// 验证路由中的 stageId 是否存在于工作流阶段中
		const foundStage = workflowStages.value.find(
			(stage) => stage.stageId === stageIdFromRoute.value
		);
		if (foundStage) {
			newStageId = stageIdFromRoute.value;
		}
	}

	// 如果路由中没有 stageId 或者无效，则使用默认逻辑
	if (!newStageId) {
		const sortedStages = [...workflowStages.value].sort(
			(a, b) => (a.order || 0) - (b.order || 0)
		);
		const firstIncompleteStage = sortedStages.find((stage) => !stage.isCompleted);
		newStageId =
			firstIncompleteStage?.stageId || sortedStages[sortedStages.length - 1]?.stageId;
	}

	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === newStageId
	);

	// 更新AI Summary显示
	updateAISummaryFromStageInfo();

	return newStageId;
};

const loadOnboardingDetail = async () => {
	if (!onboardingId.value) {
		ElMessage.error('Invalid onboarding ID');
		return;
	}

	try {
		const response = await getOnboardingByLead(onboardingId.value, true);
		if (response.code === '200') {
			const newStageId = processOnboardingData(response.data);
			if (newStageId) {
				activeStage.value = newStageId;
				// 设置 activeStage 后，加载当前阶段的基础数据
				await loadCurrentStageData();
				await checkAndGenerateAISummary();
			}
		}
	} finally {
		initialLoading.value = false;
		refreshChangeLog();
	}
};

// 其他函数的简化版本
const getChecklistDataForComponent = (component: ComponentData) => {
	if (!component.checklistIds || component.checklistIds.length === 0) {
		return [];
	}
	return checklistsData.value.filter((checklist) =>
		component.checklistIds.includes(checklist.id)
	);
};

const getQuestionnaireDataForComponent = (component: ComponentData) => {
	if (!component.questionnaireIds || component.questionnaireIds.length === 0) {
		return null;
	}
	for (const questionnaire of questionnairesData.value) {
		if (component.questionnaireIds.includes(questionnaire.id)) {
			return questionnaire;
		}
	}
	return null;
};

const getQuestionnaireAnswersForComponent = (component: ComponentData) => {
	if (!component.questionnaireIds || component.questionnaireIds.length === 0) {
		return undefined;
	}
	const qId = component.questionnaireIds[0];
	return questionnaireAnswersMap.value[qId];
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
			// 获取已完成的任务信息，包含完成者与完成时间
			const completedTasksMap = new Map<string, any>();
			if (completionResponse.code === '200' && completionResponse.data) {
				if (Array.isArray(completionResponse.data)) {
					completionResponse.data.forEach((completedTask: any) => {
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

			// 处理每个 checklist 的数据，合并完成状态与完成者信息
			const processedChecklists = (checklistResponse.data || []).map((checklist: any) => {
				if (!checklist.tasks || !Array.isArray(checklist.tasks)) {
					checklist.tasks = [];
				}

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

				const completedTasks = checklist.tasks.filter(
					(task: any) => task.isCompleted
				).length;
				const totalTasks = checklist.tasks.length;
				const completionRate =
					totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;

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
		console.error('Failed to load checklists:', error);
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
			const map: Record<string, SectionAnswer> = {};
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

// 加载静态字段值
const loadStaticFieldValues = async () => {
	if (!onboardingId.value) return;

	try {
		const response = await getStaticFieldValuesByOnboarding(onboardingId.value);
		if (response.code === '200' && response.data && Array.isArray(response.data)) {
			staticFormRefs.value.forEach((formRef) => {
				formRef.setFieldValues(response.data);
			});
		}
	} catch (error) {
		console.error('Failed to load static field values:', error);
		ElMessage.error('Failed to load static field values');
	}
};

// 加载依赖stageId的数据
const loadStageRelatedData = async (stageId: string) => {
	if (!stageId) return;

	try {
		stageDataLoading.value = true;
		// 清理之前的组件refs
		clearStaticFormRefs();
		clearQuestionnaireDetailsRefs();
		documentsRef.value = [];

		// 并行加载依赖stageId的数据
		await Promise.all([
			loadCheckListData(onboardingId.value, stageId),
			loadQuestionnaireDataBatch(onboardingId.value, stageId),
		]);
	} finally {
		stageDataLoading.value = false;
	}
};

// 加载当前阶段的基础数据
const loadCurrentStageData = async () => {
	if (!activeStage.value) return;
	await loadStageRelatedData(activeStage.value);
	await loadStaticFieldValues();
};

// 任务切换处理
const handleTaskToggled = async (task: any) => {
	try {
		checkLoading.value = true;
		const res = await saveCheckListTask({
			checklistId: task.checklistId,
			isCompleted: task.isCompleted,
			taskId: task.id,
			onboardingId: onboardingId.value,
			stageId: activeStage.value,
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

// Removed isStageAccessible function - allow access to all stages

// 重新加载 activeStage 并加载相关数据
const setActiveStageWithData = async (stageId: string) => {
	if (activeStage.value === stageId) {
		return;
	}

	// Removed stage access restriction - allow access to all stages

	// 取消当前正在进行的AI摘要生成（如果有）
	if (aiSummaryAbortController) {
		aiSummaryAbortController.abort();
		aiSummaryLoading.value = false;
		console.log('🚫 [Stage Switch] Cancelled AI summary generation due to stage change');
	}

	activeStage.value = stageId;
	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === stageId
	);

	// 更新AI Summary显示
	updateAISummaryFromStageInfo();

	// 重新加载依赖stageId的数据
	await loadStageRelatedData(stageId);
	await loadStaticFieldValues();

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

// 保存所有表单数据的函数
const saveAllLoading = ref(false);
const saveAllForm = async (isValidate: boolean = true) => {
	try {
		saveAllLoading.value = true;
		// 串行执行保存操作 - 先保存StaticForm组件
		if (staticFormRefs.value.length > 0) {
			for (let i = 0; i < staticFormRefs.value.length; i++) {
				const formRef = staticFormRefs.value[i];
				if (formRef && typeof formRef.handleSave === 'function') {
					const result = await formRef.handleSave(isValidate);
					if (result !== true) {
						return false;
					}
				}
			}
		}

		// 串行执行保存操作 - 再保存QuestionnaireDetails组件
		if (questionnaireDetailsRefs.value.length > 0) {
			for (let i = 0; i < questionnaireDetailsRefs.value.length; i++) {
				const questRef = questionnaireDetailsRefs.value[i];
				if (questRef && typeof questRef.handleSave === 'function') {
					const result = await questRef.handleSave(false, isValidate);
					if (result !== true) {
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
						if (!result) {
							return false;
						}
					} catch (error) {
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

// 其他处理函数
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
					instance.confirmButtonText = 'Completing...';
					completing.value = true;
					try {
						// 1. 先进行隐藏组件校验
						const hiddenValidation = await validateHiddenComponents();
						if (!hiddenValidation.isValid) {
							showHiddenComponentErrors(hiddenValidation.hiddenValidationErrors);
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Complete Stage';
							done();
							return; // 不关闭对话框，让用户知道问题
						}

						// 2. 再进行常规可见组件校验和保存
						const res = await saveAllForm();
						if (!res) {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Complete Stage';
						} else {
							// 3. 执行Complete Stage操作
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

const handleStageUpdated = async () => {
	loadOnboardingDetail();
};

const handleDocumentUploaded = () => {
	refreshChangeLog();
};

const handleDocumentDeleted = () => {
	refreshChangeLog();
};

const handleEditDialogClose = () => {
	editDialogVisible.value = false;
};

const handleSaveEdit = async () => {
	try {
		saving.value = true;
		ElMessage.success('Saved successfully');
		editDialogVisible.value = false;
	} catch (error) {
		console.error('Failed to save edit:', error);
		ElMessage.error('Failed to save');
	} finally {
		saving.value = false;
	}
};

const refreshChangeLog = () => {
	// ChangeLog component removed
};

// 生命周期
onMounted(async () => {
	if (!onboardingId.value) {
		ElMessage.error('Invalid onboarding ID from route parameters');
		router.push('/customer-portal');
		return;
	}
	await loadOnboardingDetail();
});

// 监听路由中 stageId 的变化
watch(stageIdFromRoute, async (newStageId) => {
	if (newStageId && workflowStages.value.length > 0) {
		// 验证新的 stageId 是否有效
		const foundStage = workflowStages.value.find((stage) => stage.stageId === newStageId);
		if (foundStage && activeStage.value !== newStageId) {
			await setActiveStageWithData(newStageId);
		}
	}
});
</script>

<style scoped lang="scss">
/* Portal custom classes */
.portal-btn-hover:hover {
	background-color: var(--el-fill-color-light);
}

.portal-nav-item {
	color: var(--el-text-color-regular);
	&:hover {
		background-color: var(--el-fill-color-lighter);
		color: var(--el-text-color-primary);
	}
}

.portal-nav-active {
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
}

.portal-company-icon {
	background-color: var(--el-color-primary-light-9);
	padding: 0.5rem;
	border-radius: 9999px;
	color: var(--el-color-primary);
}

.portal-text-primary {
	color: var(--el-text-color-primary);
}

.portal-text-secondary {
	color: var(--el-text-color-secondary);
}

.portal-sidebar-border {
	border-right: 1px solid var(--el-border-color-light);
}

.portal-loading-text {
	color: var(--el-text-color-secondary);
}

html.dark .portal-loading-text {
	color: var(--el-text-color-placeholder);
}

.editable-header-card {
	background: var(--el-color-primary);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
	display: flex;
	flex-direction: column;
	gap: 16px;
	transition: all 0.2s ease;

	&:hover {
		box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
		transform: translateY(-1px);
	}
}
/* Smooth transitions */
.transition-colors {
	transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
	transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
	transition-duration: 150ms;
}

.transition-all {
	transition-property: all;
	transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
	transition-duration: 150ms;
}

/* Focus styles */
button:focus {
	outline: 2px solid transparent;
	outline-offset: 2px;
}

/* Hover effects */
button:hover {
	transition: all 0.15s ease-in-out;
}

/* Shadow styles to match original */
.shadow-sm {
	box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
}

/* 文字溢出处理 */
.word-wrap {
	word-wrap: break-word;
	-webkit-hyphens: auto;
	-moz-hyphens: auto;
	hyphens: auto;
}
</style>
