<template>
	<div class="flex h-screen bg-gray-50">
		<!-- 左侧边栏骨架 -->
		<div class="w-64 h-screen">
			<div class="p-5">
				<el-skeleton-item variant="h1" class="w-32 h-10 mb-4" />
			</div>
			<div class="border-gray-200 bg-white rounded-lg mx-4">
				<div class="p-4 bg-blue-50 rounded-t-lg">
					<el-skeleton-item variant="h3" class="w-16 h-6" />
				</div>
				<div class="p-4 space-y-2">
					<div v-for="i in 7" :key="i">
						<el-skeleton-item variant="button" class="w-full h-8" />
					</div>
				</div>
			</div>
		</div>

		<!-- 主内容区骨架 -->
		<div class="flex-1 flex flex-col border-gray-200 rounded-lg">
			<!-- 头部骨架 -->
			<div class="p-4">
				<div class="flex items-center justify-between mb-6">
					<el-skeleton-item variant="h1" class="w-32 h-6" style="visibility: hidden" />
					<el-skeleton-item variant="button" class="w-32 h-9" />
				</div>
				<div class="bg-blue-50 rounded-lg p-4">
					<div class="flex items-center justify-between mb-3">
						<el-skeleton-item variant="h3" class="w-24 h-6" />
						<div class="relative w-64">
							<el-skeleton-item variant="button" class="w-full h-9" />
						</div>
					</div>
					<el-skeleton-item variant="text" class="w-96 h-4" />
				</div>
			</div>

			<!-- 检查清单内容骨架 -->
			<div class="flex-1 p-4 bg-gray-50">
				<div class="space-y-4">
					<div
						v-for="i in 4"
						:key="i"
						class="shadow-sm border-gray-200 rounded-lg bg-white"
					>
						<!-- 检查清单头部骨架 -->
						<div class="p-4">
							<div class="flex items-center justify-between">
								<div class="flex-1">
									<div class="flex items-center justify-between mb-2">
										<el-skeleton-item variant="h3" class="w-48 h-5" />
										<div class="flex items-center gap-2">
											<el-skeleton-item variant="button" class="w-16 h-6" />
											<el-skeleton-item variant="button" class="w-12 h-6" />
											<el-skeleton-item variant="button" class="w-6 h-6" />
										</div>
									</div>
									<el-skeleton-item variant="text" class="w-80 h-4 mb-1" />
									<div class="flex items-center gap-1">
										<el-skeleton-item variant="text" class="w-32 h-3" />
										<span class="text-gray-400">•</span>
										<el-skeleton-item variant="text" class="w-24 h-3" />
									</div>
								</div>
							</div>
						</div>

						<!-- 任务部分骨架 (展开状态) -->
						<div v-if="i <= 2" class="p-4 bg-white border-t border-gray-100 rounded-lg">
							<!-- 任务头部 -->
							<div class="flex items-center justify-between mb-4">
								<el-skeleton-item variant="text" class="w-12 h-4" />
								<el-skeleton-item variant="button" class="w-8 h-8" />
							</div>

							<!-- 任务列表骨架 -->
							<div class="space-y-3">
								<div
									v-for="j in 3"
									:key="j"
									class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
								>
									<div class="flex items-center gap-3 flex-1">
										<el-skeleton-item variant="button" class="w-4 h-4" />
										<div class="flex-1">
											<el-skeleton-item
												variant="text"
												class="w-64 h-4 mb-1"
											/>
											<el-skeleton-item variant="text" class="w-40 h-3" />
										</div>
									</div>
									<div class="flex items-center gap-2">
										<el-skeleton-item variant="button" class="w-16 h-6" />
										<el-skeleton-item variant="button" class="w-6 h-6" />
									</div>
								</div>
							</div>

							<!-- 添加新任务骨架 -->
							<div class="mt-4 p-3 border-2 border-dashed border-gray-300 rounded-lg">
								<div class="flex items-center gap-2">
									<el-skeleton-item variant="button" class="w-4 h-4" />
									<el-skeleton-item variant="text" class="w-32 h-4" />
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
// 这是一个纯展示组件，不需要任何逻辑
</script>

<style scoped lang="scss">
.space-y-2 > * + * {
	margin-top: 0.5rem;
}

.space-y-3 > * + * {
	margin-top: 0.75rem;
}

.space-y-4 > * + * {
	margin-top: 1rem;
}

.gap-1 {
	gap: 0.25rem;
}

.gap-2 {
	gap: 0.5rem;
}

.gap-3 {
	gap: 0.75rem;
}

.bg-blue-50 {
	background-color: #eff6ff;
}

.bg-gray-50 {
	background-color: #f9fafb;
}

.border-gray-200 {
	border-color: #e5e7eb;
}

.border-gray-100 {
	border-color: #f3f4f6;
}

.border-gray-300 {
	border-color: #d1d5db;
}

.border-dashed {
	border-style: dashed;
}

.border-t {
	border-top-width: 1px;
}

.border-2 {
	border-width: 2px;
}

.rounded-lg {
	border-radius: 0.5rem;
}

.rounded-t-lg {
	border-top-left-radius: 0.5rem;
	border-top-right-radius: 0.5rem;
}

.shadow-sm {
	box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
}

.p-3 {
	padding: 0.75rem;
}

.p-4 {
	padding: 1rem;
}

.p-5 {
	padding: 1.25rem;
}

.mb-1 {
	margin-bottom: 0.25rem;
}

.mb-2 {
	margin-bottom: 0.5rem;
}

.mb-3 {
	margin-bottom: 0.75rem;
}

.mb-4 {
	margin-bottom: 1rem;
}

.mt-4 {
	margin-top: 1rem;
}

.mx-4 {
	margin-left: 1rem;
	margin-right: 1rem;
}

.flex-1 {
	flex: 1 1 0%;
}

.w-6 {
	width: 1.5rem;
}

.w-8 {
	width: 2rem;
}

.w-12 {
	width: 3rem;
}

.w-16 {
	width: 4rem;
}

.w-24 {
	width: 6rem;
}

.w-32 {
	width: 8rem;
}

.w-40 {
	width: 10rem;
}

.w-48 {
	width: 12rem;
}

.w-64 {
	width: 16rem;
}

.w-80 {
	width: 20rem;
}

.w-96 {
	width: 24rem;
}

.w-full {
	width: 100%;
}

.w-64 {
	width: 16rem;
}

.h-3 {
	height: 0.75rem;
}

.h-4 {
	height: 1rem;
}

.h-5 {
	height: 1.25rem;
}

.h-6 {
	height: 1.5rem;
}

.h-8 {
	height: 2rem;
}

.h-9 {
	height: 2.25rem;
}

.h-10 {
	height: 2.5rem;
}

.h-screen {
	height: 100vh;
}

.items-center {
	align-items: center;
}

.justify-between {
	justify-content: space-between;
}

.flex {
	display: flex;
}

.flex-col {
	flex-direction: column;
}

.relative {
	position: relative;
}

.text-gray-400 {
	color: #9ca3af;
}

/* 深色模式支持 */
html.dark {
	.bg-gray-50 {
		@apply bg-black-400 !important;
	}

	.bg-white {
		@apply bg-black-300 !important;
	}

	.border-gray-200,
	.border-gray-100 {
		@apply border-gray-600 !important;
	}

	.border-gray-300 {
		@apply border-gray-500 !important;
	}

	.text-gray-400 {
		@apply text-gray-500 !important;
	}

	.bg-blue-50 {
		@apply bg-blue-900/20 !important;
	}
}
</style>
