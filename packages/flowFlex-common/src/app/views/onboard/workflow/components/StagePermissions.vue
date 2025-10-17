<template>
	<div class="space-y-6">
		<!-- Portal Availability -->
		<div class="space-y-2">
			<label class="text-base font-bold">Portal Availability</label>

			<el-radio-group v-model="localPermissions.portalAvailability" class="w-full">
				<div class="flex flex-row justify-center items-center gap-4">
					<el-radio
						:value="PortalAvailabilityEnum.Viewable"
						class="permission-radio-horizontal"
					>
						<span class="font-medium">Viewable</span>
					</el-radio>
					<el-radio
						:value="PortalAvailabilityEnum.Completable"
						class="permission-radio-horizontal"
					>
						<span class="font-medium">Completable</span>
					</el-radio>
					<el-radio
						:value="PortalAvailabilityEnum.NotAvailable"
						class="permission-radio-horizontal"
					>
						<span class="font-medium">Not Available</span>
					</el-radio>
				</div>
			</el-radio-group>
		</div>

		<!-- Access Control -->
		<div class="space-y-4">
			<div class="space-y-2">
				<label class="text-base font-bold">Access Control</label>
				<p class="text-sm">Configure view and operate permissions for each field</p>
			</div>

			<div class="space-y-4">
				<!-- 每个字段的权限卡片 -->
				<div v-for="field in permissionFields" :key="field.id" class="permission-card">
					<!-- 字段名和标签 -->
					<div class="field-header">
						<span class="field-name">{{ field.name }}</span>
						<el-tag type="primary" size="small">
							{{ field.type === 'stage' ? 'Stage' : 'Field' }}
						</el-tag>
					</div>

					<!-- 使用 PermissionSelector 组件 -->
					<PermissionSelector
						v-model="field.permissions"
						@update:model-value="handleFieldPermissionChange"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, watch } from 'vue';
import PermissionSelector from './PermissionSelector.vue';
import { PortalAvailabilityEnum, ViewPermissionModeEnum } from '@/enums/permissionEnum';

interface PermissionData {
	viewPermissionMode: number;
	viewTeams: string[];
	useSameGroups: boolean;
	operateTeams: string[];
}

interface PermissionField {
	id: string;
	name: string;
	type: 'stage' | 'field';
	permissions: PermissionData;
}

interface Props {
	modelValue?: {
		portalAvailability: number;
		fields: PermissionField[];
	};
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		portalAvailability: PortalAvailabilityEnum.Viewable,
		fields: [
			{
				id: 'stage',
				name: 'Stage',
				type: 'stage',
				permissions: {
					viewPermissionMode: ViewPermissionModeEnum.Public,
					viewTeams: [],
					useSameGroups: true,
					operateTeams: [],
				},
			},
			{
				id: 'companyName',
				name: 'Company Name',
				type: 'field',
				permissions: {
					viewPermissionMode: ViewPermissionModeEnum.Public,
					viewTeams: [],
					useSameGroups: true,
					operateTeams: [],
				},
			},
		],
	}),
});

const emit = defineEmits(['update:modelValue']);

// 本地数据
const localPermissions = reactive({
	portalAvailability: props.modelValue.portalAvailability ?? PortalAvailabilityEnum.Viewable,
});

// 字段权限列表（临时写死）
const permissionFields = reactive<PermissionField[]>([
	{
		id: 'stage',
		name: 'Stage',
		type: 'stage',
		permissions: {
			viewPermissionMode: ViewPermissionModeEnum.Public,
			viewTeams: [],
			useSameGroups: true,
			operateTeams: [],
		},
	},
	{
		id: 'companyName',
		name: 'Company Name',
		type: 'field',
		permissions: {
			viewPermissionMode: ViewPermissionModeEnum.Public,
			viewTeams: [],
			useSameGroups: true,
			operateTeams: [],
		},
	},
]);

// 处理字段权限变化
const handleFieldPermissionChange = () => {
	emitUpdate();
};

// 向父组件发送更新
const emitUpdate = () => {
	emit('update:modelValue', {
		portalAvailability: localPermissions.portalAvailability,
		fields: permissionFields.map((f) => ({
			...f,
			permissions: { ...f.permissions },
		})),
	});
};

// 监听本地数据变化
watch(
	() => localPermissions.portalAvailability,
	() => {
		emitUpdate();
	}
);

// 监听外部数据变化
watch(
	() => props.modelValue,
	(newVal) => {
		if (newVal) {
			localPermissions.portalAvailability =
				newVal.portalAvailability ?? PortalAvailabilityEnum.Viewable;
			if (newVal.fields && newVal.fields.length > 0) {
				permissionFields.splice(0, permissionFields.length, ...newVal.fields);
			}
		}
	},
	{ deep: true }
);
</script>

<style scoped>
/* 横向单选按钮样式 */
.permission-radio-horizontal {
	display: flex;
	align-items: center;
	padding: 10px 20px;
	border: 1px solid var(--el-border-color);
	border-radius: 8px;
	margin: 0;
	transition: all 0.2s ease;
	white-space: nowrap;
}

.permission-radio-horizontal:hover {
	border-color: var(--el-color-primary);
	background-color: var(--el-fill-color-light);
}

:deep(.el-radio__input.is-checked + .el-radio__label) {
	color: var(--el-color-primary);
	font-weight: 500;
}

/* 权限卡片样式 */
.permission-card {
	padding: 20px;
	border: 1px solid var(--el-border-color);
	border-radius: 12px;
	background-color: var(--el-fill-color-blank);
	transition: all 0.2s ease;
}

.permission-card:hover {
	border-color: var(--el-border-color-dark);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

/* 字段头部 */
.field-header {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 16px;
	padding-bottom: 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.field-name {
	font-weight: 600;
	font-size: 15px;
	color: var(--el-text-color-primary);
}
</style>
