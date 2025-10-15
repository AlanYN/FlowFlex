<template>
	<div class="pb-6 overview-loading-bg">
		<!-- 顶部导航栏骨架 -->
		<div class="flex justify-between items-center mb-6">
			<div class="flex items-center">
				<el-skeleton-item variant="button" class="mr-2 w-16 h-8" />
				<el-skeleton-item variant="h1" class="w-80 h-8" />
			</div>
			<div class="flex items-center space-x-2">
				<el-skeleton-item variant="button" class="w-32 h-9" />
				<el-skeleton-item variant="button" class="w-28 h-9" />
			</div>
		</div>

		<!-- Customer Info Card 骨架 -->
		<el-card class="mb-6">
			<template #header>
				<el-skeleton-item variant="h3" class="w-48 h-6" />
			</template>
			<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
				<div v-for="i in 4" :key="i">
					<el-skeleton-item variant="text" class="w-32 h-4 mb-2" />
					<el-skeleton-item variant="text" class="w-40 h-5" />
				</div>
			</div>
		</el-card>

		<!-- Search and Filters 骨架 -->
		<el-card class="mb-6">
			<div class="pt-6">
				<div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
					<div v-for="i in 4" :key="i" class="space-y-2">
						<el-skeleton-item variant="text" class="w-40 h-4" />
						<el-skeleton-item variant="button" class="w-full h-8" />
					</div>
				</div>
				<div class="flex items-center justify-between">
					<el-skeleton-item variant="text" class="w-64 h-4" />
					<div class="flex items-center space-x-2">
						<el-skeleton-item variant="button" class="w-20 h-6" />
						<el-skeleton-item variant="button" class="w-24 h-6" />
					</div>
				</div>
			</div>
		</el-card>

		<!-- Questionnaire Cards 骨架 -->
		<div class="space-y-6">
			<el-card v-for="i in 3" :key="i">
				<template #header>
					<div class="flex justify-between items-center bg-blue-50 -m-4 p-4">
						<div class="flex-1">
							<el-skeleton-item variant="h3" class="w-64 h-6 mb-2" />
							<el-skeleton-item variant="text" class="w-32 h-4 mb-1" />
							<el-skeleton-item variant="text" class="w-80 h-4" />
						</div>
						<div class="flex flex-col items-end space-y-1">
							<el-skeleton-item variant="button" class="w-24 h-6" />
						</div>
					</div>
				</template>

				<!-- 表格骨架 -->
				<div class="w-full">
					<div class="flex overview-table-header pb-2 mb-4">
						<div class="w-32 mr-4">
							<el-skeleton-item variant="text" class="w-20 h-4" />
						</div>
						<div class="flex-1 mr-4">
							<el-skeleton-item variant="text" class="w-16 h-4" />
						</div>
						<div class="flex-1 mr-4">
							<el-skeleton-item variant="text" class="w-12 h-4" />
						</div>
						<div class="w-60">
							<el-skeleton-item variant="text" class="w-24 h-4" />
						</div>
					</div>

					<!-- 表格行骨架 -->
					<div v-for="j in 4" :key="j" class="flex items-start py-3 overview-table-row">
						<div class="w-32 mr-4">
							<el-skeleton-item variant="button" class="w-20 h-6" />
						</div>
						<div class="flex-1 mr-4">
							<el-skeleton-item variant="text" class="w-full h-4 mb-1" />
							<el-skeleton-item variant="text" class="w-3/4 h-3" />
						</div>
						<div class="flex-1 mr-4">
							<div class="bg-blue-50 p-2 rounded">
								<el-skeleton-item variant="text" class="w-full h-4 mb-1" />
								<el-skeleton-item variant="text" class="w-2/3 h-4" />
							</div>
						</div>
						<div class="w-60">
							<div class="space-y-1">
								<el-skeleton-item variant="text" class="w-32 h-3" />
								<el-skeleton-item variant="text" class="w-28 h-3" />
								<el-skeleton-item variant="text" class="w-36 h-3" />
							</div>
						</div>
					</div>
				</div>
			</el-card>
		</div>

		<!-- Summary Statistics 骨架 -->
		<el-card class="mt-6">
			<template #header>
				<el-skeleton-item variant="h3" class="w-40 h-6" />
			</template>
			<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
				<div v-for="i in 4" :key="i" class="text-center">
					<el-skeleton-item variant="text" class="w-12 h-8 mx-auto mb-2" />
					<el-skeleton-item variant="text" class="w-24 h-4 mx-auto" />
				</div>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts">
// 这是一个纯展示组件，不需要任何逻辑
</script>

<style scoped lang="scss">
.overview-loading-bg {
	background-color: var(--el-bg-color-page);
}

.overview-table-header {
	border-bottom: 1px solid var(--el-border-color-light);
}

.overview-table-row {
	border-bottom: 1px solid var(--el-border-color-lighter);
}
</style>
