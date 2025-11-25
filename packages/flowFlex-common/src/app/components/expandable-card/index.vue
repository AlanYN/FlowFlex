<template>
	<div
		ref="cardRef"
		class="expandable-card"
		:class="{ 'is-expanded': isExpanded }"
		:style="expandedStyle"
		@mouseenter="handleMouseEnter"
		@mouseleave="handleMouseLeave"
		@click="handleClick"
	>
		<div class="card-base">
			<!-- 卡片内容 - 正常状态 -->
			<div class="card-content-normal">
				<slot name="normal"></slot>
			</div>
		</div>

		<!-- 展开内容使用 Teleport 传送到 body，避免被父组件限制 -->
		<Teleport to="body" :disabled="!isExpanded">
			<div
				v-if="isExpanded"
				ref="expandedContentRef"
				class="card-content-expanded"
				:style="expandedContentStyle"
				@mouseenter="handleExpandedMouseEnter"
				@mouseleave="handleExpandedMouseLeave"
			>
				<slot name="expanded"></slot>
			</div>
		</Teleport>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted, onMounted, nextTick, watch } from 'vue';

interface Props {
	/** 悬浮延迟时间（毫秒），默认500 */
	hoverDelay?: number;
	/** 放大倍数，默认1.2 */
	scale?: number;
}

interface Emits {
	(e: 'click'): void;
}

const props = withDefaults(defineProps<Props>(), {
	hoverDelay: 500,
	scale: 1.2,
});

const emit = defineEmits<Emits>();

const cardRef = ref<HTMLElement>();
const expandedContentRef = ref<HTMLElement>();
const isExpanded = ref(false);
let hoverTimer: ReturnType<typeof setTimeout> | null = null;
let expandedHoverTimer: ReturnType<typeof setTimeout> | null = null;

/**
 * 计算展开时的样式，避免超出可视区域
 */
const expandedStyle = computed(() => {
	if (!isExpanded.value || !cardRef.value) {
		return {};
	}

	const card = cardRef.value;
	const rect = card.getBoundingClientRect();
	const viewportWidth = window.innerWidth;
	const viewportHeight = window.innerHeight;
	const scale = props.scale;

	// 计算放大后的尺寸增量
	const widthIncrease = (rect.width * scale - rect.width) / 2;
	const heightIncrease = (rect.height * scale - rect.height) / 2;

	// 计算各方向的可用空间
	const spaceLeft = rect.left;
	const spaceRight = viewportWidth - rect.right;
	const spaceTop = rect.top;
	const spaceBottom = viewportHeight - rect.bottom;

	// 计算需要的偏移量
	let translateX = 0;
	let translateY = 0;
	let originX = 'center';
	let originY = 'center';

	// 处理水平方向
	if (spaceLeft < widthIncrease && spaceRight < widthIncrease) {
		// 两边空间都不足，居中显示
		originX = 'center';
		translateX = 0;
	} else if (spaceLeft < widthIncrease) {
		// 左边空间不足，以左边为原点，向右偏移
		originX = 'left';
		translateX = widthIncrease - spaceLeft;
	} else if (spaceRight < widthIncrease) {
		// 右边空间不足，以右边为原点，向左偏移
		originX = 'right';
		translateX = -(widthIncrease - spaceRight);
	}

	// 处理垂直方向
	if (spaceTop < heightIncrease && spaceBottom < heightIncrease) {
		// 上下空间都不足，居中显示
		originY = 'center';
		translateY = 0;
	} else if (spaceTop < heightIncrease) {
		// 上边空间不足，以上边为原点，向下偏移
		originY = 'top';
		translateY = heightIncrease - spaceTop;
	} else if (spaceBottom < heightIncrease) {
		// 下边空间不足，以下边为原点，向上偏移
		originY = 'bottom';
		translateY = -(heightIncrease - spaceBottom);
	}

	return {
		transform: `scale(${scale}) translate(${translateX}px, ${translateY}px)`,
		transformOrigin: `${originX} ${originY}`,
	};
});

/**
 * 计算展开内容的 fixed 定位样式
 */
