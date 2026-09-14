<template>
	<div class="space-y-6">
		<!-- ========== Section 1: Template Permissions ========== -->
		<div class="space-y-3">
			<div class="space-y-1">
				<h3 class="text-base font-bold">Template Permissions</h3>
				<p class="text-sm text-gray-600">Configure who can view and operate this stage's template.</p>
			</div>

			<el-checkbox v-model="formData.templateUseSameAsWorkflow">
				Use same permissions as Workflow Template Permissions
			</el-checkbox>

			<!-- Inherited: read-only display -->
			<div v-if="formData.templateUseSameAsWorkflow" class="space-y-1 text-sm text-gray-600 pl-1">
				<p>Matches the workflow's Template view/operate permission.</p>
				<p>EFFECTIVE TEAMS (View): {{ effectiveTemplateViewDisplay }}</p>
				<p>EFFECTIVE TEAMS (Operate): {{ effectiveTemplateOperateDisplay }}</p>
			</div>

			<!-- Independent: PermissionSelector -->
			<div v-else>
				<PermissionSelector
					v-model="templatePermissionsData"
					:view-limit-data="workFlowViewTeams"
					:operate-limit-data="workFlowOperateTeams"
					:work-flow-view-permission-mode="workFlowViewPermissionMode"
					:work-flow-view-use-same-team-for-operate="workFlowViewUseSameTeamForOperate"
					:is-workflow-level="false"
				/>
			</div>
		</div>

		<!-- ========== Section 2: Runtime Permissions ========== -->
		<div class="space-y-3 border-t border-gray-200 pt-4">
			<div class="space-y-1">
				<h3 class="text-base font-bold">Runtime Permissions</h3>
			</div>

			<el-checkbox v-model="formData.runtimeUseSameAsWorkflow">
				Use same permission as Workflow Runtime Permissions
			</el-checkbox>

			<!-- Inherited: read-only display -->
			<div v-if="formData.runtimeUseSameAsWorkflow" class="space-y-1 text-sm text-gray-600 pl-1">
				<p>Matches the workflow's Runtime view/operate permission.</p>
				<p>EFFECTIVE TEAMS (View): {{ workFlowEffectiveRuntimeViewDisplay }}</p>
				<p>EFFECTIVE TEAMS (Operate): {{ workFlowEffectiveRuntimeOperateDisplay }}</p>
			</div>

			<!-- Independent: grid 2-col layout -->
			<div v-else class="grid grid-cols-2 gap-6 divide-x divide-gray-300">
				<!-- Left: Runtime View -->
				<div class="space-y-4">
					<div class="space-y-2">
						<label class="text-base font-bold">View Permission</label>
						<el-select
							v-model="formData.runtimeViewPermissionMode"
							class="w-full"
							placeholder="Select permission type"
						>
							<el-option
								v-for="option in permissionTypeOptions"
								:key="option.value"
								:label="option.label"
								:value="option.value"
							/>
						</el-select>
					</div>
					<div v-if="shouldShowRuntimeViewTeams" class="space-y-2">
						<label class="text-base font-bold">Team</label>
						<FlowflexUserSelector
							v-model="formData.runtimeViewTeams"
							selectionType="team"
							:clearable="true"
							:choosable-tree-data="runtimeViewChoosableTreeData"
						/>
					</div>
					<p class="text-sm text-gray-500">
						EFFECTIVE TEAMS: {{ runtimeViewEffectiveDisplay }}
					</p>
				</div>

				<!-- Right: Runtime Operate (NO "Use same" checkbox) -->
				<div class="space-y-4 pl-4">
					<div class="space-y-2">
						<label class="text-base font-bold">Operate Permission</label>
						<!-- Note: Runtime Operate at stage level has no "Use same" checkbox per Requirement 16.6 -->
					</div>
					<div class="space-y-2">
						<label class="text-base font-bold">Team</label>
						<FlowflexUserSelector
							v-model="formData.runtimeOperateTeams"
							selectionType="team"
							:clearable="true"
							:choosable-tree-data="runtimeOperateChoosableTreeData"
						/>
					</div>
					<p class="text-sm text-gray-500">
						EFFECTIVE TEAMS: {{ runtimeOperateEffectiveDisplay }}
					</p>
				</div>
			</div>
		</div>

		<!-- ========== Section 3: Roll Back Teams ========== -->
		<div class="space-y-3 border-t border-gray-200 pt-4">
			<div class="space-y-1">
				<label class="text-base font-bold inline-flex items-center gap-x-1">
					Roll Back Teams
					<el-tooltip
						content="Only users from these teams can roll back this stage. Leave empty to disable roll back for all users."
						placement="top"
					>
						<Icon icon="mdi:information-outline" class="text-gray-400 cursor-help" />
					</el-tooltip>
				</label>
				<p class="text-sm text-gray-600">
					Can only include teams that also have Runtime Operate permission.
				</p>
			</div>
			<FlowflexUserSelector
				v-model="formData.rollBackTeams"
				selectionType="team"
				:clearable="true"
				:choosable-tree-data="rollBackChoosableTreeData"
			/>
			<p class="text-sm text-gray-500">
				EFFECTIVE TEAMS: {{ effectiveRollBackDisplay }}
			</p>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import PermissionSelector from './PermissionSelector.vue';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

