<template>
	<div class="new-workflow-form p-1">
		<PrototypeTabs
			v-model="currentTab"
			:tabs="tabsConfig"
			class="workflow-tabs"
			content-class="workflow-content"
		>
			<!-- Basic Info Tab -->
			<TabPane value="basicInfo">
				<el-form
					ref="formRef"
					:model="formData"
					:rules="rules"
					label-position="top"
					class="p-1"
					@submit.prevent="submitForm"
				>
					<el-form-item label="Workflow Name" prop="name">
						<el-input
							v-model="formData.name"
							placeholder="Enter workflow name"
							data-tour="workflow-form-name-input"
						/>
					</el-form-item>

					<el-form-item label="Description" prop="description">
						<el-input
							v-model="formData.description"
							type="textarea"
							placeholder="Enter workflow description"
							:rows="3"
						/>
					</el-form-item>

					<el-form-item label="Set as active workflow" class="switch-group-item">
						<div class="switch-container">
							<el-switch
								v-model="isActiveSwitch"
								class="ml-2"
								inline-prompt
								style="
									--el-switch-on-color: var(--el-color-success);
									--el-switch-off-color: var(--el-color-danger);
								"
								active-text="Active"
								inactive-text="Inactive"
							/>
						</div>
					</el-form-item>

					<el-form-item label="Set as default workflow" class="switch-group-item">
						<div class="switch-container">
							<el-switch
								v-model="formData.isDefault"
								class="ml-2"
								inline-prompt
								style="
									--el-switch-on-color: var(--el-color-success);
									--el-switch-off-color: var(--el-color-danger);
								"
								active-text="Default"
								inactive-text="Not Default"
								:disabled="isDefaultDisabled"
							/>
						</div>
					</el-form-item>
				</el-form>
			</TabPane>

			<!-- Permissions Tab -->
			<TabPane value="permissions">
				<div class="space-y-4">
					<!-- Task 10.1: Template Permissions 区块 -->
					<div class="space-y-1">
						<h3 class="text-base font-bold">Template Permissions</h3>
						<p class="text-sm text-gray-600">
							Configure who can view and edit this workflow template.
						</p>
					</div>
					<PermissionSelector v-model="permissionsData" :is-workflow-level="true" />

					<!-- Task 10.2: Runtime Permissions 区块 -->
					<div class="space-y-4 border-t border-gray-200 pt-4">
						<div class="space-y-1">
							<h3 class="text-base font-bold">Runtime Permissions</h3>
							<p class="text-sm text-gray-600">
								Set the maximum runtime access for cases and stages using this workflow.
							</p>
						</div>

						<el-checkbox v-model="formData.runtimeUseSameAsTemplate">
							Use same permissions as Template Permissions
						</el-checkbox>

						<!-- 继承状态：只读展示 Effective Teams -->
						<div v-if="formData.runtimeUseSameAsTemplate" class="grid grid-cols-2 gap-6 divide-x divide-gray-300">
							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-500">View Permission (Inherited from Template)</label>
								<p class="text-sm text-gray-600">
									EFFECTIVE TEAMS: {{ effectiveTemplateViewDisplay }}
								</p>
							</div>
							<div class="space-y-2 pl-6">
								<label class="text-sm font-medium text-gray-500">Operate Permission (Inherited from Template)</label>
								<p class="text-sm text-gray-600">
									EFFECTIVE TEAMS: {{ effectiveTemplateOperateDisplay }}
								</p>
							</div>
						</div>

						<!-- 独立配置状态 -->
						<div v-else class="grid grid-cols-2 gap-6 divide-x divide-gray-300">
							<!-- 左侧：Runtime View -->
							<div class="space-y-4">
								<div class="space-y-2">
									<label class="text-base font-bold">View Permission</label>
									<el-select
										v-model="formData.runtimeViewPermissionMode"
										class="w-full"
										placeholder="Select permission type"
										@change="(v) => { formData.runtimeViewPermissionMode = Number(v); }"
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
									/>
									<p class="text-sm text-gray-500">
										EFFECTIVE TEAMS: {{ runtimeViewEffectiveDisplay }}
									</p>
								</div>
							</div>

							<!-- 右侧：Runtime Operate (no "Use same" checkbox) -->
							<div class="space-y-4 pl-6">
								<div class="space-y-2">
									<label class="text-base font-bold">Operate Permission</label>
									<!-- 注意：Runtime Operate 无 Use same checkbox，始终独立 -->
								</div>
								<div class="space-y-2">
									<label class="text-base font-bold">Team</label>
									<FlowflexUserSelector
										v-model="formData.runtimeOperateTeams"
										selectionType="team"
										:clearable="true"
										:choosable-tree-data="runtimeOperateChoosableTreeData"
									/>
									<p class="text-sm text-gray-500">
										EFFECTIVE TEAMS: {{ runtimeOperateEffectiveDisplay }}
									</p>
								</div>
							</div>
						</div>
					</div>
				</div>
			</TabPane>
		</PrototypeTabs>

		<!-- 表单操作按钮移到 tabs 外面 -->
		<div class="form-actions">
			<el-button @click="$emit('cancel')" data-tour="workflow-form-cancel-btn">
				Cancel
			</el-button>
			<el-button
				type="primary"
				:loading="loading"
				:disabled="loading"
				@click="submitForm"
				data-tour="workflow-form-submit-btn"
			>
				{{ isEditing ? 'Update Workflow' : 'Create Workflow' }}
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { getWorkflowList } from '@/apis/ow';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import PermissionSelector from './PermissionSelector.vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { menuRoles } from '@/stores/modules/menuFunction';
import type { FlowflexUser } from '#/golbal';

