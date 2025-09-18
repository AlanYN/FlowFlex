<template>
	<div :class="[tabsListClasses, 'tab-button-group']">
		<el-tooltip
			v-for="(tab, index) in tabs"
			:key="tab[keys.value]"
			:content="tab[keys.label]"
			placement="bottom"
			:show-after="500"
		>
			<button
				type="button"
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
				<el-badge
					v-if="tab.badge"
					:value="tab.badge"
					:type="tab.badgeType || 'primary'"
					class="tab-badge"
				/>
			</button>
		</el-tooltip>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

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
	keys?: {
		label: string;
		value: string;
	};
}

const props = withDefaults(defineProps<Props>(), {
	size: 'default',
	type: 'adaptive',
	tabsListClass: '',
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

// 计算属性
const tabsListClasses = computed(() => {
	const baseClass = 'tabs-list';
	const sizeClass = `tabs-list--${props.size}`;
	const typeClass = `tabs-list--${props.type}`;

	return [baseClass, sizeClass, typeClass, props.tabsListClass].filter(Boolean).join(' ');
});

// 方法
const handleTabClick = (value: string, index: number) => {
	if (value !== props.modelValue) {
		// 触发事件
		emit('update:modelValue', value);
		emit('tab-click', value);
		emit('tab-change', value);
	}
};
</script>

<style lang="scss">
// 引用与PrototypeTabs相同的样式
@use './index.scss';

// 针对TabButtonGroup的特殊样式调整
.tab-button-group.tabs-list {
	// 隐藏文字标签，只显示图标
	.tab-label {
		display: none;
	}

	// 调整按钮样式以适应只显示图标的情况
	.tab-trigger {
		min-width: auto;
		padding: 8px 12px;

		// 确保图标居中显示
		display: flex;
		align-items: center;
		justify-content: center;

		// 图标样式调整
		.tab-icon {
			margin: 0;
			font-size: 16px;
		}

		// Badge 位置调整
		.tab-badge {
			position: absolute;
			top: -4px;
			right: -4px;
		}
	}
}
</style>
