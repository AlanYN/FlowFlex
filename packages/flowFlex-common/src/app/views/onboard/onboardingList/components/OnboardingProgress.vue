<template>
	<div class="wfe-global-block-bg">
		<!-- 统一的头部卡片 -->
		<div
			class="case-stageList-header rounded-xl"
			:class="{ expanded: isOpen }"
			@click="toggleOpen"
		>
			<div class="flex justify-between">
				<div>
					<div class="flex items-center">
						<el-icon
							class="case-component-expand-icon text-lg mr-2"
							:class="{ rotated: isOpen }"
						>
							<ArrowRight />
						</el-icon>
						<h3 class="case-component-title">Case Progress</h3>
					</div>
					<div class="case-component-subtitle"></div>
				</div>
				<div class="case-component-info">
					<span class="case-component-percentage">{{ progressPercentage }}%</span>
					<span class="case-component-label">Completed</span>
				</div>
			</div>
			<!-- 统一进度条 -->
			<div class="w-full">
				<div class="case-component-bar rounded-xl">
					<div
						class="case-component-fill rounded-xl"
						:style="{ width: `${progressPercentage}%` }"
					></div>
				</div>
			</div>
		</div>

		<!-- 可折叠的内容 -->
		<el-collapse-transition>
			<div v-show="isOpen" class="p-4">
				<!-- View All Stages 切换按钮 -->
				<div class="mb-4">
					<el-button
						class="w-full justify-between flex-row-reverse"
						data-tour="progress-toggle-btn"
						@click="toggleStagesView"
						:icon="showAllStages ? ArrowUp : ArrowDown"
					>
						{{ showAllStages ? 'Show Current Stages' : 'View All Stages' }}
					</el-button>
				</div>

				<!-- 阶段列表 -->
				<el-scrollbar class="pr-4" max-height="384px">
					<div class="space-y-1">
						<div
							v-for="(stage, index) in displayedStages"
							:key="stage.stageId"
							class="flex items-center gap-2 p-3 transition-colors hover:bg-gray-50 dark:hover:bg-indigo-900/20 rounded-xl min-w-0 w-full"
							:class="[
								stage.completed
									? 'border-primary'
									: 'border-gray-300 dark:border-gray-600',
								activeStage === stage.stageId
									? 'bg-indigo-50 dark:bg-indigo-900/30'
									: '',
								index === displayedStages.length - 1 ? '!border-l-0' : '',
								isStageAccessible(stage)
									? 'cursor-pointer hover:bg-gray-50 dark:hover:bg-indigo-900/20'
									: 'cursor-not-allowed opacity-60 hover:bg-gray-100 dark:hover:bg-indigo-900/10',
							]"
							:data-tour="
								activeStage === stage.stageId ? 'progress-active-stage' : undefined
							"
							@click="isStageAccessible(stage) && handleStageClick(stage.stageId)"
							@mouseenter="showStageDetail(stage, $event)"
							@mouseleave="hideStageDetail"
						>
							<!-- 阶段状态图标 -->
							<div
								class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
								:class="[
									stage.completed
										? 'bg-primary text-white'
										: onboardingData?.currentStageId === stage.stageId
										? 'bg-primary-500 text-white'
										: 'bg-[var(--el-bg-color-page)] dark:bg-black',
								]"
								:title="stage?.status"
							>
								<el-icon v-if="stage.completed" class="text-xs">
									<Check />
								</el-icon>
								<el-icon
									v-else-if="onboardingData?.currentStageId === stage.stageId"
									class="text-xs"
								>
									<Clock />
								</el-icon>
								<Icon
									v-else-if="stage.status == 'Skipped'"
									icon="mdi:transit-skip"
									class="rotate-180"
								/>
								<text v-else class="text-xs font-bold leading-6">
									{{ getOriginalStageIndex(stage) + 1 }}
								</text>
							</div>

							<!-- 阶段内容 -->
							<div class="space-y-1 w-full min-w-0">
								<div class="font-medium flex items-start min-w-0">
									<div class="flex-1 min-w-0">
										<div class="flex items-center gap-2 min-w-0">
											<div
												class="text-gray-900 flex gap-x-1 items-center dark:text-white-100 text-sm stage-title-text flex-1 min-w-0"
												:title="stage.title"
											>
												{{ stage.title }}
											</div>
											<!-- Action Tag for completed stages -->
											<div class="flex items-center gap-2 flex-shrink-0">
												<!-- Required + Skipped: 蓝灰色提示样式 -->
												<el-tooltip
													v-if="
														stage.required && stage.status === 'Skipped'
													"
													content="This required stage was skipped"
													placement="top"
												>
													<div
														class="text-slate-500 px-2 border border-slate-400 rounded-xl flex items-center gap-x-2 text-sm bg-slate-50 dark:text-slate-300 dark:border-slate-500 dark:bg-slate-800"
													>
														<Icon icon="mdi:skip-forward" />
														Skipped Required
													</div>
												</el-tooltip>
												<!-- Required: 正常样式 -->
												<el-tooltip
													v-else-if="stage.required"
													content="Users must complete this stage before proceeding to subsequent stages"
													placement="top"
												>
													<div
														class="text-orange-400 px-2 border border-orange-400 rounded-xl flex items-center gap-x-2 text-sm dark:bg-orange-900"
														data-tour="progress-required-tag"
													>
														<Icon icon="mdi:information-outline" />
														Required
													</div>
												</el-tooltip>

												<template
													v-if="
														stage.completed &&
														stage.actions &&
														stage.actions.length > 0
													"
												>
													<ActionTag
														:actions="stage.actions"
														:triggerSourceId="stage.stageId"
														:onboarding-id="onboardingData.id"
														type="warning"
														size="small"
													/>
												</template>
											</div>
										</div>
									</div>
								</div>
								<div
									v-if="stage.completedBy || stage.savedBy"
									class="text-xs text-gray-400 ml-2 min-w-0"
								>
									<span
										class="completion-info-text block min-w-0"
										:title="
											stage.showSaveOrComplete
												? `Saved by ${stage.savedBy} on ${stage.saveTime}`
												: `Completed by ${stage.completedBy} on ${stage.date}`
										"
									>
										{{
											stage.showSaveOrComplete
												? `Saved by ${stage.savedBy} on ${stage.saveTime}`
												: `Completed by ${stage.completedBy} on ${stage.date}`
										}}
									</span>
								</div>
								<!-- Roll Back 按钮：仅对已完成且有权限的 Stage 显示 -->
								<div
									v-if="stage.status === 'Completed' && stage.canRollBack"
									class="mt-1 flex justify-end"
								>
									<el-button
										type="warning"
										size="small"
										@click.stop="handleRollBack(stage)"
									>
										Roll Back
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-scrollbar>
			</div>
		</el-collapse-transition>

		<!-- Roll Back Stage 确认弹窗 -->
		<el-dialog
			v-model="rollBackDialogVisible"
			title="Roll Back Stage"
			width="500px"
			:close-on-click-modal="!rollBackLoading"
			append-to-body
		>
			<div class="space-y-4">
				<p class="text-gray-600">
					This action will reopen
					<strong>{{ rollBackTargetStage?.title }}</strong>
					and set it back to InProgress.
				</p>
				<div>
					<label class="block text-sm text-gray-500 mb-1">Reason (optional)</label>
					<el-input
						v-model="rollBackReason"
						type="textarea"
						:rows="3"
						placeholder="Enter reason (optional)"
						:disabled="rollBackLoading"
					/>
				</div>
			</div>
			<template #footer>
				<el-button @click="rollBackDialogVisible = false" :disabled="rollBackLoading">
					Cancel
				</el-button>
				<el-button type="warning" :loading="rollBackLoading" @click="handleRollBackConfirm">
					Confirm Roll Back
				</el-button>
			</template>
		</el-dialog>

		<!-- Stage 详情 Popover (hover 触发，复用甘特图的 gsp-* 样式) -->
		<el-popover
			v-model:visible="stageDetailVisible"
			:virtual-ref="stageDetailTriggerRef"
			virtual-triggering
			placement="left-start"
			:width="300"
			trigger="manual"
			popper-class="gantt-stage-popover"
		>
			<template v-if="hoveredStage">
				<div
					class="gsp-wrap"
					@mouseenter="cancelHideDetail"
					@mouseleave="handlePopoverMouseLeave"
				>
					<!-- 顶部：Stage 名称 + 状态 -->
					<div class="gsp-header">
						<div class="gsp-header__left">
							<h3 class="gsp-stage-name">{{ hoveredStage.title }}</h3>
						</div>
						<div class="gsp-header__right">
							<!-- 偏差天数 -->
							<span
								v-if="hoveredStageVariance !== 0"
								class="gsp-variance"
								:class="
									hoveredStageVariance > 0
										? 'gsp-variance--late'
										: 'gsp-variance--early'
								"
							>
								{{ hoveredStageVariance > 0 ? '+' : '' }}{{ hoveredStageVariance }}d
							</span>
							<!-- 状态标签 -->
							<el-tag
								:type="getStageTagType(hoveredStage)"
								size="small"
								effect="light"
							>
								{{ getStageStatusLabel(hoveredStage) }}
							</el-tag>
						</div>
					</div>

					<!-- 警告提示 -->
					<div
						v-if="hoveredStageVariance > 0 || hoveredStage.status === 'Blocked'"
						class="gsp-alert"
					>
						<el-icon class="gsp-alert__icon"><InfoFilled /></el-icon>
						<span v-if="hoveredStageVariance > 0">
							Finished {{ hoveredStageVariance }} day{{
								hoveredStageVariance > 1 ? 's' : ''
							}}
							later than planned.
						</span>
						<span v-else>This stage is currently blocked.</span>
					</div>

					<!-- Assignee -->
					<div
						v-if="
							hoveredStage.assignee?.length ||
							hoveredStage.assignedGroup ||
							hoveredStage.defaultAssignee
						"
						class="gsp-assignee"
					>
						<!-- loading 时显示 skeleton -->
						<template v-if="usersLoading && hoveredStage.assignee?.length">
							<div class="gsp-assignee__avatar gsp-skeleton"></div>
							<div class="gsp-assignee__info">
								<span class="gsp-skeleton gsp-skeleton--text"></span>
							</div>
						</template>
						<template v-else>
							<div class="gsp-assignee__avatar">
								{{
									(
										getAssigneeDisplayName(hoveredStage.assignee) ||
										hoveredStage.assignedGroup ||
										hoveredStage.defaultAssignee ||
										'?'
									)
										.charAt(0)
										.toUpperCase()
								}}
							</div>
							<div class="gsp-assignee__info">
								<span class="gsp-assignee__name">
									{{
										getAssigneeDisplayName(hoveredStage.assignee) ||
										hoveredStage.assignedGroup ||
										hoveredStage.defaultAssignee ||
										'—'
									}}
								</span>
							</div>
						</template>
					</div>

					<!-- TIMELINE -->
					<div class="gsp-section">
						<div class="gsp-section__title">
							<el-icon><Calendar /></el-icon>
							TIMELINE
						</div>

						<!-- PLANNED -->
						<div
							v-if="hoveredStage.startTime || hoveredStage.endTime"
							class="gsp-time-block"
						>
							<div class="gsp-time-block__label">
								PLANNED
								<el-tooltip
									content="Original plan, set when case started, does not change"
									placement="top"
								>
									<el-icon class="gsp-info-icon"><InfoFilled /></el-icon>
								</el-tooltip>
							</div>
							<div class="gsp-time-rows">
								<div class="gsp-time-row">
									<span>START</span>
									<span>{{ formatStageDate(hoveredStage.startTime) }}</span>
								</div>
								<div class="gsp-time-row">
									<span>ETA</span>
									<span>{{ formatStageDate(hoveredStage.endTime) }}</span>
								</div>
								<div v-if="hoveredStage.estimatedDuration" class="gsp-time-row">
									<span>DURATION</span>
									<span>{{ hoveredStage.estimatedDuration }} days</span>
								</div>
							</div>
						</div>

						<div v-if="hoveredStage.customEndTime" class="gsp-divider"></div>

						<!-- PROJECTED -->
						<div v-if="hoveredStage.customEndTime" class="gsp-time-block">
							<div class="gsp-time-block__label">
								PROJECTED
								<el-tooltip
									content="Current forecast, updates as stages complete"
									placement="top"
									:show-after="200"
								>
									<el-icon class="gsp-info-icon"><InfoFilled /></el-icon>
								</el-tooltip>
							</div>
							<div class="gsp-time-rows">
								<div v-if="hoveredStage.startTime" class="gsp-time-row">
									<span>START</span>
									<span>{{ formatStageDate(hoveredStage.startTime) }}</span>
								</div>
								<div class="gsp-time-row">
									<span>END</span>
									<span>{{ formatStageDate(hoveredStage.customEndTime) }}</span>
								</div>
							</div>
						</div>

						<!-- ACTUAL（进行中或已完成的 stage） -->
						<template v-if="hoveredStage.startTime">
							<div class="gsp-divider"></div>
							<div class="gsp-time-block">
								<div class="gsp-time-block__label">ACTUAL</div>
								<div class="gsp-time-rows">
									<div class="gsp-time-row">
										<span>START</span>
										<span>{{ formatStageDate(hoveredStage.startTime) }}</span>
									</div>
									<div class="gsp-time-row">
										<span>END</span>
										<span
											:class="{
												'gsp-time-row__value--muted':
													!hoveredStage.completed,
											}"
										>
											{{
												hoveredStage.completed && hoveredStage.date
													? hoveredStage.date
													: '—'
											}}
										</span>
									</div>
									<div v-if="hoveredStageDaysElapsed > 0" class="gsp-time-row">
										<span>DAYS ELAPSED</span>
										<span>{{ hoveredStageDaysElapsed }} days</span>
									</div>
								</div>
							</div>
						</template>
					</div>

					<!-- 完成 / 保存信息 -->
					<div v-if="hoveredStage.completedBy || hoveredStage.savedBy" class="gsp-meta">
						<template v-if="hoveredStage.showSaveOrComplete">
							Last saved by
							<strong>{{ hoveredStage.savedBy }}</strong>
							<span v-if="hoveredStage.saveTime">on {{ hoveredStage.saveTime }}</span>
						</template>
						<template v-else>
							Completed by
							<strong>{{ hoveredStage.completedBy }}</strong>
							<span v-if="hoveredStage.date">on {{ hoveredStage.date }}</span>
						</template>
					</div>

					<!-- Mark as Blocked（仅对 InProgress 的 stage 显示） -->
					<div
						v-if="isCurrentActiveStage(hoveredStage)"
						class="gsp-footer"
						@mouseenter="cancelHideDetail"
					>
						<!-- 展开状态：输入原因 -->
						<template v-if="blockingStageId === hoveredStage.stageId">
							<el-input
								v-model="blockReason"
								placeholder="Reason for blocking..."
								class="mb-2"
								autofocus
								@focus="isInputFocused = true"
								@blur="handleInputBlur"
								@keydown.enter="confirmBlock"
								@keydown.esc="cancelBlock"
							/>
							<div class="flex gap-2">
								<el-button
									type="info"
									class="flex-1"
									:loading="blockLoading"
									@click.stop="confirmBlock"
								>
									<el-icon class="mr-1"><CircleClose /></el-icon>
									Confirm
								</el-button>
								<el-button @click.stop="cancelBlock">Cancel</el-button>
							</div>
						</template>
						<!-- 收起状态：Mark as Blocked 按钮 -->
						<template v-else>
							<button
								class="gsp-action gsp-action--block"
								@click.stop="startBlock(hoveredStage)"
							>
								<el-icon><CircleClose /></el-icon>
								Mark as Blocked
							</button>
						</template>
					</div>
				</div>
			</template>
		</el-popover>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import {
	Check,
	Clock,
	ArrowDown,
	ArrowUp,
	ArrowRight,
	InfoFilled,
	Calendar,
	CircleClose,
} from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { OnboardingItem, Stage } from '#/onboard';
import { timeZoneConvert } from '@/hooks/time';
import { projectTenMinutesSsecondsDate, projectDate } from '@/settings/projectSetting';
import ActionTag from '@/components/actionTools/ActionTag.vue';
import { rollBackStage } from '@/apis/ow/onboarding';
import { getAllUser } from '@/apis/global';
import dayjs from 'dayjs';

