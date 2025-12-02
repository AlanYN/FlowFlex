<template>
	<el-card class="integration-card overflow-hidden transition-all cursor-pointer">
		<!-- 卡片头部 -->
		<template #header>
			<div class="card-header">
				<div class="flex items-center justify-between w-full">
					<div class="flex items-center space-x-3 flex-1 min-w-0" @click="handleClick">
						<div
							class="card-icon rounded-lg flex-shrink-0 flex items-center justify-center"
						>
							<Icon icon="lucide:settings-2" />
						</div>
						<h3 class="card-title tracking-tight truncate" :title="integration.name">
							{{ integration.name }}
						</h3>
						<el-tag :type="integration.status === 1 ? 'success' : 'info'">
							{{ integration.status === 1 ? 'Connected' : 'Disconnected' }}
						</el-tag>
					</div>
					<el-dropdown
						trigger="click"
						@command="handleCommand"
						class="flex-shrink-0"
						@click.stop
					>
						<el-button text class="card-more-btn" link>
							<el-icon class="h-4 w-4"><MoreFilled /></el-icon>
						</el-button>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item command="edit">
									<el-icon class="mr-2"><Edit /></el-icon>
									Edit
								</el-dropdown-item>
								<el-divider class="my-0" />
								<el-dropdown-item
									command="delete"
									class="text-red-500 hover:!bg-red-500 hover:!text-white"
								>
									<el-icon class="mr-2"><Delete /></el-icon>
									Delete
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</div>
				<p v-if="integration.description" class="text-sm mt-2 truncate">
					{{ truncatedDescription }}
				</p>
				<p v-else class="text-sm mt-2 text-text-secondary">No description</p>
			</div>
		</template>

		<!-- 卡片内容 -->
		<div class="flex flex-col space-y-3 mt-[-10px]">
			<!-- 折线图 -->
			<div v-if="hasChartData" class="chart-container">
				<div ref="chartRef" class="chart-wrapper"></div>
			</div>

			<div class="flex items-center gap-2 text-sm font-medium">Entity Types:</div>
			<div class="entity-types-container">
				<div class="flex flex-wrap gap-x-2">
					<template v-for="value in visibleEntityTypes" :key="value">
						<el-tag type="info">
							{{ value }}
						</el-tag>
					</template>
					<el-tag v-if="remainingCount > 0" type="info" class="remaining-tag">
						+{{ remainingCount }}
					</el-tag>
					<span
						v-if="
							!integration.configuredEntityTypeNames ||
							integration.configuredEntityTypeNames.length === 0
						"
						class="text-sm text-text-secondary"
					>
						No entity types configured
					</span>
				</div>
			</div>
		</div>
	</el-card>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, onMounted, onBeforeUnmount, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Delete, Edit, MoreFilled } from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';
import { deleteIntegration } from '@/apis/integration';
import type { IIntegrationConfig } from '#/integration';
import * as echarts from 'echarts';

interface Props {
	integration: IIntegrationConfig;
}