// 定义 props
interface Props {
	initialData?: {
		[key: string]: any;
	};
	isEditing?: boolean;
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	initialData: () => ({}),
	isEditing: false,
	loading: false,
});

// Tab 配置
const currentTab = ref('basicInfo');
const tabsConfig = [
	{ value: 'basicInfo', label: 'Basic Info' },
	{ value: 'permissions', label: 'Permissions' },
];

// 表单数据
const formData = reactive({
	name: '',
	description: '',
	status: 'active' as 'active' | 'inactive',
	isDefault: false, // 初始值为 false，由后续逻辑决定
	// Template 权限字段
	viewPermissionMode: ViewPermissionModeEnum.Public,
	viewTeams: [] as string[],
	operateTeams: [] as string[],
	useSameTeamForOperate: true as boolean,
	// Runtime 权限字段
	runtimeUseSameAsTemplate: true as boolean,
	runtimeViewPermissionMode: ViewPermissionModeEnum.Public,
	runtimeViewTeams: [] as string[],
	runtimeOperateTeams: [] as string[],
});

// menuStore for resolving team names
const menuStore = menuRoles();
const fullTreeCache = ref<FlowflexUser[] | null>(null);

const getFullTree = async (): Promise<FlowflexUser[]> => {
	if (!fullTreeCache.value) {
		const data = await menuStore.getFlowflexUserDataWithCache('');
		fullTreeCache.value = Array.isArray(data) ? data : [];
	}
	return fullTreeCache.value!;
};

// Build id→name map from tree (synchronous, uses cache)
const buildNameMap = (nodes: FlowflexUser[], map = new Map<string, string>()): Map<string, string> => {
	nodes.forEach((n) => {
		map.set(n.id, n.name || n.id);
		if (n.children?.length) buildNameMap(n.children, map);
	});
	return map;
};

const teamNameMap = ref<Map<string, string>>(new Map());

// Resolve IDs to names, fallback to ID if not found
const resolveNames = (ids: string[]): string[] =>
	ids.map((id) => teamNameMap.value.get(id) || id);

// Runtime Operate choosable tree data（受 Runtime View 约束）
const runtimeOperateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

const buildRuntimeOperateChoosableTree = async () => {
	const mode = Number(formData.runtimeViewPermissionMode);
	if (mode !== ViewPermissionModeEnum.VisibleTo) {
		// Public 或 InvisibleTo：不限制
		runtimeOperateChoosableTreeData.value = undefined;
		return;
	}
	if (formData.runtimeViewTeams.length === 0) {
		// VisibleTo 但还没选：可选范围为空
		runtimeOperateChoosableTreeData.value = [];
		return;
	}
	const fullTree = await getFullTree();
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
	runtimeOperateChoosableTreeData.value = filterNodes(fullTree);
};

