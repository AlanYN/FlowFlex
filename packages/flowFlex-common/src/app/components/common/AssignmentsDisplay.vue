<template>
	<!-- 只有当有 assignments 数据时才显示整个区域 -->
	<div v-if="hasAssignments || showWhenEmpty" class="assignments-section space-y-2">
		<!-- 标签 -->
		<div class="flex items-center text-sm">
			<span class="card-label">Assignments:</span>
		</div>

		<!-- 内容区域 -->
		<div class="assignments-display">
			<!-- 有数据时显示 assignments -->
			<div
				v-if="hasAssignments"
				class="assignments-container"
				:style="{ height: containerHeight }"
			>
				<template
					v-for="(assignment, index) in displayedAssignments"
					:key="`${assignment.workflowId}-${assignment.stageId}`"
				>
					<!-- 第一个assignment独占一行 -->
					<div v-if="index === 0" class="flex gap-2 mb-2">
						<span
							class="card-link card-link-full"
							:title="`${getWorkflowName(assignment.workflowId)} → ${getStageName(
								assignment.stageId
							)}`"
						>
							<span
								class="w-full text-center overflow-hidden text-ellipsis whitespace-nowrap block"
							>
								{{
									`${getWorkflowName(assignment.workflowId)} → ${getStageName(
										assignment.stageId
									)}`
								}}
							</span>
						</span>
					</div>
					<!-- 第二个assignment，根据是否有剩余内容决定是否与+几按钮共享一行 -->
					<div v-if="index === 1" class="flex gap-2 items-center">
						<span
							:class="{
								'card-link': true,
								'card-link-full': uniqueAssignments.length <= 2,
								'card-link-shared': uniqueAssignments.length > 2,
							}"
							:title="`${getWorkflowName(assignment.workflowId)} → ${getStageName(
								assignment.stageId
							)}`"
						>
							<span
								class="w-full text-center overflow-hidden text-ellipsis whitespace-nowrap block"
							>
								{{
									`${getWorkflowName(assignment.workflowId)} → ${getStageName(
										assignment.stageId
									)}`
								}}
							</span>
						</span>
						<!-- 显示剩余数量的按钮 -->
						<el-popover
							v-if="uniqueAssignments.length > 2"
							placement="top"
							:width="400"
							trigger="click"
						>
							<template #reference>
								<span class="card-link-more">
									+{{ uniqueAssignments.length - 2 }}
								</span>
							</template>
							<div class="popover-content">
								<h4 class="popover-title">More Assignments</h4>
								<div class="popover-tags">
									<el-tag
										class="popover-tag"
										v-for="moreAssignment in uniqueAssignments.slice(2)"
										:key="`${moreAssignment.workflowId}-${moreAssignment.stageId}`"
										:title="`${getWorkflowName(
											moreAssignment.workflowId
										)} → ${getStageName(moreAssignment.stageId)}`"
									>
										<span class="popover-tag-text">
											{{
												`${getWorkflowName(
													moreAssignment.workflowId
												)} → ${getStageName(moreAssignment.stageId)}`
											}}
										</span>
									</el-tag>
								</div>
							</div>
						</el-popover>
					</div>
				</template>
			</div>

			<!-- 无数据时显示黄色边框提示 -->
			<div v-else class="no-assignments-container">
				<div class="no-assignments-message">
					<Icon icon="material-symbols:warning-outline" class="warning-icon" />
					<span>No Assignments</span>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Icon } from '@iconify/vue';
import { ElTag, ElPopover } from 'element-plus';
import { defaultStr } from '@/settings/projectSetting';

// Props
interface Props {
	assignments?: any[];
	workflows?: any[];
	allStages?: any[];
	containerHeight?: string;
	displayCount?: number;
	showWhenEmpty?: boolean; // 是否在无数据时也显示组件（显示黄色边框提示）
}

const props = withDefaults(defineProps<Props>(), {
	assignments: () => [],
	workflows: () => [],
	allStages: () => [],
	containerHeight: '60px',
	displayCount: 2,
	showWhenEmpty: true, // 默认显示 "No Assignments" 提示
});

// Computed
const hasAssignments = computed(() => {
	return props.assignments && props.assignments.length > 0;
});

// 获取去重后的所有数据
const uniqueAssignments = computed(() => {
	if (!props.assignments || props.assignments.length === 0) {
		return [];
	}

	return props.assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});
});

// 获取显示的分配数量（去重）
const displayedAssignments = computed(() => {
	if (!hasAssignments.value) {
		return [];
	}

	// 返回前N个去重后的数据
	return uniqueAssignments.value.slice(0, props.displayCount);
});

// Methods
const getWorkflowName = (workflowId: string) => {
	if (!workflowId || workflowId === '0') return defaultStr;
	const workflow = props.workflows.find((w) => w.id === workflowId);
	return workflow?.name || workflowId;
};

const getStageName = (stageId: string) => {
	if (!stageId || stageId === '0') return defaultStr;
	const stage = props.allStages.find((s) => s.id === stageId);
	return stage ? stage.name : stageId;
};
</script>

<style scoped lang="scss">
.assignments-display {
	width: 100%;
}

/* 无数据提示样式 */
.no-assignments-container {
	height: 60px;
	display: flex;
	align-items: center;
	justify-content: center;
	border: 2px dashed var(--el-color-warning);
	border-radius: 8px;
	background-color: var(--el-color-warning-light-9);
	transition: all 0.3s ease;
}

.no-assignments-message {
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
.no-assignments-container:hover {
	border-color: var(--el-color-warning-dark-2);
	background-color: var(--el-color-warning-light-8);
	transform: translateY(-1px);
}
</style>
