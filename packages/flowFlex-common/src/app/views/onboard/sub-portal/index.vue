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
					<h1 class="text-xl font-bold">Customer Portal</h1>
					<el-button @click="sidebarOpen = false">
						<Icon icon="mdi:close" class="h-5 w-5" />
					</el-button>
				</div>
				<nav class="flex-1 space-y-1 px-2 py-4">
					<div
						v-for="item in navigation"
						:key="item.name"
						:class="[
							' flex items-center px-2 py-2 text-sm font-medium rounded-xl cursor-pointer ',
							currentView === item.view ? 'portal-nav-active' : '',
						]"
						@click="
							handleNavigation(item.view);
							sidebarOpen = false;
						"
					>
						<Icon :icon="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-xl border bg-siderbarGray dark:bg-black p-4 shadow-sm">
						<div class="flex items-center space-x-3">
							<div class="portal-company-icon">
								<Icon icon="mdi:office-building-outline" class="h-5 w-5" />
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
			<div
				class="flex flex-col flex-grow bg-siderbarGray dark:bg-black border-r border-gray-200"
			>
				<div class="flex h-16 items-center px-4 border-b">
					<h1 class="text-xl font-bold">Customer Portal</h1>
				</div>
				<nav class="flex-1 space-y-1 px-2 py-4">
					<div
						v-for="item in navigation"
						:key="item.name"
						:class="[
							'group flex items-center px-2 py-2 text-sm font-medium rounded-xl cursor-pointer ',
							currentView === item.view ? 'portal-nav-active' : '',
						]"
						@click="handleNavigation(item.view)"
					>
						<Icon :icon="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-xl border bg-siderbarGray dark:bg-black p-4 shadow-sm">
						<div class="flex items-center space-x-3 mb-3">
							<div class="portal-company-icon">
								<Icon icon="mdi:office-building-outline" class="h-5 w-5" />
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
								<Icon icon="mdi:account-outline" class="h-3 w-3 mr-1" />
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
			<div
				class="flex h-16 items-center justify-between border-b bg-siderbarGray dark:bg-black px-4 lg:hidden"
			>
				<el-button @click="sidebarOpen = true">
					<Icon icon="mdi:menu" class="h-5 w-5" />
				</el-button>
				<h1 class="text-lg font-bold">Customer Portal</h1>
				<div></div>
			</div>

			<!-- Page content -->
			<main class="flex-1 p-6" style="background: var(--el-bg-color-page)">
				<!-- Onboarding Progress View -->
				<div v-if="currentView === 'progress'" class="space-y-6">
					<!-- Loading State -->
					<div v-if="loading" class="flex items-center justify-center py-12">
						<div class="text-center">
							<div
								class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-primary"
							></div>
							<p class="mt-2 text-gray-600">Loading onboarding data...</p>
						</div>
					</div>

					<!-- 统一页面头部 -->
					<PageHeader
						v-else
						title="Case Progress"
						description="Track your journey with us"
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
					</PageHeader>

					<!-- Overall Progress -->
					<div
						v-if="!loading"
						class="rounded-xl border bg-white dark:bg-black p-6 shadow-sm"
					>
						<div class="mb-6">
							<div class="flex items-center mb-2">
								<Icon icon="mdi:chart-bar" class="mr-2 h-5 w-5 text-gray-700" />
								<h3 class="text-lg font-semibold text-gray-900">
									Overall Progress
								</h3>
							</div>
							<p class="text-sm text-gray-600">Your onboarding journey with us</p>
						</div>
						<div class="space-y-4">
							<div class="flex items-center justify-between">
								<span class="text-sm font-medium text-gray-700">Progress</span>
								<span class="text-sm font-medium text-gray-700">
									{{ progressPercentage }}%
								</span>
							</div>
							<div class="w-full bg-gray-200 rounded-full h-3">
								<div
									class="bg-primary h-3 rounded-full transition-all duration-300"
									:style="{ width: progressPercentage + '%' }"
								></div>
							</div>
							<div
								class="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm text-gray-600"
							>
								<div class="flex items-center space-x-2">
									<Icon icon="mdi:calendar-outline" class="h-4 w-4" />
									<span>Started: {{ customerData.startDate }}</span>
								</div>
								<div class="flex items-center space-x-2"></div>
								<div class="flex items-center space-x-2">
									<Icon icon="mdi:account-outline" class="h-4 w-4" />
									<span>
										{{ completedStages }} of {{ totalStages }} stages completed
									</span>
								</div>
							</div>
							<div
								v-if="currentStageData"
								class="mt-4 p-3 border rounded-xl bg-black-400"
							>
								<p class="text-sm font-medium">
									Current Stage: {{ currentStageData.name }}
								</p>
								<p class="text-sm">
									{{ currentStageData.description }}
								</p>
							</div>
						</div>
					</div>

					<!-- Next Steps - Action Required -->
					<div v-if="!loading" class="rounded-xl bg-white dark:bg-black p-6">
						<div class="mb-6">
							<div class="flex items-center mb-2">
								<Icon icon="mdi:star" class="mr-2 h-5 w-5" />
								<h3 class="text-lg font-semibold">Next Steps - Action Required</h3>
							</div>
							<p class="text-sm">
								Complete these steps to continue your onboarding process
							</p>
						</div>
						<div class="space-y-3">
							<div
								v-for="stage in nextSteps"
								:key="stage.id"
								class="flex items-center justify-between p-4 bg-black-400 rounded-xl border shadow-sm"
							>
								<div class="flex items-center space-x-3">
									<div class="flex-shrink-0">
										<div
											class="w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm"
											:style="{ backgroundColor: stage.color }"
										>
											<Icon icon="mdi:clock-outline" class="h-4 w-4" />
										</div>
									</div>
									<div>
										<p class="font-medium text-gray-900">{{ stage.name }}</p>
										<p class="text-sm text-gray-600">{{ stage.description }}</p>
										<span
											class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary mt-1 text-white"
										>
											In Progress
										</span>
									</div>
								</div>
								<div class="flex items-center space-x-2">
									<el-button
										@click="handleStageAction(stage)"
										:disabled="!isStageEditable"
										type="primary"
									>
										Continue
										<Icon icon="mdi:chevron-right" class="ml-2 h-4 w-4" />
									</el-button>
								</div>
							</div>
							<div v-if="nextSteps.length === 0" class="text-center py-6">
								<Icon
									icon="mdi:check-circle-outline"
									class="h-12 w-12 text-green-500 mx-auto mb-3"
								/>
								<p class="text-gray-600">
									No immediate action required. We're working on the next steps!
								</p>
							</div>
						</div>
					</div>

					<!-- Stages Timeline -->
					<div
						v-if="!loading"
						class="rounded-xl border bg-white dark:bg-black p-6 shadow-sm"
					>
						<div class="mb-6">
							<h3 class="text-lg font-semibold">Case Stages</h3>
							<p class="text-sm text-gray-600">
								Track your progress through each stage
							</p>
						</div>
						<div class="space-y-4">
							<div
								v-for="stage in customerStages"
								:key="stage.id"
								:class="[
									'flex items-start space-x-4 p-4 rounded-xl border transition-colors bg-white dark:bg-black-400 border-gray-200',
								]"
							>
								<div
									class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
									:class="[
										stage.status === 'completed'
											? 'bg-primary text-white'
											: onboardingData?.currentStageId === stage.stageId
											? 'bg-primary-500 text-white'
											: 'bg-[var(--el-bg-color-page)] dark:bg-black',
									]"
									:title="stage?.status"
								>
									<el-icon v-if="stage.status === 'completed'" class="text-xs">
										<Check />
									</el-icon>
									<el-icon
										v-else-if="onboardingData?.currentStageId === stage.stageId"
										class="text-xs"
									>
										<Clock />
									</el-icon>
									<Icon
										v-else-if="stage.status == 'skipped'"
										icon="mdi:transit-skip"
										class="rotate-180"
									/>
									<text v-else class="text-xs font-bold leading-6">
										{{ stage.order }}
									</text>
								</div>

								<div class="flex-1 min-w-0">
									<div class="flex items-center justify-between">
										<h3 class="font-medium text-gray-900">
											{{ stage.name }}
										</h3>
										<div class="flex items-center space-x-2">
											<!-- 状态标签 -->
											<span
												:class="[
													'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border',
													stage.status === 'completed' &&
														'bg-primary text-white border-gray-200',
													stage.status === 'in_progress' &&
														'bg-primary text-white border-gray-200',
													stage.status === 'skipped' &&
														'bg-slate-100 text-slate-500 border-slate-300 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-500',
													stage.status === 'pending' &&
														'bg-gray-100 text-gray-500 border-gray-200',
												]"
											>
												<Icon
													v-if="stage.status === 'skipped'"
													icon="mdi:skip-forward"
													class="h-3 w-3 mr-1"
												/>
												{{ getStageStatusText(stage.status) }}
											</span>
											<!-- Required + Skipped 特殊标签 -->
											<el-tooltip
												v-if="stage.required && stage.status === 'skipped'"
												content="This required stage was skipped"
												placement="top"
											>
												<span
													class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-slate-50 text-slate-500 border border-slate-400 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-500"
												>
													<Icon
														v-if="stage.status === 'skipped'"
														icon="mdi:skip-forward"
														class="h-3 w-3 mr-1"
													/>
													Skipped Required
												</span>
											</el-tooltip>
											<!-- Required 标签 (非跳过状态) -->
											<el-tooltip
												v-else-if="stage.required"
												content="Users must complete this stage before proceeding"
												placement="top"
											>
												<span
													class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-orange-50 text-orange-500 border border-orange-400 dark:bg-orange-900 dark:text-orange-300"
												>
													<Icon
														icon="mdi:information-outline"
														class="h-3 w-3 mr-1"
													/>
													Required
												</span>
											</el-tooltip>
											<el-button
												v-if="
													stage.editable &&
													(stage.status === 'completed' ||
														stage.status === 'in_progress')
												"
												@click="handleStageAction(stage)"
												:disabled="!isStageEditable"
												type="primary"
											>
												<Icon
													v-if="stage.status === 'completed'"
													icon="mdi:eye-outline"
													class="mr-1 h-3 w-3"
												/>
												<Icon
													v-else
													icon="mdi:pencil-outline"
													class="mr-1 h-3 w-3"
												/>
												{{
													stage.status === 'completed'
														? 'View'
														: 'Continue'
												}}
											</el-button>
										</div>
									</div>
									<p class="text-sm text-gray-600 mt-1">
										{{ stage.description }}
									</p>
									<p
										v-if="stage.completedDate"
										class="text-xs text-primary-600 mt-1"
									>
										Completed on {{ stage.completedDate }}
									</p>
								</div>
							</div>
						</div>
					</div>
				</div>

				<!-- Other Views -->
				<MessageCenter v-else-if="currentView === 'messages'" />
				<DocumentCenter v-else-if="currentView === 'documents'" />
			</main>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { Check, Clock } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { getOnboardingByLead } from '@/apis/ow/onboarding';
