<template>
	<div>
		<!-- 加载状态 -->
		<div v-if="loading" class="flex justify-center items-center py-12">
			<el-icon class="animate-spin h-8 w-8 text-primary-500">
				<Loading />
			</el-icon>
			<span class="ml-2 text-primary-600">Loading questionnaires...</span>
		</div>

		<!-- 问卷卡片网格 -->
		<div v-else class="questionnaire-grid">
			<template v-if="questionnaires.length > 0">
				<el-card
					v-for="questionnaire in questionnaires"
					:key="questionnaire.id"
					class="questionnaire-card overflow-hidden transition-all"
				>
					<!-- 卡片头部 -->
					<template #header>
						<div class="card-header -m-5 p-4">
							<div class="flex items-center justify-between w-full">
								<div class="flex items-center space-x-3 flex-1 min-w-0">
									<div
										class="card-icon rounded-full flex-shrink-0 flex items-center justify-center"
									>
										<Icon
											icon="material-symbols:edit-document-outline"
											class="w-6 h-6"
										/>
									</div>
									<h3
										class="card-title text-xl font-semibold leading-tight tracking-tight truncate"
										:title="questionnaire.name"
									>
										{{ questionnaire.name }}
									</h3>
								</div>
								<el-dropdown
									trigger="click"
									@command="(cmd) => handleCommand(cmd, questionnaire)"
									class="flex-shrink-0"
								>
									<el-button text class="card-more-btn" link>
										<el-icon class="h-4 w-4"><MoreFilled /></el-icon>
									</el-button>
									<template #dropdown>
										<el-dropdown-menu>
											<el-dropdown-item
												command="edit"
												v-if="
													functionPermission(
														ProjectPermissionEnum.question.update
													)
												"
											>
												<el-icon class="mr-2"><Edit /></el-icon>
												Edit
											</el-dropdown-item>
											<el-dropdown-item
												command="preview"
												v-if="
													functionPermission(
														ProjectPermissionEnum.question.read
													)
												"
											>
												<el-icon class="mr-2"><View /></el-icon>
												Preview
											</el-dropdown-item>
											<el-dropdown-item
												command="duplicate"
												v-if="
													functionPermission(
														ProjectPermissionEnum.question.create
													)
												"
											>
												<el-icon class="mr-2"><CopyDocument /></el-icon>
												Duplicate
											</el-dropdown-item>
											<el-divider class="my-0" />
											<el-dropdown-item
												v-if="
													functionPermission(
														ProjectPermissionEnum.question.read
													)
												"
											>
												<HistoryButton
													:id="questionnaire.id"
													:type="WFEMoudels.Questionnaire"
												/>
											</el-dropdown-item>
											<el-divider class="my-0" />
											<el-dropdown-item
												command="delete"
												v-if="
													functionPermission(
														ProjectPermissionEnum.question.delete
													)
												"
												class="text-red-500 hover:!bg-red-500 hover:!text-white"
											>
												<el-icon class="mr-2"><Delete /></el-icon>
												Delete
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</div>
							<p class="text-white text-sm mt-1.5 truncate h-6">
								{{ questionnaire.description }}
							</p>
						</div>
					</template>

					<!-- 卡片内容 -->
					<div class="">
						<div class="space-y-3">
							<!-- Assignments区域 -->
							<AssignmentsDisplay
								:assignments="questionnaire.assignments"
								:workflows="workflows"
								:all-stages="allStages"
								container-height="60px"
								:display-count="2"
							/>
							<div class="flex items-center justify-between text-sm">
								<el-tooltip class="flex-1" content="total number of sections">
									<div class="flex flex-1 items-center gap-2">
										<Icon
											icon="material-symbols-light:insert-page-break"
											class="text-primary-500 w-5 h-5"
										/>
										<span class="font-medium">
											{{
												questionnaire.structureJson
													? JSON.parse(
															questionnaire.structureJson
													  )?.sections?.filter(
															(section) => !section.isDefault
													  )?.length || 0
													: 0
											}}
										</span>
									</div>
								</el-tooltip>
								<el-tooltip class="flex-1" content="total number of questions">
									<div class="flex flex-1 items-center gap-2">
										<Icon
											icon="material-symbols:format-list-bulleted"
											class="text-primary-500 w-5 h-5"
										/>
										<span class="font-medium">
											{{ questionnaire.totalQuestions }}
										</span>
									</div>
								</el-tooltip>
							</div>
							<div class="flex items-center justify-between text-sm">
								<el-tooltip class="flex-1" content="last mdify by">
									<div class="flex flex-1 items-center gap-2">
										<Icon
											icon="ic:baseline-person-3"
											class="text-primary-500 w-5 h-5"
										/>
										<span class="font-medium">
											{{ questionnaire.modifyBy }}
										</span>
									</div>
								</el-tooltip>
								<el-tooltip class="flex-1" content="last modify date">
									<div class="flex flex-1 items-center gap-2">
										<Icon
											icon="ic:baseline-calendar-month"
											class="text-primary-500 w-5 h-5"
										/>
										<span class="font-medium">
											{{
												timeZoneConvert(
													questionnaire.modifyDate,
													false,
													projectTenMinuteDate
												)
											}}
										</span>
									</div>
								</el-tooltip>
							</div>
						</div>
					</div>
				</el-card>
			</template>
		</div>

		<!-- 空状态 -->
		<div
			v-if="questionnaires.length === 0 && !loading"
			class="empty-state flex flex-col items-center justify-center py-12 text-center rounded-xl shadow-sm"
		>
			<div class="empty-icon-bg p-4 rounded-full mb-4">
				<el-icon class="h-12 w-12 empty-icon"><Document /></el-icon>
			</div>
			<h3 class="text-lg font-medium empty-title">No questionnaires found</h3>
			<p class="empty-subtitle mt-1 mb-4">
				{{ emptyMessage }}
			</p>
			<el-button type="primary" @click="$emit('new-questionnaire')">
				<el-icon class="mr-2"><Plus /></el-icon>
				Create Your First Questionnaire
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';
import {
	Plus,
	Edit,
	CopyDocument,
	Delete,
	Document,
	MoreFilled,
	View,
	Loading,
} from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate } from '@/settings/projectSetting';
import { WFEMoudels } from '@/enums/appEnum';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';
import AssignmentsDisplay from '@/components/common/AssignmentsDisplay.vue';
// Props
defineProps<{
	questionnaires: any[];
	loading: boolean;
	emptyMessage: string;
	workflows: any[];
	allStages: any[];
}>();

