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
import { ref, computed, inject, nextTick } from 'vue';
import { useUserStore } from '@/stores/modules/user';
import { switchingCompany } from '@/apis/login/user';
import { ElMessage } from 'element-plus';
import { NotificationAction } from '#/golbal';
import { useI18n } from '@/hooks/useI18n';
import { useWujie } from '@/hooks/wujie/micro-app.config';

const userStore = useUserStore();
const { t } = useI18n();

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

	const fromTenantId = String(userStore.getUserInfo?.tenantId || '');
	const toTenantId = String(id);

	try {
		checkCompany.value = true;

		// 步骤1：开始切换，显示遮罩
		userStore.startTenantSwitching(fromTenantId, toTenantId);

		// 步骤2：等待DOM更新和动画开始
		await nextTick();
		await new Promise((resolve) => setTimeout(resolve, 150));

		// 步骤3：模拟进度更新（4个阶段）
		const steps = [
			{ key: 'validating', progress: 25, duration: 800 },
			{ key: 'loading', progress: 50, duration: 1000 },
			{ key: 'configuring', progress: 75, duration: 800 },
			{ key: 'finalizing', progress: 95, duration: 600 },
		];

		let currentStepIndex = 0;
		const updateStep = () => {
			// 检查用户是否取消了切换
			if (
				!userStore.getTenantSwitching.isActive ||
				userStore.getTenantSwitching.currentStep === 'cancelling'
			) {
				clearInterval(progressInterval);
				return;
			}
			if (currentStepIndex < steps.length) {
				const step = steps[currentStepIndex];
				userStore.updateProgress(step.progress, step.key);
				currentStepIndex++;
			}
		};

		// 启动进度模拟
		const progressInterval = setInterval(updateStep, 800);
		updateStep(); // 立即执行第一步

		// 步骤4：执行API调用（与进度模拟并行）
		const res = await switchingCompany(id);

		// 清除进度模拟
		clearInterval(progressInterval);

		// 检查用户是否在API调用期间取消了切换
		if (!userStore.getTenantSwitching.isActive) {
			checkCompany.value = false;
			return;
		}

		if (res.code == '200' || res.code == 1) {
			// 成功：更新到100%
			userStore.updateProgress(100, 'finalizing');
			await new Promise((resolve) => setTimeout(resolve, 300));

			// 执行登出和重新登录
			userStore.setSessionTimeout(true);
			const { tokenExpiredLogOut } = useWujie();
			if (tokenExpiredLogOut) {
				await tokenExpiredLogOut(true);
			}
			await userStore.afterLoginAction(true);
		} else {
			// 失败：根据错误码显示不同的错误消息
			let errorMessage = '';

			if (res.code === 4032) {
				// 情况1：没有访问权限
				errorMessage = t('sys.tenant.error.noAccess');
			} else if (res.code === 4033) {
				// 情况2：可以访问但没有角色权限
				errorMessage = t('sys.tenant.error.noRole');
			} else {
				// 其他错误
				errorMessage = res.msg || t('sys.tenant.error.general');
			}

			userStore.setTenantError(errorMessage);
			checkCompany.value = false;
			ElMessage.error(errorMessage);
			return;
		}
	} catch (error) {
		// 异常：显示网络错误
		const errorMessage = t('sys.tenant.error.network');
		userStore.setTenantError(errorMessage);
		checkCompany.value = false;
		ElMessage.error(errorMessage);
		console.error('Tenant switch error:', error);
		return;
	}

	// 步骤5：通知租户变更
	checkCompany.value = false;
	notifyTenantChange();

	// 步骤6：刷新页面（遮罩保持显示）
	window.location.reload();
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

:deep(.el-check-tag.el-check-tag--primary.is-checked) {
	background-color: var(--el-color-primary) !important;
	color: var(--el-color-white) !important;
}
</style>
