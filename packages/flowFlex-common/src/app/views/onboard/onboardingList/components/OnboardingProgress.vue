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
							class="flex items-center gap-2 p-3 cursor-pointer transition-colors hover:bg-gray-50 dark:hover:bg-indigo-900/20 rounded-xl min-w-0 w-full"
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
							@click="handleStageClick(stage.stageId)"
						>
							<!-- 阶段状态图标 -->
							<div
								class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
								:class="[
									stage.completed
										? 'bg-primary text-white'
										: activeStage === stage.stageId
										? 'bg-primary-500 text-white'
										: 'bg-[var(--el-bg-color-page)] dark:bg-black',
								]"
							>
								<el-icon v-if="stage.completed" class="text-xs">
									<Check />
								</el-icon>
								<el-icon v-else-if="activeStage === stage.stageId" class="text-xs">
									<Clock />
								</el-icon>
								<text v-else class="text-xs font-bold leading-6">
									{{ getOriginalStageIndex(stage) + 1 }}
								</text>
							</div>

							<!-- 阶段内容 -->
							<div class="space-y-1 w-full min-w-0">
								<div class="font-medium flex items-start min-w-0">
									<!-- <span
										class="mr-2 text-sm font-bold text-[var(--el-text-color-secondary)] flex-shrink-0"
									>
										{{ getOriginalStageIndex(stage) + 1 }}.
									</span> -->
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
												<el-tooltip
													v-if="stage.required"
													content="Users must complete this stage before proceeding to subsequent stages"
													placement="top"
												>
													<div
														class="text-orange-400 px-2 border border-orange-400 rounded-xl flex items-center gap-x-2 text-sm dark:bg-orange-900"
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
							</div>
						</div>
					</div>
				</el-scrollbar>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { Check, Clock, ArrowDown, ArrowUp, ArrowRight } from '@element-plus/icons-vue';
import { OnboardingItem, Stage } from '#/onboard';
import { timeZoneConvert } from '@/hooks/time';
import { defaultStr, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';
import ActionTag from '@/components/actionTools/ActionTag.vue';

// Props
interface Props {
	activeStage: string;
	onboardingData: OnboardingItem;
	workflowStages: Stage[]; // 从父组件传递的工作流阶段
	stageAccessCheck?: (stageId: string) => boolean; // 阶段访问权限检查函数
}

const props = defineProps<Props>();

// Emits
const emit = defineEmits<{
	setActiveStage: [stageId: string];
	stageCompleted: [];
}>();

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
		assignee: stage.defaultAssignedGroup || defaultStr,
		completedBy: stage.completedBy,
		showSaveOrComplete: getSaveOrCompleteFlag(
			stage?.completionTime || '',
			stage?.saveTime || ''
		),
	}));
});

const progressPercentage = computed(() => {
	const completedStages = stages.value.filter((stage) => stage.completed).length;
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
		return true; // 如果没有权限检查函数，默认允许访问
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
	// 如果提供了权限检查函数，先检查权限
	if (props.stageAccessCheck && !props.stageAccessCheck(stageId)) {
		// 权限检查失败，不发送事件，让父组件处理
		emit('setActiveStage', stageId);
		return;
	}

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