// Props
interface Props {
	activeStage: string;
	onboardingData: OnboardingItem;
	workflowStages: Stage[]; // 从父组件传递的工作流阶段
	stageAccessCheck?: (stageId: string) => boolean; // 阶段访问权限检查函数
	onboardingId: string; // 父组件传入的 onboarding ID
}

const props = defineProps<Props>();

// Emits
const emit = defineEmits<{
	setActiveStage: [stageId: string];
	stageCompleted: [];
	stageRolledBack: [];
}>();

// Roll Back 弹窗状态
const rollBackDialogVisible = ref(false);
const rollBackReason = ref('');
const rollBackLoading = ref(false);
const rollBackTargetStage = ref<any>(null);

// 用户列表（用于 assignee ID → 名字映射，组件挂载时即加载）
const allUserOptions = ref<{ key: string; value: string }[]>([]);
let usersFetched = false;
const usersLoading = ref(false);

const fetchAllUsers = async () => {
	if (usersFetched) return;
	usersLoading.value = true;
	try {
		const res = await getAllUser();
		if (res?.data && Array.isArray(res.data)) {
			allUserOptions.value = res.data.map((user: any) => ({
				key: String(user?.id),
				value: user?.name ?? '',
			}));
			usersFetched = true;
		}
	} catch {
		// 静默失败，不影响主流程
	} finally {
		usersLoading.value = false;
	}
};

