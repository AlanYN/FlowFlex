<template>
	<div>
		<!-- 加载状态 -->
		<div v-if="loading" class="flex justify-center items-center py-12">
			<el-icon class="animate-spin h-8 w-8 text-primary-500">
				<Loading />
			</el-icon>
			<span class="ml-2 text-primary-600">Loading workflows...</span>
		</div>

		<!-- 工作流卡片网格 -->
		<div v-else class="workflow-grid">
			<template v-if="workflows.length > 0">
				<el-card
					v-for="workflow in workflows"
					:key="workflow.id"
					class="workflow-card overflow-hidden transition-all cursor-pointer hover:shadow-lg"
				>
					<!-- 卡片头部 -->
					<template #header>
						<div class="card-header -m-5 p-4">
							<div class="flex items-center justify-between w-full">
								<div
									class="flex items-center space-x-3 flex-1 min-w-0"
									:class="
										functionPermission(ProjectPermissionEnum.workflow.read)
											? 'cursor-pointer'
											: 'cursor-not-allowed'
									"
									@click="handleWorkflowSelect(workflow)"
								>
									<div
										class="card-icon rounded-full flex-shrink-0 flex items-center justify-center"
									>
										<Icon
											icon="material-symbols:account-tree-outline"
											class="w-6 h-6"
										/>
									</div>
									<div class="flex-1 min-w-0">
										<h3
											class="card-title text-xl font-semibold leading-tight tracking-tight truncate"
											:title="workflow.name"
										>
											{{ workflow.name }}
										</h3>
									</div>
								</div>
								<el-dropdown
									trigger="click"
									@click.stop
									@command="(cmd) => handleCommand(cmd, workflow)"
									class="flex-shrink-0"
									:disabled="isWorkflowActionLoading(workflow.id)"
								>
									<el-button
										text
										class="card-more-btn"
										link
										:loading="isWorkflowActionLoading(workflow.id)"
									>
										<el-icon
											v-if="!isWorkflowActionLoading(workflow.id)"
											class="h-4 w-4"
										>
											<MoreFilled />
										</el-icon>
									</el-button>
									<template #dropdown>
										<el-dropdown-menu>
											<el-dropdown-item
												v-if="
													hasPermission(
														workflow.id,
														ProjectPermissionEnum.workflow.update
													)
												"
												command="edit"
											>
												<el-icon class="mr-2"><Edit /></el-icon>
												Edit Workflow
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													!workflow.isDefault &&
													workflow.status === 'active' &&
													hasPermission(
														workflow.id,
														ProjectPermissionEnum.workflow.update
													)
												"
												command="setDefault"
											>
												<el-icon class="mr-2"><Star /></el-icon>
												Set as Default
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													workflow.status === 'active' &&
													hasPermission(
														workflow.id,
														ProjectPermissionEnum.workflow.update
													)
												"
												command="deactivate"
											>
												<el-icon class="mr-2"><CircleClose /></el-icon>
												Set as Inactive
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													workflow.status === 'inactive' &&
													hasPermission(
														workflow.id,
														ProjectPermissionEnum.workflow.update
													)
												"
												command="activate"
											>
												<el-icon class="mr-2"><Check /></el-icon>
												Set as Active
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													hasPermission(
														workflow.id,
														ProjectPermissionEnum.workflow.create
													)
												"
												command="duplicate"
											>
												<el-icon class="mr-2"><CopyDocument /></el-icon>
												Duplicate
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													functionPermission(
														ProjectPermissionEnum.workflow.read
													)
												"
												divided
											>
												<HistoryButton
													:id="workflow.id"
													:type="WFEMoudels.Workflow"
												/>
											</el-dropdown-item>
											<el-dropdown-item
												v-if="
													functionPermission(
														ProjectPermissionEnum.workflow.read
													)
												"
												command="export"
											>
												<el-icon class="mr-2"><Download /></el-icon>
												Export Workflow
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</div>
							<p class="text-white text-sm mt-1.5 truncate h-6">
								{{ workflow.description }}
							</p>
						</div>
					</template>

					<!-- 卡片内容 -->
					<div class="">
						<div class="space-y-3">
							<!-- Stages区域 -->
							<StagesDisplay :stages="workflow.stages" container-height="60px" />
							<!-- 状态标签区域 -->
							<div class="flex items-center gap-2 text-sm">
								<el-tag v-if="workflow.isAIGenerated" type="primary" size="small">
									<div class="flex items-center gap-1">
										<span class="ai-sparkles">✨</span>
										AI
									</div>
								</el-tag>
								<el-tag v-if="workflow.isDefault" type="warning" size="small">
									<div class="flex items-center gap-1">
										<StarIcon />
										Default
									</div>
								</el-tag>
								<el-tag
									v-if="workflow.status === 'active'"
									type="success"
									size="small"
								>
									Active
								</el-tag>
								<el-tag v-else type="danger" size="small">Inactive</el-tag>
							</div>
							<div class="flex items-center justify-between text-sm">
								<el-tooltip class="flex-1" content="last modify by">
									<div class="flex flex-1 items-center gap-2">
										<Icon
											icon="ic:baseline-person-3"
											class="text-primary-500 w-5 h-5"
										/>
										<span class="font-medium">
											{{ workflow.modifyBy || 'Unknown' }}
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
													workflow.modifyDate,
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
			v-if="workflows.length === 0 && !loading"
			class="empty-state flex flex-col items-center justify-center py-12 text-center rounded-xl shadow-sm"
		>
			<div class="empty-icon-bg p-4 rounded-full mb-4">
				<el-icon class="h-12 w-12 empty-icon"><DocumentAdd /></el-icon>
			</div>
			<h3 class="text-lg font-medium empty-title">No workflows found</h3>
			<p class="empty-subtitle mt-1 mb-4">
				{{ emptyMessage }}
			</p>
			<el-button type="primary" @click="$emit('new-workflow')">
				<el-icon class="mr-2"><Plus /></el-icon>
				Create Your First Workflow
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import {
	Plus,
	Edit,
	CopyDocument,
	DocumentAdd,
	MoreFilled,
	Loading,
	Star,
	CircleClose,
	Check,
	Download,
} from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinuteDate } from '@/settings/projectSetting';
import { WFEMoudels } from '@/enums/appEnum';
import StarIcon from '@assets/svg/workflow/star.svg';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';
import { functionPermission } from '@/hooks';
import StagesDisplay from '@/components/common/StagesDisplay.vue';

