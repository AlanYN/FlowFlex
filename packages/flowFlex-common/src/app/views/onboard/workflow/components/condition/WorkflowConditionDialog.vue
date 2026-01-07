<template>
	<el-dialog
		v-model="visible"
		title="Workflow Conditions"
		:width="900"
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
					Configure conditions for each stage to control workflow flow and trigger
					actions.
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
					:class="{ 'has-connector': index < stages.length - 1 }"
				>
					<!-- Stage 卡片 -->
					<div
						class="stage-condition-item"
						:style="{ borderLeftColor: stage.color || 'var(--el-color-primary)' }"
					>
						<!-- Stage 信息 -->
						<div class="stage-info">
							<div
								class="stage-avatar"
								:style="{
									backgroundColor: stage.color || 'var(--el-color-primary)',
								}"
							>
								{{ index + 1 }}
							</div>
							<div class="stage-details">
								<span class="stage-name">{{ stage.name }}</span>
								<span v-if="stage.description" class="stage-desc">
									{{ stage.description }}
								</span>
							</div>
						</div>

						<!-- Condition 操作区 -->
						<div class="condition-actions">
							<!-- 已有 Condition -->
							<template v-if="getStageCondition(stage.id)">
								<div class="condition-summary">
									<el-tag type="primary">
										<el-icon class="mr-1"><Connection /></el-icon>
										{{ getStageCondition(stage.id)?.name }}
									</el-tag>
									<span class="condition-rules-count">
										{{ getRulesCount(stage.id) }} rules
									</span>
								</div>
								<div class="condition-btns">
									<el-button
										text
										type="primary"
										@click="handleEditCondition(stage)"
									>
										<el-icon><Edit /></el-icon>
										Edit
									</el-button>
									<el-button
										text
										type="danger"
										@click="handleDeleteCondition(stage)"
									>
										<el-icon><Delete /></el-icon>
										Delete
									</el-button>
								</div>
							</template>

							<!-- 无 Condition -->
							<template v-else>
								<span class="no-condition-text">No condition configured</span>
								<el-button type="primary" plain @click="handleAddCondition(stage)">
									<el-icon><Plus /></el-icon>
									Add Condition
								</el-button>
							</template>
						</div>
					</div>

					<!-- 连接线 -->
					<div v-if="index < stages.length - 1" class="stage-connector">
						<div class="connector-line"></div>
						<div class="connector-arrow">
							<el-icon><ArrowDown /></el-icon>
						</div>
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
			v-model:visible="editorVisible"
			:stage-id="currentStage?.id || ''"
			:stage-name="currentStage?.name || ''"
			:condition="currentCondition"
			:stages="stages"
			:current-stage-index="currentStageIndex"
			:workflow-id="workflowId"
			@save="handleSaveCondition"
			@cancel="editorVisible = false"
		/>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, PropType } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Edit, Delete, Connection, ArrowDown } from '@element-plus/icons-vue';
import { Stage } from '#/onboard';
import type { StageCondition } from '#/condition';
import StageConditionEditor from './StageConditionEditor.vue';
import { getConditionsByWorkflow, deleteCondition as deleteConditionApi } from '@/apis/ow';

const props = defineProps({
	modelValue: {
		type: Boolean,
		default: false,
	},
	workflowId: {
		type: String,
		required: true,
	},
	stages: {
		type: Array as PropType<Stage[]>,
		default: () => [],
	},
});

const emit = defineEmits(['update:modelValue', 'refresh']);

// 弹窗可见性
const visible = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

// 状态
const loading = ref(false);
const conditions = ref<StageCondition[]>([]);
const editorVisible = ref(false);
const currentStage = ref<Stage | null>(null);
const currentCondition = ref<StageCondition | null>(null);
const currentStageIndex = ref(0);

// 获取 Stage 的 Condition
const getStageCondition = (stageId: string): StageCondition | undefined => {
	return conditions.value.find((c) => c.stageId === stageId);
};

// 获取规则数量
const getRulesCount = (stageId: string): number => {
	const condition = getStageCondition(stageId);
	return condition?.rulesJson?.rules?.length || 0;
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

// 添加 Condition
const handleAddCondition = (stage: Stage) => {
	currentStage.value = stage;
	currentCondition.value = null;
	currentStageIndex.value = props.stages.findIndex((s) => s.id === stage.id);
	editorVisible.value = true;
};

// 编辑 Condition
const handleEditCondition = (stage: Stage) => {
	currentStage.value = stage;
	currentCondition.value = getStageCondition(stage.id) || null;
	currentStageIndex.value = props.stages.findIndex((s) => s.id === stage.id);
	editorVisible.value = true;
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

		const res: any = await deleteConditionApi(stage.id, condition.id);
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
	editorVisible.value = false;
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
	min-height: 300px;
	max-height: 60vh;
	overflow-y: auto;
	padding: 4px;
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
	padding: 16px;
	border: 1px solid var(--el-border-color-light);
	border-radius: 12px;
	border-left-width: 4px;
	border-left-style: solid;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
	transition: all 0.2s ease;
}

.stage-condition-item:hover {
	box-shadow: 0 6px 20px rgba(0, 0, 0, 0.08);
	transform: translateY(-1px);
}

/* 连接线样式 */
.stage-connector {
	display: flex;
	flex-direction: column;
	align-items: center;
	padding: 8px 0;
}

.connector-line {
	width: 2px;
	height: 16px;
	background: var(--el-border-color);
}

.connector-arrow {
	color: var(--el-text-color-placeholder);
	font-size: 14px;
	line-height: 1;
}

.stage-info {
	display: flex;
	align-items: center;
	gap: 12px;
	flex: 1;
	min-width: 0;
}

.stage-avatar {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	font-weight: 600;
	color: var(--el-color-white);
	flex-shrink: 0;
}

.stage-details {
	display: flex;
	flex-direction: column;
	gap: 2px;
	min-width: 0;
	flex: 1;
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

.condition-actions {
	display: flex;
	align-items: center;
	gap: 16px;
	flex-shrink: 0;
}

.condition-summary {
	display: flex;
	align-items: center;
	gap: 8px;
}

.condition-rules-count {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.condition-btns {
	display: flex;
	gap: 4px;
}

.no-condition-text {
	font-size: 13px;
	color: var(--el-text-color-placeholder);
}

.empty-state {
	padding: 40px 0;
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
}

html.dark .stage-condition-item:hover {
	box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
}

html.dark .connector-line {
	background: var(--el-border-color-darker);
}
</style>
