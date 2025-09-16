<template>
	<el-menu
		:default-active="computedActiveMenu"
		:collapse="collapse"
		:unique-opened="uniqueOpened"
		class="el-menu-vertical-demo"
		:mode="mode"
		:collapse-transition="collapseTransition"
		:router="true"
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
					<component :is="item?.meta?.icon" :title="t(item.meta.title)" />
					<span
						:title="t(item.meta.title)"
						v-if="!collapse"
						class="ml-2 font-bold"
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
							<component
								v-if="!collapse"
								class="mb-auto"
								:is="isLastMenu(chil, item.children) ? parent : customerICon"
							/>
							<span class="ml-2" :class="{ 'beta-label': chil.meta.beta }">
								{{ t(chil.meta.title) }}
							</span>
						</template>
					</el-menu-item>
				</template>
			</el-sub-menu>

			<el-menu-item
				:index="`${item.path}/index`"
				:disabled="!item.meta.status"
				:title="t(item?.meta?.title)"
				v-else-if="!item.meta.hidden"
			>
				<component :is="item?.meta?.icon" v-if="item?.meta?.icon" />
				<span
					v-if="!collapse"
					class="ml-2 font-bold"
					:class="{ 'beta-label': item.meta.beta }"
				>
					{{ t(item.meta.title) }}
				</span>
			</el-menu-item>
		</template>
	</el-menu>
</template>

<script lang="ts" setup>
import { computed, toRaw } from 'vue';
// import { Routes } from '@/router/routers';
import { useI18n } from '@/hooks/useI18n';
import { useRoute } from 'vue-router';

import OpenIcon from '@assets/svg/global/open.svg';
import CloseIcon from '@assets/svg/global/close.svg';

import customerICon from '@assets/svg/menu/customer.svg';
import parent from '@assets/svg/menu/parent.svg';

import { usePermissionStore } from '@/stores/modules/permission';

import { PageEnum } from '@/enums/pageEnum';

const { t } = useI18n();
const currentRoute = useRoute();

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
		type: String,
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

// const defaultActive = computed((): string => {
// 	return (router.currentRoute.value.path as string) || '/home/index';
// });

// 计算活跃menu
const computedActiveMenu = computed((): string => {
	// console.log('currentRoute:', currentRoute.query);
	let activePath = '';
	if (Object.keys(currentRoute.query).includes('customerId')) {
		activePath = currentRoute.query.isCustomer
			? (currentRoute.meta.activeMenu as string)
			: '/foundationData/index';
	} else {
		activePath = currentRoute.meta.activeMenu
			? (currentRoute.meta.activeMenu as string)
			: currentRoute.path || `${PageEnum.BASE_HOME}`;
	}
	return activePath as string;
});
</script>

<style lang="scss" scoped>
:deep(.el-icon) {
	width: 14px;
	height: 14px;

	svg {
		width: 14px;
		height: 14px;
	}
}

.beta-label {
	position: relative;
	display: inline-flex;
	align-items: center;

	&::after {
		content: 'BETA';
		position: static;
		display: inline-block;
		margin-left: 8px;
		font-size: 10px;
		font-weight: 700;
		padding: 0 8px;
		background-color: var(--primary-100);
		color: var(--primary-500);
		line-height: 20px;
		letter-spacing: 1.5px;
		@apply rounded-xl;
	}
}
</style>