const expandedContentStyle = computed(() => {
	if (!isExpanded.value || !cardRef.value) {
		return {};
	}

	const card = cardRef.value;
	const rect = card.getBoundingClientRect();
	const scale = props.scale;

	// 计算放大后的尺寸
	const scaledWidth = rect.width * scale;
	const scaledHeight = rect.height * scale;

	// 计算各方向的可用空间
	const viewportWidth = window.innerWidth;
	const viewportHeight = window.innerHeight;
	const spaceLeft = rect.left;
	const spaceRight = viewportWidth - rect.right;
	const spaceTop = rect.top;
	const spaceBottom = viewportHeight - rect.bottom;

	// 计算放大后的增量
	const widthIncrease = (scaledWidth - rect.width) / 2;
	const heightIncrease = (scaledHeight - rect.height) / 2;

	// 计算位置，考虑边界情况
	let left = rect.left;
	let top = rect.top;

	// 处理水平方向
	if (spaceLeft < widthIncrease) {
		left = Math.max(0, rect.left - widthIncrease);
	} else if (spaceRight < widthIncrease) {
		left = Math.min(viewportWidth - scaledWidth, rect.right - scaledWidth + widthIncrease);
	} else {
		left = rect.left - widthIncrease;
	}

	// 处理垂直方向
	if (spaceTop < heightIncrease) {
		top = Math.max(0, rect.top - heightIncrease);
	} else if (spaceBottom < heightIncrease) {
		top = Math.min(viewportHeight - scaledHeight, rect.bottom - scaledHeight + heightIncrease);
	} else {
		top = rect.top - heightIncrease;
	}

	return {
		width: `${scaledWidth}px`,
		height: `${scaledHeight}px`,
		left: `${left}px`,
		top: `${top}px`,
	};
});

/**
 * 处理鼠标进入
 */
function handleMouseEnter() {
	// 清除之前的定时器
	if (hoverTimer) {
		clearTimeout(hoverTimer);
	}

	// 设置延迟展开
	hoverTimer = setTimeout(() => {
		isExpanded.value = true;
		// 等待 DOM 更新后重新计算位置
		nextTick(() => {
			// 触发样式重新计算
			if (cardRef.value) {
				cardRef.value.offsetHeight;
			}
		});
	}, props.hoverDelay);
}

/**
 * 处理鼠标离开
 */
function handleMouseLeave() {
	// 清除定时器
	if (hoverTimer) {
		clearTimeout(hoverTimer);
		hoverTimer = null;
	}

	// 延迟收起，给用户时间移动到展开内容
	if (expandedHoverTimer) {
		clearTimeout(expandedHoverTimer);
	}
	expandedHoverTimer = setTimeout(() => {
		isExpanded.value = false;
		expandedHoverTimer = null;
	}, 100);
}

/**
 * 处理展开内容的鼠标进入
 */
function handleExpandedMouseEnter() {
	// 清除收起定时器
	if (expandedHoverTimer) {
		clearTimeout(expandedHoverTimer);
		expandedHoverTimer = null;
	}
}

/**
 * 处理展开内容的鼠标离开
 */
function handleExpandedMouseLeave() {
	// 延迟收起
	if (expandedHoverTimer) {
		clearTimeout(expandedHoverTimer);
	}
	expandedHoverTimer = setTimeout(() => {
		isExpanded.value = false;
		expandedHoverTimer = null;
	}, 100);
}

/**
 * 处理点击事件
 */
function handleClick() {
	emit('click');
}

/**
 * 监听滚动，更新展开内容位置
 */
function handleScroll() {
	// 触发样式重新计算
	if (isExpanded.value && cardRef.value) {
		cardRef.value.offsetHeight;
	}
}

// 监听展开状态，添加/移除滚动监听
watch(isExpanded, (expanded) => {
	if (expanded) {
		window.addEventListener('scroll', handleScroll, true);
		window.addEventListener('resize', handleScroll);
	} else {
		window.removeEventListener('scroll', handleScroll, true);
		window.removeEventListener('resize', handleScroll);
	}
});

// 组件挂载
onMounted(() => {
	// 初始化
});

// 组件卸载时清理定时器和事件监听
onUnmounted(() => {
	if (hoverTimer) {
		clearTimeout(hoverTimer);
	}
	if (expandedHoverTimer) {
		clearTimeout(expandedHoverTimer);
	}
	window.removeEventListener('scroll', handleScroll, true);
	window.removeEventListener('resize', handleScroll);
});
</script>

<style scoped lang="scss">
.expandable-card {
	position: relative;
	transition:
		transform 0.3s cubic-bezier(0.4, 0, 0.2, 1),
		z-index 0s,
		transform-origin 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	z-index: 1;
	transform-origin: center center;
	will-change: transform;

	&.is-expanded {
		z-index: 100;
	}
}

.card-base {
	position: relative;
}

.card-content-normal {
	opacity: 1;
	visibility: visible;
	transition:
		opacity 0.2s ease,
		visibility 0s 0.2s;

	.expandable-card.is-expanded & {
		opacity: 0;
		visibility: hidden;
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		pointer-events: none;
		transition:
			opacity 0.2s ease,
			visibility 0s;
	}
}

.card-content-expanded {
	position: fixed;
	z-index: 9999;
	opacity: 1;
	visibility: visible;
	pointer-events: auto;
	transform-origin: center center;
	overflow: visible;
	transition: opacity 0.3s ease 0.1s;
}
</style>
