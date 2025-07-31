<!--
 * @Author: zhanghan
 * @Date: 2021-06-01 09:26:44
 * @LastEditors: zhanghan 294333196@qq.com
 * @LastEditTime: 2023-11-15 19:03:02
 * @Descripttion: 分页组件
-->
<template>
	<div
		:class="{ hidden: hidden || total === 0 }"
		class="flex flex-row justify-between px-6 bg-white py-2 dark:bg-black-500 rounded-[0px_0px_8px_8px]"
	>
		<span class="leading-8 text-gray-300 text-xs">{{ total }} Results</span>
		<div>
			<span class="leading-4 px-2 font-bold text-xs mr-4">Show:</span>
			<el-select
				v-model="limit"
				placeholder="Select"
				class="w-20 mr-5 page-limit"
				@change="handleSizeChange"
			
			:teleported="false">
				<el-option
					v-for="(item, index) in pageOfNumber"
					:key="index"
					:label="item"
					:value="item"
				/>
			</el-select>
			<!-- <span class="leading-8 px-3">records</span> -->
			<el-pagination
				v-model:page-size="limit"
				v-model:current-page="page"
				:page-sizes="pageOfNumber"
				:background="background"
				:layout="layout"
				:total="total"
				v-bind="$attrs"
				@current-change="handleCurrentChange"
			/>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, nextTick, watchEffect, toRefs } from 'vue';
import type { PaginationData } from './types.d.ts';
import { slice } from 'lodash-es';
import { pageOfNumber } from '@/settings/projectSetting';

const props = defineProps({
	total: {
		required: true,
		type: Number,
	},
	page: {
		type: Number,
		default: 1,
	},
	limit: {
		type: Number,
		default: 10,
	},
	pageSizes: {
		type: Array as () => number[],
		default() {
			return [10, 20, 50, 100, 300, 500, 1000];
		},
	},
	layout: {
		type: String,
		default: 'prev, pager, next',
	},
	background: {
		type: Boolean,
		default: true,
	},
	hidden: {
		type: Boolean,
		default: false,
	},
	// 分片列表
	sliceList: {
		type: Array,
		default: () => [],
	},
	// 所有列表数据
	allList: {
		type: Array,
		default: () => [],
	},
	// 是否初始化触发pagination事件
	initPagination: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['update:page', 'update:limit', 'update:sliceList', 'pagination']);

const page = computed({
	get() {
		return props.page;
	},
	set(val) {
		emit('update:page', val);
	},
});

const limit = computed({
	get() {
		return props.limit;
	},
	set(val) {
		emit('update:limit', val);
	},
});

const sliceList = computed({
	get() {
		return props.sliceList;
	},
	set(val) {
		emit('update:sliceList', val);
	},
});

// 初始化列表切片起始坐标
let start = computed(() => {
	return (page.value - 1) * limit.value;
});
let end = computed(() => {
	const val =
		(limit.value * page.value > props.total ? props.total : limit.value * page.value) - 1;
	return val || 0;
});

// 判断是否触发分页事件初始化
props.initPagination && handleCurrentChange(1);

// 监听列表数据的初始化传入及页码的变动，进行分片处理
const { allList } = toRefs(props);
watchEffect(() => {
	sliceList.value = slice(allList.value, start.value, Number(end.value) + 1) || [];
});

// 分页大小改变
function handleSizeChange(val: number) {
	// 更新分页大小
	limit.value = val;
	// 初始化分页数触发分页数点击
	page.value = 1;
	nextTick(() => {
		handleCurrentChange(page.value);
	});
}

// 分页数改变
function handleCurrentChange(val: number) {
	// 更新分页数
	page.value = val;
	nextTick(() => {
		emit('pagination', {
			page: val,
			limit: limit.value,
			start: start.value,
			end: end.value,
			sliceList: sliceList.value,
		} as PaginationData);
	});
}
</script>

<style lang="scss">
.page-limit {
	.el-input .el-input__wrapper {
		@apply dark:bg-transparent;
	}
}

.el-pagination.is-background {
	@apply mt-0;

	.btn-prev,
	.btn-next {
		@apply bg-transparent px-3 py-1.5 text-sm text-neutral-600 hover:bg-neutral-100 dark:text-white-100 dark:hover:bg-neutral-700 dark:hover:text-white-100;

		&:disabled {
			@apply bg-transparent;
		}
	}

	.el-pager {
		li {
			@apply bg-transparent;
		}

		li.is-active {
			background-color: rgb(227 235 247 / var(--tw-bg-opacity));
			@apply relative block rounded-lg px-3 py-1.5 text-sm dark:bg-night-200;
		}

		li:hover {
			@apply text-primary-500;
		}
	}
}
</style>
