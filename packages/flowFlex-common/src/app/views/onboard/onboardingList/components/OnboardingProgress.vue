<template>
	<el-card class="shadow-sm">
		<!-- 可折叠的头部 -->
		<template #header>
			<div
				class="bg-gradient-to-r from-primary-600 to-indigo-600 text-white -mx-5 -mt-5 px-5 py-4 rounded-t-lg cursor-pointer hover:from-primary-700 hover:to-indigo-700 transition-colors"
				@click="toggleOpen"
			>
				<div class="flex items-center justify-between">
					<div class="flex items-center gap-2">
						<el-icon class="transition-transform" :class="{ 'rotate-90': isOpen }">
							<ArrowRight />
						</el-icon>
						<h3 class="text-lg font-semibold">Onboarding Progress</h3>
					</div>
					<div class="flex items-center space-x-2">
						<span class="text-sm font-medium">{{ progressPercentage }}% Complete</span>
					</div>
				</div>
				<!-- 进度条 -->
				<div class="w-full bg-white/25 rounded-full h-1.5 mt-6">
					<div
						class="bg-white h-1.5 rounded-full transition-all duration-300"
						:style="{ width: `${progressPercentage}%` }"
					></div>
				</div>
			</div>
		</template>

		<!-- 可折叠的内容 -->
		<el-collapse-transition>
			<div v-show="isOpen" class="pt-4 p-4">
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
							class="flex items-center gap-2 p-3 cursor-pointer transition-colors hover:bg-gray-50 dark:hover:bg-black-300 rounded-lg"
							:class="[
								stage.completed
									? 'border-green-500'
									: 'border-gray-300 dark:border-gray-600',
								activeStage === stage.stageId
									? 'bg-primary-50 dark:bg-primary-900/20'
									: '',
								index === displayedStages.length - 1 ? '!border-l-0' : '',
							]"
							@click="handleStageClick(stage.stageId)"
						>
							<!-- 阶段状态图标 -->
							<div
								class="w-6 h-6 rounded-full flex items-center justify-center"
								:class="[
									stage.completed
										? 'bg-green-500 text-white'
										: activeStage === stage.stageId
										? 'bg-primary-500 text-white'
										: 'bg-gray-300 text-gray-600 dark:bg-gray-600 dark:text-gray-300',
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
							<div class="space-y-1">
								<div class="font-medium flex items-start">
									<span
										class="mr-2 text-sm font-bold text-gray-500 dark:text-gray-400"
									>
										{{ getOriginalStageIndex(stage) + 1 }}.
									</span>
									<div class="flex-1">
										<div
											class="text-gray-900 dark:text-white-100 text-sm stage-title-text"
											:title="stage.title"
										>
											{{ stage.title }}
										</div>
									</div>
								</div>
								<div
									v-if="stage.completedBy || stage.savedBy"
									class="text-xs text-green-600 dark:text-green-400 ml-6"
								>
									<span
										class="completion-info-text"
										:title="
											stage.showSaveOrComplete
												? `Save by ${stage.savedBy} on ${stage.saveTime}`
												: `Completed by ${stage.completedBy} on ${stage.date}`
										"
									>
										{{
											stage.showSaveOrComplete
												? `Save by ${stage.savedBy} on ${stage.saveTime}`
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
	</el-card>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { Check, Clock, ArrowDown, ArrowUp, ArrowRight } from '@element-plus/icons-vue';
import { OnboardingItem } from '#/onboard';
import { timeZoneConvert } from '@/hooks/time';
import { defaultStr, projectTenMinutesSsecondsDate } from '@/settings/projectSetting';

// Props
interface Props {
	activeStage: string;
	onboardingData: OnboardingItem;
	workflowStages: any[]; // 从父组件传递的工作流阶段
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
		showSaveOrComplete: getSaveOrCompleteFlag(stage?.completionTime, stage?.saveTime),
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

// 事件处理函数
const toggleOpen = () => {
	isOpen.value = !isOpen.value;
};

const toggleStagesView = () => {
	showAllStages.value = !showAllStages.value;
};

const handleStageClick = (stageId: string) => {
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
:deep(.el-card__body) {
	@apply p-0;
}

:deep(.el-card__header) {
	@apply pb-0;
}

.rotate-180 {
	transform: rotate(180deg);
}

/* 完成信息文本样式 - 参考 index.vue 的实现 */
.completion-info-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	max-width: 100%;
	display: block;
	cursor: help;
}

/* 阶段标题文本样式 */
.stage-title-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	max-width: 100%;
	display: block;
}
</style>
