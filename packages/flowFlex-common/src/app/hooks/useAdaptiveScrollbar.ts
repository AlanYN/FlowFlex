import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import type { ElScrollbar } from 'element-plus';

type ScrollbarRef = InstanceType<typeof ElScrollbar> | HTMLUListElement | null;

export function useAdaptiveScrollbar(footerHeight: number = 20) {
	const scrollbarRef = ref<ScrollbarRef>(null);

	const updateScrollbarHeight = () => {
		nextTick(() => {
			if (scrollbarRef.value) {
				const windowHeight = window.innerHeight;
				const scrollbarElement = isElScrollbar(scrollbarRef.value)
					? scrollbarRef.value.$el
					: scrollbarRef.value;
				const scrollbarRect = scrollbarElement.getBoundingClientRect();
				const scrollbarTop = scrollbarRect.top;
				const newHeight = windowHeight - scrollbarTop - footerHeight;
				scrollbarElement.style.height = `${newHeight}px`;

				// 为 ul 元素添加滚动样式
				if (scrollbarElement instanceof HTMLUListElement) {
					scrollbarElement.style.overflowY = 'auto';
					scrollbarElement.style.overflowX = 'hidden';
				}
			}
		});
	};

	onMounted(() => {
		updateScrollbarHeight();
		window.addEventListener('resize', updateScrollbarHeight);
	});

	onUnmounted(() => {
		window.removeEventListener('resize', updateScrollbarHeight);
	});

	return {
		scrollbarRef,
		updateScrollbarHeight,
	};
}

// 类型守卫函数
function isElScrollbar(ref: ScrollbarRef): ref is InstanceType<typeof ElScrollbar> {
	return ref !== null && '$el' in ref;
}
