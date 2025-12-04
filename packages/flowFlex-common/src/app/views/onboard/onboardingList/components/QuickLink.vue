<template>
	<div class="space-y-3">
		<div
			v-for="(link, index) in quickLinks"
			:key="link.id || index"
			class="quick-link-card wfe-global-block-bg"
			:class="{ 'opacity-50 cursor-not-allowed': disabled || !link.isActive }"
			@click="handleLinkClick(link)"
		>
			<!-- Left Icon -->
			<div class="quick-link-icon">
				<Icon :icon="getIconComponent(link.displayIcon)" />
			</div>

			<!-- Content -->
			<div class="quick-link-content">
				<div class="quick-link-title">{{ link.linkName }}</div>
				<div class="quick-link-description">
					{{ getDescription(link) }}
				</div>
			</div>

			<!-- Right Icon -->
			<div class="quick-link-arrow">
				<Icon icon="lucide:external-link" />
			</div>
		</div>

		<!-- Loading State -->
		<div v-if="isLoading" class="flex items-center justify-center py-4">
			<el-icon class="is-loading text-lg">
				<Loading />
			</el-icon>
		</div>

		<!-- Empty State -->
		<div
			v-if="!isLoading && quickLinks.length === 0"
			class="text-sm text-text-secondary text-center py-4"
		>
			No quick links configured
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Loading } from '@element-plus/icons-vue';
import { Icon } from '@iconify/vue';
import { IQuickLink } from '#/integration';
import { StageComponentData } from '#/onboard';
import { getQuickLink } from '@/apis/integration';
import {
	RedirectType,
	ValueSource,
	PageParameterDetail,
	LoginUserInfoDetail,
	SystemVariableDetail,
} from '@/enums/integration';
import { useUserStore } from '@/stores/modules/user';

interface Props {
	component: StageComponentData;
	onboardingId: string;
	stageId: string;
	disabled: boolean;
}

const props = defineProps<Props>();
const route = useRoute();
const userStore = useUserStore();

const isLoading = ref(false);
const quickLinks = ref<IQuickLink[]>([]);

/**
 * 获取描述信息
 */
function getDescription(link: IQuickLink): string {
	return (link as any).description || 'Open external link';
}

/**
 * 获取图标组件
 */
function getIconComponent(iconName?: string): string {
	const iconMap: Record<string, string> = {
		link: 'lucide:external-link',
		chain: 'lucide:link',
		arrow: 'lucide:arrow-right',
	};
	return iconMap[iconName?.toLowerCase() || 'link'] || 'lucide:external-link';
}

/**
 * 解析 URL 参数值
 */
function resolveParameterValue(
	valueSource: ValueSource | number | string,
	valueDetail: string
): string {
	// 处理 valueSource 类型转换
	let source: ValueSource;
	if (typeof valueSource === 'number') {
		source = valueSource as ValueSource;
	} else if (typeof valueSource === 'string') {
		const sourceMap: Record<string, ValueSource> = {
			PageParameter: ValueSource.PageParameter,
			LoginUserInfo: ValueSource.LoginUserInfo,
			FixedValue: ValueSource.FixedValue,
			SystemVariable: ValueSource.SystemVariable,
		};
		source = sourceMap[valueSource] || ValueSource.FixedValue;
	} else {
		source = valueSource as ValueSource;
	}

	switch (source) {
		case ValueSource.PageParameter:
			// 从路由参数或查询参数获取
			if (valueDetail === PageParameterDetail.CaseId) {
				return props.onboardingId || '';
			}
			if (valueDetail === PageParameterDetail.CustomerId) {
				// 从路由查询参数获取 customerId
				return (route.query.customerId as string) || '';
			}
			if (valueDetail === PageParameterDetail.OrderNumber) {
				// 从路由查询参数获取 orderNumber
				return (route.query.orderNumber as string) || '';
			}
			return '';

		case ValueSource.LoginUserInfo:
			// 从用户信息获取
			if (valueDetail === LoginUserInfoDetail.UserId) {
				return String(userStore.userInfo?.userId || '');
			}
			if (valueDetail === LoginUserInfoDetail.Username) {
				return userStore.userInfo?.userName || '';
			}
			if (valueDetail === LoginUserInfoDetail.Email) {
				return userStore.userInfo?.email || '';
			}
			return '';

		case ValueSource.SystemVariable:
			// 系统变量
			if (valueDetail === SystemVariableDetail.CurrentTimestamp) {
				return String(Date.now());
			}
			if (valueDetail === SystemVariableDetail.CurrentDate) {
				return new Date().toISOString().split('T')[0];
			}
			return '';

		case ValueSource.FixedValue:
		default:
			// 固定值，直接返回
			return valueDetail || '';
	}
}