interface Props {
	modelValue?: {
		// Template fields (existing)
		viewPermissionMode: number;
		viewTeams: string[];
		operateTeams: string[];
		useSameTeamForOperate: boolean;
		rollBackTeams: string[];
		// New fields
		templateUseSameAsWorkflow: boolean;
		runtimeUseSameAsWorkflow: boolean;
		runtimeViewPermissionMode: number;
		runtimeViewTeams: string[];
		runtimeOperateTeams: string[];
	};
	// Props for Workflow template context (already exist)
	workFlowOperateTeams?: string[];
	workFlowViewTeams?: string[];
	workFlowViewPermissionMode?: number;
	workFlowViewUseSameTeamForOperate?: boolean;
	// New props: Workflow Runtime Effective values (for "inherited" read-only preview)
	workFlowEffectiveRuntimeViewTeams?: string[];
	workFlowEffectiveRuntimeOperateTeams?: string[];
	workFlowEffectiveRuntimeViewPermissionMode?: number;
	// Roll Back choosable range (EffectiveRollBackTeams from StageOutputDto)
	effectiveRollBackChoosableTeams?: string[];
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		viewPermissionMode: ViewPermissionModeEnum.Public,
		viewTeams: [],
		operateTeams: [],
		useSameTeamForOperate: true,
		rollBackTeams: [],
		templateUseSameAsWorkflow: true,
		runtimeUseSameAsWorkflow: true,
		runtimeViewPermissionMode: ViewPermissionModeEnum.Public,
		runtimeViewTeams: [],
		runtimeOperateTeams: [],
	}),
	workFlowOperateTeams: () => [],
	workFlowViewTeams: () => [],
	workFlowViewPermissionMode: ViewPermissionModeEnum.Public,
	workFlowViewUseSameTeamForOperate: undefined,
	workFlowEffectiveRuntimeViewTeams: () => [],
	workFlowEffectiveRuntimeOperateTeams: () => [],
	workFlowEffectiveRuntimeViewPermissionMode: undefined,
	effectiveRollBackChoosableTeams: () => [],
});

const emit = defineEmits(['update:modelValue']);

const menuStore = menuRoles();

// ── Team name resolver ─────────────────────────────────────────────────
const teamNameMap = ref<Map<string, string>>(new Map());

const buildNameMap = (nodes: FlowflexUser[], map = new Map<string, string>()): Map<string, string> => {
	nodes.forEach((n) => {
		map.set(n.id, n.name || n.id);
		if (n.children?.length) buildNameMap(n.children, map);
	});
	return map;
};

const resolveNames = (ids: string[]): string[] =>
	ids.map((id) => teamNameMap.value.get(id) || id);