onMounted(() => {
	fetchAllUsers();
});

const getUserName = (userId: string): string =>
	allUserOptions.value.find((u) => u.key === userId)?.value ?? userId;

/** 将 assignee string[] 转成可读的名字列表（最多显示前3个，逗号拼接） */
const getAssigneeDisplayName = (assignee: string[] | undefined): string => {
	if (!assignee || assignee.length === 0) return '';
	return assignee
		.slice(0, 3)
		.map((id) => getUserName(id))
		.join(', ');
};

// Roll Back 事件处理
const handleRollBack = (stage: any) => {
	rollBackTargetStage.value = stage;
	rollBackReason.value = '';
	rollBackDialogVisible.value = true;
};

const handleRollBackConfirm = async () => {
	if (!props.onboardingId || !rollBackTargetStage.value?.stageId) return;
	rollBackLoading.value = true;
	try {
		await rollBackStage(props.onboardingId, rollBackTargetStage.value.stageId, {
			reason: rollBackReason.value || undefined,
		});
		ElMessage.success('Stage has been rolled back successfully.');
		rollBackDialogVisible.value = false;
		rollBackReason.value = '';
		rollBackTargetStage.value = null;
		emit('stageRolledBack');
	} finally {
		rollBackLoading.value = false;
	}
};

