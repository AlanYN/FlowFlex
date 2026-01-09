<template>
	<el-dialog
		v-model="visible"
		title="Workflow Conditions"
		:width="800"
		destroy-on-close
		:show-close="true"
		:close-on-click-modal="false"
		top="5vh"
		append-to-body
	>
		<template #header>
			<div class="dialog-header">
				<h2 class="dialog-title">Workflow Conditions</h2>
				<p class="dialog-subtitle">
					Configure conditions for each stage to control workflow transitions.
				</p>
			</div>
		</template>

		<div class="condition-dialog-content">
			<!-- 加载状态 -->
			<div v-if="loading">
				<el-skeleton :rows="5" animated />
			</div>

			<!-- Stage 列表 -->
			<div v-else class="stages-condition-list">
				<div
					v-for="(stage, index) in stages"
					:key="stage.id"
					class="stage-condition-wrapper"
				>
					<!-- Stage 卡片 -->
					<div
						class="stage-condition-item"
						:class="{ 'has-condition': getStageCondition(stage.id) }"
						@click="handleStageClick(stage)"
					>
						<!-- 左侧：Stage 信息 -->
						<div class="stage-left">
							<div
								class="stage-order"
								:style="{
									backgroundColor: stage.color || 'var(--el-color-primary)',
								}"
							>
								{{ index + 1 }}
							</div>
							<div class="stage-info">
								<div class="flex items-center gap-x-2">
									<span class="stage-name">{{ stage.name }}</span>
									<el-tooltip
										v-if="stage.required"
										content="Users must complete this stage before proceeding to subsequent stages"
										placement="top"
									>
										<div
											class="text-orange-400 px-2 border border-orange-400 rounded-xl flex items-center gap-x-2 dark:bg-orange-900"
										>
											<Icon icon="mdi:information-outline" />
											Required
										</div>
									</el-tooltip>
								</div>
								<span class="stage-desc">
									{{ stage.components?.length || 0 }}
									{{ stage.components.length > 1 ? 'Components' : 'Component' }}
								</span>
							</div>
						</div>

						<!-- 右侧：Condition 状态 -->
						<div class="stage-right">
							<template v-if="getStageCondition(stage.id)">
								<el-tag type="primary" size="small">
									{{ getStageCondition(stage.id)?.name }}
								</el-tag>
								<el-dropdown
									trigger="click"
									@command="(cmd: string) => handleConditionCommand(cmd, stage)"
									@click.stop
								>
									<el-button text class="more-btn" @click.stop>
										<el-icon><MoreFilled /></el-icon>
									</el-button>
									<template #dropdown>
										<el-dropdown-menu>
											<el-dropdown-item command="edit">
												<el-icon><Edit /></el-icon>
												Edit Condition
											</el-dropdown-item>
											<el-dropdown-item command="delete" divided>
												<el-icon class="text-red-500"><Delete /></el-icon>
												<span class="text-red-500">Delete</span>
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</template>
							<template v-else>
								<span class="no-condition">No condition</span>
								<el-icon class="arrow-icon"><ArrowRight /></el-icon>
							</template>
						</div>
					</div>

					<!-- 连接线 -->
					<div v-if="index < stages.length - 1" class="stage-connector">
						<div class="connector-line"></div>
					</div>
				</div>

				<!-- 空状态 -->
				<div v-if="stages.length === 0" class="empty-state">
					<el-empty description="No stages in this workflow" />
				</div>
			</div>
		</div>

		<template #footer>
			<div class="dialog-footer">
				<el-button @click="handleClose">Close</el-button>
			</div>
		</template>

		<!-- Condition 编辑器抽屉 -->
		<StageConditionEditor
			ref="conditionEditorRef"
			:stages="stages"
			:workflow-id="workflowId"
			@save="handleSaveCondition"
		/>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, useTemplateRef } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Edit, Delete, ArrowRight, MoreFilled } from '@element-plus/icons-vue';
import { Stage } from '#/onboard';
import type { StageCondition } from '#/condition';
import StageConditionEditor from './StageConditionEditor.vue';
import { getConditionsByWorkflow, deleteCondition as deleteConditionApi } from '@/apis/ow';

