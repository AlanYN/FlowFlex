<template>
	<div class="stage-form-container">
		<PrototypeTabs
			v-model="currentTab"
			:tabs="tabsConfig"
			class="editor-tabs"
			content-class="editor-content"
			@tab-change="onTabChange"
		>
			<TabPane value="basicInfo">
				<el-form
					ref="formRef"
					:model="formData"
					:rules="rules"
					label-position="top"
					class="p-1"
					@submit.prevent
				>
					<el-form-item label="Stage Name" prop="name">
						<el-input v-model="formData.name" placeholder="Enter stage name" />
					</el-form-item>

					<el-form-item label="Description" prop="description">
						<el-input
							v-model="formData.description"
							type="textarea"
							:rows="3"
							placeholder="Enter stage description"
						/>
					</el-form-item>

					<!-- Visible in Customer Portal -->
					<el-form-item label="Available in Customer Portal" prop="visibleInPortal">
						<el-switch
							v-model="formData.visibleInPortal"
							inline-prompt
							active-text="Yes"
							inactive-text="No"
						/>
					</el-form-item>

					<!-- Portal Permission Options - only show when visibleInPortal is true -->
					<el-form-item v-if="formData.visibleInPortal" prop="portalPermission">
						<el-radio-group
							v-model="formData.portalPermission"
							class="portal-permission-group"
						>
							<el-radio
								v-for="option in portalPermissionOptions"
								:key="option.value"
								:value="option.value"
								class="portal-permission-option"
							>
								{{ option.label }}
							</el-radio>
						</el-radio-group>
					</el-form-item>

					<div class="flex items-center gap-2 w-full">
						<el-form-item
							label="Default Assignee"
							prop="defaultAssignee"
							class="w-full"
						>
							<FlowflexUser
								v-model="formData.defaultAssignee"
								placeholder="Select default assignee"
								:clearable="true"
								selection-type="user"
								:choosable-tree-data="availableAssigneeData"
							/>
						</el-form-item>
					</div>

					<div class="flex items-center gap-2 w-full">
						<el-form-item label="Co-assignees" prop="coAssignees" class="w-full">
							<FlowflexUser
								v-model="formData.coAssignees"
								placeholder="Select Co-assignees"
								:clearable="true"
								selection-type="user"
								:choosable-tree-data="availableCoAssigneesData"
							/>
						</el-form-item>
					</div>

					<el-form-item label="Estimated Duration" prop="estimatedDuration">
						<InputNumber
							v-model="formData.estimatedDuration as number"
							:decimalPlaces="2"
							placeholder="e.g., 3 days"
						/>
					</el-form-item>

					<el-form-item label="Stage Color" prop="color">
						<div class="color-picker-container">
							<div class="color-grid">
								<div
									v-for="color in colorOptions"
									:key="color"
									class="color-option"
									:class="{ selected: formData.color === color }"
									:style="{ backgroundColor: color }"
									@click="formData.color = color"
								></div>
							</div>
						</div>
					</el-form-item>

					<el-form-item label="Required Stage" prop="required">
						<template #label="{ label }">
							<span class="inline-flex items-center gap-x-1">
								{{ label }}
								<el-tooltip
									content="Users must complete this stage before proceeding to subsequent stages"
									placement="top"
								>
									<Icon
										icon="mdi:information-outline"
										class="text-gray-400 cursor-help"
									/>
								</el-tooltip>
							</span>
						</template>

						<el-switch
							v-model="formData.required"
							inline-prompt
							active-text="Yes"
							inactive-text="No"
						/>
					</el-form-item>
				</el-form>
			</TabPane>
			<TabPane value="components">
				<StageComponentsSelector
					:checklists="checklists"
					:questionnaires="questionnaires"
					:quickLinks="quickLinks"
					:model-value="{
						components: formData.components,
						visibleInPortal: formData.visibleInPortal,
						portalPermission: formData.portalPermission,
						attachmentManagementNeeded: formData.attachmentManagementNeeded,
					}"
					:stage="stage"
					:staticFields="staticFields"
					@update:model-value="updateComponentsData"
				/>
			</TabPane>
			<TabPane value="permissions">
				<StagePermissions
					v-model="permissionsData"
					:work-flow-operate-teams="workFlowOperateTeams"
					:work-flow-view-teams="workFlowViewTeams"
					:work-flow-view-permission-mode="workFlowViewPermissionMode"
					:work-flow-view-use-same-team-for-operate="workFlowViewUseSameTeamForOperate"
				/>
			</TabPane>
		</PrototypeTabs>

		<div class="form-actions mr-4">
			<el-button @click="$emit('cancel')">Cancel</el-button>
			<el-button
				type="primary"
				:loading="loading"
				:disabled="!isFormValid"
				@click="submitForm"
			>
				{{ isEditing ? 'Update Stage' : 'Add Stage' }}
			</el-button>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, PropType, computed } from 'vue';
