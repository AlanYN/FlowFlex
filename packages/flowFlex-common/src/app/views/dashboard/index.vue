<template>
	<div>
		<PageHeader title="Dashboard" description="Dashboard">
			<template #actions>
				<el-button
					type="primary"
					size="default"
					class="page-header-btn page-header-btn-primary"
				>
					View All Cases
				</el-button>
			</template>
		</PageHeader>
	</div>
</template>

<script setup lang="ts">
import PageHeader from '@/components/global/PageHeader/index.vue';
</script>