// 预加载 team 树（组件挂载时触发一次）
menuStore.getFlowflexUserDataWithCache('').then((tree) => {
	teamNameMap.value = buildNameMap(Array.isArray(tree) ? tree : []);
});

// ── Form data ──────────────────────────────────────────────────────────
const formData = reactive({
	// Template fields
	viewPermissionMode: props.modelValue?.viewPermissionMode ?? ViewPermissionModeEnum.Public,
	viewTeams: [...(props.modelValue?.viewTeams || [])],
	operateTeams: [...(props.modelValue?.operateTeams || [])],
	useSameTeamForOperate: props.modelValue?.useSameTeamForOperate ?? true,
	rollBackTeams: [...(props.modelValue?.rollBackTeams || [])],
	// New fields
	templateUseSameAsWorkflow: props.modelValue?.templateUseSameAsWorkflow ?? true,
	runtimeUseSameAsWorkflow: props.modelValue?.runtimeUseSameAsWorkflow ?? true,
	runtimeViewPermissionMode:
		props.modelValue?.runtimeViewPermissionMode ?? ViewPermissionModeEnum.Public,
	runtimeViewTeams: [...(props.modelValue?.runtimeViewTeams || [])],
	runtimeOperateTeams: [...(props.modelValue?.runtimeOperateTeams || [])],
});

// ── Permission type options ────────────────────────────────────────────
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleTo },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleTo },
];

// ── Section 1: Template computed helpers ──────────────────────────────
const templatePermissionsData = computed({
	get: () => ({
		viewPermissionMode: formData.viewPermissionMode,
		viewTeams: formData.viewTeams,
		useSameTeamForOperate: formData.useSameTeamForOperate,
		operateTeams: formData.operateTeams,
	}),
	set: (value: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameTeamForOperate: boolean;
		operateTeams: string[];
	}) => {
		formData.viewPermissionMode = value.viewPermissionMode;
		formData.viewTeams = value.viewTeams;
		formData.useSameTeamForOperate = value.useSameTeamForOperate;
		formData.operateTeams = value.operateTeams;
		emitUpdate();
	},
});

const effectiveTemplateViewDisplay = computed(() => {
	if (props.workFlowViewPermissionMode === ViewPermissionModeEnum.Public) return 'All teams';
	return (props.workFlowViewTeams?.length ?? 0) > 0
		? resolveNames(props.workFlowViewTeams!).join(', ')
		: 'None';
});

const effectiveTemplateOperateDisplay = computed(() => {
	if (props.workFlowViewUseSameTeamForOperate) return effectiveTemplateViewDisplay.value;
	return (props.workFlowOperateTeams?.length ?? 0) > 0
		? resolveNames(props.workFlowOperateTeams!).join(', ')
		: 'None';
});

// ── Section 2: Runtime computed helpers ───────────────────────────────
const shouldShowRuntimeViewTeams = computed(() => {
	return (
		formData.runtimeViewPermissionMode === ViewPermissionModeEnum.VisibleTo ||
		formData.runtimeViewPermissionMode === ViewPermissionModeEnum.InvisibleTo
	);
});

const runtimeViewEffectiveDisplay = computed(() => {
	if (formData.runtimeViewPermissionMode === ViewPermissionModeEnum.Public) return 'All teams';
	if (formData.runtimeViewPermissionMode === ViewPermissionModeEnum.VisibleTo) {
		return formData.runtimeViewTeams.length > 0
			? resolveNames(formData.runtimeViewTeams).join(', ')
			: 'None selected';
	}
	return formData.runtimeViewTeams.length > 0
		? `Excluding: ${resolveNames(formData.runtimeViewTeams).join(', ')}`
		: 'All teams (no exclusions)';
});

const runtimeOperateEffectiveDisplay = computed(() => {
	return formData.runtimeOperateTeams.length > 0
		? resolveNames(formData.runtimeOperateTeams).join(', ')
		: 'None selected';
});