const props = defineProps<{
	modelValue: boolean;
	workflowId: string;
	stages: Stage[];
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', value: boolean): void;
	(e: 'refresh'): void;
}>();

// 弹窗可见性
const visible = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

// 状态
const loading = ref(false);
const conditions = ref<StageCondition[]>([]);
const conditionEditorRef = useTemplateRef('conditionEditorRef');

// 获取 Stage 的 Condition
const getStageCondition = (stageId: string): StageCondition | undefined => {
	return conditions.value.find((c) => c.stageId === stageId);
};

// 加载 Conditions
const fetchConditions = async () => {
	if (!props.workflowId) return;
	try {
		loading.value = true;
		const res: any = await getConditionsByWorkflow(props.workflowId);
		if (res.code === '200') {
			conditions.value = res.data || [];
		} else {
			conditions.value = [];
		}
	} catch (error) {
		console.error('Failed to fetch conditions:', error);
		conditions.value = [];
	} finally {
		loading.value = false;
	}
};

// 监听弹窗打开
watch(
	() => props.modelValue,
	(val) => {
		if (val) {
			fetchConditions();
		}
	}
);

// 点击 Stage 卡片
const handleStageClick = (stage: Stage) => {
	const stageIndex = props.stages.findIndex((s) => s.id === stage.id);
	const condition = getStageCondition(stage.id);
	conditionEditorRef.value?.open(stage.id, stage.name, stageIndex, condition);
};

// 处理 Condition 命令
const handleConditionCommand = (command: string, stage: Stage) => {
	if (command === 'edit') {
		handleStageClick(stage);
	} else if (command === 'delete') {
		handleDeleteCondition(stage);
	}
};

// 删除 Condition
const handleDeleteCondition = async (stage: Stage) => {
	const condition = getStageCondition(stage.id);
	if (!condition) return;

	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete the condition "${condition.name}"?`,
			'Delete Condition',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		const res: any = await deleteConditionApi(condition.id);
		if (res.code === '200') {
			ElMessage.success('Condition deleted successfully');
			await fetchConditions();
			emit('refresh');
		}
	} catch (error) {
		// 用户取消或请求失败
	}
};

// 保存 Condition
const handleSaveCondition = async () => {
	await fetchConditions();
	emit('refresh');
};

// 关闭弹窗
const handleClose = () => {
	visible.value = false;
};
</script>

<style scoped>
.dialog-header {
	padding-bottom: 8px;
}

.dialog-title {
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin: 0;
}

.dialog-subtitle {
	font-size: 14px;
	color: var(--el-text-color-secondary);
	margin: 8px 0 0 0;
}

.condition-dialog-content {
	min-height: 200px;
	max-height: 60vh;
	overflow-y: auto;
}

.stages-condition-list {
	display: flex;
	flex-direction: column;
}

.stage-condition-wrapper {
	position: relative;
}

.stage-condition-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 12px 16px;
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
	cursor: pointer;
	transition: all 0.2s ease;
	background: var(--el-bg-color);
}

.stage-condition-item:hover {
	border-color: var(--el-color-primary-light-5);
	background: var(--el-color-primary-light-9);
}

.stage-left {
	display: flex;
	align-items: center;
	gap: 12px;
	flex: 1;
	min-width: 0;
}

.stage-order {
	width: 28px;
	height: 28px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	font-weight: 600;
	color: #fff;
	flex-shrink: 0;
}

.stage-info {
	display: flex;
	flex-direction: column;
	gap: 2px;
	min-width: 0;
}

.stage-name {
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.stage-desc {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.stage-right {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-shrink: 0;
}

.more-btn {
	padding: 4px;
	height: auto;
}

.no-condition {
	font-size: 13px;
	color: var(--el-text-color-placeholder);
}

.arrow-icon {
	color: var(--el-text-color-placeholder);
	font-size: 14px;
}

/* 连接线 */
.stage-connector {
	display: flex;
	justify-content: center;
	padding: 4px 0;
}

.connector-line {
	width: 2px;
	height: 12px;
	background: var(--el-border-color);
}

.empty-state {
	padding: 40px 0;
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
}

/* Dark mode */
html.dark .stage-condition-item {
	background: var(--el-bg-color);
}

html.dark .stage-condition-item:hover {
	background: var(--el-color-primary-light-9);
}
</style>
