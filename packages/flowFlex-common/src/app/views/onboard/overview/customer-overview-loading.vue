<template>
	<div class="pb-6 bg-gray-50 dark:bg-black-400">
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
					<div class="flex border-b border-gray-200 pb-2 mb-4">
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
					<div
						v-for="j in 4"
						:key="j"
						class="flex items-start py-3 border-b border-gray-100"
					>
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
.space-y-2 > * + * {
	margin-top: 0.5rem;
}

.space-y-6 > * + * {
	margin-top: 1.5rem;
}

.space-x-2 > * + * {
	margin-left: 0.5rem;
}

.grid {
	display: grid;
}

.grid-cols-1 {
	grid-template-columns: repeat(1, minmax(0, 1fr));
}

@media (min-width: 768px) {
	.md\:grid-cols-4 {
		grid-template-columns: repeat(4, minmax(0, 1fr));
	}
}

.gap-4 {
	gap: 1rem;
}

.bg-blue-50 {
	background-color: #eff6ff;
}

.border-b {
	border-bottom-width: 1px;
}

.border-gray-200 {
	border-color: #e5e7eb;
}

.border-gray-100 {
	border-color: #f3f4f6;
}

.rounded {
	border-radius: 0.25rem;
}

.p-2 {
	padding: 0.5rem;
}

.p-4 {
	padding: 1rem;
}

.py-3 {
	padding-top: 0.75rem;
	padding-bottom: 0.75rem;
}

.pb-2 {
	padding-bottom: 0.5rem;
}

.mb-1 {
	margin-bottom: 0.25rem;
}

.mb-2 {
	margin-bottom: 0.5rem;
}

.mb-4 {
	margin-bottom: 1rem;
}

.mr-4 {
	margin-right: 1rem;
}

.flex-1 {
	flex: 1 1 0%;
}

.w-12 {
	width: 3rem;
}

.w-16 {
	width: 4rem;
}

.w-20 {
	width: 5rem;
}

.w-24 {
	width: 6rem;
}

.w-28 {
	width: 7rem;
}

.w-32 {
	width: 8rem;
}

.w-36 {
	width: 9rem;
}

.w-40 {
	width: 10rem;
}

.w-48 {
	width: 12rem;
}

.w-60 {
	width: 15rem;
}

.w-64 {
	width: 16rem;
}

.w-80 {
	width: 20rem;
}

.w-full {
	width: 100%;
}

.w-2\/3 {
	width: 66.666667%;
}

.w-3\/4 {
	width: 75%;
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

.text-center {
	text-align: center;
}

.mx-auto {
	margin-left: auto;
	margin-right: auto;
}

.items-start {
	align-items: flex-start;
}

.items-center {
	align-items: center;
}

.items-end {
	align-items: flex-end;
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
</style>