// Workflow runtime inherited display
const workFlowEffectiveRuntimeViewDisplay = computed(() => {
	const mode = props.workFlowEffectiveRuntimeViewPermissionMode;
	if (mode === ViewPermissionModeEnum.Public || mode === undefined) return 'All teams';
	return (props.workFlowEffectiveRuntimeViewTeams?.length ?? 0) > 0
		? resolveNames(props.workFlowEffectiveRuntimeViewTeams!).join(', ')
		: 'None';
});

const workFlowEffectiveRuntimeOperateDisplay = computed(() => {
	return (props.workFlowEffectiveRuntimeOperateTeams?.length ?? 0) > 0
		? resolveNames(props.workFlowEffectiveRuntimeOperateTeams!).join(', ')
		: 'None';
});

// ── Section 3: Roll Back computed helpers ─────────────────────────────
const rollBackChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

// Compute the effective Runtime Operate teams for Roll Back range constraint.
// Rule (Req 16.7): Roll Back Teams must be ⊆ Stage Effective Runtime Operate Teams.
//   - if runtimeUseSameAsWorkflow → inherit from workFlowEffectiveRuntimeOperateTeams
//   - if independent → use formData.runtimeOperateTeams
const getRollBackAllowedIds = (): string[] => {
	if (formData.runtimeUseSameAsWorkflow) {
		return props.workFlowEffectiveRuntimeOperateTeams ?? [];
	}
	return formData.runtimeOperateTeams;
};

const buildRollBackChoosableTree = async () => {
	const choosableIds = getRollBackAllowedIds();

	if (!choosableIds || choosableIds.length === 0) {
		// No operate teams configured → no one can roll back; lock to empty
		rollBackChoosableTreeData.value = [];
		return;
	}

	try {
		const fullTree = await menuStore.getFlowflexUserDataWithCache('');
		const treeData = Array.isArray(fullTree) ? fullTree : [];
		const idSet = new Set<string>(choosableIds);

		const filterTeams = (nodes: FlowflexUser[]): FlowflexUser[] => {
			const result: FlowflexUser[] = [];
			nodes.forEach((node) => {
				if (node.type === 'team' && idSet.has(node.id)) {
					const cloned: FlowflexUser = { ...node };
					if (cloned.children && cloned.children.length > 0) {
						cloned.children = filterTeams(cloned.children);
					}
					result.push(cloned);
				} else if (node.type === 'team' && node.children && node.children.length > 0) {
					// Check descendants
					const filteredChildren = filterTeams(node.children);
					if (filteredChildren.length > 0) {
						result.push({ ...node, children: filteredChildren });
					}
				}
			});
			return result;
		};

		const filtered = filterTeams(treeData);
		rollBackChoosableTreeData.value = filtered.length > 0 ? filtered : [];
	} catch (error) {
		console.error('Failed to build roll back choosable tree:', error);
		rollBackChoosableTreeData.value = undefined;
	}
};

// Rebuild Roll Back choosable range whenever the effective operate scope changes
watch(
	[
		() => formData.runtimeUseSameAsWorkflow,
		() => props.workFlowEffectiveRuntimeOperateTeams,
		() => formData.runtimeOperateTeams,
	],
	() => buildRollBackChoosableTree(),
	{ immediate: true, deep: true }
);

const effectiveRollBackDisplay = computed(() => {
	return formData.rollBackTeams.length > 0
		? resolveNames(formData.rollBackTeams).join(', ')
		: 'None';
});

// ── Runtime Operate choosable range（受 Runtime View 约束）─────────────
const runtimeOperateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

// ── Runtime View choosable range（受 Workflow Runtime View Teams 约束）──────
// When workflow runtime view is VisibleTo, stage view teams must be a subset.
const runtimeViewChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

