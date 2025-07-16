<template>
	<div class="pb-6 bg-gray-50 dark:bg-black-400">
		<!-- 顶部导航栏 -->
		<div class="flex justify-between items-center mb-6">
			<div class="flex items-center">
				<el-button link size="small" @click="handleBack"
					class="mr-2 !p-1 hover:bg-gray-100 dark:hover:bg-black-200 rounded">
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
				<el-button type="primary" @click="messageDialogVisible = true">
					<el-icon>
						<ChatDotSquare />
					</el-icon>
					&nbsp;&nbsp;Send Message
				</el-button>
			</div>
		</div>

		<!-- 主要内容区域 -->
		<div class="flex gap-6">
			<!-- 左侧阶段详情 (2/3 宽度) -->
			<div class="flex-[2]">
				<div class="rounded-md el-card is-always-shadow rounded-md el-card__header">
					<div
						class="bg-gradient-to-r from-blue-500 to-indigo-500 text-white -mx-5 -my-5 px-5 py-4 rounded-t-lg">
						<h2 class="text-lg font-semibold">{{ currentStageTitle }}</h2>
					</div>
				</div>
				<el-scrollbar ref="leftScrollbarRef" class="h-full">
					<div class="space-y-6 pr-4 mt-4">
						<!-- Stage Details 加载状态 -->
						<div v-if="stageDataLoading" class="bg-white dark:bg-black-300 rounded-md p-8">
							<div class="flex flex-col items-center justify-center space-y-4">
								<el-icon class="is-loading text-4xl text-primary-500">
									<Loading />
								</el-icon>
								<p class="text-gray-500 dark:text-gray-400">
									Loading stage details...
								</p>
							</div>
						</div>

						<StaticForm v-show="onboardingData && activeStage && onboardingId && !stageDataLoading
							" ref="staticFormRef" :static-fields="onboardingActiveStageInfo?.staticFields || []"
							:onboarding-id="onboardingId" :stage-id="activeStage" />

						<StageDetails v-show="onboardingData && activeStage && onboardingId && !stageDataLoading
							" :stage-id="activeStage" :lead-id="onboardingId" :onboarding-id="onboardingId" :lead-data="onboardingData"
							:workflow-stages="workflowStages" :questionnaire-data="questionnaireData"
							:static-fields="onboardingActiveStageInfo?.staticFields || []"
							@stage-updated="handleStageUpdated" ref="stageDetailsRef" />

						<!-- CheckList 加载状态 -->
						<div v-if="checkListLoading" class="bg-white dark:bg-black-300 rounded-md p-8">
							<div class="flex flex-col items-center justify-center space-y-4">
								<el-icon class="is-loading text-4xl text-primary-500">
									<Loading />
								</el-icon>
								<p class="text-gray-500 dark:text-gray-400">Loading checklist...</p>
							</div>
						</div>

						<CheckList v-if="activeStage && onboardingId && !checkListLoading"
							:checklist-data="checkListData" @task-toggled="handleTaskToggled" />

						<!-- Documents 组件 -->
						<div class="rounded-md overflow-hidden">
							<Documents v-if="activeStage && onboardingId" :onboarding-id="onboardingId"
								:stage-id="activeStage" @document-uploaded="handleDocumentUploaded"
								@document-deleted="handleDocumentDeleted" />
						</div>
					</div>
				</el-scrollbar>
			</div>

			<!-- 右侧进度和笔记 (1/3 宽度) -->
			<div class="flex-1">
				<el-scrollbar ref="rightScrollbarRef" class="h-full">
					<div class="space-y-6 pr-4">
						<!-- OnboardingProgress组件 -->
						<div class="rounded-md overflow-hidden">
							<OnboardingProgress v-if="onboardingData && onboardingId" :active-stage="activeStage"
								:onboarding-data="onboardingData" :workflow-stages="workflowStages"
								@set-active-stage="setActiveStage" @stage-completed="loadOnboardingDetail"
								class="bg-white dark:bg-black-300 rounded-md shadow-lg border border-gray-200 dark:border-gray-600" />
						</div>

						<!-- 笔记区域 -->
						<div class="rounded-md overflow-hidden">
							<InternalNotes v-if="activeStage && onboardingId" :onboarding-id="onboardingId"
								:stage-id="activeStage" @note-added="handleNoteAdded" />
						</div>
					</div>
				</el-scrollbar>
			</div>
		</div>

		<!-- 变更日志 -->
		<!-- ChangeLog 加载状态 -->
		<div class="mt-4">
			<div v-if="stageDataLoading"
				class="flex flex-col items-center justify-center space-y-4 py-8 bg-white dark:bg-black-300 rounded-md mt-6">
				<el-icon class="is-loading text-4xl text-primary-500">
					<Loading />
				</el-icon>
				<p class="text-gray-500 dark:text-gray-400">Loading change log...</p>
			</div>

			<ChangeLog v-if="onboardingId && !stageDataLoading" :onboarding-id="onboardingId" :stage-id="activeStage" />
		</div>

		<!-- 编辑对话框 -->
		<el-dialog v-model="editDialogVisible" title="Edit Onboarding" width="500px"
			:before-close="handleEditDialogClose">
			<el-form :model="editForm" label-width="100px">
				<el-form-item label="Priority">
					<el-select v-model="editForm.priority" placeholder="Select Priority" class="w-full">
						<el-option label="High" value="High" />
						<el-option label="Medium" value="Medium" />
						<el-option label="Low" value="Low" />
					</el-select>
				</el-form-item>
				<el-form-item label="Assignee">
					<el-input v-model="editForm.assignee" placeholder="Enter assignee name" />
				</el-form-item>
				<el-form-item label="Notes">
					<el-input v-model="editForm.notes" type="textarea" :rows="3" placeholder="Enter notes" />
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

		<!-- 消息对话框 -->
		<MessageDialog v-model="messageDialogVisible" :onboarding-data="onboardingData" />
		<!-- Portal Access Management 对话框 -->
		<el-dialog v-model="portalAccessDialogVisible" title="Portal Access Management" width="800px"
			:before-close="() => (portalAccessDialogVisible = false)">
			<PortalAccessContent :onboarding-id="onboardingId" :onboarding-data="onboardingData" />
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowLeft, ChatDotSquare, Loading, User } from '@element-plus/icons-vue';
import {
	getStageCompletionLogsByStage,
	getOnboardingByLead,
	getCheckList,
	getCheckListIsCompleted,
	getStaticFieldValuesByOnboarding,
	saveCheckListTask,
} from '@/apis/ow/onboarding';
import { getStageQuestionnaire } from '@/apis/ow/questionnaire';
import { OnboardingItem } from '#/onboard';
import { useAdaptiveScrollbar } from '@/hooks/useAdaptiveScrollbar';
import { useI18n } from 'vue-i18n';
import { defaultStr } from '@/settings/projectSetting';
// 导入组件
import OnboardingProgress from './components/OnboardingProgress.vue';
import StageDetails from './components/StageDetails.vue';
import InternalNotes from './components/InternalNotes.vue';
import ChangeLog from './components/ChangeLog.vue';
import MessageDialog from './components/MessageDialog.vue';
import CheckList from './components/CheckList.vue';
import Documents from './components/Documents.vue';
import StaticForm from './components/StaticForm.vue';
import PortalAccessContent from './components/PortalAccessContent.vue';
import * as userInvitationApi from '@/apis/ow/userInvitation';


