<template>
	<div v-show="isActive || !activeTab" class="tab-pane-container">
		<div ref="paneRef" class="tab-pane" :class="paneClass">
			<slot></slot>
		</div>
	</div>
</template>

<script setup lang="ts">
import { inject, computed, ref, type ComputedRef } from 'vue';

// 组件属性
interface Props {
	value: string;
	label?: string;
	disabled?: boolean;
	paneClass?: string;
}

const props = withDefaults(defineProps<Props>(), {
	label: '',
	disabled: false,
	paneClass: '',
});

// 模板引用
const paneRef = ref<HTMLElement>();

// 从父组件注入当前激活的tab值
const activeTab = inject<ComputedRef<string>>('activeTab');

// 计算是否为激活状态
const isActive = computed(() => {
	if (!activeTab) return true; // 如果没有inject到值，默认显示第一个
	return props.value === activeTab.value;
});
</script>

<style scoped lang="scss">
.tab-pane-container {
	width: 100%;
	height: 100%;
	position: relative;
}

.tab-pane {
	width: 100%;
	height: 100%;
}

/* 减少动画偏好设置 */
@media (prefers-reduced-motion: reduce) {
	.tab-pane {
		opacity: 1 !important;
		transform: none !important;
	}
}

/* 打印样式 */
@media print {
	.tab-pane {
		opacity: 1 !important;
		transform: none !important;
	}
}
</style>
