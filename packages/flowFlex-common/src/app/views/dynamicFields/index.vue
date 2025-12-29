<template>
	<div>
		<PageHeader
			title="Dynamic Fields"
			description="Manage custom fields for your workflows and cases"
		>
			<template #actions>
				<el-button
					class="page-header-btn page-header-btn-secondary"
					@click="handleExport"
					:loading="loading"
					:disabled="loading"
				>
					<el-icon><Download /></el-icon>
					Export All
				</el-button>
				<el-button
					class="page-header-btn page-header-btn-primary"
					type="primary"
					@click="handleNewField"
					:disabled="loading"
					:loading="loading"
					:icon="Plus"
				>
					<span>New Case</span>
				</el-button>
			</template>
		</PageHeader>
	</div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import { Plus, Download } from '@element-plus/icons-vue';
import { dynamicFieldList } from '@/apis/global/dyanmicField';
import { DynamicList } from '#/dynamic';

const loading = ref(false);

const handleExport = () => {
	loading.value = true;
	setTimeout(() => {
		loading.value = false;
	}, 3000);
};

const handleNewField = () => {
	loading.value = true;
	setTimeout(() => {
		loading.value = false;
	}, 3000);
};

const dynamicFieldListData = ref<DynamicList[]>([]);
const dynamicList = async () => {
	try {
		loading.value = true;
		const res = await dynamicFieldList();
		if (res.code == '200') {
			dynamicFieldListData.value = res.data;
		}
	} finally {
		loading.value = false;
	}
};

onMounted(() => {
	dynamicList();
});
</script>