import { timeZoneConvert } from '@/hooks/time';
import MessageCenter from './components/MessageCenter.vue';
import DocumentCenter from './components/DocumentCenter.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import GradientTag from '@/components/global/GradientTag/index.vue';
import { defaultStr } from '@/settings/projectSetting';
import { Stage } from '#/onboard';

// 类型定义
interface NavigationItem {
	name: string;
	view: string;
	icon: string;
}

const route = useRoute();
const router = useRouter();

// 响应式数据
const sidebarOpen = ref<boolean>(false);
const currentView = ref<string>('progress');
const loading = ref<boolean>(true);
const onboardingData = ref<any>(null);

// 导航菜单
const navigation = ref<NavigationItem[]>([
	{
		name: 'Case Progress',
		view: 'progress',
		icon: 'mdi:home-outline',
	},
	{
		name: 'Case Detail',
		view: 'detail',
		icon: 'mdi:file-document-outline',
	},
]);

// 从路由参数获取 onboardingId
const onboardingId = computed<string>(() => {
	const id = route.query.onboardingId;
	if (Array.isArray(id)) {
		return id[0] || '';
	}
	return id || '';
});

// 加载 onboarding 数据
const loadOnboardingData = async () => {
	try {
		loading.value = true;
		const response = await getOnboardingByLead(onboardingId.value, true);
		if (response.code === '200') {
			onboardingData.value = response.data;
		} else {
			ElMessage.error('Failed to load onboarding data');
		}
	} finally {
		loading.value = false;
	}
};

