<template>
	<div class="side-panel-wrapper" :style="{ height: height }">
		<!-- 折叠按钮放在wrapper外层，确保不会被内容覆盖 -->
		<div class="collapse-button" :class="{ collapsed: isCollapsed }" @click="handleButtonClick">
			<el-tooltip
				:content="title"
				:disabled="!title"
				ref="tooltipRef"
				:enterable="false"
				:hide-after="0"
			>
				<el-icon>
					<ArrowRight />
				</el-icon>
			</el-tooltip>
		</div>
		<!-- 面板容器 -->
		<div class="side-panel-container" :class="{ collapsed: isCollapsed }">
			<!-- 内容区域 -->
			<div class="panel-content" :class="{ collapsed: isCollapsed }">
				<div :style="{ display: isCollapsed ? 'none' : 'block' }">
					<slot></slot>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { ArrowRight } from '@element-plus/icons-vue';

interface Props {
	title?: string; // 折叠后显示的标题
	width?: string; // 展开时的宽度
	collapsedWidth?: string; // 折叠时的宽度
	position?: 'left' | 'right'; // 折叠按钮的位置
	modelValue?: boolean; // 是否折叠，支持v-model
	height?: string; // 面板高度
}

const props = withDefaults(defineProps<Props>(), {
	title: '',
	width: '422px',
	collapsedWidth: '0',
	position: 'right',
	modelValue: false,
	height: 'auto', // 默认自动高度，可从外部指定固定高度
});

const emit = defineEmits(['update:modelValue', 'collapse-change']);

const isCollapsed = ref(props.modelValue);
const tooltipRef = ref(null);

// 监听props变化
watch(
	() => props.modelValue,
	(newValue) => {
		isCollapsed.value = newValue;
	}
);

// 处理按钮点击
const handleButtonClick = () => {
	// 先关闭tooltip
	if (tooltipRef.value) {
		// @ts-ignore - Element Plus的tooltip组件有hide方法但TypeScript可能无法识别
		tooltipRef.value.hide?.();
	}

	// 然后执行折叠/展开操作
	toggleCollapse();
};

// 切换折叠状态
const toggleCollapse = () => {
	isCollapsed.value = !isCollapsed.value;
	emit('update:modelValue', isCollapsed.value);
	emit('collapse-change', isCollapsed.value);
};
</script>

<style scoped>
.side-panel-wrapper {
	position: relative;
	display: flex;
	flex-direction: column;
	min-height: 100px; /* 设置最小高度，防止完全塌陷 */
}

.side-panel-container {
	position: relative;
	transition: width 0.3s ease;
	width: v-bind('props.width');
	overflow: hidden;
	height: 100%;
	flex: 1;
}

.side-panel-container.collapsed {
	width: v-bind('props.collapsedWidth');
}

.collapse-button {
	position: absolute;
	top: 50px;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 26px;
	height: 26px;
	background-color: var(--el-color-primary);
	color: white;
	border-radius: 50%;
	cursor: pointer;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
	transition: all 0.3s ease;
	transform: translateY(-50%);
	z-index: 50;
}

.collapse-button:hover {
	opacity: 0.9;
	transform: translateY(-50%) scale(1.1);
	box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3);
}

.collapse-button.collapsed {
	transform: translateY(-50%) rotate(180deg);
}

.collapse-button.collapsed:hover {
	transform: translateY(-50%) rotate(180deg) scale(1.1);
}

.panel-content {
	height: 100%;
	width: 100%;
	transition: all 0.3s ease;
	overflow-y: auto;
}

.panel-content.collapsed {
	overflow: hidden;
}

/* 根据position调整按钮位置 */
.collapse-button {
	right: v-bind("props.position === 'right' ? '-15px' : 'auto'");
	left: v-bind("props.position === 'left' ? '-15px' : 'auto'");
}
</style>
