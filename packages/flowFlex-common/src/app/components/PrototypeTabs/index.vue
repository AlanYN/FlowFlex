<template>
	<div class="prototype-tabs">
		<!-- Tab List -->
		<el-scrollbar class="">
			<div ref="tabsListRef" class="tabs-list" :class="tabsListClass">
				<!-- 移动指示器 -->
				<div ref="indicatorRef" class="tab-indicator"></div>

				<button
					v-for="(tab, index) in tabs"
					:key="tab.value"
					ref="tabButtonRefs"
					:class="[
						'tab-trigger',
						{
							'tab-trigger--active': modelValue === tab.value,
							'tab-trigger--disabled': tab.disabled,
						},
					]"
					:disabled="tab.disabled"
					@click="handleTabClick(tab.value, index)"
				>
					<component v-if="tab.icon" :is="tab.icon" class="tab-icon" />
					<span class="tab-label">{{ tab.label }}</span>
					<el-badge
						v-if="tab.badge"
						:value="tab.badge"
						:type="tab.badgeType || 'primary'"
						class="tab-badge"
					/>
				</button>
			</div>
		</el-scrollbar>

		<!-- Tab Content -->
		<div class="tabs-content" :class="contentClass">
			<slot></slot>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, provide, ref, onMounted, nextTick, watch, onUnmounted } from 'vue';
import { gsap } from 'gsap';

// 定义Tab项的类型
interface TabItem {
	value: string;
	label: string;
	icon?: any;
	disabled?: boolean;
	badge?: string | number;
	badgeType?: 'primary' | 'success' | 'warning' | 'danger' | 'info';
}

// 组件属性
interface Props {
	modelValue: string;
	tabs: TabItem[];
	size?: 'small' | 'default' | 'large';
	type?: 'default' | 'card' | 'border-card' | 'adaptive';
	tabsListClass?: string;
	contentClass?: string;
}

const props = withDefaults(defineProps<Props>(), {
	size: 'default',
	type: 'default',
	tabsListClass: '',
	contentClass: '',
});

// 组件事件
const emit = defineEmits<{
	'update:modelValue': [value: string];
	'tab-click': [value: string];
	'tab-change': [value: string];
}>();

// 模板引用
const tabsListRef = ref<HTMLElement>();
const indicatorRef = ref<HTMLElement>();
const tabButtonRefs = ref<HTMLElement[]>([]);

// 向子组件提供当前激活的tab值
provide(
	'activeTab',
	computed(() => props.modelValue)
);

// 计算属性
const tabsListClass = computed(() => {
	const baseClass = 'tabs-list';
	const sizeClass = `tabs-list--${props.size}`;
	const typeClass = `tabs-list--${props.type}`;

	return [baseClass, sizeClass, typeClass, props.tabsListClass].filter(Boolean).join(' ');
});

// 当前激活的tab索引
const activeTabIndex = computed(() => {
	return props.tabs.findIndex((tab) => tab.value === props.modelValue);
});

// 更新指示器位置
const updateIndicator = (targetIndex: number, animate = true) => {
	if (!indicatorRef.value || !tabButtonRefs.value[targetIndex]) return;

	const targetButton = tabButtonRefs.value[targetIndex];
	const rect = targetButton.getBoundingClientRect();
	const containerRect = tabsListRef.value?.getBoundingClientRect();

	if (!containerRect) return;

	const left = rect.left - containerRect.left;
	const width = rect.width;

	if (animate) {
		// 使用GSAP动画
		gsap.to(indicatorRef.value, {
			x: left,
			width: width,
			duration: 0.3,
			ease: 'power2.out',
		});
	} else {
		// 立即设置位置（初始化时）
		gsap.set(indicatorRef.value, {
			x: left,
			width: width,
		});
	}
};

// 窗口大小变化处理函数
const handleResize = () => {
	if (activeTabIndex.value >= 0) {
		// 使用requestAnimationFrame确保在DOM更新后执行
		requestAnimationFrame(() => {
			updateIndicator(activeTabIndex.value, false);
		});
	}
};

// 防抖处理的resize函数
let resizeTimer: number | null = null;
const debouncedHandleResize = () => {
	if (resizeTimer) {
		clearTimeout(resizeTimer);
	}
	resizeTimer = window.setTimeout(handleResize, 100);
};

// ResizeObserver实例
let resizeObserver: ResizeObserver | null = null;

// 初始化ResizeObserver
const initResizeObserver = () => {
	if (typeof ResizeObserver !== 'undefined' && tabsListRef.value) {
		resizeObserver = new ResizeObserver(() => {
			debouncedHandleResize();
		});
		resizeObserver.observe(tabsListRef.value);
	}
};

// 清理ResizeObserver
const cleanupResizeObserver = () => {
	if (resizeObserver) {
		resizeObserver.disconnect();
		resizeObserver = null;
	}
};

// 方法
const handleTabClick = (value: string, index: number) => {
	if (value !== props.modelValue) {
		// 先更新指示器位置
		// updateIndicator(index, true);

		// 然后触发事件
		emit('update:modelValue', value);
		emit('tab-click', value);
		emit('tab-change', value);
	}
};

// 监听激活tab变化
watch(activeTabIndex, (newIndex, oldIndex) => {
	if (newIndex >= 0 && newIndex !== oldIndex) {
		nextTick(() => {
			updateIndicator(newIndex, true);
		});
	}
});

// 监听tabs配置变化
watch(
	() => props.tabs,
	() => {
		nextTick(() => {
			if (activeTabIndex.value >= 0) {
				updateIndicator(activeTabIndex.value, false);
			}
		});
	},
	{ deep: true }
);

// 组件挂载后初始化指示器位置并添加resize监听器
onMounted(() => {
	nextTick(() => {
		if (activeTabIndex.value >= 0) {
			updateIndicator(activeTabIndex.value, false);
		}
	});

	// 添加窗口大小变化监听器
	window.addEventListener('resize', debouncedHandleResize);

	// 初始化ResizeObserver
	initResizeObserver();
});

// 组件卸载时移除监听器
onUnmounted(() => {
	window.removeEventListener('resize', debouncedHandleResize);
	if (resizeTimer) {
		clearTimeout(resizeTimer);
	}

	// 清理ResizeObserver
	cleanupResizeObserver();
});
</script>

<style scoped lang="scss">
@use './index.scss';
</style>