// 计算属性 - 客户数据
const customerData = computed(() => {
	const data = onboardingData.value;
	return {
		...data,
		id: data?.leadId,
		companyName: data?.caseName,
		contactName: data?.contactPerson,
		email: data?.contactEmail,
		currentStage: data?.currentStageName,
		overallProgress: Math.round(data?.completionRate || 0),
		startDate: data?.startDate ? timeZoneConvert(data?.startDate) : '',
		estimatedCompletion: timeZoneConvert(
			data?.estimatedCompletionDate || data?.targetCompletionDate || ''
		),
		accountManager: data?.ownershipName || data?.createBy || '',
		onboardingId: data?.id,
	};
});

// 计算属性 - 客户阶段
const customerStages = computed(() => {
	if (!onboardingData.value || !onboardingData.value.stagesProgress) {
		return [];
	}

	// 只显示在Portal中可见的阶段
	const visibleStages = onboardingData.value.stagesProgress.filter(
		(stage: any) => stage.visibleInPortal !== false
	); // 默认显示，除非明确设置为false

	// 找到第一个未完成的阶段作为当前阶段
	let currentStageFound = false;

	return visibleStages.map((stage: any, index: number) => {
		// 检查是否被跳过
		const isSkipped = stage.status === 'Skipped';

		// 根据 stage.status 和 isCompleted 确定状态
		let status = 'pending';
		if (isSkipped) {
			status = 'skipped';
		} else if (stage.isCompleted) {
			status = 'completed';
		} else if (stage.isCurrent) {
			status = 'in_progress';
			currentStageFound = true;
		} else if (!currentStageFound && !stage.isCompleted) {
			// 如果还没找到当前阶段，且这个阶段未完成，则设为当前阶段
			status = 'in_progress';
			currentStageFound = true;
		}

		return {
			...stage,
			id: stage.stageId,
			name: stage.stageName,
			description: stage.stageDescription || stage.stageName, // 优先使用阶段描述，如果没有则使用阶段名称
			order: index + 1, // 从1开始重新编号，而不是使用原始的 stageOrder
			originalOrder: stage.stageOrder, // 保留原始顺序用于后端交互
			status: status,
			isSkipped: isSkipped,
			required: stage.required || false, // 是否必填
			editable: status !== 'completed' && status !== 'skipped', // 已完成或跳过的不可编辑
			completedDate: stage.completionTime ? timeZoneConvert(stage.completionTime) : null,
			portalVisible: true,
			portalEditable: status !== 'completed' && status !== 'skipped',
			estimatedDays: stage.estimatedDays,
			actualDays: stage.actualDays,
			startTime: stage.startTime,
			completionTime: stage.completionTime,
			components: stage.components || [],
		};
	});
});