import type { FormInstance } from 'element-plus';
import InputNumber from '@/components/form/InputNumber/index.vue';
import { stageColorOptions, StageColorType } from '@/enums/stageColorEnum';
import { PortalPermissionEnum, portalPermissionOptions } from '@/enums/portalPermissionEnum';
import StageComponentsSelector from './StageComponentsSelector.vue';
import FlowflexUser from '@/components/form/flowflexUser/index.vue';

import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import { Checklist, Questionnaire, Stage, ComponentsData, StageComponentData } from '#/onboard';
import StagePermissions from './StagePermissions.vue';
import { ViewPermissionModeEnum } from '@/enums/permissionEnum';
import { useUserStore } from '@/stores/modules/user';
import { menuRoles } from '@/stores/modules/menuFunction';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FlowflexUser as FlowflexUserType } from '#/golbal';
import { IQuickLink } from '#/integration';
import { DynamicList } from '#/dynamic';

// 颜色选项
const colorOptions = stageColorOptions;

// Store instances
const userStore = useUserStore();
const menuStore = menuRoles();

// 用户数据
const allUserData = ref<FlowflexUserType[]>([]);

// 获取用户数据
const fetchUserData = async () => {
	try {
		const data = await menuStore.getFlowflexUserDataWithCache();
		allUserData.value = Array.isArray(data) ? data : [];
	} catch (error) {
		console.error('Failed to fetch user data:', error);
		allUserData.value = [];
	}
};

// 递归过滤用户数据，排除指定的用户ID
const filterUserData = (data: FlowflexUserType[], excludeIds: string[]): FlowflexUserType[] => {
	if (!excludeIds.length) return data;

	return data
		.map((item) => {
			// 如果是用户类型且在排除列表中，则跳过
			if (item.type === 'user' && excludeIds.includes(item.id)) {
				return null;
			}

			// 如果有子节点，递归过滤
			if (item.children && item.children.length > 0) {
				const filteredChildren = filterUserData(item.children, excludeIds);
				return {
					...item,
					children: filteredChildren,
				};
			}

			return item;
		})
		.filter(Boolean) as FlowflexUserType[];
};

// 计算属性 - Default Assignee 可选用户（排除已选的 Co-assignees）
const availableAssigneeData = computed(() => {
	return filterUserData(allUserData.value, formData.value.coAssignees);
});

// 计算属性 - Co-assignees 可选用户（排除已选的 Default Assignee）
const availableCoAssigneesData = computed(() => {
	return filterUserData(allUserData.value, formData.value.defaultAssignee);
});

// Props
const props = defineProps({
	stage: {
		type: Object as PropType<Stage | null>,
		default: null,
	},
	isEditing: {
		type: Boolean,
		default: false,
	},
	loading: {
		type: Boolean,
		default: false,
	},
	checklists: {
		type: Array as PropType<Checklist[]>,
		default: () => [],
	},
	questionnaires: {
		type: Array as PropType<Questionnaire[]>,
		default: () => [],
	},
	quickLinks: {
		type: Array as PropType<IQuickLink[]>,
		default: () => [],
	},
	workflowId: {
		type: String,
		default: '',
	},
	workFlowOperateTeams: {
		type: Array as PropType<string[]>,
		default: () => [],
	},
	workFlowViewTeams: {
		type: Array as PropType<string[]>,
		default: () => [],
	},
	workFlowViewPermissionMode: {
		type: Number as PropType<number>,
		default: undefined,
	},
	workFlowViewUseSameTeamForOperate: {
		type: Boolean as PropType<boolean>,
		default: undefined,
	},
	staticFields: {
		type: Array as PropType<DynamicList[]>,
		default: () => [],
		required: true,
	},
});