// 响应式数据
const isOpen = ref(true);
const showAllStages = ref(true);

// 判断显示保存还是完成状态的函数
const getSaveOrCompleteFlag = (completionTime: string, saveTime: string): boolean => {
	// 如果没有保存时间或完成时间，返回false
	if (!saveTime || !completionTime) {
		return !!saveTime;
	}

	try {
		const saveDate = new Date(saveTime);
		const completeDate = new Date(completionTime);

		// 验证日期是否有效
		if (isNaN(saveDate.getTime()) || isNaN(completeDate.getTime())) {
			return false;
		}

		// 如果saveTime的时间比completionTime更大，则显示保存状态
		return saveDate > completeDate;
	} catch (error) {
		console.error('Error comparing times:', error);
		return false;
	}
};

// 计算属性
const stages = computed(() => {
	// 根据传入的工作流阶段和当前业务数据设置阶段完成状态
	return props.workflowStages.map((stage, index) => ({
		...stage,
		title: stage.stageName, // 使用 name 作为 title
		completed: stage.isCompleted,
		date: timeZoneConvert(stage?.completionTime || '', false, projectTenMinutesSsecondsDate),
		saveTime: timeZoneConvert(stage?.saveTime || '', false, projectTenMinutesSsecondsDate),
		assignedGroup: stage.defaultAssignedGroup || '',
		completedBy: stage.completedBy,
		showSaveOrComplete: getSaveOrCompleteFlag(
			stage?.completionTime || '',
			stage?.saveTime || ''
		),
		canRollBack: (stage as any).canRollBack ?? false,
	}));
});