// 其他计算属性
const currentStageData = computed(() => {
	return customerStages.value.find((stage) => stage.status === 'in_progress');
});

const completedStages = computed(() => {
	return customerStages.value.filter(
		(stage) => stage.status === 'completed' || stage.status === 'skipped'
	).length;
});

const totalStages = computed(() => {
	return customerStages.value.length;
});

const progressPercentage = computed(() => {
	// 基于可见阶段重新计算进度百分比
	if (customerStages.value.length === 0) {
		return 0;
	}
	const completedVisibleStages = customerStages.value.filter(
		(stage) => stage.status === 'completed' || stage.status === 'skipped'
	).length;
	return Math.round((completedVisibleStages / customerStages.value.length) * 100);
});

const nextSteps = computed(() => {
	// 优先返回当前进行中的阶段，如果没有则返回第一个待处理的阶段
	const inProgressStage = customerStages.value.find((stage) => stage.status === 'in_progress');
	if (inProgressStage) {
		return [inProgressStage];
	}

	// 如果没有进行中的阶段，找到第一个待处理且可编辑的阶段
	const nextPendingStage = customerStages.value.find(
		(stage) => stage.status === 'pending' && stage.editable
	);
	return nextPendingStage ? [nextPendingStage] : [];
});

// 状态显示映射 - 与portal.vue保持一致
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