const { t } = useI18n();

// 变更类型定义 - 匹配ChangeLog组件期望的数据结构
type Change = {
	id: string;
	tenantId?: string;
	onboardingId?: string;
	stageId?: string;
	action: string;
	logType: string;
	data?: any;
	success: boolean;
	errorMessage?: string;
	networkStatus?: string;
	responseTime?: number | null;
	endpoint?: string;
	userAgent?: string;
	method?: string | null;
	ipAddress?: string;
	clientInfo?: string | null;
	createDate: string;
	createBy: string;
};

// 常量定义
const router = useRouter();
const route = useRoute();

// 响应式数据
const onboardingData = ref<OnboardingItem | null>(null);
const activeStage = ref<string>(''); // 初始为空，等待从服务器获取当前阶段
const workflowStages = ref<any[]>([]);
const changeLogData = ref<Change[]>([]);
const questionnaireData = ref<any>(null); // 当前阶段的问卷数据
const checkListData = ref<any>(null); // 当前阶段的检查清单数据
const editDialogVisible = ref(false);
const messageDialogVisible = ref(false);
const portalAccessDialogVisible = ref(false);
const saving = ref(false);

// Loading状态管理
const stageDataLoading = ref(false); // 初始加载和阶段完成后的数据加载状态
const checkListLoading = ref(false); // 检查清单加载状态
const initialLoading = ref(true); // 初始页面加载状态

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

