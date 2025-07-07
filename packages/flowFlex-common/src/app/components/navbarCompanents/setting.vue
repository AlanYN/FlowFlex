<template>
	<div>
		<div>
			<el-icon><Setting @click="handleOpenSetting" /></el-icon>
		</div>
	</div>
</template>

<script setup lang="ts">
import { Setting } from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';

const router = useRouter();

const handleOpenSetting = () => {
	router.push({
		path: `/setting/module/preferences/general`,
	});
};
</script>