// 计算是否禁用编辑按钮 - 与portal.vue保持一致
const isStageEditable = computed(() => {
	const status = onboardingData.value?.status;
	if (!status) return true;

	// 对于Aborted/Cancelled/Paused/Force Completed状态，禁用编辑
	return !['Aborted', 'Cancelled', 'Paused', 'Force Completed'].includes(status);
});

// 方法
const getStageStatusText = (status: string): string => {
	switch (status) {
		case 'completed':
			return 'Completed';
		case 'in_progress':
			return 'In Progress';
		case 'skipped':
			return 'Skipped';
		default:
			return 'Pending';
	}
};

const handleNavigation = (view: string): void => {
	if (view === 'detail') {
		// Navigate to portal page with onboardingId
		router.push({
			path: '/onboard/sub-portal/portal',
			query: {
				onboardingId: onboardingId.value,
			},
		});
	} else {
		currentView.value = view;
	}
};

const handleStageAction = (stage: Stage): void => {
	// 检查状态是否允许编辑
	if (!isStageEditable.value) {
		ElMessage.warning('This case cannot be edited in its current status');
		return;
	}

	// Navigate to portal page with specific onboardingId
	router.push({
		path: '/onboard/sub-portal/portal',
		query: {
			onboardingId: customerData.value.onboardingId,
			stageId: stage.id,
		},
	});
};

// 生命周期
onMounted(() => {
	// 首先检查URL中的token参数，如果存在则进行portal验证
	checkUrlTokenAndVerify();
});

// 检查URL中的token参数并自动验证
const checkUrlTokenAndVerify = async () => {
	const urlToken = route.query.token;
	const portalAccessToken = localStorage.getItem('portal_access_token');

	// 如果URL中有token但localStorage中没有portal_access_token，进行自动验证
	if (urlToken && !portalAccessToken) {
		try {
			// 这里我们需要知道用户的邮箱才能验证
			// 但是URL中没有邮箱信息，所以我们需要重定向到portal-access页面
			router.replace({
				path: '/portal-access',
				query: { token: urlToken },
			});
			return;
		} catch (error) {
			console.error('Auto verification failed:', error);
			// 如果自动验证失败，也重定向到portal-access页面
			router.replace({
				path: '/portal-access',
				query: { token: urlToken },
			});
			return;
		}
	}

	// 如果已经有portal_access_token或者没有token参数，正常加载数据
	loadOnboardingData();
};
</script>

<style scoped lang="scss">
.portal-nav-active {
	@apply bg-primary-500 text-white;
}

.portal-company-icon {
	background-color: var(--el-color-primary-light-9);
	padding: 0.5rem;
	border-radius: 9999px;
	color: var(--el-color-primary);
}
</style>