const buildRuntimeViewChoosableTree = async () => {
	const wfMode = props.workFlowEffectiveRuntimeViewPermissionMode;
	const wfViewTeams = props.workFlowEffectiveRuntimeViewTeams ?? [];

	// Workflow runtime is Public (or unset) → no restriction on stage view teams
	if (wfMode === ViewPermissionModeEnum.Public || wfMode === undefined || wfViewTeams.length === 0) {
		runtimeViewChoosableTreeData.value = undefined;
		return;
	}

	// Workflow runtime is VisibleTo → restrict to workflow's view teams
	try {
		const fullTree = await menuStore.getFlowflexUserDataWithCache('');
		const treeData = Array.isArray(fullTree) ? fullTree : [];
		const allowedSet = new Set<string>(wfViewTeams);
		const filterNodes = (nodes: FlowflexUser[]): FlowflexUser[] => {
			const result: FlowflexUser[] = [];
			nodes.forEach((n) => {
				if (n.type === 'team' && allowedSet.has(n.id)) {
					result.push({ ...n, children: n.children ? filterNodes(n.children) : [] });
				} else if (n.type === 'team' && n.children?.length) {
					const filtered = filterNodes(n.children);
					if (filtered.length) result.push({ ...n, children: filtered });
				}
			});
			return result;
		};
		runtimeViewChoosableTreeData.value = filterNodes(treeData);
	} catch {
		runtimeViewChoosableTreeData.value = undefined;
	}
};

watch(
	() => [props.workFlowEffectiveRuntimeViewPermissionMode, props.workFlowEffectiveRuntimeViewTeams],
	() => buildRuntimeViewChoosableTree(),
	{ immediate: true, deep: true }
);

const buildRuntimeOperateChoosableTree = async () => {
	const mode = Number(formData.runtimeViewPermissionMode);
	if (mode !== ViewPermissionModeEnum.VisibleTo) {
		runtimeOperateChoosableTreeData.value = undefined;
		return;
	}
	if (formData.runtimeViewTeams.length === 0) {
		runtimeOperateChoosableTreeData.value = [];
		return;
	}
	const fullTree = await menuStore.getFlowflexUserDataWithCache('');
	const treeData = Array.isArray(fullTree) ? fullTree : [];
	const allowedSet = new Set<string>(formData.runtimeViewTeams);
	const filterNodes = (nodes: FlowflexUser[]): FlowflexUser[] => {
		const result: FlowflexUser[] = [];
		nodes.forEach((n) => {
			if (n.type === 'team' && allowedSet.has(n.id)) {
				result.push({ ...n, children: n.children ? filterNodes(n.children) : [] });
			} else if (n.type === 'team' && n.children?.length) {
				const filtered = filterNodes(n.children);
				if (filtered.length) result.push({ ...n, children: filtered });
			}
		});
		return result;
	};
	runtimeOperateChoosableTreeData.value = filterNodes(treeData);
};

watch(
	() => [Number(formData.runtimeViewPermissionMode), ...formData.runtimeViewTeams],
	() => buildRuntimeOperateChoosableTree(),
	{ deep: true }
);

// 初始化时也执行一次
buildRuntimeOperateChoosableTree();

// ── Emit helper ───────────────────────────────────────────────────────
const emitUpdate = () => {
	emit('update:modelValue', {
		viewPermissionMode: formData.viewPermissionMode,
		viewTeams: formData.viewTeams,
		operateTeams: formData.operateTeams,
		useSameTeamForOperate: formData.useSameTeamForOperate,
		rollBackTeams: formData.rollBackTeams,
		templateUseSameAsWorkflow: formData.templateUseSameAsWorkflow,
		runtimeUseSameAsWorkflow: formData.runtimeUseSameAsWorkflow,
		runtimeViewPermissionMode: formData.runtimeViewPermissionMode,
		runtimeViewTeams: formData.runtimeViewTeams,
		runtimeOperateTeams: formData.runtimeOperateTeams,
		// Stage Runtime Operate 设计上无 "Use same" checkbox，始终独立配置
		runtimeUseSameTeamForOperate: false,
	});
};

// ── Watch formData fields for emit ────────────────────────────────────
watch(
	() => formData.rollBackTeams,
	() => emitUpdate(),
	{ deep: true }
);