const progressPercentage = computed(() => {
	const completedStages = stages.value.filter(
		(stage) => stage.completed || stage.status == 'Skipped'
	).length;
	const totalStages = stages.value.length;

	if (totalStages === 0) return 0;

	const percentage = (completedStages / totalStages) * 100;
	// 四舍五入到整数，并确保不超过100%
	return Math.min(Math.round(percentage), 100);
});

const displayedStages = computed(() => {
	if (showAllStages.value) {
		return stages.value;
	} else {
		// 只显示未完成的阶段和当前阶段
		const currentStageIndex = stages.value.findIndex((stage) => !stage.completed);
		return stages.value.filter(
			(stage, index) => !stage.completed || index === currentStageIndex
		);
	}
});

// 工具函数
const getOriginalStageIndex = (stage: any) => {
	return stages.value.findIndex((s) => s.stageId === stage.stageId);
};

// 检查阶段是否可以访问
const isStageAccessible = (stage: any): boolean => {
	if (!props.stageAccessCheck) {
		return !!stage.permission?.canView; // 如果没有权限检查函数，默认允许访问
	}
	return props.stageAccessCheck(stage.stageId);
};

// 事件处理函数
const toggleOpen = () => {
	isOpen.value = !isOpen.value;
};

const toggleStagesView = () => {
	showAllStages.value = !showAllStages.value;
};

