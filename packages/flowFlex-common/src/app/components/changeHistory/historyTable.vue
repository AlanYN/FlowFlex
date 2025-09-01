<template>
	<el-dialog
		v-model="dialogTableVisible"
		:close-on-click-modal="false"
		title="Change History"
		:width="bigDialogWidth"
		@close="closeVisible"
		draggable
	>
		<el-table
			:data="tablePagesCount"
			v-loading="loading"
			:border="true"
			:scrollbar-always-on="true"
			max-height="500"
		>
			<template #empty>
				<el-empty description="No Data" :image-size="50" />
			</template>
			<el-table-column prop="userName" label="USER NAME" width="150" />
			<el-table-column prop="opType" label="TYPE" width="100">
				<template #default="scope">
					<el-tag :type="opTagTypeEnum[scope.row.opType]">
						{{ opTypeEnum[scope.row.opType] }}
					</el-tag>
				</template>
			</el-table-column>
			<el-table-column prop="message" label="DESCRIPTION" min-width="400">
				<template #default="scope">
					<div class="flex flex-col gap-y-2">
						<div v-for="item in scope.row.message" :key="item" class="flex">
							<div v-if="item.includes('$')" class="avatar-icon">
								<CRMAIIcon />
							</div>
							{{ item.replace('$', '') }}
						</div>
					</div>
				</template>
			</el-table-column>
			<el-table-column prop="opTime" label="TIME" min-width="200" />
		</el-table>
		<CustomerPagination
			:total="total"
			:limit="paginationPages.pageSize"
			:page="paginationPages.pageIndex"
			:background="true"
			@pagination="handleCurrentChange"
			@update:page="handlePageUpdate"
			@update:limit="handleLimitUpdate"
		/>
	</el-dialog>
</template>

<script setup lang="ts">
import { watch, computed, ref } from 'vue';
import { useRoute } from 'vue-router';
import {
	getChangeHistory,
	getLeadsChangeHistory,
	getDealChangeHistory,
} from '@/apis/customer/global';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { timeZoneConvert } from '@/hooks/time';
import { receptionDate, bigDialogWidth } from '@/settings/projectSetting';
import CRMAIIcon from '@assets/svg/global/crmAI.svg';

const props = defineProps({
	showDialog: {
		type: Boolean,
		default: false,
	},
});

const route = useRoute();
const loading = ref(false);
const total = ref(0);
// 分页
const paginationPages = ref({
	pageIndex: 1,
	pageSize: 15,
});

const opTypeEnum = {
	0: 'create',
	1: 'update',
	2: 'delete',
};

const opTagTypeEnum = {
	0: 'success',
	1: 'warning',
	2: 'danger',
};

watch(
	() => props.showDialog,
	(newValue) => {
		loading.value = newValue;
		if (newValue) {
			getHisttoryTable();
		} else {
			tablePagesCount.value = [];
		}
	}
);

const getHisttoryTable = async () => {
	try {
		loading.value = true;
		let res = null as any;
		console.log('route:', route);
		if (
			route.meta?.changeCode == 'EDIT:EDIT:LEADS:COMPANIES' ||
			route.meta?.changeCode == 'EDIT:LEADS:CONTACTS'
		) {
			res = await getLeadsChangeHistory(route.query?.id as string, {
				category: route.meta?.changeCode,
				...paginationPages.value,
			});
		} else if (route.meta?.changeCode == 'EDIT:DEALS') {
			res = await getDealChangeHistory(route.query?.id as string, {
				category: route.meta?.changeCode,
				...paginationPages.value,
			});
		} else {
			res = await getChangeHistory(route.query?.customerId as string, {
				category: route.meta?.code,
				customerId: route.query?.customerId,
				...paginationPages.value,
			});
		}

		const data = res.data.list?.map((item) => {
			return {
				...item,
				opTime: timeZoneConvert(item.opTime, false, receptionDate),
			};
		});
		tablePagesCount.value = data;
		total.value = res.data.totalCount;
		paginationPages.value.pageIndex = res.data.currentPage;
	} finally {
		loading.value = false;
	}
};

const emit = defineEmits(['visible:hideDialog', 'visible:showDialog']);

const dialogTableVisible = computed({
	get: () => {
		return props.showDialog;
	},
	set: () => {},
});

const closeVisible = () => {
	emit('visible:hideDialog');
};

const handlePageUpdate = (val: number) => {
	paginationPages.value.pageIndex = val;
};
const handleLimitUpdate = (val) => {
	paginationPages.value.pageSize = val;
};
const handleCurrentChange = () => {
	getHisttoryTable();
};

const tablePagesCount = ref([]);
</script>

<style scoped lang="scss">
.avatar-icon {
	width: 22px;
	height: 22px;
	background-color: hsl(0 0% 96.1%);
	@apply flex items-center justify-center mr-2
	rounded-full bg-gradient-to-r 
	from-[var(--primary-500)] to-[var(--primary-reverse-500)] text-white;

	svg {
		transform: scale(0.6);
	}
}
</style>