// 开关状态计算属性
const isActiveSwitch = computed({
	get: () => formData.status === 'active',
	set: (value: boolean) => {
		formData.status = value ? 'active' : 'inactive';
	},
});

// 权限数据计算属性（用于 PermissionSelector 的 v-model）
const permissionsData = computed({
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
	},
});

// Runtime 权限相关选项与计算属性
const permissionTypeOptions = [
	{ label: 'Public', value: ViewPermissionModeEnum.Public },
	{ label: 'Visible to', value: ViewPermissionModeEnum.VisibleTo },
	{ label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleTo },
];

// 继承模式下 Template 有效展示
const effectiveTemplateViewDisplay = computed(() => {
	if (formData.viewPermissionMode === ViewPermissionModeEnum.Public) return 'All teams';
	return formData.viewTeams.length > 0 ? resolveNames(formData.viewTeams).join(', ') : 'None';
});

const effectiveTemplateOperateDisplay = computed(() => {
	if (formData.useSameTeamForOperate) return effectiveTemplateViewDisplay.value;
	return formData.operateTeams.length > 0 ? resolveNames(formData.operateTeams).join(', ') : 'None';
});

// Runtime 独立配置 — 是否展示 View Teams 选择器
const shouldShowRuntimeViewTeams = computed(() => {
	const mode = Number(formData.runtimeViewPermissionMode);
	return mode === ViewPermissionModeEnum.VisibleTo || mode === ViewPermissionModeEnum.InvisibleTo;
});

// Runtime View 有效展示
const runtimeViewEffectiveDisplay = computed(() => {
	if (Number(formData.runtimeViewPermissionMode) === ViewPermissionModeEnum.Public) return 'All teams';
	if (Number(formData.runtimeViewPermissionMode) === ViewPermissionModeEnum.VisibleTo) {
		return formData.runtimeViewTeams.length > 0
			? resolveNames(formData.runtimeViewTeams).join(', ')
			: 'None selected';
	}
	// InvisibleTo: 展示被排除的团队
	return formData.runtimeViewTeams.length > 0
		? `Excluding: ${resolveNames(formData.runtimeViewTeams).join(', ')}`
		: 'All teams (no exclusions)';
});

// Runtime Operate 有效展示
const runtimeOperateEffectiveDisplay = computed(() => {
	return formData.runtimeOperateTeams.length > 0
		? resolveNames(formData.runtimeOperateTeams).join(', ')
		: 'None selected';
});

// 检查系统中是否已有默认工作流
const checkAndSetDefaultValue = async () => {
	try {
		// 只在非编辑模式下执行此逻辑
		if (props.isEditing) {
			console.log('编辑模式，跳过默认值检查');
			return;
		}

		console.log('开始检查系统中的默认工作流...');
		const res = await getWorkflowList();

		if (res.code === '200' && res.data) {
			const workflowCount = res.data.length;
			const hasDefaultWorkflow = res.data.some((workflow: any) => workflow.isDefault);

			// 如果系统为空（没有任何工作流），设为默认
			if (workflowCount === 0) {
				formData.isDefault = true;
				console.log('系统为空，设置新工作流为默认');
			} else if (!hasDefaultWorkflow) {
				// 如果系统中没有默认工作流，新工作流自动设为默认
				formData.isDefault = true;
				console.log('系统中没有默认工作流，设置新工作流为默认');
			} else {
				// 如果系统中已有默认工作流，新工作流设为非默认
				formData.isDefault = false;
				console.log('系统中已有默认工作流，新工作流设为非默认');
			}
		} else {
			// 如果无法获取工作流列表，默认设为 true（假设系统为空）
			formData.isDefault = true;
			console.log('无法获取工作流列表，设置为默认');
		}
	} catch (error) {
		console.warn('检查默认工作流失败，设置为默认:', error);
		// 出错时默认设为 true，确保至少有一个默认工作流
		formData.isDefault = true;
	}
};