const handleStageClick = (stageId?: string) => {
	if (!stageId) return;

	emit('setActiveStage', stageId);
};

// 监听activeStage变化
watch(
	() => props.activeStage,
	(newStage) => {
		// 可以在这里添加额外的逻辑
		console.log('Active stage changed to:', newStage);
	}
);

// ========================= Stage 详情 Popover =========================

const stageDetailVisible = ref(false);
const hoveredStage = ref<any>(null);
const stageDetailTriggerRef = ref<HTMLElement | null>(null);
let hideDetailTimer: ReturnType<typeof setTimeout> | null = null;

function showStageDetail(stage: any, event: MouseEvent) {
	if (hideDetailTimer) {
		clearTimeout(hideDetailTimer);
		hideDetailTimer = null;
	}
	hoveredStage.value = stage;
	console.log('hoveredStage.value:', hoveredStage.value);
	stageDetailTriggerRef.value = event.currentTarget as HTMLElement;
	stageDetailVisible.value = true;
}

function hideStageDetail() {
	// block 输入框展开时不关闭弹窗
	if (blockingStageId.value) return;
	hideDetailTimer = setTimeout(() => {
		stageDetailVisible.value = false;
		hideDetailTimer = null;
	}, 200);
}

function cancelHideDetail() {
	if (hideDetailTimer) {
		clearTimeout(hideDetailTimer);
		hideDetailTimer = null;
	}
}

function handlePopoverMouseLeave() {
	// IME 候选词选择时鼠标会离开 popover，此处是关键拦截点
	if (isInputFocused.value || blockingStageId.value) return;
	stageDetailVisible.value = false;
}

/** 格式化日期 */
function formatStageDate(d: string | null | undefined): string {
	if (!d) return '—';
	return timeZoneConvert(d, false, projectDate);
}

/** 计算 Stage 偏差天数（endTime vs 实际完成时间） */
const hoveredStageVariance = computed(() => {
	const s = hoveredStage.value;
	if (!s?.endTime || !s?.completionTime) return 0;
	return dayjs(s.completionTime).diff(dayjs(s.endTime), 'day');
});

/** Stage Tag 类型 */
function getStageTagType(stage: any): '' | 'success' | 'warning' | 'danger' | 'info' {
	if (stage.completed) return 'success';
	if (stage.status === 'Skipped') return 'info';
	if (stage.status === 'InProgress') return '';
	return 'info';
}

/** Stage 状态展示文字 */
function getStageStatusLabel(stage: any): string {
	if (stage.completed) {
		const variance = hoveredStageVariance.value;
		if (variance === 0) return 'Completed';
		return `Completed ${variance > 0 ? '+' : ''}${variance}d`;
	}
	if (stage.status === 'Skipped') return 'Skipped';
	if (stage.status === 'InProgress') return 'In Progress';
	return stage.status || 'Not Started';
}

/** 已过去天数（从 startTime 到今天） */
const hoveredStageDaysElapsed = computed(() => {
	const s = hoveredStage.value;
	if (!s?.startTime) return 0;
	const start = dayjs(s.startTime);
	const end = s.completed && s.completionTime ? dayjs(s.completionTime) : dayjs();
	return Math.max(0, end.diff(start, 'day'));
});

