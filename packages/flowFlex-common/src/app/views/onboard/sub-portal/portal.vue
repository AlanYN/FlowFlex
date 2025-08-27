<template>
	<div class="min-h-screen bg-gray-50">
		<!-- Mobile sidebar -->
		<div :class="['fixed inset-0 z-50 lg:hidden', sidebarOpen ? 'block' : 'hidden']">
			<div class="fixed inset-0 bg-gray-600 bg-opacity-75" @click="sidebarOpen = false"></div>
			<div class="fixed inset-y-0 left-0 flex w-64 flex-col bg-white">
				<div class="flex h-16 items-center justify-between px-4 border-b">
					<h1 class="text-xl font-bold text-blue-600">Customer Portal</h1>
					<button @click="sidebarOpen = false" class="p-1 rounded-md hover:bg-gray-100">
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
							'group flex items-center px-2 py-2 text-sm font-medium rounded-md cursor-pointer',
							currentView === item.view
								? 'bg-blue-100 text-blue-900'
								: 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
						]"
						@click="handleNavigation(item)"
					>
						<component :is="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-lg border bg-white p-4 shadow-sm">
						<div class="flex items-center space-x-3">
							<div class="bg-blue-100 p-2 rounded-full">
								<svg
									class="h-5 w-5 text-blue-600"
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
								<p class="text-sm font-medium text-gray-900 truncate">
									{{ customerData.companyName }}
								</p>
								<p class="text-xs text-gray-500 truncate">
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
			<div class="flex flex-col flex-grow bg-white border-r border-gray-200">
				<div class="flex h-16 items-center px-4 border-b">
					<h1 class="text-xl font-bold text-blue-600">Customer Portal</h1>
				</div>
				<nav class="flex-1 space-y-1 px-2 py-4">
					<div
						v-for="item in navigation"
						:key="item.name"
						:class="[
							'group flex items-center px-2 py-2 text-sm font-medium rounded-md cursor-pointer',
							currentView === item.view
								? 'bg-blue-100 text-blue-900'
								: 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
						]"
						@click="handleNavigation(item)"
					>
						<component :is="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-lg border bg-white p-4 shadow-sm">
						<div class="flex items-center space-x-3 mb-3">
							<div class="bg-blue-100 p-2 rounded-full">
								<svg
									class="h-5 w-5 text-blue-600"
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
								<p class="text-sm font-medium text-gray-900 truncate">
									{{ customerData.companyName }}
								</p>
								<p class="text-xs text-gray-500 truncate">
									{{ customerData.contactName }}
								</p>
							</div>
						</div>
						<div class="space-y-1">
							<div class="flex items-center text-xs text-gray-500">
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
				<button @click="sidebarOpen = true" class="p-1 rounded-md hover:bg-gray-100">
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
			<main class="flex-1 p-6">
				<!-- Onboarding Detail View -->
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
								Onboarding Details: {{ onboardingData?.leadId }}
								{{ onboardingData?.leadName }}
							</h1>
						</div>
						<div class="flex items-center space-x-2">
							<el-button
								type="primary"
								@click="saveQuestionnaireAndField"
								:loading="saveAllLoading"
							>
								<el-icon class="mr-1">
									<Document />
								</el-icon>
								Save
							</el-button>
							<el-button
								type="primary"
								@click="handleCompleteStage"
								:loading="completing"
							>
								<el-icon class="mr-1">
									<Check />
								</el-icon>
								Complete Stage
							</el-button>
						</div>
					</div>

					<!-- 主要内容区域 -->
					<div class="flex gap-6">
						<!-- 左侧阶段详情 (2/3 宽度) -->
						<div class="flex-[2] min-w-0 overflow-hidden">
							<div
								class="rounded-md el-card is-always-shadow rounded-md el-card__header"
							>
								<div
									class="bg-gradient-to-r from-blue-500 to-indigo-500 text-white -mx-5 -my-5 px-5 py-4 rounded-t-lg"
								>
									<h2 class="text-lg font-semibold">{{ currentStageTitle }}</h2>
								</div>
							</div>
							<el-scrollbar ref="leftScrollbarRef" class="h-full pr-4">
								<div class="space-y-6 mt-4">
									<!-- AI Summary 展示（当前阶段） -->
									<div
										v-if="showAISummarySection"
										class="bg-white dark:bg-black-300 rounded-md p-4"
									>
										<div class="flex justify-between items-center mb-2">
											<div class="text-sm text-gray-500">AI Summary</div>
											<el-button
												:icon="Refresh"
												size="small"
												circle
												:loading="aiSummaryLoading"
												@click="refreshAISummary"
												title="Refresh AI Summary"
											/>
										</div>

										<!-- AI Summary content (always visible if exists) -->
										<div v-if="currentAISummary" class="ai-summary-content">
											<p
												class="whitespace-pre-line text-sm leading-6 text-gray-700 dark:text-gray-200"
												:class="{ streaming: aiSummaryLoading }"
											>
												{{ currentAISummary }}
												<span
													v-if="aiSummaryLoading"
													class="typing-indicator"
												>
													|
												</span>
											</p>
										</div>

										<!-- Loading state (only when no content yet) -->
										<div
											v-else-if="aiSummaryLoading"
											class="flex items-center space-x-2 py-4"
										>
											<el-icon class="is-loading text-lg text-primary-500">
												<Loading />
											</el-icon>
											<span class="text-sm text-gray-500">
												{{ aiSummaryLoadingText }}
											</span>
										</div>

										<!-- Empty state -->
										<div v-else class="text-sm text-gray-400 italic py-2">
											No AI summary available. Click refresh to generate.
										</div>

										<!-- Loading indicator when streaming content -->
										<div
											v-if="aiSummaryLoading && currentAISummary"
											class="flex items-center space-x-2 py-2 mt-2"
										>
											<el-icon class="is-loading text-sm text-primary-500">
												<Loading />
											</el-icon>
											<span class="text-xs text-gray-500">
												{{ aiSummaryLoadingText }}
											</span>
										</div>

										<div
											v-if="currentAISummaryGeneratedAt"
											class="mt-1 text-xs text-gray-400"
										>
											<span>
												Generated at:
												{{ formatUsDate(currentAISummaryGeneratedAt) }}
											</span>
										</div>
									</div>
									<!-- Stage Details 加载状态 -->
									<div
										v-if="stageDataLoading"
										class="bg-white dark:bg-black-300 rounded-md p-8"
									>
										<div
											class="flex flex-col items-center justify-center space-y-4"
										>
											<el-icon class="is-loading text-4xl text-primary-500">
												<Loading />
											</el-icon>
											<p class="text-gray-500 dark:text-gray-400">
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
								</div>
							</el-scrollbar>
						</div>

						<!-- 右侧进度和笔记 (1/3 宽度) -->
						<div class="flex-1">
							<el-scrollbar ref="rightScrollbarRef" class="h-full pr-4">
								<div class="space-y-6">
									<!-- OnboardingProgress组件 -->
									<div class="rounded-md overflow-hidden">
										<OnboardingProgress
											v-if="onboardingData && onboardingId"
											:active-stage="activeStage"
											:onboarding-data="onboardingData"
											:workflow-stages="workflowStages"
											@set-active-stage="setActiveStageWithData"
											@stage-completed="loadOnboardingDetail"
											class="bg-white dark:bg-black-300 rounded-md shadow-lg border border-gray-200 dark:border-gray-600"
										/>
									</div>

									<!-- 笔记区域 -->
									<!-- <div class="rounded-md overflow-hidden">
										<InternalNotes v-if="activeStage && onboardingId" :onboarding-id="onboardingId"
											:stage-id="activeStage" @note-added="handleNoteAdded" />
									</div> -->
								</div>
							</el-scrollbar>
						</div>
					</div>

					<!-- 编辑对话框 -->
					<el-dialog
						v-model="editDialogVisible"
						title="Edit Onboarding"
						width="500px"
						:before-close="handleEditDialogClose"
					>
						<el-form :model="editForm" label-width="100px">
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
import { ArrowLeft, Loading, Check, Document, Refresh } from '@element-plus/icons-vue';
import {
	getOnboardingByLead,
	getStaticFieldValuesByOnboarding,
	saveCheckListTask,
	getCheckListIds,
	getCheckListIsCompleted,
	getQuestionIds,
	getQuestionnaireAnswer,
	completeCurrentStage,
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
		name: 'Onboarding Progress',
		view: 'progress',
		icon: HomeIcon,
	},
	{
		name: 'Onboarding Detail',
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

const sortedComponents = computed(() => {
	if (!onboardingActiveStageInfo.value?.components) {
		return [];
	}
	return [...onboardingActiveStageInfo.value.components].sort((a, b) => {
		if (a.key === 'fields' && b.key !== 'fields') {
			return -1;
		}
		if (a.key !== 'fields' && b.key === 'fields') {
			return 1;
		}
		return a.order - b.order;
	});
});

// AI Summary 状态与工具
const aiSummaryLoading = ref(false);
const aiSummaryLoadingText = ref('Generating AI summary...');
const currentAISummary = ref('');
const currentAISummaryGeneratedAt = ref('');
const showAISummarySection = ref(true);

const formatUsDate = (value?: string | Date) => {
	if (!value) return '';
	try {
		const d = typeof value === 'string' ? new Date(value) : value;
		return new Intl.DateTimeFormat('en-US', {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit',
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
			hour12: false,
		}).format(d);
	} catch {
		return String(value);
	}
};

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

	// 重置状态，开始流式生成
	aiSummaryLoading.value = true;
	aiSummaryLoadingText.value = 'Starting AI summary generation...';
	currentAISummary.value = ''; // 清空现有内容，准备流式显示
	console.log('🔄 [AI Summary] Starting generation, loading =', aiSummaryLoading.value);

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
		const url = `/api/ow/stages/v1/${activeStage.value}/ai-summary/stream?onboardingId=${onboardingId.value}`;
		const response = await fetch(url, {
			method: 'POST',
			headers,
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

		// 流结束，设置状态
		console.log('✅ [AI Summary] Stream completed');
		currentAISummaryGeneratedAt.value = new Date().toISOString();
		aiSummaryLoading.value = false;
		ElMessage.success('AI Summary generated successfully');

		// 更新本地stage信息
		if (onboardingActiveStageInfo.value) {
			(onboardingActiveStageInfo.value as any).aiSummary = currentAISummary.value;
			(onboardingActiveStageInfo.value as any).aiSummaryGeneratedAt =
				currentAISummaryGeneratedAt.value;
			console.log('📝 [AI Summary] Updated stage info');
		}
	} catch (error) {
		console.error('Error generating AI summary:', error);
		aiSummaryLoading.value = false;
		ElMessage.error('Failed to generate AI summary');
	}
};

const checkAndGenerateAISummary = async () => {
	if (!onboardingActiveStageInfo.value?.aiSummary && !aiSummaryLoading.value) {
		await refreshAISummary();
	}
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

// 重新加载 activeStage 并加载相关数据
const setActiveStageWithData = async (stageId: string) => {
	if (activeStage.value === stageId) {
		return;
	}
	activeStage.value = stageId;
	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === stageId
	);
	// 重新加载依赖stageId的数据
	await loadStageRelatedData(stageId);
	await loadStaticFieldValues();
	updateAISummaryFromStageInfo();
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
						// 先保存所有表单数据
						const res = await saveAllForm();
						if (!res) {
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Complete Stage';
						} else {
							// 保存成功后再完成阶段
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

<style scoped>
/* Ensure consistent border radius */
.rounded-lg {
	border-radius: 0.5rem;
}

.rounded-md {
	border-radius: 0.375rem;
}

.rounded-full {
	border-radius: 9999px;
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

/* AI Summary Streaming Styles */
.ai-summary-content .streaming {
	background: linear-gradient(
		90deg,
		transparent 0%,
		rgba(59, 130, 246, 0.1) 50%,
		transparent 100%
	);
	background-size: 200% 100%;
	animation: shimmer 2s infinite;
	padding: 8px;
	border-radius: 4px;
}

@keyframes shimmer {
	0% {
		background-position: -200% 0;
	}
	100% {
		background-position: 200% 0;
	}
}

.typing-indicator {
	animation: blink 1s infinite;
	color: #3b82f6;
	font-weight: bold;
}

@keyframes blink {
	0%,
	50% {
		opacity: 1;
	}
	51%,
	100% {
		opacity: 0;
	}
}
</style>
