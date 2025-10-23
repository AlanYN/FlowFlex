<template>
	<div class="custom-tree">
		<div v-if="!data || (data.length === 0 && !loading)" class="empty-tree">
			<slot name="empty">No data</slot>
		</div>
		<div v-else class="tree-nodes">
			<CustomTreeNode
				v-for="(node, index) in data"
				:key="`${node.id}-${index}`"
				:node="node"
				:level="0"
				:show-checkbox="showCheckbox"
				:checked-keys="checkedKeys"
				:expanded-keys="expandedKeys"
				:indeterminate-keys="indeterminateKeys"
				:indent="indent"
				:check-strictly="checkStrictly"
				@node-click="handleNodeClick"
				@node-check="handleNodeCheck"
				@node-expand="handleNodeExpand"
			>
				<template #default="slotProps">
					<slot :node="slotProps.node" :data="slotProps.data"></slot>
				</template>
			</CustomTreeNode>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import CustomTreeNode from './CustomTreeNode.vue';
import type { FlowflexUser } from '#/golbal';

interface TreeNode extends FlowflexUser {}

interface Props {
	data: TreeNode[];
	showCheckbox?: boolean;
	defaultExpandAll?: boolean;
	defaultCheckedKeys?: string[];
	defaultExpandedKeys?: string[];
	indent?: number;
	checkStrictly?: boolean;
	loading?: boolean;
}

interface Emits {
	(e: 'node-click', node: TreeNode, nodeData: TreeNode): void;
	(
		e: 'check',
		node: TreeNode,
		checkState: { checkedKeys: string[]; checkedNodes: TreeNode[] }
	): void;
	(e: 'node-expand', node: TreeNode, expanded: boolean): void;
}

const props = withDefaults(defineProps<Props>(), {
	showCheckbox: false,
	defaultExpandAll: false,
	defaultCheckedKeys: () => [],
	defaultExpandedKeys: () => [],
	indent: 20,
	checkStrictly: false,
});

const emit = defineEmits<Emits>();

// 内部状态
const checkedKeys = ref<Set<string>>(new Set(props.defaultCheckedKeys));
const expandedKeys = ref<Set<string>>(new Set(props.defaultExpandedKeys));
const indeterminateKeys = ref<Set<string>>(new Set());

// 计算属性（现在直接使用 ref 值）

// 获取所有节点（扁平化）
const getAllNodes = (nodes: TreeNode[], result: TreeNode[] = []): TreeNode[] => {
	nodes.forEach((node) => {
		result.push(node);
		if (node.children && node.children.length > 0) {
			getAllNodes(node.children, result);
		}
	});
	return result;
};

// 根据ID查找所有相同ID的节点
const findNodesByIds = (targetId: string): TreeNode[] => {
	const allNodes = getAllNodes(props.data);
	return allNodes.filter((node) => node.id === targetId);
};

// 初始化展开状态
const initializeExpandedKeys = () => {
	if (props.defaultExpandAll) {
		const allNodes = getAllNodes(props.data);
		const allKeys = allNodes
			.filter((node) => node.children && node.children.length > 0)
			.map((node) => node.id);
		expandedKeys.value = new Set([...expandedKeys.value, ...allKeys]);
	}
};

// 处理节点点击
const handleNodeClick = (node: TreeNode) => {
	emit('node-click', node, node);
};

// 更新所有父节点状态（简化版本）
const updateAllParentStates = (): void => {
	// 如果是严格模式，不更新父节点状态
	if (props.checkStrictly) return;

	// 递归更新每个节点的状态
	const updateNodeState = (node: TreeNode): void => {
		if (node.children && node.children.length > 0) {
			// 先递归更新所有子节点
			node.children.forEach((child) => updateNodeState(child));

			// 然后更新当前节点状态
			const allChildrenChecked = node.children.every((child) =>
				isNodeEffectivelyChecked(child)
			);
			const someChildrenChecked = node.children.some((child) =>
				isNodeEffectivelyChecked(child)
			);

			if (allChildrenChecked) {
				// 所有子节点都选中，选中父节点，移除半选状态
				checkedKeys.value.add(node.id);
				indeterminateKeys.value.delete(node.id);
			} else if (someChildrenChecked) {
				// 部分子节点选中，父节点设为半选状态
				checkedKeys.value.delete(node.id);
				indeterminateKeys.value.add(node.id);
			} else {
				// 没有子节点选中，取消父节点和半选状态
				checkedKeys.value.delete(node.id);
				indeterminateKeys.value.delete(node.id);
			}
		}
	};

	// 从根节点开始更新
	props.data.forEach((rootNode) => updateNodeState(rootNode));
};