/** 是否是当前活跃（InProgress）的 Stage */
function isCurrentActiveStage(stage: any): boolean {
	return (
		!stage.completed &&
		stage.status !== 'Skipped' &&
		(stage.stageId === props.activeStage ||
			props.onboardingData?.currentStageId === stage.stageId)
	);
}

// ========================= Mark as Blocked =========================

const blockingStageId = ref<string | null>(null);
const blockReason = ref('');
const blockLoading = ref(false);
/** IME 输入锁：focus 时设 true，blur 后 300ms 才解锁 */
const isInputFocused = ref(false);
let blurUnlockTimer: ReturnType<typeof setTimeout> | null = null;

function handleInputBlur() {
	// 延迟解锁，给 IME 候选词选择（mousedown）留出时间
	blurUnlockTimer = setTimeout(() => {
		isInputFocused.value = false;
		blurUnlockTimer = null;
	}, 300);
}

function startBlock(stage: any) {
	blockingStageId.value = stage.stageId;
	blockReason.value = '';
}

function cancelBlock() {
	blockingStageId.value = null;
	blockReason.value = '';
	isInputFocused.value = false;
	if (blurUnlockTimer) {
		clearTimeout(blurUnlockTimer);
		blurUnlockTimer = null;
	}
}

async function confirmBlock() {
	if (!blockingStageId.value) return;
	blockLoading.value = true;
	try {
		// 调用 blockStage API（gantt.ts 中已预留）
		const { blockStage } = await import('@/apis/ow/gantt');
		await blockStage(props.onboardingId, {
			stageId: blockingStageId.value,
			reason: blockReason.value,
		});
		ElMessage.success('Stage has been marked as blocked.');
		blockingStageId.value = null;
		blockReason.value = '';
		stageDetailVisible.value = false;
	} catch {
		ElMessage.error('Failed to mark stage as blocked.');
	} finally {
		blockLoading.value = false;
	}
}
</script>

<style scoped lang="scss">
.rotate-180 {
	transform: rotate(180deg);
}

/* 完成信息文本样式 - 参考 index.vue 的实现 */
.completion-info-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	width: 100%;
	cursor: help;
}

/* 阶段标题文本样式 */
.stage-title-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	width: 100%;
}
</style>

<style lang="scss">
/* ===== Stage 详情 Popover（复用 GanttChart 的 gsp-* 样式）===== */
.gantt-stage-popover.el-popover {
	padding: 0 !important;
	border: 1px solid var(--el-border-color-light) !important;
	border-radius: var(--el-border-radius-large, 16px) !important;
	box-shadow: var(--el-box-shadow) !important;
	background-color: var(--el-bg-color-overlay) !important;
	overflow: hidden;
	max-height: 80vh;
	overflow-y: auto;
	pointer-events: auto;
}

.gsp-wrap {
	padding: 18px;
	background-color: var(--el-bg-color-overlay);
	color: var(--el-text-color-regular);
}

.gsp-header {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 8px;
	margin-bottom: 10px;

	&__left {
		min-width: 0;
		flex: 1;
	}

	&__right {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 4px;
		flex-shrink: 0;
	}
}

.gsp-stage-num {
	display: block;
	font-size: 11px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	letter-spacing: 0.06em;
	text-transform: uppercase;
	margin-bottom: 2px;
}

.gsp-stage-name {
	font-size: 16px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	margin: 0;
	line-height: 1.3;
}

.gsp-variance {
	font-size: 11px;
	font-weight: 600;
	color: var(--el-text-color-secondary);

	&--late {
		color: var(--el-color-danger);
	}
	&--early {
		color: var(--el-color-success);
	}
}

.gsp-alert {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	padding: 8px 10px;
	background-color: var(--el-fill-color-light);
	border-radius: var(--el-border-radius-small, 8px);
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-bottom: 10px;

	&__icon {
		flex-shrink: 0;
		margin-top: 1px;
		color: var(--el-text-color-secondary);
	}
}