interface Emits {
	(e: 'refresh'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();
const router = useRouter();

// Entity Types 显示限制（最多显示的数量）
const MAX_VISIBLE_ENTITY_TYPES = 3;

// Description 显示限制（正常状态下最多显示的字符数）
const MAX_DESCRIPTION_LENGTH = 80;

// 图表相关
const chartRef = ref<HTMLElement>();
let chartInstance: echarts.ECharts | null = null;

/**
 * 计算可见的 Entity Types
 */
const visibleEntityTypes = computed(() => {
	const types = props.integration.configuredEntityTypeNames || [];
	return types.slice(0, MAX_VISIBLE_ENTITY_TYPES);
});

/**
 * 计算剩余的 Entity Types 数量
 */
const remainingCount = computed(() => {
	const types = props.integration.configuredEntityTypeNames || [];
	return Math.max(0, types.length - MAX_VISIBLE_ENTITY_TYPES);
});

/**
 * 截断的 Description（正常状态显示）
 */
const truncatedDescription = computed(() => {
	const desc = props.integration.description || '';
	if (desc.length <= MAX_DESCRIPTION_LENGTH) {
		return desc;
	}
	return desc.substring(0, MAX_DESCRIPTION_LENGTH) + '...';
});

/**
 * 是否有图表数据
 */
const hasChartData = computed(() => {
	const data = props.integration.lastDaysSeconds;
	return data && Object.keys(data).length > 0;
});

/**
 * 处理图表数据
 */
const chartData = computed(() => {
	const data = props.integration.lastDaysSeconds || {};
	const dates: string[] = [];
	const values: number[] = [];

	// 按日期排序
	const sortedEntries = Object.entries(data).sort(([dateA], [dateB]) => {
		return new Date(dateA).getTime() - new Date(dateB).getTime();
	});

	sortedEntries.forEach(([date, seconds]) => {
		dates.push(date);
		// 将秒数转换为数字，如果转换失败则使用 0
		const secondsNum = parseFloat(seconds) || 0;
		values.push(secondsNum);
	});

	return {
		dates,
		values,
	};
});

/**
 * 点击卡片
 */
function handleClick() {
	router.push({
		name: 'IntegrationDetail',
		params: { id: props.integration.id },
	});
}

/**
 * 处理下拉菜单命令
 */
function handleCommand(command: string) {
	if (command === 'edit') {
		handleClick();
	} else if (command === 'delete') {
		handleDelete();
	}
}

/**
 * 初始化图表
 */
const initChart = () => {
	if (!chartRef.value || !hasChartData.value) return;

	// 如果图表已存在，先销毁
	if (chartInstance) {
		chartInstance.dispose();
		chartInstance = null;
	}

	// 创建图表实例
	chartInstance = echarts.init(chartRef.value);

	// 准备数据
	const { dates, values } = chartData.value;

	// 配置选项
	const option: echarts.EChartsOption = {
		grid: {
			left: '2%',
			right: '2%',
			bottom: '0%',
			top: '0%',
			containLabel: false,
		},
		xAxis: {
			type: 'category',
			data: dates,
			boundaryGap: false,
			axisLine: {
				show: false,
			},
			axisTick: {
				show: false,
			},
			axisLabel: {
				show: false,
			},
		},
		yAxis: {
			type: 'value',
			show: false,
		},
		series: [
			{
				name: 'Seconds',
				type: 'line',
				data: values,
				smooth: true,
				symbol: 'none',
				lineStyle: {
					color: '#10b981', // teal color
					width: 1.5,
				},
				areaStyle: {
					color: {
						type: 'linear',
						x: 0,
						y: 0,
						x2: 0,
						y2: 1,
						colorStops: [
							{
								offset: 0,
								color: 'rgba(16, 185, 129, 0.3)', // teal with opacity
							},
							{
								offset: 1,
								color: 'rgba(16, 185, 129, 0)', // transparent
							},
						],
					},
				},
			},
		],
		tooltip: {
			trigger: 'axis',
			backgroundColor: 'rgba(0, 0, 0, 0.8)',
			borderColor: 'transparent',
			textStyle: {
				color: '#fff',
				fontSize: 12,
			},
			formatter: (params: any) => {
				const param = params[0];
				const date = param.axisValue;
				const value = param.value;
				return `${date}<br/>${value}`;
			},
		},
	};

	// 设置配置项
	chartInstance.setOption(option);

	// 监听窗口大小变化
	window.addEventListener('resize', handleResize);
};

/**
 * 处理窗口大小变化
 */
const handleResize = () => {
	if (chartInstance) {
		chartInstance.resize();
	}
};

/**
 * 销毁图表
 */
const destroyChart = () => {
	if (chartInstance) {
		chartInstance.dispose();
		chartInstance = null;
	}
	window.removeEventListener('resize', handleResize);
};

// 监听数据变化，重新渲染图表
watch(
	() => props.integration.lastDaysSeconds,
	() => {
		nextTick(() => {
			initChart();
		});
	},
	{ deep: true }
);

// 组件挂载时初始化图表
onMounted(() => {
	nextTick(() => {
		initChart();
	});
});

// 组件卸载时销毁图表
onBeforeUnmount(() => {
	destroyChart();
});

/**
 * 删除集成
 */
function handleDelete() {
	ElMessageBox({
		title: 'Confirm Deletion',
		message: `Are you sure you want to delete the integration "${props.integration.name}"? This action cannot be undone.`,
		showCancelButton: true,
		confirmButtonText: 'Delete',
		cancelButtonText: 'Cancel',
		type: 'warning',
		confirmButtonClass: 'el-button--danger',
		distinguishCancelAndClose: true,
		beforeClose: async (action, instance, done) => {
			if (action === 'confirm') {
				instance.confirmButtonLoading = true;
				instance.confirmButtonText = 'Deleting...';
				try {
					const res = await deleteIntegration(props.integration.id as string | number);
					instance.confirmButtonText = 'Delete';
					instance.confirmButtonLoading = false;
					if (res.success) {
						ElMessage.success('Integration deleted successfully');
						nextTick(() => {
							emit('refresh');
							done();
						});
					} else {
						ElMessage.error(res.msg || 'Failed to delete integration');
						done();
					}
				} finally {
					instance.confirmButtonText = 'Delete';
					instance.confirmButtonLoading = false;
					done();
				}
			} else {
				done();
			}
		},
	});
}
</script>

<style scoped lang="scss">
.chart-container {
	width: 100%;
	margin-top: 4px;
	margin-bottom: 2px;
}

.chart-wrapper {
	width: 100%;
	height: 40px;
}
</style>
