<template>
	<div v-if="atPresentCompany">
		<el-popover placement="bottom" popper-class="!pr-1" trigger="click" width="300">
			<template #reference>
				<div class="company_check-button" :title="atPresentCompany">
					{{ atPresentCompany }}
				</div>
			</template>
			<div class="gap-4 flex flex-wrap w-full" v-loading="checkCompany">
				<div class="font-bold text-xs">SWITCH COMPANY</div>
				<el-scrollbar max-height="300px" class="w-full pr-3">
					<div class="flex flex-col w-full">
						<el-input placeholder="Search Company" v-model="searchText" />
						<el-check-tag
							v-for="item in companyList"
							:key="item.id"
							class="font-semibold text-xs my-2 w-full text-center"
							style="word-wrap: break-word"
							:checked="atPresentCompany == item.name"
							@change="changeCompany(item.id)"
						>
							{{ item.name }}
						</el-check-tag>
					</div>
				</el-scrollbar>
			</div>
		</el-popover>
	</div>
</template>

<script lang="ts" setup>
import { ref, computed, inject } from 'vue';
import { useUserStore } from '@/stores/modules/user';
import { switchingCompany } from '@/apis/login/user';
import { ElMessage } from 'element-plus';
import { NotificationAction } from '#/golbal';

const userStore = useUserStore();

interface Company {
	id: string;
	name: string;
}

const searchText = ref('');

// 切换公司
// 公司列表
const companyList = computed<Company[]>(() => {
	const { tenants } = userStore.getUserInfo;
	const arr = [] as Company[];
	for (let key in tenants) {
		arr.push({
			id: key,
			name: tenants[key],
		});
	}

	if (searchText.value) {
		return arr.filter((item) =>
			item.name.toLocaleLowerCase().includes(searchText.value.toLocaleLowerCase())
		);
	}

	return arr;
});

const atPresentCompany = computed(() => {
	return userStore.getUserInfo.tenants && userStore.getUserInfo?.tenantId
		? userStore.getUserInfo.tenants[userStore.getUserInfo.tenantId]
		: '';
});

const checkCompany = ref(false);
const changeCompany = async (id) => {
	if (id == userStore.getUserInfo?.tenantId) return;
	try {
		checkCompany.value = true;
		const res = await switchingCompany(id);
		if (res.code == '200' || res.code == 1) {
			userStore.setSessionTimeout(true);
			await userStore.afterLoginAction(true);
		} else {
			res.msg && ElMessage.error(res.msg);
		}
	} finally {
		checkCompany.value = false;
		notifyTenantChange();
		window.location.reload();
	}
};

const notifiers = inject<Array<NotificationAction>>('ancestor-notifiers') ?? [];
const notifyTenantChange = () =>
	notifiers.forEach((fn) => fn({ name: 'tenant-change', msg: undefined }));
</script>

<style lang="scss" scoped>
.company_check-button {
	padding: 10px;
	border-radius: 6px;
	max-width: 200px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.company_check-button:hover {
	@apply bg-primary-200 text-primary-500;
}

:deep(.el-check-tag) {
	width: 100%;
	box-sizing: border-box;
	overflow: hidden;
	display: flex;
	justify-content: center;

	.el-check-tag__content {
		overflow: hidden;
		width: 100%;
	}
}
</style>