.gsp-assignee {
	display: flex;
	align-items: center;
	gap: 10px;
	margin-bottom: 14px;

	&__avatar {
		width: 32px;
		height: 32px;
		border-radius: 50%;
		background-color: var(--el-color-primary-light-7);
		color: var(--el-color-primary);
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 13px;
		font-weight: 700;
		flex-shrink: 0;
		text-transform: uppercase;
	}

	&__info {
		min-width: 0;
	}

	&__name {
		font-size: 15px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		display: block;
		line-height: 1.3;
	}

	&__email {
		font-size: 12px;
		font-weight: 400;
		color: var(--el-text-color-secondary);
		display: block;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	&__co {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-left: 4px;
	}
}

.gsp-section {
	padding: 12px 0 0;
	margin-bottom: 8px;

	&__title {
		display: flex;
		align-items: center;
		gap: 5px;
		font-size: 10px;
		font-weight: 700;
		color: var(--el-text-color-secondary);
		letter-spacing: 0.08em;
		text-transform: uppercase;
		margin-bottom: 12px;
	}

	&__title--with-pct {
		justify-content: space-between;
	}

	&__pct {
		font-size: 13px;
		font-weight: 700;
		color: var(--el-text-color-primary);
	}
}

.gsp-time-block {
	margin-bottom: 2px;

	&__label {
		display: flex;
		align-items: center;
		gap: 4px;
		font-size: 10px;
		font-weight: 700;
		color: var(--el-text-color-secondary);
		letter-spacing: 0.08em;
		text-transform: uppercase;
		margin-bottom: 8px;
	}
}

.gsp-info-icon {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	cursor: help;
}

.gsp-time-rows {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 10px 8px;
}

.gsp-time-row {
	display: flex;
	flex-direction: column;
	gap: 2px;

	span:first-child {
		font-size: 10px;
		font-weight: 600;
		color: var(--el-text-color-secondary);
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}

	span:last-child {
		font-size: 15px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		line-height: 1.2;
	}

	&--full {
		grid-column: 1 / -1;
	}

	&__value--muted span:last-child,
	span.gsp-time-row__value--muted {
		color: var(--el-text-color-placeholder) !important;
		font-weight: 400;
	}
}

.gsp-divider {
	height: 1px;
	background-color: var(--el-border-color-lighter);
	margin: 12px 0;
}

.gsp-meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-bottom: 12px;
	padding-top: 8px;
	border-top: 1px solid var(--el-border-color-lighter);

	strong {
		color: var(--el-text-color-primary);
		font-weight: 600;
	}
}

/* ===== Skeleton loading ===== */
@keyframes gsp-shimmer {
	0% {
		background-position: -200px 0;
	}
	100% {
		background-position: calc(200px + 100%) 0;
	}
}

.gsp-skeleton {
	background: linear-gradient(
		90deg,
		var(--el-fill-color-light) 25%,
		var(--el-fill-color) 50%,
		var(--el-fill-color-light) 75%
	);
	background-size: 200px 100%;
	animation: gsp-shimmer 1.2s ease-in-out infinite;
	border-radius: var(--el-border-radius-small, 6px);

	&--text {
		display: block;
		width: 120px;
		height: 14px;
		border-radius: 4px;
	}

	// 用于圆形头像 skeleton，保持圆形
	&.gsp-assignee__avatar {
		border-radius: 50%;
	}
}

.gsp-footer {
	display: flex;
	flex-direction: column;
	gap: 8px;
	padding-top: 12px;
	border-top: 1px solid var(--el-border-color-lighter);
	margin-top: 4px;
}

.gsp-action {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 5px 14px;
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-border-radius-round, 20px);
	background-color: var(--el-fill-color-blank);
	color: var(--el-text-color-regular);
	font-size: 13px;
	font-weight: 500;
	cursor: pointer;
	transition:
		background-color 0.15s,
		border-color 0.15s,
		color 0.15s;

	&--block {
		width: 100%;
		justify-content: center;
		padding: 8px 16px;
		background-color: transparent;

		&:hover {
			background-color: var(--el-color-danger-light-7);
			border-color: var(--el-color-danger);
			color: var(--el-color-danger);
		}
	}

	&:hover {
		background-color: var(--el-color-primary-light-9);
		border-color: var(--el-color-primary-light-7);
		color: var(--el-color-primary);
	}
}
</style>
