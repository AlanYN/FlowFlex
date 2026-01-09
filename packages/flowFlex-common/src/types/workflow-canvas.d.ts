/**
 * Workflow Canvas 类型定义
 * 用于可视化流程编辑器
 */

import type { Node, Edge } from '@vue-flow/core';
import type { Stage } from './onboard';
import type { StageCondition } from './condition';

// ============ 节点数据类型 ============

/**
 * Stage 节点数据
 */
export interface StageNodeData {
	type: 'stage';
	stage: Stage;
	hasCondition: boolean;
	index: number;
}

/**
 * Condition 节点数据
 */
export interface ConditionNodeData {
	type: 'condition';
	condition: StageCondition;
	stageId: string;
	stageName: string;
	/** 所有 stages 的映射，用于查找 stage 名称 */
	stagesMap?: Record<string, string>;
}

/**
 * 画布节点数据联合类型
 */
export type CanvasNodeData = StageNodeData | ConditionNodeData;

// ============ 边类型 ============

/**
 * 边类型
 */
export type EdgeType = 'default' | 'success' | 'fallback' | 'loop';

// ============ Store 状态类型 ============

/**
 * Workflow Canvas Store 状态
 */
export interface WorkflowCanvasState {
	// 基础数据
	workflowId: string | null;
	workflow: any | null;
	stages: Stage[];
	conditions: StageCondition[];

	// 画布状态
	nodes: Node<CanvasNodeData>[];
	edges: Edge[];
	selectedNodeId: string | null;
	zoom: number;

	// UI 状态
	loading: boolean;
	saving: boolean;
	error: string | null;
	hasUnsavedChanges: boolean;
	panelVisible: boolean;
}
