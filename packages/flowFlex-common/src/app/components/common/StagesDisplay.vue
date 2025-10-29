<template>
	<div v-if="hasStages || showWhenEmpty" class="stages-section space-y-2">
		<!-- 标签 -->
		<div class="flex items-center text-sm">
			<span class="flex items-center">
				<text class="card-label">Stages:</text>
				{{ stages.length }}
			</span>
		</div>

		<!-- 内容区域 -->
		<div class="stages-display">
			<!-- 有数据时显示 stages -->
			<div
				v-if="hasStages"
				ref="stagesContainer"
				class="stages-container"
				:style="{ height: containerHeight }"
			>
				<div class="stages-list" ref="stagesList">
					<!-- 显示的stages -->
					<span
						v-for="stage in visibleStages"
						:key="stage.id"
						ref="stageItems"
						class="stage-tag"
						:style="{
							borderColor: stage?.color || 'var(--el-color-primary)',
							color: stage?.color || 'var(--el-color-primary)',
						}"
						:title="stage.name"
					>
						<span class="stage-tag-text">
							{{ stage.name }}
						</span>
					</span>

					<!-- 显示剩余数量的按钮 -->
					<el-popover
						v-if="hiddenStages.length > 0"
						placement="top"
						:width="400"
						trigger="click"
					>
						<template #reference>
							<span class="stage-more-btn">+{{ hiddenStages.length }}</span>
						</template>
						<div class="popover-content">
							<h4 class="popover-title">More Stages</h4>
							<div class="popover-stages">
								<span
									v-for="moreStage in hiddenStages"
									:key="moreStage.id"
									class="popover-stage-tag"
									:style="{
										borderColor: moreStage?.color || 'var(--el-color-primary)',
										color: moreStage?.color || 'var(--el-color-primary)',
									}"
									:title="moreStage.name"
								>
									<span class="popover-stage-text">
										{{ moreStage.name }}
									</span>
								</span>
							</div>
						</div>
					</el-popover>
				</div>
			</div>

			<!-- 无数据时显示提示 -->
			<div v-else class="no-stages-container">
				<div class="no-stages-message">
					<Icon icon="material-symbols:warning-outline" class="warning-icon" />
					<span>No Stages</span>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { Icon } from '@iconify/vue';
import { ElPopover } from 'element-plus';

// Props
interface Props {
	stages?: any[];
	containerHeight?: string;
	showWhenEmpty?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	stages: () => [],
	containerHeight: '60px',
	showWhenEmpty: true,
});

// Refs
const stagesContainer = ref<HTMLElement>();
const stagesList = ref<HTMLElement>();
const stageItems = ref<HTMLElement[]>([]);

// State
const visibleStageCount = ref(0);

// Computed
const hasStages = computed(() => {
	return props.stages && props.stages.length > 0;
});

const visibleStages = computed(() => {
	if (!hasStages.value) return [];
	return props.stages.slice(0, visibleStageCount.value);
});

const hiddenStages = computed(() => {
	if (!hasStages.value) return [];
	return props.stages.slice(visibleStageCount.value);
});

// Methods
const calculateVisibleStages = async () => {
	if (!hasStages.value || !stagesContainer.value) {
		visibleStageCount.value = 0;
		return;
	}

	await nextTick();

	const containerHeight = parseInt(props.containerHeight);

	// 临时显示所有stages来测量
	visibleStageCount.value = props.stages.length;
	await nextTick();

	if (stageItems.value && stageItems.value.length > 0 && stagesList.value) {
		// 检查当前布局是否超出容器高度
		const currentHeight = stagesList.value.offsetHeight;

		if (currentHeight <= containerHeight) {
			// 如果当前高度没有超出，显示所有stages
			visibleStageCount.value = props.stages.length;
		} else {
			// 如果超出了，需要逐个减少直到高度合适
			let count = props.stages.length - 1;

			while (count > 0) {
				visibleStageCount.value = count;
				await nextTick();

				// 重新测量高度（包括 "+几" 按钮）
				const newHeight = stagesList.value.offsetHeight;

				if (newHeight <= containerHeight) {
					break;
				}
				count--;
			}

			// 确保至少显示一个stage
			visibleStageCount.value = Math.max(1, count);
		}
	}
};

// Watchers
watch(() => props.stages, calculateVisibleStages, { deep: true });

// Lifecycle
onMounted(() => {
	calculateVisibleStages();

	// 监听窗口大小变化
	const resizeObserver = new ResizeObserver(() => {
		calculateVisibleStages();
	});

	if (stagesContainer.value) {
		resizeObserver.observe(stagesContainer.value);
	}

	// 清理
	onUnmounted(() => {
		resizeObserver.disconnect();
	});
});
</script>

<style scoped lang="scss">
.stages-display {
	width: 100%;
}

/* stages列表样式 */
.stages-list {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
	align-items: flex-start;
	overflow: hidden;
}

.stage-tag {
	display: inline-flex;
	align-items: center;
	background-color: transparent;
	font-size: 12px;
	font-weight: 500;
	transition: all 0.2s ease;
	white-space: nowrap;
	flex-shrink: 0;
	@apply border px-2.5 py-0.5 rounded-full;
}

.stage-tag-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.stage-more-btn {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	background-color: var(--el-color-primary);
	color: var(--el-color-white);
	font-size: 12px;
	font-weight: 500;
	white-space: nowrap;
	cursor: pointer;
	transition: all 0.2s ease;
	flex-shrink: 0;
	min-width: 40px;
	@apply border px-2.5 py-0.5 rounded-full;
	border-color: var(--el-color-primary);
}

.stage-more-btn:hover {
	background-color: var(--el-color-primary-dark-2);
	border-color: var(--el-color-primary-dark-2);
}

/* 弹出层样式 */
.popover-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 10px;
}

.popover-stages {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.popover-stage-tag {
	display: inline-flex;
	align-items: center;
	background-color: transparent;
	font-size: 12px;
	font-weight: 500;
	transition: all 0.2s ease;
	white-space: nowrap;
	@apply border px-2.5 py-0.5 rounded-full;
}

.popover-stage-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 无数据提示样式 */
.no-stages-container {
	height: 60px;
	display: flex;
	align-items: center;
	justify-content: center;
	border: 2px dashed var(--el-color-warning);
	border-radius: 8px;
	background-color: var(--el-color-warning-light-9);
	transition: all 0.3s ease;
}

.no-stages-message {
	display: flex;
	align-items: center;
	gap: 8px;
	color: var(--el-color-warning-dark-2);
	font-size: 14px;
	font-weight: 500;
}

.warning-icon {
	width: 18px;
	height: 18px;
	color: var(--el-color-warning);
}

/* hover 效果 */
.no-stages-container:hover {
	border-color: var(--el-color-warning-dark-2);
	background-color: var(--el-color-warning-light-8);
	transform: translateY(-1px);
}
</style>