// Tab配置
const currentTab = ref('basicInfo');
const tabsConfig = ref([
	{
		value: 'basicInfo',
		label: 'Basic Info',
	},
	{
		value: 'components',
		label: 'Components',
	},
	{
		value: 'permissions',
		label: 'Permissions',
	},
]);

// 表单数据
const formData = ref({
	id: '',
	name: '',
	description: '',
	visibleInPortal: false,
	portalPermission: PortalPermissionEnum.Viewable,
	defaultAssignedGroup: '',
	defaultAssignee: [] as string[],
	estimatedDuration: null as number | null,
	requiredFieldsJson: '',
	components: [] as StageComponentData[],
	order: 0,
	color: colorOptions[Math.floor(Math.random() * colorOptions.length)] as StageColorType,
	attachmentManagementNeeded: false,
	// 权限字段
	viewPermissionMode: ViewPermissionModeEnum.Public,
	viewTeams: [] as string[],
	operateTeams: [] as string[],
	useSameTeamForOperate: true,
	coAssignees: [] as string[],
	required: false,
});

// 表单验证规则
const rules = {
	name: [
		{ required: true, message: 'Please enter stage name', trigger: 'blur' },
		{ min: 1, max: 50, message: 'Length should be 1 to 50 characters', trigger: 'blur' },
	],

	estimatedDuration: [
		{ required: true, message: 'Please enter estimated duration', trigger: 'change' },
	],
};

// 计算属性
const isFormValid = computed(() => {
	return !!formData.value.name && !isNaN(formData.value.estimatedDuration as number);
});

// 权限数据计算属性（用于 PermissionSelector 的 v-model）
const permissionsData = computed({
	get: () => ({
		viewPermissionMode: formData.value.viewPermissionMode,
		viewTeams: formData.value.viewTeams,
		useSameTeamForOperate: formData.value.useSameTeamForOperate,
		operateTeams: formData.value.operateTeams,
	}),
	set: (value: {
		viewPermissionMode: number;
		viewTeams: string[];
		useSameTeamForOperate: boolean;
		operateTeams: string[];
	}) => {
		formData.value.viewPermissionMode = value.viewPermissionMode;
		formData.value.viewTeams = value.viewTeams;
		formData.value.useSameTeamForOperate = value.useSameTeamForOperate;
		formData.value.operateTeams = value.operateTeams;
	},
});

// 表单引用
const formRef = ref<FormInstance>();

const onTabChange = (tab: string) => {
	currentTab.value = tab;
};

// 初始化表单数据
onMounted(async () => {
	// 获取用户数据
	await fetchUserData();

	if (props.stage) {
		Object.keys(formData.value).forEach((key) => {
			if (key === 'color') {
				formData.value[key] =
					props.stage && props.stage?.color
						? (props.stage[key] as StageColorType)
						: (colorOptions[
								Math.floor(Math.random() * colorOptions.length)
						  ] as StageColorType);
			} else if (key === 'components') {
				formData.value[key] = props.stage?.components || [];
			} else if (key === 'portalPermission') {
				formData.value[key] =
					props.stage?.portalPermission || PortalPermissionEnum.Viewable;
			} else if (key === 'viewPermissionMode') {
				formData.value[key] =
					(props.stage as any)?.viewPermissionMode ?? ViewPermissionModeEnum.Public;
			} else if (key === 'viewTeams') {
				formData.value[key] = (props.stage as any)?.viewTeams || [];
			} else if (key === 'operateTeams') {
				formData.value[key] = (props.stage as any)?.operateTeams || [];
			} else if (key === 'useSameTeamForOperate') {
				formData.value[key] = (props.stage as any)?.useSameTeamForOperate ?? true;
			} else if (key === 'defaultAssignee') {
				// 处理 defaultAssignee 数组类型
				const value = (props.stage as any)?.defaultAssignee;
				if (Array.isArray(value)) {
					formData.value[key] = value;
				} else if (value) {
					// 兼容旧数据：如果是字符串，转换为数组
					formData.value[key] = [value];
				} else {
					formData.value[key] = [];
				}
			} else if (key === 'coAssignees') {
				// 处理 coAssignees 数组类型
				const value = (props.stage as any)?.coAssignees;
				if (Array.isArray(value)) {
					formData.value[key] = value;
				} else if (value) {
					// 兼容旧数据：如果是字符串，转换为数组
					formData.value[key] = [value];
				} else {
					formData.value[key] = [];
				}
			} else {
				formData.value[key] = props.stage ? (props.stage as any)[key] : '';
			}
		});
	}
});

