<template>
	<div class="workflow-stage-assignments">
		<div class="assignment-header">
			<label class="assignment-title">Workflow & Stage Assignments</label>
			<el-button
				type="primary"
				size="small"
				@click="addAssignment"
				class="add-assignment-btn"
			>
				<el-icon><Plus /></el-icon>
				Add Assignment
			</el-button>
		</div>

		<el-scrollbar max-height="400px" class="assignments-scrollbar">
			<div class="assignments-container">
				<div
					v-for="(assignment, index) in extendedAssignments"
					:key="`assignment-${index}`"
					class="assignment-item"
				>
					<div class="assignment-item-header mb-2">
						<span class="assignment-label">Assignment {{ index + 1 }}</span>
						<el-button
							v-if="extendedAssignments.length > 1"
							type="danger"
							text
							size="small"
							@click="removeAssignment(index)"
							class="delete-btn"
						>
							<el-icon><Delete /></el-icon>
						</el-button>
					</div>

					<el-form :model="assignment" label-position="left" label-width="80px">
						<el-form-item>
							<template #label>
								<span class="font-bold">Workflow</span>
							</template>
							<el-select
								v-model="assignment.workflowId"
								placeholder="Select workflow"
								style="width: 100%"
								@change="handleWorkflowChange(index, $event)"
								clearable
							>
								<el-option
									v-for="workflow in workflows"
									:key="workflow.id"
									:label="workflow.name"
									:value="workflow.id"
									:disabled="workflow.status === 'inactive'"
								>
									<div class="flex items-center justify-between">
										<span>{{ workflow.name }}</span>
										<div class="flex items-center gap-1">
											<div v-if="workflow.isDefault">⭐</div>
											<el-icon
												v-if="workflow.status === 'inactive'"
												class="inactive-icon"
											>
												<VideoPause />
											</el-icon>
										</div>
									</div>
								</el-option>
								<!-- 移除特殊选项 -->
							</el-select>
						</el-form-item>

						<el-form-item>
							<template #label>
								<span class="font-bold">Stage</span>
							</template>
							<el-select
								v-model="assignment.stageId"
								placeholder="Select stage"
								style="width: 100%"
								:disabled="!assignment.workflowId || assignment.stagesLoading"
								:loading="assignment.stagesLoading"
								@change="handleStageChange(index, $event)"
								clearable
							>
								<el-option
									v-for="stage in assignment.stages"
									:key="stage.id"
									:label="stage.name"
									:value="stage.id"
									:disabled="isStageDisabled(stage.id, index)"
								/>
							</el-select>
						</el-form-item>
					</el-form>
				</div>

				<div v-if="extendedAssignments.length === 0" class="empty-assignments">
					<el-empty description="No assignments yet" :image-size="80">
						<el-button type="primary" @click="addAssignment">
							Add First Assignment
						</el-button>
					</el-empty>
				</div>
			</div>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts">
import { Plus, Delete, VideoPause } from '@element-plus/icons-vue';
import { ref, watch, onMounted } from 'vue';
import { getStagesByWorkflow } from '@/apis/ow';
import { Assignment, ExtendedAssignment, Workflow } from '#/onboard';

interface Props {
	assignments: Assignment[];
	workflows: Workflow[];
}

const props = defineProps<Props>();

// 子组件内部管理完整的 assignments 状态
const extendedAssignments = ref<ExtendedAssignment[]>([]);

// stages 数据缓存
const stagesCache = ref<Record<string, Array<{ id: string; name: string }>>>({});

// 初始化数据
const initializeAssignments = async () => {
	extendedAssignments.value = props.assignments.map((assignment) => {
		// 如果workflowId为"0"，将其设置为空字符串
		const workflowId = assignment.workflowId === '0' ? '' : assignment.workflowId;
		return {
			...assignment,
			workflowId,
			stages: workflowId ? stagesCache.value[workflowId] || [] : [],
			stagesLoading: false,
		};
	});

	// 如果没有初始数据，添加一个空的 assignment
	if (extendedAssignments.value.length === 0) {
		addAssignment();
	}

	// 加载所有需要的 stages 数据
	await loadAllStagesData();
};

// 加载所有需要的 stages 数据
const loadAllStagesData = async () => {
	const workflowIds = [
		...new Set(extendedAssignments.value.map((a) => a.workflowId).filter((id) => id)),
	];

	for (const workflowId of workflowIds) {
		if (workflowId && !stagesCache.value[workflowId]) {
			await loadStagesForWorkflow(workflowId);
		}
	}

	// 更新 extendedAssignments 中的 stages 数据
	extendedAssignments.value.forEach((assignment) => {
		if (assignment.workflowId && stagesCache.value[assignment.workflowId]) {
			assignment.stages = stagesCache.value[assignment.workflowId];
		}
	});
};