// Emits
const emit = defineEmits<{
	command: [command: string, questionnaire: any];
	'new-questionnaire': [];
}>();

// Methods
const handleCommand = (command: string, questionnaire: any) => {
	emit('command', command, questionnaire);
};

// 这些方法已经移动到 AssignmentsDisplay 组件中
</script>

<style scoped lang="scss">
/* 问卷卡片网格布局 */
.questionnaire-grid {
	display: grid;
	gap: 24px;
	/* 使用auto-fill保持卡片合适宽度，避免过度拉伸 */
	grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
	width: 100%;

	/* 响应式断点调整 - 主要调整gap和minmax，避免使用固定列数 */
	@media (max-width: 480px) {
		/* 超小屏幕：1列，全宽 */
		grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
		gap: 16px;
		padding: 0 8px;
	}

	@media (min-width: 481px) and (max-width: 768px) {
		/* 小屏幕：自适应，但偏向1列 */
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 20px;
	}

	@media (min-width: 769px) and (max-width: 1024px) {
		/* 中等屏幕：自适应，偏向2列 */
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 20px;
	}

	@media (min-width: 1025px) and (max-width: 1400px) {
		/* 大屏幕：自适应，2-3列之间 */
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 24px;
	}

	@media (min-width: 1401px) and (max-width: 1920px) {
		/* 更大屏幕：自适应，偏向3列 */
		grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
		gap: 28px;
	}

	@media (min-width: 1921px) and (max-width: 2560px) {
		/* 超宽屏：自适应，3-4列之间 */
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 32px;
	}

	@media (min-width: 2561px) {
		/* 超大屏幕：自适应，4列以上 */
		grid-template-columns: repeat(auto-fill, minmax(420px, 1fr));
		gap: 32px;
	}

	/* 限制单个卡片的最大宽度，防止过度拉伸 */
	& > .questionnaire-card {
		max-width: 600px;
		width: 100%;
	}
}

/* 空状态样式 */
.empty-state {
	@apply bg-white dark:bg-black-400;
	border: 1px solid var(--primary-100);
	@apply dark:border-black-200;
}

.empty-icon-bg {
	background-color: var(--primary-50);
	@apply dark:bg-primary-800;
}

.empty-icon {
	color: var(--primary-400);
	@apply dark:text-primary-500;
}

.empty-title {
	color: var(--primary-800);
	@apply dark:text-white;
}

.empty-subtitle {
	color: var(--primary-600);
	@apply dark:text-primary-300;
}
</style>
