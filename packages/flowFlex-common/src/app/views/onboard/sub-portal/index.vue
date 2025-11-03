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
					<button @click="sidebarOpen = false" class="p-1 rounded-xl hover:bg-gray-100">
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
							'group flex items-center px-2 py-2 text-sm font-medium rounded-xl cursor-pointer',
							currentView === item.view
								? 'bg-primary-100 text-primary-900'
								: 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
						]"
						@click="
							handleNavigation(item.view);
							sidebarOpen = false;
						"
					>
						<component :is="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-xl border bg-siderbarGray dark:bg-black p-4 shadow-sm">
						<div class="flex items-center space-x-3">
							<div class="bg-primary-100 p-2 rounded-full">
								<svg
									class="h-5 w-5 text-primary-600"
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
			<div
				class="flex flex-col flex-grow bg-siderbarGray dark:bg-black border-r border-gray-200"
			>
				<div class="flex h-16 items-center px-4 border-b">
					<h1 class="text-xl font-bold text-primary">Customer Portal</h1>
				</div>
				<nav class="flex-1 space-y-1 px-2 py-4">
					<div
						v-for="item in navigation"
						:key="item.name"
						:class="[
							'group flex items-center px-2 py-2 text-sm font-medium rounded-xl cursor-pointer',
							currentView === item.view
								? 'bg-primary-100 text-primary-900'
								: 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
						]"
						@click="handleNavigation(item.view)"
					>
						<component :is="item.icon" class="mr-3 h-5 w-5" />
						{{ item.name }}
					</div>
				</nav>

				<!-- Customer Info Card -->
				<div class="p-4 border-t">
					<div class="rounded-xl border bg-siderbarGray dark:bg-black p-4 shadow-sm">
						<div class="flex items-center space-x-3 mb-3">
							<div class="bg-primary-100 p-2 rounded-full">
								<svg
									class="h-5 w-5 text-primary-600"
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
				<button @click="sidebarOpen = true" class="p-1 rounded-xl hover:bg-gray-100">
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
								<svg
									class="mr-2 h-5 w-5 text-gray-700"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
									/>
								</svg>
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
									<svg
										class="h-4 w-4"
										fill="none"
										stroke="currentColor"
										viewBox="0 0 24 24"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
										/>
									</svg>
									<span>Started: {{ customerData.startDate }}</span>
								</div>
								<div class="flex items-center space-x-2"></div>
								<div class="flex items-center space-x-2">
									<svg
										class="h-4 w-4"
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
									<span>
										{{ completedStages }} of {{ totalStages }} stages completed
									</span>
								</div>
							</div>
							<div v-if="currentStageData" class="mt-4 p-3 bg-primary-50 rounded-xl">
								<p class="text-sm font-medium text-primary-900">
									Current Stage: {{ currentStageData.name }}
								</p>
								<p class="text-sm text-primary-700">
									{{ currentStageData.description }}
								</p>
							</div>
						</div>
					</div>

					<!-- Next Steps - Action Required -->
					<div v-if="!loading" class="rounded-xl bg-white dark:bg-black p-6">
						<div class="mb-6">
							<div class="flex items-center mb-2 text-white">
								<svg
									class="mr-2 h-5 w-5 text-white"
									fill="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"
									/>
								</svg>
								<h3 class="text-lg font-semibold">Next Steps - Action Required</h3>
							</div>
							<p class="text-sm text-white">
								Complete these steps to continue your onboarding process
							</p>
						</div>
						<div class="space-y-3">
							<div
								v-for="stage in nextSteps"
								:key="stage.id"
								class="flex items-center justify-between p-4 bg-white dark:bg-black rounded-xl border shadow-sm"
							>
								<div class="flex items-center space-x-3">
									<div class="flex-shrink-0">
										<div
											class="w-8 h-8 rounded-full flex items-center justify-center text-white font-bold text-sm"
											:style="{ backgroundColor: stage.color }"
										>
											<svg
												class="h-4 w-4"
												fill="none"
												stroke="currentColor"
												viewBox="0 0 24 24"
											>
												<path
													stroke-linecap="round"
													stroke-linejoin="round"
													stroke-width="2"
													d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
												/>
											</svg>
										</div>
									</div>
									<div>
										<p class="font-medium text-gray-900">{{ stage.name }}</p>
										<p class="text-sm text-gray-600">{{ stage.description }}</p>
										<span
											class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary text-white mt-1"
										>
											In Progress
										</span>
									</div>
								</div>
								<div class="flex items-center space-x-2">
									<button
										@click="handleStageAction(stage)"
										:disabled="!isStageEditable"
										:class="[
											'inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-orange-500',
											isStageEditable
												? 'bg-primary hover:bg-primary cursor-pointer'
												: 'bg-gray-400 cursor-not-allowed opacity-60',
										]"
									>
										Continue
										<svg
											class="ml-2 h-4 w-4"
											fill="none"
											stroke="currentColor"
											viewBox="0 0 24 24"
										>
											<path
												stroke-linecap="round"
												stroke-linejoin="round"
												stroke-width="2"
												d="M9 5l7 7-7 7"
											/>
										</svg>
									</button>
								</div>
							</div>
							<div v-if="nextSteps.length === 0" class="text-center py-6">
								<svg
									class="h-12 w-12 text-green-500 mx-auto mb-3"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
									/>
								</svg>
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
									'flex items-start space-x-4 p-4 rounded-xl border transition-colors',
									stage.status === 'completed' &&
										'bg-white dark:bg-black border-gray-200',
									stage.status === 'in_progress' &&
										'bg-white dark:bg-black border-gray-200',
									stage.status === 'pending' &&
										'bg-white dark:bg-black border-gray-200',
								]"
							>
								<div class="flex-shrink-0 mt-1">
									<svg
										v-if="stage.status === 'completed'"
										class="h-5 w-5 text-primary-500"
										fill="none"
										stroke="currentColor"
										viewBox="0 0 24 24"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
										/>
									</svg>
									<svg
										v-else-if="stage.status === 'in_progress'"
										class="h-5 w-5 text-primary-500"
										fill="none"
										stroke="currentColor"
										viewBox="0 0 24 24"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
										/>
									</svg>
									<div
										v-else
										class="h-5 w-5 rounded-full border-2 border-primary-200"
									></div>
								</div>
								<div class="flex-1 min-w-0">
									<div class="flex items-center justify-between">
										<h3 class="font-medium text-gray-900">
											{{ stage.order }}. {{ stage.name }}
										</h3>
										<div class="flex items-center space-x-2">
											<span
												:class="[
													'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border',
													stage.status === 'completed' &&
														'bg-primary text-white border-gray-200',
													stage.status === 'in_progress' &&
														'bg-primary text-white border-gray-200',
													stage.status === 'pending' &&
														'bg-primary text-white border-gray-200',
												]"
											>
												{{ getStageStatusText(stage.status) }}
											</span>
											<button
												v-if="
													stage.editable &&
													(stage.status === 'completed' ||
														stage.status === 'in_progress')
												"
												@click="handleStageAction(stage)"
												:disabled="!isStageEditable"
												:class="[
													'inline-flex items-center px-3 py-1 border text-sm font-medium rounded-xl focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500',
													isStageEditable
														? 'border-gray-200 text-gray-700 bg-gray-50 hover:bg-primary cursor-pointer'
														: 'border-gray-300 text-gray-400 bg-gray-100 cursor-not-allowed opacity-60',
												]"
											>
												<svg
													v-if="stage.status === 'completed'"
													class="mr-1 h-3 w-3"
													fill="none"
													stroke="currentColor"
													viewBox="0 0 24 24"
												>
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
													/>
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
													/>
												</svg>
												<svg
													v-else
													class="mr-1 h-3 w-3"
													fill="none"
													stroke="currentColor"
													viewBox="0 0 24 24"
												>
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
													/>
												</svg>
												{{
													stage.status === 'completed'
														? 'View'
														: 'Continue'
												}}
											</button>
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
									<div
										v-if="stage.status === 'in_progress'"
										class="mt-2 p-2 bg-primary rounded-xl text-sm text-white flex items-center"
									>
										<svg
											class="h-4 w-4 mr-1"
											fill="none"
											stroke="currentColor"
											viewBox="0 0 24 24"
										>
											<path
												stroke-linecap="round"
												stroke-linejoin="round"
												stroke-width="2"
												d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
											/>
										</svg>
										Action required: Please complete this stage to continue
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>

				<!-- Other Views -->
				<MessageCenter v-else-if="currentView === 'messages'" />
				<DocumentCenter v-else-if="currentView === 'documents'" />
				<ContactUs v-else-if="currentView === 'contact'" />
			</main>
		</div>
	</div>
