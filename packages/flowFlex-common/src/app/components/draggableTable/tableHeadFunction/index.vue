<template>
	<div class="flex mb-4 w-full px-4 justify-between min-h-[20px]">
		<div class="flex items-center gap-x-1">
			<div v-if="showTools" class="flex items-center gap-x-1">
				<el-button
					type="primary"
					v-if="isArchive"
					link
					:icon="RefreshLeft"
					@click="tools('restore')"
				>
					<el-link>Restore</el-link>
				</el-button>
				<el-button
					type="primary"
					v-if="isCanEdit"
					link
					:icon="Edit"
					@click="tools('assign')"
				>
					<el-link>Assign</el-link>
				</el-button>
				<el-button
					type="primary"
					v-if="isCanDelete"
					link
					:icon="Delete"
					@click="tools('delete')"
				>
					<el-link>Delete</el-link>
				</el-button>
				<el-button type="primary" v-if="isCanEdit" link :icon="Plus" @click="tools('task')">
					<el-link>Create tasks</el-link>
				</el-button>
				<el-button
					type="primary"
					v-if="isCanEdit"
					link
					:icon="Plus"
					@click="tools('notes')"
				>
					<el-link>Create notes</el-link>
				</el-button>
			</div>
		</div>
		<div>
			<div v-if="showType" class="flex items-center">
				<div>
					<el-tooltip content="List view" placement="bottom-start">
						<el-button
							class="font-bold rounded-none rounded-s-lg"
							:icon="AccountListIcon"
							:type="buttonStatus == 'row' ? 'primary' : ''"
							@click="changeStatus('row')"
						/>
					</el-tooltip>
				</div>
				<el-tooltip content="Card view" placement="bottom-start">
					<el-button
						class="font-bold rounded-none rounded-e-lg"
						:icon="AccountColumnIcon"
						:type="buttonStatus == 'col' ? 'primary' : ''"
						@click="changeStatus('col')"
					/>
				</el-tooltip>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue';
import type { PropType } from 'vue';
import AccountColumnIcon from '@assets/svg/customer/column.svg';
import AccountListIcon from '@assets/svg/customer/accountList.svg';
import { Delete, Edit, Plus, RefreshLeft } from '@element-plus/icons-vue';

const props = defineProps({
	type: {
		type: Boolean,
		default: false,
	},
	showTools: {
		type: Boolean,
		default: false,
	},
	showType: {
		type: Boolean,
		default: true,
	},
	/**
	 * 2:Contact | 3:Company | 4:Deal
	 */
	pipelineType: {
		type: Number as PropType<2 | 3 | 4>,
		default: 0,
	},
	isCanDelete: {
		type: Boolean,
		default: false,
	},
	isCanEdit: {
		type: Boolean,
		default: false,
	},
	isArchive: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['changeStatus', 'tools']);
const buttonStatus = ref(props.type ? 'row' : 'col');
const changeStatus = (type: string) => {
	if (buttonStatus.value == type) return;
	buttonStatus.value = type;
	emit('changeStatus', buttonStatus.value == 'row');
};

const tools = (type: string) => {
	emit('tools', type);
};
</script>
