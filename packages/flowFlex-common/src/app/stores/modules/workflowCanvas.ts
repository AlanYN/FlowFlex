import { defineStore } from 'pinia';
import { store } from '@/stores';
import type { Node, Edge } from '@vue-flow/core';
import type { Stage } from '#/onboard';
import type { StageCondition, StageConditionInput } from '#/condition';
import type {
	CanvasNodeData,
	StageNodeData,
	ConditionNodeData,
	WorkflowCanvasState,
} from '#/workflow-canvas';
import {
	getWorkflowDetail,
	getStagesByWorkflow,
	getConditionsByWorkflow,
	createCondition,
	updateCondition,
	deleteCondition,
} from '@/apis/ow';
import { ElMessage, ElMessageBox } from 'element-plus';

// ============ 常量定义 ============
const STAGE_NODE_WIDTH = 280;
const STAGE_NODE_HEIGHT = 80;
const CONDITION_NODE_HEIGHT = 60;
const VERTICAL_GAP = 100;
const HORIZONTAL_GAP = 150;
const MIN_ZOOM = 0.25;
const MAX_ZOOM = 2;
const ZOOM_STEP = 0.25;

export const useWorkflowCanvasStore = defineStore({
	id: 'item-wfe-workflow-canvas',
	state: (): WorkflowCanvasState => ({
		workflowId: null,
		workflow: null,
		stages: [],
		conditions: [],
		nodes: [],
		edges: [],
		selectedNodeId: null,
		zoom: 1,
		loading: false,
		saving: false,
		error: null,
		hasUnsavedChanges: false,
		panelVisible: false,
	}),

	getters: {
		// 获取选中的节点
		getSelectedNode(state): Node<CanvasNodeData> | null {
			return state.nodes.find((n) => n.id === state.selectedNodeId) || null;
		},

		// 获取缩放百分比
		getZoomPercent(state): number {
			return Math.round(state.zoom * 100);
		},

		// 根据 ID 获取 Stage
		getStageById:
			(state) =>
			(stageId: string): Stage | undefined => {
				return state.stages.find((s) => s.id === stageId);
			},

		// 根据 Stage ID 获取 Condition
		getConditionByStageId:
			(state) =>
			(stageId: string): StageCondition | undefined => {
				return state.conditions.find((c) => c.stageId === stageId);
			},

		// 获取选中节点的类型
		getSelectedNodeType(state): 'stage' | 'condition' | null {
			const node = state.nodes.find((n) => n.id === state.selectedNodeId);
			if (!node) return null;
			return node.data?.type || null;
		},

		// 获取选中的 Stage 数据
		getSelectedStage(state): Stage | null {
			const node = state.nodes.find((n) => n.id === state.selectedNodeId);
			if (!node || node.data?.type !== 'stage') return null;
			return (node.data as StageNodeData).stage;
		},

		// 获取选中的 Condition 数据
		getSelectedCondition(state): StageCondition | null {
			const node = state.nodes.find((n) => n.id === state.selectedNodeId);
			if (!node || node.data?.type !== 'condition') return null;
			return (node.data as ConditionNodeData).condition;
		},
	},

	actions: {
		// ============ 初始化 ============

		/**
		 * 初始化画布
		 */
		async initCanvas(workflowId: string): Promise<void> {
			this.workflowId = workflowId;
			this.resetState();
			await this.loadData();
		},

		/**
		 * 加载所有数据
		 */
		async loadData(): Promise<void> {
			if (!this.workflowId) return;

			this.loading = true;
			this.error = null;

			try {
				await Promise.all([this.loadWorkflow(), this.loadStages(), this.loadConditions()]);
				this.generateNodesAndEdges();
			} catch (error: any) {
				this.error = error.message || 'Failed to load data';
				console.error('Failed to load workflow canvas data:', error);
			} finally {
				this.loading = false;
			}
		},

		/**
		 * 加载工作流详情
		 */
		async loadWorkflow(): Promise<void> {
			if (!this.workflowId) return;
			const res: any = await getWorkflowDetail(this.workflowId);
			if (res.code === '200') {
				this.workflow = res.data;
			}
		},

		/**
		 * 加载 Stages
		 */
		async loadStages(): Promise<void> {
			if (!this.workflowId) return;
			const res: any = await getStagesByWorkflow(this.workflowId);
			if (res.code === '200') {
				this.stages = res.data || [];
			}
		},

		/**
		 * 加载 Conditions
		 */
		async loadConditions(): Promise<void> {
			if (!this.workflowId) return;
			const res: any = await getConditionsByWorkflow(this.workflowId);
			if (res.code === '200') {
				this.conditions = res.data || [];
			}
		},

		// ============ 节点和边生成 ============

		/**
		 * 生成节点和边
		 */
		generateNodesAndEdges(): void {
			const nodes: Node<CanvasNodeData>[] = [];
			const edges: Edge[] = [];

			// 创建 Stage 索引映射
			const stageIndexMap = new Map(this.stages.map((s, i) => [s.id, i]));

			// 生成 Stage 节点
			this.stages.forEach((stage, index) => {
				const hasCondition = this.conditions.some((c) => c.stageId === stage.id);
				const yPos = index * (STAGE_NODE_HEIGHT + VERTICAL_GAP);

				nodes.push({
					id: `stage-${stage.id}`,
					type: 'stage',
					position: { x: 100, y: yPos },
					data: {
						type: 'stage',
						stage,
						hasCondition,
						index,
					},
				});
			});

			// 生成 Condition 节点
			this.conditions.forEach((condition, index) => {
				const stageIndex = stageIndexMap.get(condition.stageId);
				if (stageIndex === undefined) return;

				const stage = this.stages[stageIndex];
				const yPos =
					stageIndex * (STAGE_NODE_HEIGHT + VERTICAL_GAP) + STAGE_NODE_HEIGHT / 5;

				// 创建 stages 名称映射，用于在 ConditionNode 中查找 stage 名称
				const stagesMap: Record<string, string> = {};
				this.stages.forEach((s) => {
					stagesMap[s.id] = s.name;
				});

				nodes.push({
					id: `condition-${condition.id}`,
					type: 'condition',
					position: {
						x:
							100 +
							STAGE_NODE_WIDTH +
							(CONDITION_NODE_HEIGHT + HORIZONTAL_GAP) * (index + 1),
						y: yPos,
					},
					data: {
						type: 'condition',
						// 深拷贝 condition 对象，确保 Vue 能检测到数据变化
						condition: JSON.parse(JSON.stringify(condition)),
						stageId: condition.stageId,
						stageName: stage?.name || '',
						stagesMap,
					},
				});
			});

			// 生成边
			this.stages.forEach((stage, index) => {
				// Stage 之间的默认连接（始终生成，表示正常流程）
				if (index < this.stages.length - 1) {
					edges.push({
						id: `edge-stage-${stage.id}-to-${this.stages[index + 1].id}`,
						source: `stage-${stage.id}`,
						target: `stage-${this.stages[index + 1].id}`,
						sourceHandle: 'bottom-out',
						targetHandle: 'top-in',
						type: 'smoothstep',
						style: { stroke: 'var(--el-border-color)' },
						animated: false,
					});
				}
			});

			// 生成 Condition 相关的边
			this.conditions.forEach((condition) => {
				const sourceStageIndex = stageIndexMap.get(condition.stageId);
				if (sourceStageIndex === undefined) return;

				// Stage 到 Condition 的连接
				edges.push({
					id: `edge-stage-${condition.stageId}-to-condition-${condition.id}`,
					source: `stage-${condition.stageId}`,
					target: `condition-${condition.id}`,
					sourceHandle: 'condition-out',
					type: 'smoothstep',
					style: { stroke: 'var(--el-color-primary)' },
					animated: false,
				});

				// 安全解析 actionsJson（可能是字符串或对象）
				let actions: any[] = [];
				try {
					actions =
						typeof condition.actionsJson === 'string'
							? JSON.parse(condition.actionsJson)
							: condition.actionsJson || [];
				} catch {
					actions = [];
				}

				// 处理每个 action 的边（从底部输出）
				actions.forEach((action: any, actionIndex: number) => {
					if (action.type === 'GoToStage' && action.targetStageId) {
						// GoToStage: 跳转到指定 stage，连接到目标 stage 的右侧
						const targetIndex = stageIndexMap.get(action.targetStageId);
						const isLoop = targetIndex !== undefined && targetIndex <= sourceStageIndex;

						edges.push({
							id: `edge-condition-${condition.id}-action-${actionIndex}-to-${action.targetStageId}`,
							source: `condition-${condition.id}`,
							target: `stage-${action.targetStageId}`,
							sourceHandle: `action-${actionIndex}`,
							targetHandle: 'right-in', // GoToStage 使用第一个右侧 handle
							type: 'smoothstep',
							style: {
								stroke: isLoop
									? 'var(--el-color-danger)'
									: 'var(--el-color-success)',
								strokeDasharray: isLoop ? '5,5' : undefined,
							},
							label: isLoop ? '⚠️ Loop' : `Go To `,
							labelStyle: {
								fill: isLoop ? 'var(--el-color-danger)' : 'var(--el-color-success)',
							},
							animated: false,
						});
					} else if (action.type === 'SkipStage') {
						// SkipStage: 跳过当前 stage，连接到下一个 stage 的右侧
						const nextStageIndex = sourceStageIndex + 1;
						if (nextStageIndex < this.stages.length) {
							const nextStage = this.stages[nextStageIndex];
							edges.push({
								id: `edge-condition-${condition.id}-action-${actionIndex}-skip-to-${nextStage.id}`,
								source: `condition-${condition.id}`,
								target: `stage-${nextStage.id}`,
								sourceHandle: `action-${actionIndex}`,
								targetHandle: 'right-in-2', // SkipStage 使用第二个右侧 handle
								type: 'smoothstep',
								style: { stroke: 'var(--el-color-success)' },
								label: 'Skip',
								labelStyle: { fill: 'var(--el-color-success)' },
								animated: false,
							});
						}
					}
				});

				// 处理条件不满足时的动作（"Fallback" 分支）
				if (condition.fallbackStageId) {
					// 配置了 fallback stage，连接到右侧第三个 handle
					edges.push({
						id: `edge-condition-${condition.id}-fallback-to-${condition.fallbackStageId}`,
						source: `condition-${condition.id}`,
						target: `stage-${condition.fallbackStageId}`,
						sourceHandle: 'fallback-out',
						targetHandle: 'right-in-3', // Fallback 使用第三个右侧 handle
						type: 'smoothstep',
						style: { stroke: 'var(--el-color-warning)' },
						label: 'Fallback',
						labelStyle: { fill: 'var(--el-color-warning)' },
						animated: false,
					});
				}
			});

			this.nodes = nodes;
			this.edges = edges;
		},

		// ============ 节点选择 ============

		/**
		 * 选择节点
		 */
		selectNode(nodeId: string | null): void {
			this.selectedNodeId = nodeId;
			this.panelVisible = nodeId !== null;
		},

		/**
		 * 关闭面板
		 */
		closePanel(): void {
			this.panelVisible = false;
			this.selectedNodeId = null;
		},

		// ============ Condition CRUD ============

		/**
		 * 创建 Condition
		 */
		async createConditionForStage(
			stageId: string,
			input: StageConditionInput
		): Promise<boolean> {
			if (!this.workflowId) return false;

			this.saving = true;
			try {
				const res: any = await createCondition({
					stageId,
					workflowId: this.workflowId,
					name: input.name,
					description: input.description,
					rulesJson: JSON.stringify(input.rulesJson),
					actionsJson: JSON.stringify(input.actionsJson),
					fallbackStageId: input.fallbackStageId,
					isActive: input.isActive ?? true,
				});

				if (res.code === '200') {
					ElMessage.success('Condition created successfully');
					await this.loadConditions();
					this.generateNodesAndEdges();
					this.hasUnsavedChanges = false;
					return true;
				} else {
					ElMessage.error(res.msg || 'Failed to create condition');
					return false;
				}
			} catch (error: any) {
				ElMessage.error(error.message || 'Failed to create condition');
				return false;
			} finally {
				this.saving = false;
			}
		},

		/**
		 * 更新 Condition
		 */
		async updateConditionById(
			conditionId: string,
			stageId: string,
			input: StageConditionInput
		): Promise<boolean> {
			if (!this.workflowId) return false;

			this.saving = true;
			try {
				const res: any = await updateCondition(conditionId, {
					stageId,
					workflowId: this.workflowId,
					name: input.name,
					description: input.description,
					rulesJson: JSON.stringify(input.rulesJson),
					actionsJson: JSON.stringify(input.actionsJson),
					fallbackStageId: input.fallbackStageId,
					isActive: input.isActive ?? true,
				});

				if (res.code === '200') {
					ElMessage.success('Condition updated successfully');
					await this.loadConditions();
					this.generateNodesAndEdges();
					this.hasUnsavedChanges = false;
					return true;
				} else {
					res?.msg &&
						ElMessageBox.confirm(res.msg, '⚠️ Save Condition Error', {
							confirmButtonText: 'Confirm',
							confirmButtonClass: 'danger-confirm-btn',
							showCancelButton: false,
							showConfirmButton: true,
							beforeClose: async (action, instance, done) => {
								done(); // 取消或关闭时直接关闭对话框
							},
						});

					return false;
				}
			} catch (error: any) {
				ElMessage.error(error.message || 'Failed to update condition');
				return false;
			} finally {
				this.saving = false;
			}
		},

		/**
		 * 删除 Condition
		 */
		async deleteConditionById(conditionId: string): Promise<boolean> {
			this.saving = true;
			try {
				const res: any = await deleteCondition(conditionId);

				if (res.code === '200') {
					ElMessage.success('Condition deleted successfully');
					await this.loadConditions();
					this.generateNodesAndEdges();
					this.closePanel();
					return true;
				} else {
					ElMessage.error(res.msg || 'Failed to delete condition');
					return false;
				}
			} catch (error: any) {
				ElMessage.error(error.message || 'Failed to delete condition');
				return false;
			} finally {
				this.saving = false;
			}
		},

		// ============ 缩放控制 ============

		/**
		 * 设置缩放
		 */
		setZoom(zoom: number): void {
			this.zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, zoom));
		},

		/**
		 * 放大
		 */
		zoomIn(): void {
			this.setZoom(this.zoom + ZOOM_STEP);
		},

		/**
		 * 缩小
		 */
		zoomOut(): void {
			this.setZoom(this.zoom - ZOOM_STEP);
		},

		// ============ 状态管理 ============

		/**
		 * 设置未保存更改状态
		 */
		setHasUnsavedChanges(value: boolean): void {
			this.hasUnsavedChanges = value;
		},

		/**
		 * 重置状态
		 */
		resetState(): void {
			this.workflow = null;
			this.stages = [];
			this.conditions = [];
			this.nodes = [];
			this.edges = [];
			this.selectedNodeId = null;
			this.zoom = 1;
			this.loading = false;
			this.saving = false;
			this.error = null;
			this.hasUnsavedChanges = false;
			this.panelVisible = false;
		},
	},
});

// 在 setup 外部使用
export function useWorkflowCanvasStoreWithOut() {
	return useWorkflowCanvasStore(store);
}