/**
 * 构建最终 URL
 */
function buildUrl(link: IQuickLink): string {
	let url = link.targetUrl || '';
	if (!url) return '';

	// 如果有 URL 参数，需要替换参数值
	if (link.urlParameters && link.urlParameters.length > 0) {
		const urlObj = new URL(url);
		link.urlParameters.forEach((param) => {
			const paramValue = resolveParameterValue(param.valueSource, param.valueDetail);
			urlObj.searchParams.set(param.name, paramValue);
		});
		url = urlObj.toString();
	}

	return url;
}

/**
 * 处理链接点击
 */
async function handleLinkClick(link: IQuickLink) {
	if (props.disabled || !link.isActive) return;

	const url = buildUrl(link);
	if (!url) {
		ElMessage.warning('Target URL is empty or invalid');
		return;
	}

	// 如果是 PopupConfirmation 类型，需要弹窗确认
	if (link.redirectType === RedirectType.PopupConfirmation) {
		try {
			await ElMessageBox.confirm(
				`Are you sure you want to open this link?\n\n${link.linkName}`,
				'Confirm Redirect',
				{
					confirmButtonText: 'Open',
					cancelButtonText: 'Cancel',
					type: 'info',
				}
			);
			// 用户确认后，在新标签页打开
			window.open(url, '_blank');
		} catch {
			// 用户取消，不执行任何操作
		}
	} else {
		// Direct 类型，直接在新标签页打开
		window.open(url, '_blank');
	}
}

/**
 * 加载快速链接详情
 */
async function loadQuickLinks() {
	if (!props.component.quickLinkIds || props.component.quickLinkIds.length === 0) {
		quickLinks.value = [];
		return;
	}

	isLoading.value = true;
	try {
		const linkPromises = props.component.quickLinkIds.map((id) => getQuickLink(id));
		const responses = await Promise.all(linkPromises);

		quickLinks.value = responses
			.map((response) => {
				if (response.success && response.data) {
					return response.data;
				}
				return null;
			})
			.filter((link): link is IQuickLink => link !== null && !!link.isActive);
	} catch (error) {
		console.error('Failed to load quick links:', error);
		quickLinks.value = [];
	} finally {
		isLoading.value = false;
	}
}

// 监听组件变化
watch(
	() => props.component.quickLinkIds,
	() => {
		loadQuickLinks();
	},
	{ immediate: true, deep: true }
);

// 初始化加载
onMounted(() => {
	loadQuickLinks();
});
</script>

<style scoped lang="scss">
.quick-link-card {
	@apply flex items-center gap-4 p-4 cursor-pointer transition-all;
	transition: all 0.2s ease;
}

.quick-link-icon {
	@apply flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center;
	background-color: var(--el-color-primary);
	color: white;

	:deep(svg) {
		width: 20px;
		height: 20px;
	}
}

.quick-link-content {
	@apply flex-1 min-w-0;
}

.quick-link-title {
	@apply font-semibold text-sm mb-1;
	color: var(--el-text-color-primary);
}

.quick-link-description {
	@apply text-xs;
	color: var(--el-text-color-secondary);
}

.quick-link-arrow {
	@apply flex-shrink-0 text-text-secondary;
	:deep(svg) {
		width: 18px;
		height: 18px;
	}
}

.quick-link-card:hover:not(.opacity-50) .quick-link-arrow {
	color: var(--el-color-primary);
}

html.dark {
	.quick-link-card {
		background-color: var(--el-bg-color-overlay);
		border-color: var(--el-border-color);

		&:hover:not(.opacity-50) {
			background-color: var(--el-fill-color-dark);
		}
	}
}
</style>