// 检查节点是否应该被认为是"选中"状态（直接选中或所有子节点都选中）
const isNodeEffectivelyChecked = (node: TreeNode): boolean => {
	// 如果节点直接被选中，返回 true
	if (checkedKeys.value.has(node.id)) return true;

	// 如果节点没有子节点，只看直接选中状态
	if (!node.children || node.children.length === 0) return false;

	// 如果节点有子节点，检查是否所有子节点都被"有效选中"
	return node.children.every((child) => isNodeEffectivelyChecked(child));
};

// 递归选中/取消所有子节点
const updateChildNodes = (node: TreeNode, checked: boolean): void => {
	if (node.children && node.children.length > 0) {
		node.children.forEach((child) => {
			if (checked) {
				checkedKeys.value.add(child.id);
				indeterminateKeys.value.delete(child.id);
			} else {
				checkedKeys.value.delete(child.id);
				indeterminateKeys.value.delete(child.id);
			}
			// 递归处理子节点的子节点
			updateChildNodes(child, checked);
		});
	}
};

// 处理节点选中/取消选中
const handleNodeCheck = (node: TreeNode, checked: boolean) => {
	// 首先更新当前节点的状态，清除半选状态
	if (checked) {
		checkedKeys.value.add(node.id);
		indeterminateKeys.value.delete(node.id);
	} else {
		checkedKeys.value.delete(node.id);
		indeterminateKeys.value.delete(node.id);
	}

	// 找到所有相同ID的其他节点并联动选择
	const sameIdNodes = findNodesByIds(node.id);
	sameIdNodes.forEach((sameNode) => {
		if (checked) {
			checkedKeys.value.add(sameNode.id);
			indeterminateKeys.value.delete(sameNode.id);
		} else {
			checkedKeys.value.delete(sameNode.id);
			indeterminateKeys.value.delete(sameNode.id);
		}
	});

	// 如果不是严格模式，执行父子节点联动逻辑
	if (!props.checkStrictly) {
		// 如果选中父节点，自动选中所有子节点
		if (checked && node.children && node.children.length > 0) {
			updateChildNodes(node, true);
		}

		// 如果取消父节点，自动取消所有子节点
		if (!checked && node.children && node.children.length > 0) {
			updateChildNodes(node, false);
		}

		// 更新所有父节点状态
		updateAllParentStates();
	}

	// 获取当前选中的节点数据
	const allNodes = getAllNodes(props.data);
	const checkedNodes = allNodes.filter((n) => checkedKeys.value.has(n.id));

	// 触发事件
	emit('check', node, {
		checkedKeys: Array.from(checkedKeys.value),
		checkedNodes: checkedNodes,
	});
};

// 处理节点展开/折叠
const handleNodeExpand = (node: TreeNode, expanded: boolean) => {
	if (expanded) {
		expandedKeys.value.add(node.id);
	} else {
		expandedKeys.value.delete(node.id);
	}
	emit('node-expand', node, expanded);
};

// 公开方法
const getCheckedKeys = (): string[] => {
	return Array.from(checkedKeys.value);
};

const getCheckedNodes = (): TreeNode[] => {
	const allNodes = getAllNodes(props.data);
	return allNodes.filter((node) => checkedKeys.value.has(node.id));
};

const setCheckedKeys = (keys: string[]) => {
	checkedKeys.value = new Set(keys);
	indeterminateKeys.value.clear();

	// 如果不是严格模式，设置后重新评估所有父节点状态
	if (!props.checkStrictly) {
		updateAllParentStates();
	}
};

const setChecked = (key: string, checked: boolean) => {
	if (checked) {
		checkedKeys.value.add(key);
	} else {
		checkedKeys.value.delete(key);
	}
	// 如果不是严格模式，设置后重新评估所有父节点状态
	if (!props.checkStrictly) {
		updateAllParentStates();
	}
};

// 监听数据变化，重新初始化展开状态
watch(
	() => props.data,
	() => {
		nextTick(() => {
			initializeExpandedKeys();
		});
	},
	{ immediate: true }
);

// 监听默认选中键值变化
watch(
	() => props.defaultCheckedKeys,
	(newKeys) => {
		checkedKeys.value = new Set(newKeys);
		// 如果不是严格模式，重新评估所有父节点状态
		if (!props.checkStrictly) {
			nextTick(() => {
				updateAllParentStates();
			});
		}
	},
	{ immediate: true }
);

// 暴露方法给父组件
defineExpose({
	getCheckedKeys,
	getCheckedNodes,
	setCheckedKeys,
	setChecked,
});
</script>

<style scoped>
.custom-tree {
	width: 100%;
}

.empty-tree {
	padding: 20px;
	text-align: center;
	color: var(--el-text-color-secondary);
}

.tree-nodes {
	width: 100%;
}
</style>