// 加载指定 workflow 的 stages 数据
const loadStagesForWorkflow = async (workflowId: string) => {
	if (!workflowId || stagesCache.value[workflowId]) return;

	try {
		const response = await getStagesByWorkflow(workflowId);
		if (response.code === '200') {
			stagesCache.value[workflowId] = response.data || [];
		} else {
			stagesCache.value[workflowId] = [];
		}
	} catch (error) {
		console.error('Failed to load stages for workflow:', workflowId, error);
		stagesCache.value[workflowId] = [];
	}
};

// 新增 assignment
const addAssignment = () => {
	extendedAssignments.value.push({
		workflowId: '',
		stageId: '',
		stages: [],
		stagesLoading: false,
	});
};

// 删除 assignment
const removeAssignment = (index: number) => {
	extendedAssignments.value.splice(index, 1);
};

// 处理 workflow 变化
const handleWorkflowChange = async (index: number, workflowId: string) => {
	const assignment = extendedAssignments.value[index];
	if (!assignment) return;

	// 如果ID为"0"，将其设置为空字符串
	if (workflowId === '0') {
		assignment.workflowId = '';
		assignment.stageId = '';
		assignment.stages = [];
		return;
	}

	// 更新本地状态
	assignment.workflowId = workflowId;
	assignment.stageId = ''; // 清空 stage 选择
	assignment.stages = [];

	// 如果选择了 workflow，加载对应的 stages
	if (workflowId) {
		if (!stagesCache.value[workflowId]) {
			assignment.stagesLoading = true;
			await loadStagesForWorkflow(workflowId);
			assignment.stagesLoading = false;
		}
		assignment.stages = stagesCache.value[workflowId] || [];
	}
};

// 处理 stage 变化
const handleStageChange = (index: number, stageId: string) => {
	const assignment = extendedAssignments.value[index];
	if (assignment) {
		assignment.stageId = stageId;
	}
};

// 检查 stage 是否被禁用（已被其他 assignment 使用）
const isStageDisabled = (stageId: string, currentIndex: number) => {
	if (!stageId) return false;

	return extendedAssignments.value.some(
		(assignment, index) => index !== currentIndex && assignment.stageId === stageId
	);
};

// 获取当前的 assignments 数据（暴露给父组件）
const getAssignments = (): Assignment[] => {
	return extendedAssignments.value.map(({ workflowId, stageId }) => ({
		// 如果workflowId为空，将其设置为"0"
		workflowId: workflowId || '0',
		stageId: stageId || null,
	}));
};

// 监听 props.assignments 的变化（用于初始化）
watch(
	() => props.assignments,
	async (newAssignments) => {
		if (newAssignments && newAssignments.length > 0) {
			await initializeAssignments();
		}
	},
	{ immediate: true, deep: true }
);

// 组件挂载时初始化
onMounted(async () => {
	if (props.assignments.length === 0) {
		// 如果没有初始数据，添加一个空的 assignment
		addAssignment();
	}
});

// 暴露方法给父组件
defineExpose({
	getAssignments,
});
</script>

<style scoped lang="scss">
.workflow-stage-assignments {
	margin: 1.5rem 0;
}

.assignment-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 1rem;
}

.assignment-title {
	font-size: 0.875rem;
	font-weight: 600;
	color: var(--primary-800);
	@apply dark:text-primary-200;
}

.add-assignment-btn {
	font-size: 0.875rem;
	padding: 0.5rem 0.75rem;
}

.assignments-scrollbar {
	width: 100%;
}

.assignments-container {
	display: flex;
	flex-direction: column;
	gap: 1rem;
	padding-right: 10px;
}

.assignment-item {
	background: var(--primary-50);
	border: 1px solid var(--primary-200);
	padding: 1rem 1rem 0 1rem;
	@apply dark:bg-primary-700 dark:border-primary-600 rounded-xl;
}

.assignment-item-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.assignment-label {
	font-weight: 600;
	color: var(--primary-700);
	font-size: 0.875rem;
	@apply dark:text-primary-200;
}

.delete-btn {
	color: var(--el-color-danger);
	padding: 0.25rem;
}

.delete-btn:hover {
	background-color: var(--el-color-danger-light-9);
}

.delete-btn:disabled {
	color: var(--el-color-info-light-5);
	cursor: not-allowed;
}

.empty-assignments {
	text-align: center;
	padding: 2rem;
	border: 1px dashed var(--primary-200);
	background: var(--primary-25);
	@apply dark:bg-primary-700 dark:border-primary-600 rounded-xl;
}

.inactive-icon {
	color: #f56c6c;
	font-size: 14px;
}
</style>