// 添加组件引用
const stageDetailsRef = ref();
const staticFormRef = ref();
const onboardingActiveStageInfo = ref<any>(null);
// 处理onboarding数据的共同逻辑
const processOnboardingData = async (responseData: any) => {
	onboardingData.value = responseData;

	workflowStages.value = responseData.stagesProgress;
	console.log('workflowStages:', workflowStages.value);
	// 设置当前活动阶段
	const newStageId =
		responseData?.currentStageId ||
		(responseData?.currentStageName &&
			workflowStages.value.find((stage) => stage.name === responseData.currentStageName)?.id);
	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === newStageId
	);
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
			const newStageId = await processOnboardingData(response.data);

			// 设置activeStage
			if (newStageId) {
				activeStage.value = newStageId;
				// 设置 activeStage 后，加载当前阶段的基础数据
				await loadCurrentStageData();
			}
		}
	} finally {
		initialLoading.value = false;
	}
};

const loadChangeLog = async (stageId?: string) => {
	if (!onboardingId.value) {
		console.error('Cannot load change log: Invalid onboarding ID');
		return;
	}

	try {
		// 如果提供了 stageId，使用 stageId 加载；否则加载整个 onboarding 的日志
		const response = await getStageCompletionLogsByStage(activeStage.value, {
			onboardingId: onboardingId.value,
		});

		if (response.code === '200') {
			changeLogData.value = response.data || [];
		}
	} catch (error) {
		console.error('Failed to load change log:', error);
		ElMessage.error('Failed to load change log');
	}
};

// 加载问卷数据
const loadQuestionnaireData = async (stageId: string) => {
	try {
		const response = await getStageQuestionnaire(stageId);
		if (response.code === '200') {
			questionnaireData.value = response.data;
		} else {
			questionnaireData.value = null;
		}
	} catch (error) {
		console.error('Failed to load questionnaire data:', error);
		questionnaireData.value = null;
	}
};

// 加载检查清单数据
const loadCheckListData = async (stageId: string) => {
	try {
		checkListLoading.value = true;
		const response = await getCheckList(stageId);

		if (response.code === '200') {
			checkListData.value = response.data;
			checkListData.value = await Promise.all(
				checkListData.value.map(async (item) => {
					const res = await getCheckListIsCompleted(onboardingId.value, item.id);
					if (res.code == '200' && res.data && res.data.length > 0) {
						const taskComplete = res.data;
						item.tasks.forEach((task) => {
							task.isCompleted = !!taskComplete.find((t) => t.taskId === task.id)
								?.isCompleted;
						});
					}
					return item;
				})
			);
		} else {
			checkListData.value = null;
		}
	} finally {
		checkListLoading.value = false;
	}
};

// 加载依赖stageId的数据（问卷、检查清单、变更日志）
const loadStageRelatedData = async (stageId: string) => {
	if (!stageId) return;

	try {
		// 设置加载状态
		stageDataLoading.value = true;

		// 并行加载依赖stageId的数据
		await Promise.all([
			loadQuestionnaireData(stageId),
			loadCheckListData(stageId),
			loadChangeLog(stageId),
		]);
	} catch (error) {
		console.error('Failed to load stage related data:', error);
		ElMessage.error('Failed to load stage data');
	} finally {
		stageDataLoading.value = false;
	}
};

// 加载当前阶段的基础数据（仅在初始加载时调用）
const loadCurrentStageData = async () => {
	if (!activeStage.value) return;

	await Promise.all([loadStageRelatedData(activeStage.value), loadStaticFieldValues()]);
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
			// 直接传递给StageDetails组件处理
			if (stageDetailsRef.value) {
				stageDetailsRef.value.setFormFieldValues?.();
				staticFormRef.value.setFieldValues(response.data);
			}
		}
	} catch (error) {
		console.error('Failed to load static field values:', error);
		ElMessage.error('Failed to load static field values');
	}
};