// Method to update components data
function updateComponentsData(val: ComponentsData) {
	console.log('更新的val:', val);
	formData.value.components = val.components;
	formData.value.visibleInPortal = val.visibleInPortal ?? false;
	if (val.portalPermission !== undefined) {
		formData.value.portalPermission = val.portalPermission;
	}
	formData.value.attachmentManagementNeeded = val.attachmentManagementNeeded ?? false;
}

// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
const validateAndCheckPermissions = async (): Promise<{
	hasWarning: boolean;
	showMessage: boolean;
	warningMessage: string;
}> => {
	const viewPermissionMode = formData.value.viewPermissionMode;
	const viewTeams = formData.value.viewTeams;
	const operateTeams = formData.value.operateTeams;
	const useSameTeamForOperate = formData.value.useSameTeamForOperate;
	if (viewPermissionMode === ViewPermissionModeEnum.Public) {
		if (useSameTeamForOperate == false && operateTeams.length === 0) {
			return {
				hasWarning: false,
				showMessage: true,
				warningMessage:
					'Please select at least one team for Operate Permission of this stage.',
			};
		}

		if (viewTeams.length > 0) {
			formData.value.viewTeams = [];
		}
		if (useSameTeamForOperate && operateTeams.length > 0) {
			formData.value.operateTeams = [];
		}

		return { hasWarning: false, showMessage: false, warningMessage: '' };
	}
	// 只在 VisibleTo 或 InvisibleTo 模式下检查

	// Validate: 检查是否至少选择了一个团队
	if (viewTeams.length === 0) {
		return {
			hasWarning: false,
			showMessage: true,
			warningMessage: 'Please select at least one team for View Permission of this stage.',
		};
	}
	if (operateTeams.length === 0) {
		return {
			hasWarning: false,
			showMessage: true,
			warningMessage: 'Please select at least one team for Operate Permission of this stage.',
		};
	}

	const currentUser = userStore.getUserInfo;
	if (!currentUser || !currentUser.userId) {
		return { hasWarning: false, showMessage: false, warningMessage: '' };
	}

	// 递归查找用户所属团队的辅助函数
	const findUserTeams = (data: FlowflexUserType[], userId: string): string[] => {
		const teams: string[] = [];
		for (const item of data) {
			if (item.type === 'team' && item.children) {
				// 检查团队下是否有当前用户
				const hasCurrentUser = item.children.some(
					(child) => child.type === 'user' && child.id === userId
				);
				if (hasCurrentUser) {
					teams.push(item.id);
				}
				// 递归查找子团队
				teams.push(...findUserTeams(item.children, userId));
			}
		}
		return teams;
	};

	try {
		// 获取用户数据
		const userData = await menuStore.getFlowflexUserDataWithCache();
		const currentUserId = String(currentUser.userId);
		//是否包含不可用team, 如果包含，则返回警告
		const collectTeamIds = (nodes: FlowflexUserType[]): Set<string> => {
			const teamIds = new Set<string>();
			const traverse = (items: FlowflexUserType[]) => {
				items.forEach((item) => {
					if (item.type === 'team') {
						teamIds.add(item.id);
					}
					if (item.children && item.children.length > 0) {
						traverse(item.children);
					}
				});
			};
			traverse(nodes);
			return teamIds;
		};

		const normalizedUserData = Array.isArray(userData) ? userData : [];
		const teamIds = collectTeamIds(normalizedUserData);
		const missingTeams = [
			...viewTeams.filter((id) => !teamIds.has(id)),
			...operateTeams.filter((id) => !teamIds.has(id)),
		];

		if (missingTeams.length > 0) {
			return {
				hasWarning: false,
				showMessage: true,
				warningMessage:
					'Some selected teams no longer exist. Please update your selection.',
			};
		}

		const userTeams = findUserTeams(normalizedUserData, currentUserId);

		let isUserExcludedFromView = false;
		let isUserExcludedFromOperate = false;

		// 检查 View Permission（基于团队）
		const isInViewList = userTeams.some((teamId) => viewTeams.includes(teamId));
		// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
		isUserExcludedFromView =
			viewPermissionMode === ViewPermissionModeEnum.VisibleTo ? !isInViewList : isInViewList;

		// 检查 Operate Permission（基于团队）
		const isInOperateList = userTeams.some((teamId) => operateTeams.includes(teamId));
		// 白名单：不在列表中 = 被排除；黑名单：在列表中 = 被排除
		isUserExcludedFromOperate =
			viewPermissionMode === ViewPermissionModeEnum.VisibleTo
				? !isInOperateList
				: isInOperateList;

		// 生成警告信息
		if (isUserExcludedFromView || isUserExcludedFromOperate) {
			let warningMessage = '';
			if (isUserExcludedFromView && isUserExcludedFromOperate) {
				warningMessage =
					'Warning: You are setting permissions that will exclude yourself from viewing and operating this stage. You will not be able to access this stage after saving. Do you want to continue?';
			} else if (isUserExcludedFromView) {
				warningMessage =
					'Warning: You are setting permissions that will exclude yourself from viewing this stage. You will not be able to access this stage after saving. Do you want to continue?';
			} else {
				warningMessage =
					'Warning: You are setting permissions that will exclude yourself from operating this stage. You will be able to view but not operate on this stage after saving. Do you want to continue?';
			}
			if (currentUser.userType === 1 || currentUser.userType === 2) {
				return { hasWarning: false, showMessage: false, warningMessage };
			}
			return { hasWarning: true, showMessage: false, warningMessage };
		}
	} catch (error) {
		console.error('Failed to check stage permissions:', error);
	}

	return { hasWarning: false, showMessage: false, warningMessage: '' };
};