</template>

<script>
import { computed, ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { getOnboardingByLead } from '@/apis/ow/onboarding';
import { formatDateUS } from '@/hooks/time';
import MessageCenter from './components/MessageCenter.vue';
import DocumentCenter from './components/DocumentCenter.vue';
import ContactUs from './components/ContactUs.vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import GradientTag from '@/components/global/GradientTag/index.vue';
import { defaultStr } from '@/settings/projectSetting';

// Icon components
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

const MessageSquareIcon = {
	template: `
		<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
		</svg>
	`,
};

const FileTextIcon = {
	template: `
		<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
		</svg>
	`,
};

const PhoneIcon = {
	template: `
		<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
		</svg>
	`,
};

export default {
	name: 'OnboardPortal', // Change name to avoid conflicts
	components: {
		MessageCenter,
		DocumentCenter,
		ContactUs,
		PageHeader,
		GradientTag,
		HomeIcon,
		DetailsIcon,
		MessageSquareIcon,
		FileTextIcon,
		PhoneIcon,
	},
	setup() {
		const route = useRoute();
		const router = useRouter();

		// 响应式数据
		const sidebarOpen = ref(false);
		const currentView = ref('progress');
		const loading = ref(true);
		const onboardingData = ref(null);

		// 导航菜单
		const navigation = ref([
			{
				name: 'Case Progress',
				view: 'progress',
				icon: HomeIcon,
			},
			{
				name: 'Case Detail',
				view: 'detail',
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

		// 从路由参数获取 onboardingId
		const onboardingId = computed(() => {
			return route.query.onboardingId || '1945406045400731649';
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
			if (!onboardingData.value) {
				return {
					id: 'CUST-001',
					companyName: 'Loading...',
					contactName: 'Loading...',
					email: '',
					phone: '',
					currentStage: '',
					overallProgress: 0,
					startDate: '',
					estimatedCompletion: '',
					accountManager: '',
					onboardingId: onboardingId.value,
				};
			}

			const data = onboardingData.value;
			return {
				id: data.leadId,
				companyName: data.leadName,
				contactName: data.contactPerson,
				email: data.contactEmail,
				phone: '',
				currentStage: data.currentStageName,
				overallProgress: Math.round(data.completionRate || 0),
				startDate: data.startDate ? formatDateUS(data.startDate) : '',
				estimatedCompletion: data.estimatedCompletionDate
					? formatDateUS(data.estimatedCompletionDate)
					: data.targetCompletionDate
					? formatDateUS(data.targetCompletionDate)
					: '',
				accountManager: data.stageUpdatedBy || '',
				onboardingId: data.id,
			};
		});

		// 计算属性 - 客户阶段
		const customerStages = computed(() => {
			if (!onboardingData.value || !onboardingData.value.stagesProgress) {
				return [];
			}

			// 只显示在Portal中可见的阶段
			const visibleStages = onboardingData.value.stagesProgress.filter(
				(stage) => stage.visibleInPortal !== false
			); // 默认显示，除非明确设置为false

			// 找到第一个未完成的阶段作为当前阶段
			let currentStageFound = false;

			return visibleStages.map((stage, index) => {
				// 根据 stage.status 和 isCompleted 确定状态
				let status = 'pending';
				if (stage.isCompleted) {
					status = 'completed';
				} else if (stage.isCurrent) {
					status = 'in_progress';
					currentStageFound = true;
				} else if (!currentStageFound && !stage.isCompleted) {
					// 如果还没找到当前阶段，且这个阶段未完成，则设为当前阶段
					status = 'in_progress';
					currentStageFound = true;
				}

				// 为每个阶段分配颜色
				const colors = [
					'#4f46e5',
					'#0ea5e9',
					'#10b981',
					'#f59e0b',
					'#ec4899',
					'#8b5cf6',
					'#06b6d4',
					'#14b8a6',
					'#22c55e',
					'#a855f7',
					'#ef4444',
					'#84cc16',
					'#10b981',
					'#0ea5e9',
					'#4f46e5',
					'#22c55e',
				];

				return {
					id: stage.stageId,
					name: stage.stageName,
					description: stage.stageDescription || stage.stageName, // 优先使用阶段描述，如果没有则使用阶段名称
					order: index + 1, // 从1开始重新编号，而不是使用原始的 stageOrder
					originalOrder: stage.stageOrder, // 保留原始顺序用于后端交互
					status: status,
					editable: status !== 'completed', // 简化条件：只要未完成就可编辑
					color: colors[index % colors.length],
					completedDate: stage.completionTime ? formatDateUS(stage.completionTime) : null,
					portalVisible: true,
					portalEditable: status !== 'completed',
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
			return customerStages.value.filter((stage) => stage.status === 'completed').length;
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
				(stage) => stage.status === 'completed'
			).length;
			return Math.round((completedVisibleStages / customerStages.value.length) * 100);
		});

		const nextSteps = computed(() => {
			// 优先返回当前进行中的阶段，如果没有则返回第一个待处理的阶段
			const inProgressStage = customerStages.value.find(
				(stage) => stage.status === 'in_progress'
			);
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
		const getStageStatusText = (status) => {
			switch (status) {
				case 'completed':
					return 'Completed';
				case 'in_progress':
					return 'In Progress';
				default:
					return 'Pending';
			}
		};

		const handleNavigation = (view) => {
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

		const handleStageAction = (stage) => {
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

		// 返回所有需要在模板中使用的数据和方法
		return {
			sidebarOpen,
			currentView,
			loading,
			navigation,
			customerData,
			customerStages,
			currentStageData,
			completedStages,
			totalStages,
			progressPercentage,
			nextSteps,
			getStageStatusText,
			handleNavigation,
			handleStageAction,
			loadOnboardingData,
			onboardingData,
			statusTagType,
			statusDisplayText,
			statusShouldPulse,
			isStageEditable,
		};
	},
};
</script>

<style scoped>
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
</style>