const setActiveStage = async (stageId: string) => {
	// 如果切换到相同的阶段，不需要重新加载
	if (activeStage.value === stageId) {
		return;
	}

	// 更新activeStage
	activeStage.value = stageId;
	onboardingActiveStageInfo.value = workflowStages.value.find(
		(stage) => stage.stageId === stageId
	);

	// 重新加载依赖stageId的数据
	await Promise.all([
		loadStageRelatedData(stageId),
		loadStaticFieldValues(), // 添加加载字段值的调用
	]);
};

const handleNoteAdded = () => {
	// 笔记添加后的处理
};

const handleDocumentUploaded = (document: any) => {
	// 文档上传后的处理
};

const handleDocumentDeleted = (documentId: string) => {
	// 文档删除后的处理
};

const handleTaskToggled = async (task: any) => {
	// 处理任务状态切换
	try {
		const res = await saveCheckListTask({
			checklistId: task.checklistId,
			isCompleted: task.isCompleted,
			taskId: task.id,
			onboardingId: onboardingId.value,
			stageId: activeStage.value, // 添加当前阶段ID
		});
		if (res.code === '200') {
			ElMessage.success(t('sys.api.operationSuccess'));
			// 直接更新本地状态，避免重新加载整个检查清单
			if (checkListData.value) {
				checkListData.value.forEach((checklist) => {
					checklist.tasks.forEach((t) => {
						if (t.id === task.id) {
							t.isCompleted = task.isCompleted;
							// 如果任务完成，更新完成时间
							if (task.isCompleted) {
								t.completedDate = new Date().toISOString();
							} else {
								t.completedDate = null;
							}
						}
					});
				});
			}
		} else {
			ElMessage.error(res.msg || t('sys.api.operationFailed'));
		}
	} catch (error) {
		console.error('Failed to toggle task:', error);
		ElMessage.error(t('sys.api.operationFailed'));
	}
};

const handleEditDialogClose = () => {
	editDialogVisible.value = false;
};

const handleSaveEdit = async () => {
	try {
		saving.value = true;
		// 这里应该调用API保存编辑的数据
		ElMessage.success('Saved successfully');
		editDialogVisible.value = false;
	} catch (error) {
		console.error('Failed to save edit:', error);
		ElMessage.error('Failed to save');
	} finally {
		saving.value = false;
	}
};

const saveAllForm = async () => {
	const res = await Promise.all([
		stageDetailsRef.value.handleSave(),
		staticFormRef.value.handleSave(),
	]);
	return res;
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
							return;
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

// 生命周期
onMounted(async () => {
	// 检查是否有有效的 onboarding ID
	if (!onboardingId.value) {
		ElMessage.error('Invalid onboarding ID from route parameters');
		router.push('/onboard/onboardList'); // 重定向到列表页
		return;
	}

	// 检查是否有token参数（从邮件链接访问）
	const token = route.query.token as string;
	if (token) {
		// 如果有token，进行验证
		try {
			// 调用API验证token是否有效
			console.log('Portal access token detected:', token);
			const response = await userInvitationApi.validateInvitationToken({
				token: token,
				onboardingId: onboardingId.value,
			});
			
			// 从响应中提取实际的验证数据
			const tokenValidation = response.data || response;
			console.log('Token validation response:', response);
			console.log('Token validation data:', tokenValidation);
			
			if (tokenValidation.isValid) {
				ElMessage.success('Welcome! You are accessing through the portal invitation.');
				// 将token存储到本地存储，供后续API调用使用
				localStorage.setItem('portal_access_token', token);
				if (tokenValidation.email) {
					localStorage.setItem('portal_user_email', tokenValidation.email);
				}
			} else {
				ElMessage.error(tokenValidation.errorMessage || 'Invalid or expired invitation link');
				return;
			}
		} catch (error) {
			console.error('Token validation failed:', error);
			ElMessage.error('Invalid or expired invitation link');
			return;
		}
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
