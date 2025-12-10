<template>
	<div>
		<!-- 页面标题 -->
		<PageHeader
			title="Integration Settings"
			description="Connect external systems and configure data exchange with WFE workflows"
		/>

		<!-- 卡片网格布局 -->
		<div class="integration-grid">
			<!-- 加载骨架屏 -->
			<template v-if="isLoading">
				<!-- Add New 卡片骨架 -->
				<div
					class="border-2 border-dashed rounded-xl p-6 flex flex-col"
					v-permission="ProjectPermissionEnum.integration.create"
				>
					<div class="flex-1 flex flex-col items-center justify-center text-center">
						<el-skeleton animated class="w-full">
							<template #template>
								<div class="flex flex-col items-center">
									<el-skeleton-item variant="circle" class="w-16 h-16 mb-4" />
									<el-skeleton-item variant="h3" class="w-48 mb-2" />
									<el-skeleton-item variant="text" class="w-56 mb-4" />
								</div>
							</template>
						</el-skeleton>
					</div>
					<el-skeleton animated class="w-full">
						<template #template>
							<el-skeleton-item variant="text" class="w-full h-10 mb-3" />
							<el-skeleton-item variant="button" class="w-full h-10" />
						</template>
					</el-skeleton>
				</div>

				<!-- 集成卡片骨架（显示3个） -->
				<div
					v-for="i in 15"
					:key="`skeleton-${i}`"
					class="bg-bg-overlay border rounded-xl p-6 flex flex-col"
				>
					<el-skeleton animated class="w-full">
						<template #template>
							<!-- 头部 -->
							<div class="flex items-start justify-between gap-4 mb-4">
								<div class="flex items-center gap-3 flex-1">
									<el-skeleton-item variant="circle" class="w-10 h-10" />
									<div class="flex-1">
										<el-skeleton-item variant="h3" class="w-32 mb-2" />
										<el-skeleton-item variant="text" class="w-24" />
									</div>
								</div>
								<el-skeleton-item variant="circle" class="w-6 h-6" />
							</div>

							<!-- 内容 -->
							<div class="space-y-2 flex-1">
								<el-skeleton-item variant="text" class="w-full" />
								<el-skeleton-item variant="text" class="w-3/4" />
								<el-skeleton-item variant="text" class="w-2/3" />
							</div>

							<!-- 底部 -->
							<div class="flex justify-between items-center pt-4 mt-4 border-t">
								<el-skeleton-item variant="text" class="w-24" />
								<el-skeleton-item variant="button" class="w-16 h-6" />
							</div>
						</template>
					</el-skeleton>
				</div>
			</template>

			<!-- 实际内容 -->
			<template v-else>
				<!-- 添加新集成卡片（始终在第一位） -->
				<div
					class="bg-white integration-card border-2 border-dashed rounded-xl p-6 hover:border-primary hover:shadow-md transition-all cursor-pointer group flex flex-col items-center justify-center"
					@click="handleAddIntegration"
					v-permission="ProjectPermissionEnum.integration.create"
				>
					<div
						class="w-16 h-16 rounded-full flex items-center justify-center mb-4 group-hover:scale-110 transition-transform"
					>
						<el-icon :size="32" class="text-primary">
							<Plus />
						</el-icon>
					</div>
					<h3 class="text-lg font-semibold text-text-primary mb-2">
						Add New Integration
					</h3>
					<p class="text-sm text-text-secondary">Connect a new external system to WFE</p>
				</div>

				<!-- 已有集成卡片列表 -->
				<template v-for="integration in integrations" :key="integration.id">
					<integration-list-card :integration="integration" @refresh="loadIntegrations" />
				</template>
			</template>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Plus } from '@element-plus/icons-vue';
import IntegrationListCard from './components/integration-list-card.vue';
import { getIntegrations } from '@/apis/integration';
import type { IIntegrationConfig } from '#/integration';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

const router = useRouter();

// 状态管理
const integrations = ref<IIntegrationConfig[]>([]);
const isLoading = ref(false);

/**
 * 加载集成列表
 */
async function loadIntegrations() {
	isLoading.value = true;
	try {
		const response = await getIntegrations();
		integrations.value = response.data?.items || [];
	} finally {
		isLoading.value = false;
	}
}

/**
 * 添加新集成 - 直接跳转到详情页面创建
 */
function handleAddIntegration() {
	router.push({
		name: 'IntegrationDetail',
		params: { id: 'new' },
	});
}

// 初始化
onMounted(() => {
	loadIntegrations();
});
</script>

<style scoped lang="scss">
/* 集成卡片网格布局 */
.integration-grid {
	display: grid;
	gap: 24px;
	/* 使用auto-fill保持卡片合适宽度，避免过度拉伸 */
	grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
	width: 100%;

	/* 响应式断点调整 - 主要调整gap和minmax，避免使用固定列数 */
	@media (max-width: 480px) {
		/* 超小屏幕：1列，全宽 */
		grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
		gap: 16px;
		padding: 0 8px;
	}

	@media (min-width: 481px) and (max-width: 768px) {
		/* 小屏幕：自适应，但偏向1列 */
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 20px;
	}

	@media (min-width: 769px) and (max-width: 1024px) {
		/* 中等屏幕：自适应，偏向2列 */
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 20px;
	}

	@media (min-width: 1025px) and (max-width: 1400px) {
		/* 大屏幕：自适应，2-3列之间 */
		grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
		gap: 24px;
	}

	@media (min-width: 1401px) and (max-width: 1920px) {
		/* 更大屏幕：自适应，偏向3列 */
		grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
		gap: 28px;
	}

	@media (min-width: 1921px) and (max-width: 2560px) {
		/* 超宽屏：自适应，3-4列之间 */
		grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
		gap: 32px;
	}

	@media (min-width: 2561px) {
		/* 超大屏幕：自适应，4列以上 */
		grid-template-columns: repeat(auto-fill, minmax(420px, 1fr));
		gap: 32px;
	}

	/* 限制单个卡片的最大宽度，防止过度拉伸 */
	& > * {
		max-width: 600px;
		width: 100%;
	}
}
</style>
