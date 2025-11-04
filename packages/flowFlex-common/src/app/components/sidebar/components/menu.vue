<template>
	<el-menu
		:key="menuKey"
		:default-active="computedActiveMenu"
		:collapse="collapse"
		:unique-opened="uniqueOpened"
		:mode="mode"
		:collapse-transition="collapseTransition"
		:router="false"
	>
		<template v-for="(item, index) in menuRoutes" :key="index">
			<el-sub-menu
				v-if="!item.meta.hideChildrenInMenu && !item.meta.hidden"
				v-show="!item.hidden"
				:disabled="!item.meta.status"
				:expand-open-icon="OpenIcon"
				:expand-close-icon="CloseIcon"
				:index="item.path"
			>
				<template #title>
					<component
						:is="item?.meta?.icon"
						:title="t(item.meta.title)"
						width="18"
						height="18"
					/>
					<span
						:title="t(item.meta.title)"
						v-if="!collapse"
						class="ml-2"
						:class="{ 'beta-label': item.meta.beta }"
					>
						{{ t(item.meta.title) }}
					</span>
				</template>

				<template v-for="chil in item.children" :key="chil.name">
					<el-menu-item
						:disabled="!item.meta.status"
						:index="`${item.path}/${chil.path}`"
						v-if="!chil.meta?.hidden && !chil.hidden"
						:title="t(chil.meta.title)"
					>
						<!-- :is="chil?.meta?.icon"  -->
						<template #title>
							<a
								:href="`${item.path}/${chil.path}`"
								@click.prevent="handleNavigate(`${item.path}/${chil.path}`)"
								data-sidebar="menu-button"
								data-size="default"
								data-active="false"
								class="flex items-center gap-2 w-full"
							>
								<span class="item-menu-icon">
									<component
										v-if="!collapse"
										:is="
											isLastMenu(chil, item.children) ? parent : customerICon
										"
										width="18"
										height="50"
									/>
								</span>
								<span :class="{ 'beta-label': chil.meta.beta }">
									{{ t(chil.meta.title) }}
								</span>
							</a>
						</template>
					</el-menu-item>
				</template>
			</el-sub-menu>
			<el-menu-item
				:index="`${item.redirect}`"
				:disabled="!item.meta.status"
				:title="t(item?.meta?.title)"
				v-else-if="!item.meta.hidden"
			>
				<a
					:href="`${item.redirect}`"
					@click.prevent="handleNavigate(`${item.redirect}`)"
					data-sidebar="menu-button"
					data-size="default"
					data-active="false"
					class="flex items-center gap-2 w-full"
				>
					<span class="item-menu-icon">
						<component
							:is="item?.meta?.icon"
							v-if="item?.meta?.icon"
							width="18"
							height="18"
						/>
					</span>
					<span v-if="!collapse" :class="{ 'beta-label': item.meta.beta }">
						{{ t(item.meta.title) }}
					</span>
				</a>
			</el-menu-item>
		</template>
	</el-menu>
</template>

<script lang="ts" setup>
import { computed, toRaw, ref } from 'vue';
// import { Routes } from '@/router/routers';
import { useI18n } from '@/hooks/useI18n';
import { useRoute, useRouter } from 'vue-router';

import OpenIcon from '@assets/svg/global/open.svg';
import CloseIcon from '@assets/svg/global/close.svg';

import customerICon from '@assets/svg/menu/customer.svg';
import parent from '@assets/svg/menu/parent.svg';

import { usePermissionStore } from '@/stores/modules/permission';

import { PageEnum } from '@/enums/pageEnum';
import { useQuestionnaireStoreWithOut } from '@/stores/modules/questionnaire';
import type { ElMenu } from 'element-plus';

const { t } = useI18n();
const currentRoute = useRoute();
const router = useRouter();
const questionnaireStore = useQuestionnaireStoreWithOut();

// 导航状态管理
const isNavigating = ref(false);
const pendingPath = ref('');
// 菜单key，用于强制重新渲染以恢复正确的选中状态
const menuKey = ref(0);

// 定义 emit 事件
const emit = defineEmits<{
	navigationStart: [path: string];
	navigationEnd: [path: string];
	navigationError: [error: any, path: string];
}>();

defineProps({
	uniqueOpened: {
		type: Boolean,
		default: false,
	},
	collapse: {
		type: Boolean,
		default: false,
	},
	mode: {
		type: String as () => 'vertical' | 'horizontal',
		default: 'vertical',
	},
	collapseTransition: {
		type: Boolean,
		default: false,
	},
});

const isLastMenu = (chil, item) => {
	const arr = item.filter((chil) => !chil.meta?.hidden && !chil.hidden) || [];
	return chil.path == arr[arr.length - 1].path;
};

// 处理导航，使用 Vue Router 进行 SPA 导航
const handleNavigate = async (indexPath: string) => {
	// 如果正在导航中，忽略新的导航请求
	if (isNavigating.value) {
		// 更新menuKey强制重新渲染，恢复正确的选中状态
		menuKey.value++;
		return;
	}

	// 如果点击的是当前路由，不进行导航
	if (currentRoute.path === indexPath) {
		// 更新menuKey强制重新渲染，恢复正确的选中状态
		menuKey.value++;
		return;
	}

	// 保存当前路由，以便导航失败时恢复
	const previousPath = currentRoute.path;

	try {
		const isQuestionnairePage = currentRoute.path.includes('/onboard/createQuestion');
		if (isQuestionnairePage) {
			const canLeave = await questionnaireStore.confirmLeave();
			if (!canLeave) {
				// 用户取消离开，更新menuKey强制重新渲染，恢复正确的选中状态
				menuKey.value++;
				return;
			}
		}
		isNavigating.value = true;
		pendingPath.value = indexPath;

		// 通知父组件开始导航
		emit('navigationStart', indexPath);

		// 执行路由导航
		await router.push(indexPath);

		// 通知父组件导航成功
		emit('navigationEnd', indexPath);

		// 导航成功后清除状态
		pendingPath.value = '';
	} catch (error) {
		console.error('导航失败:', error);

		// 导航失败时，尝试恢复到之前的路由
		try {
			if (currentRoute.path !== previousPath) {
				await router.push(previousPath);
				console.log('已恢复到之前的路由:', previousPath);
			}
		} catch (restoreError) {
			console.error('恢复路由失败:', restoreError);
		}

		// 通知父组件导航失败
		emit('navigationError', error, indexPath);

		// 导航失败时也清除状态
		pendingPath.value = '';
	} finally {
		isNavigating.value = false;
	}
};

const menuRoutes = computed<any[]>(() => {
	// Routes.sort((a: any, b: any) => {
	// 	return a.meta.orderNo - b.meta.orderNo;
	// });
	const permissionStore = usePermissionStore();
	return toRaw(permissionStore.frontMenuList).sort((a: any, b: any) => {
		return a.meta.ordinal - b.meta.ordinal;
	});
	// toRaw(permissionStore.frontMenuList);
});

// 计算活跃menu
const computedActiveMenu = computed((): string => {
	// 始终基于当前路由计算活跃状态，不受导航状态影响
	// 这样确保菜单状态与实际路由状态保持同步
	let activePath = '';
	activePath = currentRoute.meta.activeMenu
		? (currentRoute.meta.activeMenu as string)
		: currentRoute.path || `${PageEnum.BASE_HOME}`;
	return activePath as string;
});
</script>

<style lang="scss" scoped>
.item-menu-icon {
	width: 18px;
	height: 18px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
}
</style>
