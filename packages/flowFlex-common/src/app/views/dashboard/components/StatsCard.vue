<template>
	<div class="stats-card">
		<span class="stats-card__title">{{ title }}</span>
		<div class="stats-card__bottom">
			<div class="stats-card__value">
				<CountTo
					v-if="typeof value === 'number'"
					:end-val="value"
					:duration="1000"
					:suffix="valueSuffix"
				/>
				<span v-else>{{ value }}</span>
			</div>
			<div v-if="trend !== undefined" class="stats-card__trend" :class="trendClass">
				<span>{{ trendText }}</span>
				<el-icon class="stats-card__trend-icon" v-if="trendText != '0'">
					<component :is="trendIcon" />
				</el-icon>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { TopRight, BottomLeft } from '@element-plus/icons-vue';
import CountTo from '@/components/CountTo/CountTo.vue';

interface Props {
	title: string;
	value: number | string;
	trend?: number;
	trendSuffix?: string;
	valueSuffix?: string;
}

const props = withDefaults(defineProps<Props>(), {
	trend: undefined,
	trendSuffix: '',
	valueSuffix: '',
});

/**
 * 获取趋势指示器的 CSS 类名
 */
const trendClass = computed(() => {
	if (props.trend === undefined) return '';
	if (props.trend > 0) return 'stats-card__trend--positive';
	if (props.trend < 0) return 'stats-card__trend--negative';
	return 'stats-card__trend--neutral';
});

/**
 * 获取趋势图标
 */
const trendIcon = computed(() => {
	if (props.trend === undefined || props.trend === 0) return null;
	return props.trend > 0 ? TopRight : BottomLeft;
});

/**
 * 获取趋势文本
 */
const trendText = computed(() => {
	if (props.trend === undefined) return '';
	const prefix = props.trend > 0 ? '+' : '';
	const suffix = props.trendSuffix ? ` ${props.trendSuffix}` : '';
	return `${prefix}${props.trend}${suffix}`;
});
</script>

<style scoped lang="scss">
.stats-card {
	@apply rounded-xl p-5 flex flex-col justify-between min-h-[120px] transition-all duration-300;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: var(--el-box-shadow-light);

	&:hover {
		box-shadow: var(--el-box-shadow);
		transform: translateY(-2px);
	}

	&__title {
		@apply text-base font-medium;
		color: var(--el-text-color-secondary);
	}

	&__bottom {
		@apply flex justify-between items-end;
	}

	&__value {
		@apply text-4xl font-bold;
		color: var(--el-text-color-primary);
	}

	&__trend {
		@apply flex items-center gap-1.5 text-sm font-medium px-1 rounded-md border;

		&--positive {
			color: var(--el-color-success);
			background: transparent;
		}

		&--negative {
			color: var(--el-color-danger);
			background: transparent;
		}

		&--neutral {
			color: var(--el-text-color-secondary);
			background: transparent;
		}
	}

	&__trend-icon {
		@apply text-sm;
	}
}

/* 响应式调整 */
@media (max-width: 768px) {
	.stats-card {
		@apply p-4 min-h-[100px];

		&__value {
			@apply text-2xl;
		}

		&__trend {
			@apply px-2 py-1 text-xs;
		}
	}
}
</style>