// Props
const props = defineProps<{
	workflows: any[];
	loading: boolean;
	emptyMessage: string;
	actionLoading?: {
		[workflowId: string]: {
			[action: string]: boolean;
		};
	};
}>();

// Emits
const emit = defineEmits<{
	command: [command: string, workflow: any];
	'select-workflow': [workflowId: string];
	'new-workflow': [];
}>();

// 检查是否有权限（功能权限 && 数据权限）
const hasPermission = (workflowId: string, functionalPermission: string) => {
	// 从 workflows 列表中查找对应的 workflow
	const workflow = props.workflows.find((w) => w.id === workflowId);
	if (workflow && workflow.permission) {
		return functionPermission(functionalPermission) && workflow.permission.canOperate;
	}
	return functionPermission(functionalPermission);
};

// Methods
const handleCommand = (command: string, workflow: any) => {
	emit('command', command, workflow);
};

const handleWorkflowSelect = (workflow: any) => {
	if (!functionPermission(ProjectPermissionEnum.workflow.read)) return;
	emit('select-workflow', workflow.id);
};

// 检查workflow是否有任何操作正在loading
const isWorkflowActionLoading = (workflowId: string) => {
	if (!props.actionLoading || !props.actionLoading[workflowId]) {
		return false;
	}

	const workflowActions = props.actionLoading[workflowId];
	return Object.values(workflowActions).some((loading) => loading);
};
</script>

<style scoped lang="scss">
/* 工作流卡片网格布局 */
.workflow-grid {
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
	& > .workflow-card {
		max-width: 600px;
		width: 100%;
	}
}

.ai-sparkles {
	font-size: 12px;
	animation: sparkle 2s ease-in-out infinite;
	display: inline-block;
}

@keyframes sparkle {
	0%,
	100% {
		transform: scale(1) rotate(0deg);
		opacity: 1;
	}
	25% {
		transform: scale(1.1) rotate(5deg);
		opacity: 0.9;
	}
	50% {
		transform: scale(1.2) rotate(-5deg);
		opacity: 0.8;
	}
	75% {
		transform: scale(1.1) rotate(3deg);
		opacity: 0.9;
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
