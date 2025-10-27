<template>
	<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 p-1">
		<!-- 左侧：View Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">View Permission</label>

				<el-select
					v-model="localPermissions.viewPermissionMode"
					placeholder="Select permission type"
					class="w-full"
					@change="leftTypeChange"
				>
					<el-option
						v-for="option in permissionTypeOptions"
						:key="option.value"
						:label="option.label"
						:value="option.value"
					/>
				</el-select>
			</div>

			<!-- Team（仅在 VisibleToTeams 或 InvisibleToTeams 时显示）-->
			<div v-if="shouldShowTeamSelector" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					ref="viewTeamSelectorRef"
					v-model="localPermissions.viewTeams"
					selectionType="team"
					:clearable="true"
					@change="leftChange"
				/>
			</div>
		</div>

		<!-- 右侧：Operate Permission -->
		<div class="space-y-4 w-full">
			<div class="space-y-2">
				<label class="text-base font-bold">Operate Permission</label>

				<el-checkbox v-model="localPermissions.useSameGroups">
					{{ checkboxLabel }}
				</el-checkbox>
			</div>

			<!-- Team（仅在不勾选 useSameGroups 时显示）-->
			<div v-if="!localPermissions.useSameGroups" class="space-y-2">
				<label class="text-base font-bold">Team</label>
				<FlowflexUserSelector
					v-model="localPermissions.operateTeams"
					selectionType="team"
					:clearable="true"
					:choosable-tree-data="operateChoosableTreeData"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, computed, ref, toRaw } from 'vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

// Props
interface Props {
	modelValue?: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameGroups: boolean;
		operateTeams: string[];
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		useSameGroups: true,
		operateTeams: [],
	}),
});

// Emits
// const emit = defineEmits(['update:modelValue']);

// 权限类型选项
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleToTeams },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleToTeams },
];

// 是否需要显示 Team 选择器 - 只有 VisibleToTeams 和 InvisibleToTeams 需要选择团队
const shouldShowTeamSelector = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	// Public 模式下不显示
	return (
		mode === ViewPermissionModeEnum.VisibleToTeams ||
		mode === ViewPermissionModeEnum.InvisibleToTeams
	);
});

// 动态checkbox文案 - Invisible模式显示特殊文案
const checkboxLabel = computed(() => {
	const mode = localPermissions.viewPermissionMode;
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		return 'Use same team that have view permission (not editable)';
	}
	return 'Use same team that have view permission';
});

// 本地权限数据
const localPermissions = reactive({
	viewPermissionMode: props.modelValue.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue.viewTeams || [])],
	useSameGroups: props.modelValue.useSameGroups ?? true,
	operateTeams: [...(props.modelValue.operateTeams || [])],
});

// 左侧 Team 选择器的 ref
const viewTeamSelectorRef = ref<InstanceType<typeof FlowflexUserSelector> | null>(null);

// 获取 menuStore 实例
const menuStore = menuRoles();

// 右侧可选的树形数据
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

const leftTypeChange = () => {
	const treedata = viewTeamSelectorRef.value?.getSelectedData() || [];
	leftChange(localPermissions.viewTeams, treedata);
};

const leftChange = async (value, selectData) => {
	const mode = localPermissions.viewPermissionMode;

	// Convert selectData to array if it's not already
	const selectedNodes = Array.isArray(selectData) ? toRaw(selectData) : [toRaw(selectData)];
	console.log('selectedNodes:', selectedNodes);

	// Build a map of selected IDs for quick lookup
	const selectedIdSet = new Set<string>(value as string[]);

	// Build parent-child relationship map
	const nodeMap = new Map<string, FlowflexUser>();
	const childToParentMap = new Map<string, string>();

	function buildMaps(nodes: FlowflexUser[], parentId?: string) {
		nodes.forEach((node) => {
			nodeMap.set(node.id, node);
			if (parentId) {
				childToParentMap.set(node.id, parentId);
			}
			if (node.children && node.children.length > 0) {
				buildMaps(node.children, node.id);
			}
		});
	}

	buildMaps(selectedNodes);

	// Whitelist mode: only keep selected nodes (avoid parent-child duplicates)
	if (mode === ViewPermissionModeEnum.VisibleToTeams) {
		console.log('Whitelist mode: filtering selected nodes');

		// Filter logic: avoid duplicates (parent + children both selected)
		const resultIds = new Set<string>();

		selectedIdSet.forEach((nodeId: string) => {
			const node = nodeMap.get(nodeId);
			if (!node) {
				console.log(`Node ${nodeId} not found in map`);
				return;
			}

			// Check if any parent is also selected
			let currentId: string = nodeId;
			let hasSelectedParent = false;

			while (childToParentMap.has(currentId)) {
				const parentId: string = childToParentMap.get(currentId)!;
				if (selectedIdSet.has(parentId)) {
					console.log(
						`Node ${nodeId} (${node.name}) has selected parent ${parentId}, skipping`
					);
					hasSelectedParent = true;
					break;
				}
				currentId = parentId;
			}

			// Only add if no parent is selected
			if (!hasSelectedParent) {
				console.log(`Adding node ${nodeId} (${node.name})`);
				resultIds.add(nodeId);
			}
		});

		// Build final result array
		const newTreeData = Array.from(resultIds)
			.map((id: string) => nodeMap.get(id)!)
			.filter(Boolean);
		console.log('Final newTreeData (whitelist):', newTreeData);
		operateChoosableTreeData.value = newTreeData.length > 0 ? newTreeData : [];
		return;
	}

	// Blacklist mode: exclude selected nodes from full tree
	if (mode === ViewPermissionModeEnum.InvisibleToTeams) {
		console.log('Blacklist mode: excluding selected nodes from full tree');

		// Get full tree data
		const fullTreeData = await menuStore.getFlowflexUserDataWithCache('');
		console.log('Full tree data:', fullTreeData);

		// Extract all selected IDs (including children if parent is selected)
		const excludeIds = new Set<string>();

		const collectNodeAndChildren = (nodeId: string) => {
			excludeIds.add(nodeId);
			const node = nodeMap.get(nodeId);
			if (node && node.children && node.children.length > 0) {
				node.children.forEach((child) => {
					collectNodeAndChildren(child.id);
				});
			}
		};

		selectedIdSet.forEach((nodeId: string) => {
			collectNodeAndChildren(nodeId);
		});

		console.log('Exclude IDs:', Array.from(excludeIds));

		// Recursively filter tree to exclude selected nodes
		const filterTree = (nodes: FlowflexUser[]): FlowflexUser[] => {
			const result: FlowflexUser[] = [];

			for (const node of nodes) {
				// Skip excluded nodes
				if (excludeIds.has(node.id)) {
					console.log(`Excluding node: ${node.id} (${node.name})`);
					continue;
				}

				// Keep node but filter children
				const newNode: FlowflexUser = { ...node };
				if (newNode.children && newNode.children.length > 0) {
					newNode.children = filterTree(newNode.children);
				}

				result.push(newNode);
			}

			return result;
		};

		const filteredTreeData = filterTree(fullTreeData);
		console.log('Final filtered tree (blacklist):', filteredTreeData);
		operateChoosableTreeData.value = filteredTreeData.length > 0 ? filteredTreeData : [];
		return;
	}

	// Public mode or other modes
	console.log('Public or other mode: no filtering');
	operateChoosableTreeData.value = undefined;
};
</script>

<style scoped>
/* 确保 checkbox label 换行显示 */
:deep(.el-checkbox__label) {
	white-space: normal;
	line-height: 1.5;
}
/* :deep(.el-tag.el-tag--default) { */
/* } */
</style>