// 提交
async function submitForm() {
	// 验证并检查权限：验证必填项 + 检查当前用户是否会被权限设置排除
	const permissionCheck = await validateAndCheckPermissions();
	if (permissionCheck.hasWarning || permissionCheck.showMessage) {
		if (permissionCheck.showMessage) {
			ElMessage.warning(permissionCheck.warningMessage);
			return;
		} else {
			try {
				await ElMessageBox.confirm(
					permissionCheck.warningMessage,
					'⚠️ Permission Warning',
					{
						confirmButtonText: 'Continue',
						cancelButtonText: 'Cancel',
						type: 'warning',
						distinguishCancelAndClose: true,
					}
				);
			} catch (error) {
				// 用户点击取消，不提交表单
				return;
			}
		}
	}

	// 透传表单数据
	const isValid = await formRef.value?.validate();
	if (!isValid) {
		return;
	}
	const payload = { ...formData.value } as any;
	// 颜色值
	payload.color = formData.value.color;
	// 发出提交事件
	// @ts-ignore
	emit('submit', payload);
}

// emits
const emit = defineEmits(['submit', 'cancel']);
</script>

<style scoped>
.stage-form-container {
	width: 100%;
}
.editor-tabs {
	margin-bottom: 16px;
}
.editor-content {
	padding-top: 8px;
}
.color-picker-container {
	width: 100%;
}
.color-grid {
	display: grid;
	grid-template-columns: repeat(12, 1fr);
	gap: 12px;
}
.color-option {
	width: 28px;
	height: 28px;
	border-radius: 9999px;
	cursor: pointer;
	border: 2px solid transparent;
}

.color-option.selected {
	border-color: var(--el-color-black);
}

.dark .color-option.selected {
	border-color: var(--el-color-white);
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	margin-top: 16px;
}
.text-muted {
	color: var(--el-text-color-secondary);
}
.portal-permission-group {
	width: 100%;
}
.portal-permission-option {
	display: block;
	margin-bottom: 8px;
	width: 100%;
}
</style>