watch(
	() => formData.templateUseSameAsWorkflow,
	(val) => {
		if (val) {
			// Clear independent template fields on switch to inherit
			formData.viewPermissionMode = ViewPermissionModeEnum.Public;
			formData.viewTeams = [];
			formData.operateTeams = [];
			formData.useSameTeamForOperate = true;
		}
		emitUpdate();
	}
);

watch(
	() => formData.runtimeUseSameAsWorkflow,
	(val) => {
		if (val) {
			// Clear independent runtime fields on switch to inherit
			formData.runtimeViewPermissionMode = ViewPermissionModeEnum.Public;
			formData.runtimeViewTeams = [];
			formData.runtimeOperateTeams = [];
		}
		// Clear rollBackTeams that fall outside the new operate scope
		const allowed = new Set(getRollBackAllowedIds());
		formData.rollBackTeams = formData.rollBackTeams.filter((id) => allowed.has(id));
		emitUpdate();
	}
);

watch(
	() => formData.runtimeViewPermissionMode,
	() => emitUpdate()
);

watch(
	() => formData.runtimeViewTeams,
	() => emitUpdate(),
	{ deep: true }
);

watch(
	() => formData.runtimeOperateTeams,
	() => {
		// Clear rollBackTeams that fall outside the updated operate scope
		const allowed = new Set(getRollBackAllowedIds());
		if (formData.rollBackTeams.some((id) => !allowed.has(id))) {
			formData.rollBackTeams = formData.rollBackTeams.filter((id) => allowed.has(id));
		}
		emitUpdate();
	},
	{ deep: true }
);

// ── Sync incoming modelValue → formData (field-by-field to avoid loops) ──
watch(
	() => props.modelValue,
	(newVal) => {
		if (!newVal) return;

		if (
			(newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public) !==
			formData.viewPermissionMode
		) {
			formData.viewPermissionMode =
				newVal.viewPermissionMode ?? ViewPermissionModeEnum.Public;
		}
		if (JSON.stringify(newVal.viewTeams || []) !== JSON.stringify(formData.viewTeams)) {
			formData.viewTeams = [...(newVal.viewTeams || [])];
		}
		if (JSON.stringify(newVal.operateTeams || []) !== JSON.stringify(formData.operateTeams)) {
			formData.operateTeams = [...(newVal.operateTeams || [])];
		}
		if ((newVal.useSameTeamForOperate ?? true) !== formData.useSameTeamForOperate) {
			formData.useSameTeamForOperate = newVal.useSameTeamForOperate ?? true;
		}
		if (JSON.stringify(newVal.rollBackTeams || []) !== JSON.stringify(formData.rollBackTeams)) {
			formData.rollBackTeams = [...(newVal.rollBackTeams || [])];
		}
		if ((newVal.templateUseSameAsWorkflow ?? true) !== formData.templateUseSameAsWorkflow) {
			formData.templateUseSameAsWorkflow = newVal.templateUseSameAsWorkflow ?? true;
		}
		if ((newVal.runtimeUseSameAsWorkflow ?? true) !== formData.runtimeUseSameAsWorkflow) {
			formData.runtimeUseSameAsWorkflow = newVal.runtimeUseSameAsWorkflow ?? true;
		}
		if (
			(newVal.runtimeViewPermissionMode ?? ViewPermissionModeEnum.Public) !==
			formData.runtimeViewPermissionMode
		) {
			formData.runtimeViewPermissionMode =
				newVal.runtimeViewPermissionMode ?? ViewPermissionModeEnum.Public;
		}
		if (
			JSON.stringify(newVal.runtimeViewTeams || []) !==
			JSON.stringify(formData.runtimeViewTeams)
		) {
			formData.runtimeViewTeams = [...(newVal.runtimeViewTeams || [])];
		}
		if (
			JSON.stringify(newVal.runtimeOperateTeams || []) !==
			JSON.stringify(formData.runtimeOperateTeams)
		) {
			formData.runtimeOperateTeams = [...(newVal.runtimeOperateTeams || [])];
		}
	},
	{ deep: true }
);
</script>

<style scoped>
:deep(.el-checkbox__label) {
	white-space: normal;
	line-height: 1.5;
}
</style>
