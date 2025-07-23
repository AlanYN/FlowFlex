<template>
	<div class="prototype-tabs">
		<!-- Tab List -->
		<el-scrollbar class="">
			<div ref="tabsListRef" class="tabs-list" :class="tabsListClass">
				<!-- 移动指示器 -->
				<div ref="indicatorRef" class="tab-indicator"></div>

				<button
					v-for="(tab, index) in tabs"
					:key="tab[keys.value]"
					ref="tabButtonRefs"
					:class="[
						'tab-trigger',
						{
							'tab-trigger--active': modelValue === tab[keys.value],
							'tab-trigger--disabled': tab.disabled,
						},
					]"
					:disabled="tab.disabled"
					@click="handleTabClick(tab[keys.value], index)"
				>
					<component v-if="tab.icon" :is="tab.icon" class="tab-icon" />
					<span class="tab-label">{{ tab[keys.label] }}</span>
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

// 生成唯一实例 ID
const instanceId = `prototype-tabs-${Math.random().toString(36).substr(2, 9)}`;

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
	keys?: {
		label: string;
		value: string;
	};
}

const props = withDefaults(defineProps<Props>(), {
	size: 'default',
	type: 'default',
	tabsListClass: '',
	contentClass: '',
	keys: () => ({
		label: 'label',
		value: 'value',
	}),
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
	return props.tabs.findIndex((tab) => tab[props.keys.value] === props.modelValue);
});

// 更新指示器位置
const updateIndicator = (targetIndex: number, animate = true) => {
	// 安全检查
	if (!indicatorRef.value || targetIndex < 0) {
		return;
	}

	// 确保 tabButtonRefs 存在且有效
	if (!tabButtonRefs.value || !Array.isArray(tabButtonRefs.value)) {
		return;
	}

	// 确保索引在有效范围内
	if (targetIndex >= tabButtonRefs.value.length) {
		return;
	}

	const targetButton = tabButtonRefs.value[targetIndex];
	if (!targetButton) {
		return;
	}

	// 使用 nextTick 确保 DOM 更新完成
	nextTick(() => {
		try {
			const rect = targetButton.getBoundingClientRect();
			const containerRect = tabsListRef.value?.getBoundingClientRect();

			if (!containerRect || rect.width === 0) {
				return;
			}

			const left = rect.left - containerRect.left;
			const width = rect.width;

			// 使用实例 ID 确保 GSAP 动画不冲突
			const animationTarget = indicatorRef.value;
			if (!animationTarget) return;

			if (animate) {
				// 使用GSAP动画，添加实例 ID 作为标识
				gsap.to(animationTarget, {
					x: left,
					width: width,
					duration: 0.3,
					ease: 'power2.out',
					id: `indicator-${instanceId}`,
				});
			} else {
				// 立即设置位置（初始化时）
				gsap.set(animationTarget, {
					x: left,
					width: width,
					id: `indicator-${instanceId}`,
				});
			}
		} catch (error) {
			console.warn(`PrototypeTabs ${instanceId}: Failed to update indicator:`, error);
		}
	});
};

// 窗口大小变化处理函数
const handleResize = () => {
	if (activeTabIndex.value >= 0) {
		// 使用 requestAnimationFrame 确保在 DOM 更新后执行
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
		// 触发事件
		emit('update:modelValue', value);
		emit('tab-click', value);
		emit('tab-change', value);
	}
};

// 监听激活tab变化
watch(activeTabIndex, (newIndex, oldIndex) => {
	if (newIndex >= 0 && newIndex !== oldIndex) {
		// 添加延迟确保 DOM 完全更新
		setTimeout(() => {
			updateIndicator(newIndex, true);
		}, 10);
	}
});

// 监听tabs配置变化
watch(
	() => props.tabs,
	() => {
		nextTick(() => {
			if (activeTabIndex.value >= 0) {
				// 延迟更新，确保新的 tabs 完全渲染
				setTimeout(() => {
					updateIndicator(activeTabIndex.value, false);
				}, 50);
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
	// 清理 GSAP 动画
	if (indicatorRef.value) {
		gsap.killTweensOf(indicatorRef.value);
	}

	window.removeEventListener('resize', debouncedHandleResize);
	if (resizeTimer) {
		clearTimeout(resizeTimer);
	}

	// 清理ResizeObserver
	cleanupResizeObserver();
});
</script>

<style lang="scss">
@use './index.scss';
</style>