// 监听初始数据变化，用于编辑模式
watch(
	() => props.initialData,
	(newData) => {
		if (newData && Object.keys(newData).length > 0) {
			// 编辑模式：有初始数据时使用初始数据
			formData.name = newData.name || '';
			formData.description = newData.description || '';

			formData.status = newData.status || 'active';
			formData.isDefault = Object.keys(newData).includes('isDefault')
				? !!newData.isDefault
				: false; // 编辑模式下不自动设为默认

			// 初始化权限数据（直接从根节点读取）
			formData.viewPermissionMode =
				newData.viewPermissionMode ?? ViewPermissionModeEnum.Public;
			formData.viewTeams = newData.viewTeams || [];
			formData.operateTeams = newData.operateTeams || [];
			formData.useSameTeamForOperate = newData.useSameTeamForOperate ?? true;
			// 初始化 Runtime 权限数据
			formData.runtimeUseSameAsTemplate = newData.runtimeUseSameAsTemplate ?? true;
			formData.runtimeViewPermissionMode =
				newData.runtimeViewPermissionMode ?? ViewPermissionModeEnum.Public;
			formData.runtimeViewTeams = newData.runtimeViewTeams || [];
			formData.runtimeOperateTeams = newData.runtimeOperateTeams || [];
		} else if (!props.isEditing) {
			// 创建模式：没有初始数据时检查默认值
			checkAndSetDefaultValue();
		}
	},
	{ immediate: true, deep: true }
);

// 监听 status 的变化
watch(
	() => formData.status,
	(newValue) => {
		// 当状态设置为 inactive 时，自动设置为非默认工作流
		if (newValue === 'inactive') {
			formData.isDefault = false;
		}
	}
);

// Refresh name map when selections change (in case tree wasn't loaded yet)
watch(
	() => [...formData.runtimeViewTeams, ...formData.runtimeOperateTeams],
	async () => {
		if (teamNameMap.value.size === 0) {
			const tree = await getFullTree();
			teamNameMap.value = buildNameMap(tree);
		}
	}
);

// 当 Runtime View 模式或选中的 teams 变化时，重新计算 Operate 可选范围
watch(
	() => [Number(formData.runtimeViewPermissionMode), ...formData.runtimeViewTeams],
	() => {
		buildRuntimeOperateChoosableTree();
	},
	{ deep: true }
);

// 组件挂载时检查默认值（仅在非编辑模式下）
onMounted(async () => {
	// 创建模式下，无论是否有初始数据都要检查默认值
	if (!props.isEditing) {
		await checkAndSetDefaultValue();
	}
	// Preload team names for EFFECTIVE TEAMS display
	getFullTree().then((tree) => {
		teamNameMap.value = buildNameMap(tree);
	});
	buildRuntimeOperateChoosableTree();
});

// 表单验证规则
const rules = reactive<FormRules>({
	name: [
		{ required: true, message: 'Please enter workflow name', trigger: 'blur' },
		{ min: 1, max: 50, message: 'Length should be 1 to 50 characters', trigger: 'blur' },
	],
	description: [
		{ required: false, message: 'Please enter workflow description', trigger: 'blur' },
	],
});

// 表单引用
const formRef = ref<FormInstance>();

// 计算是否禁用默认工作流选项
const isDefaultDisabled = computed(() => {
	return formData.status === 'inactive';
});

// 提交表单
const submitForm = async () => {
	if (!formRef.value) return;

	await formRef.value.validate((valid, fields) => {
		if (valid) {
			emit('submit', {
				...formData,
			});
		}
	});
};

// 定义事件
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped lang="scss">
.new-workflow-form {
	margin-top: 5px;
}

.workflow-tabs {
	margin-bottom: 16px;
}

.workflow-content {
	padding-top: 8px;
}

.date-fields {
	display: flex;
	gap: 16px;
}

.date-field {
	flex: 1;
}

.switch-group-item {
	margin-top: 12px;
}

.switch-container {
	display: flex;
	align-items: center;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	margin-top: 20px;
	padding-right: 10px;
	padding-top: 16px;
	border-top: 1px solid var(--el-border-color-lighter);
}
</style>
