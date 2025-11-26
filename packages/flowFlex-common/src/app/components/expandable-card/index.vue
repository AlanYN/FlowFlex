<template>
	<div
		ref="cardRef"
		class="expandable-card"
		:class="{ 'is-expanded': isExpanded }"
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
		<Teleport to="body" :disabled="!showExpandedContent">
			<div
				v-if="showExpandedContent"
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
import { ref, computed, onUnmounted, nextTick } from 'vue';
import { useZIndex } from 'element-plus';
import { gsap } from 'gsap';

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
	hoverDelay: 1000,
	scale: 1.2,
});

const emit = defineEmits<Emits>();

// 使用 Element Plus 的 z-index 管理
const { nextZIndex } = useZIndex();

const cardRef = ref<HTMLElement>();
const expandedContentRef = ref<HTMLElement>();
const isExpanded = ref(false);
const expandedZIndex = ref<number>(0);
// 控制展开内容的显示
const showExpandedContent = ref(false);
// gsap 动画实例
let expandAnimation: gsap.core.Tween | null = null;
let collapseAnimation: gsap.core.Tween | null = null;
let hoverTimer: ReturnType<typeof setTimeout> | null = null;
let expandedHoverTimer: ReturnType<typeof setTimeout> | null = null;

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
		zIndex: expandedZIndex.value,
	};
});

/**
 * 处理鼠标进入
 */
function handleMouseEnter() {
	// 清除之前的定时器和动画
	if (hoverTimer) {
		clearTimeout(hoverTimer);
	}
	if (collapseAnimation) {
		collapseAnimation.kill();
		collapseAnimation = null;
	}

	// 设置延迟展开
	hoverTimer = setTimeout(() => {
		// 使用 Element Plus 的 z-index 管理器获取下一个 z-index
		expandedZIndex.value = nextZIndex();
		// 等待 DOM 更新后设置展开状态
		nextTick(() => {
			isExpanded.value = true;
			showExpandedContent.value = true;

			// 等待 DOM 渲染完成后再执行动画
			nextTick(() => {
				if (!expandedContentRef.value) return;

				const el = expandedContentRef.value;

				// 先停止所有动画
				gsap.killTweensOf(el);

				// 设置初始状态：从 scale 0.95 开始
				gsap.set(el, {
					scale: 0.95,
					opacity: 0,
				});

				// 执行展开动画：放大到 scale 1，同时淡入
				expandAnimation = gsap.to(el, {
					scale: 1,
					opacity: 1,
					duration: 0.3,
					ease: 'power1.out',
					onComplete: () => {
						expandAnimation = null;
					},
				});
			});
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
		handleCollapse();
		expandedHoverTimer = null;
	}, 100);
}

/**
 * 处理收起动画
 */
function handleCollapse() {
	if (!showExpandedContent.value || !expandedContentRef.value) {
		isExpanded.value = false;
		return;
	}

	const el = expandedContentRef.value;

	// 先停止所有正在进行的动画
	gsap.killTweensOf(el);
	if (expandAnimation) {
		expandAnimation.kill();
		expandAnimation = null;
	}

	// 执行收起动画：缩小到 scale 0.95，同时淡出
	collapseAnimation = gsap.to(el, {
		scale: 0.95,
		opacity: 0,
		duration: 0.2,
		ease: 'power1.in',
		onComplete: () => {
			isExpanded.value = false;
			showExpandedContent.value = false;
			collapseAnimation = null;
		},
	});
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
		handleCollapse();
		expandedHoverTimer = null;
	}, 100);
}

/**
 * 处理点击事件
 */
function handleClick() {
	emit('click');
}

// 组件卸载时清理定时器和动画
onUnmounted(() => {
	if (hoverTimer) {
		clearTimeout(hoverTimer);
	}
	if (expandedHoverTimer) {
		clearTimeout(expandedHoverTimer);
	}
	if (expandAnimation) {
		expandAnimation.kill();
	}
	if (collapseAnimation) {
		collapseAnimation.kill();
	}
	if (cardRef.value) {
		gsap.killTweensOf(cardRef.value);
	}
	if (expandedContentRef.value) {
		gsap.killTweensOf(expandedContentRef.value);
	}
});

// 暴露展开状态给父组件
defineExpose({
	isExpanded,
});
</script>

<style scoped lang="scss">
.expandable-card {
	position: relative;
	transition:
		transform 0.35s cubic-bezier(0.16, 1, 0.3, 1),
		z-index 0s,
		transform-origin 0.35s cubic-bezier(0.16, 1, 0.3, 1);
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
		opacity 0.2s cubic-bezier(0.4, 0, 1, 1),
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
			opacity 0.2s cubic-bezier(0.4, 0, 1, 1),
			visibility 0s;
	}
}

.card-content-expanded {
	position: fixed;
	pointer-events: auto;
	transform-origin: center center;
	overflow: visible;
	box-shadow:
		0 4px 6px -1px rgba(0, 0, 0, 0.1),
		0 2px 4px -1px rgba(0, 0, 0, 0.06),
		0 20px 25px -5px rgba(0, 0, 0, 0.1),
		0 10px 10px -5px rgba(0, 0, 0, 0.04);
	// gsap 会控制 opacity 和 transform
}
</style>
