<template>
	<el-menu
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
import { computed, toRaw } from 'vue';
// import { Routes } from '@/router/routers';
import { useI18n } from '@/hooks/useI18n';
import { useRoute, useRouter } from 'vue-router';

import OpenIcon from '@assets/svg/global/open.svg';
import CloseIcon from '@assets/svg/global/close.svg';

import customerICon from '@assets/svg/menu/customer.svg';
import parent from '@assets/svg/menu/parent.svg';

import { usePermissionStore } from '@/stores/modules/permission';

import { PageEnum } from '@/enums/pageEnum';

const { t } = useI18n();
const currentRoute = useRoute();
const router = useRouter();

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
const handleNavigate = (path: string) => {
	router.push(path);
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
	// console.log('currentRoute:', currentRoute.query);
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
