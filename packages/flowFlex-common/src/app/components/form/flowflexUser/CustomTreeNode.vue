<template>
	<div class="tree-node">
		<!-- 节点内容 -->
		<div
			class="tree-node-content"
			:style="{ paddingLeft: `${level * indent}px` }"
			@click="handleNodeClick"
		>
			<!-- 展开/折叠图标 -->
			<div class="expand-icon" @click.stop="handleExpandClick">
				<el-icon v-if="hasChildren" class="expand-arrow" :class="{ expanded: isExpanded }">
					<ArrowRight />
				</el-icon>
				<span v-else class="expand-placeholder"></span>
			</div>

			<!-- 复选框 -->
			<el-checkbox
				v-if="showCheckbox"
				:model-value="isChecked"
				:indeterminate="isIndeterminate"
				@change="handleCheckChange"
				@click.stop
				class="tree-checkbox"
			/>

			<!-- 节点内容插槽 -->
			<div class="node-content">
				<slot :node="node" :data="node">
					<span>{{ node.name }}</span>
				</slot>
			</div>
		</div>

		<!-- 子节点 -->
		<div v-if="hasChildren && isExpanded" class="tree-children">
			<CustomTreeNode
				v-for="(child, index) in node.children"
				:key="`${child.id}-${index}`"
				:node="child"
				:level="level + 1"
				:show-checkbox="showCheckbox"
				:checked-keys="checkedKeys"
				:expanded-keys="expandedKeys"
				:indeterminate-keys="indeterminateKeys"
				:indent="indent"
				:check-strictly="checkStrictly"
				@node-click="(node) => $emit('node-click', node)"
				@node-check="(node, checked) => $emit('node-check', node, checked)"
				@node-expand="(node, expanded) => $emit('node-expand', node, expanded)"
			>
				<template #default="slotProps">
					<slot :node="slotProps.node" :data="slotProps.data"></slot>
				</template>
			</CustomTreeNode>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { ElCheckbox, ElIcon } from 'element-plus';
import { ArrowRight } from '@element-plus/icons-vue';
import type { FlowflexUser } from '#/golbal';

interface TreeNode extends FlowflexUser {}

interface Props {
	node: TreeNode;
	level: number;
	showCheckbox: boolean;
	checkedKeys: Set<string>;
	expandedKeys: Set<string>;
	indeterminateKeys: Set<string>;
	indent: number;
	checkStrictly: boolean;
}

interface Emits {
	(e: 'node-click', node: TreeNode): void;
	(e: 'node-check', node: TreeNode, checked: boolean): void;
	(e: 'node-expand', node: TreeNode, expanded: boolean): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 计算属性
const hasChildren = computed(() => {
	return props.node.children && props.node.children.length > 0;
});

const isExpanded = computed(() => {
	return props.expandedKeys.has(props.node.id);
});

const isChecked = computed(() => {
	return props.checkedKeys.has(props.node.id);
});

const isIndeterminate = computed(() => {
	// 严格模式下不显示半选状态
	if (props.checkStrictly) return false;
	return props.indeterminateKeys.has(props.node.id);
});

// 事件处理
const handleNodeClick = () => {
	emit('node-click', props.node);
};

const handleExpandClick = () => {
	if (hasChildren.value) {
		emit('node-expand', props.node, !isExpanded.value);
	}
};

const handleCheckChange = (checked: boolean | string | number) => {
	const isChecked = Boolean(checked);
	console.log('TreeNode: Check change', {
		nodeId: props.node.id,
		nodeName: props.node.name,
		checked: isChecked,
	});
	emit('node-check', props.node, isChecked);
};
</script>

<style scoped>
.tree-node {
	width: 100%;
}

.tree-node-content {
	display: flex;
	align-items: center;
	padding: 4px 0;
	cursor: pointer;
	transition: background-color 0.2s;
	min-height: 32px;
}

.tree-node-content:hover {
	background-color: var(--el-fill-color-light);
}

.expand-icon {
	width: 24px;
	height: 24px;
	display: flex;
	align-items: center;
	justify-content: center;
	cursor: pointer;
	flex-shrink: 0;
}

.expand-arrow {
	transition: transform 0.2s;
	color: var(--el-text-color-regular);
}

.expand-arrow.expanded {
	transform: rotate(90deg);
}

.expand-placeholder {
	width: 16px;
	height: 16px;
}

.tree-checkbox {
	margin-right: 8px;
	flex-shrink: 0;
}

.node-content {
	flex: 1;
	min-width: 0;
	display: flex;
	align-items: center;
}

.tree-children {
	width: 100%;
}

/* Dark mode support */
html.dark .tree-node-content:hover {
	background-color: var(--el-fill-color);
}
</style>
