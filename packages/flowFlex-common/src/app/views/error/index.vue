<template>
	<div class="flex justify-center flex-col items-center">
		<component :is="icon" />
		<el-result class="prefixCls" icon="error" :title="title" :sub-title="subTitle">
			<template #icon>
				<span></span>
			</template>
			<template #extra>
				<el-button type="primary" @click="handler" v-if="btnText">
					{{ btnText }}
				</el-button>
			</template>
		</el-result>
	</div>
</template>

<script setup lang="ts">
import type { PropType } from 'vue';
import { ref, computed, unref } from 'vue';
import { ExceptionEnum } from '@/enums/exceptionEnum';
import { useRoute } from 'vue-router';
import { useI18n } from '@/hooks/useI18n';
import { useGo, useRedo } from '@/hooks/web/usePage';
import { PageEnum } from '@/enums/pageEnum';
import notDataSvg from '@assets/svg/global/no-data.svg';
import netWorkSvg from '@assets/svg/global/net-error.svg';
import noPagesSvg from '@assets/svg/global/no-pages.svg';

declare interface Fn<T = any, R = T> {
	(...arg: T[]): R;
}

interface MapValue {
	title: string;
	subTitle: string;
	btnText?: string;
	icon?: any;
	handler?: Fn;
	status?: string;
}

const props = defineProps({
	// 状态码
	status: {
		type: Number as PropType<number>,
		default: ExceptionEnum.PAGE_NOT_FOUND,
	},

	full: {
		type: Boolean as PropType<boolean>,
		default: false,
	},
});

const statusMapRef = ref(new Map<string | number, MapValue>());

const router = useRoute();
const go = useGo();
const redo = useRedo();
const { t } = useI18n();

const getStatus = computed(() => {
	const { status: routeStatus } = router.query;
	console.log('router.query:', router.query);
	return Number(routeStatus) || props.status;
});

const getMapValue = computed((): MapValue => {
	return unref(statusMapRef).get(unref(getStatus)) as MapValue;
});

const title = computed(() => unref(getMapValue).title);
const subTitle = computed(() => unref(getMapValue).subTitle);
const btnText = computed(() => unref(getMapValue).btnText);
const icon = computed(() => unref(getMapValue).icon);
const handler = computed(() => unref(getMapValue).handler);

const backLoginI18n = t('sys.exception.backLogin');
const backHomeI18n = t('sys.exception.backHome');

unref(statusMapRef).set(ExceptionEnum.PAGE_NOT_ACCESS, {
	title: '403',
	status: `${ExceptionEnum.PAGE_NOT_ACCESS}`,
	subTitle: t('sys.exception.subTitle403'),
	btnText: props.full ? backLoginI18n : backHomeI18n,
	handler: () => (props.full ? go(PageEnum.BASE_LOGIN) : go()),
	icon: notDataSvg,
});

unref(statusMapRef).set(ExceptionEnum.PAGE_NOT_FOUND, {
	title: '404',
	status: `${ExceptionEnum.PAGE_NOT_FOUND}`,
	subTitle: t('sys.exception.subTitle404'),
	btnText: props.full ? backLoginI18n : backHomeI18n,
	handler: () => (props.full ? go(PageEnum.BASE_LOGIN) : go()),
	icon: noPagesSvg,
});

unref(statusMapRef).set(ExceptionEnum.ERROR, {
	title: '500',
	status: `${ExceptionEnum.ERROR}`,
	subTitle: t('sys.exception.subTitle500'),
	btnText: backHomeI18n,
	handler: () => go(),
	icon: netWorkSvg,
});

unref(statusMapRef).set(ExceptionEnum.PAGE_NOT_DATA, {
	title: t('sys.exception.noDataTitle'),
	subTitle: '',
	btnText: t('sys.app.refresh'),
	handler: () => redo(),
	icon: notDataSvg,
});

unref(statusMapRef).set(ExceptionEnum.NET_WORK_ERROR, {
	title: t('sys.exception.networkErrorTitle'),
	subTitle: t('sys.exception.networkErrorSubTitle'),
	btnText: t('sys.app.refresh'),
	handler: () => redo(),
	icon: netWorkSvg,
});
</script>
