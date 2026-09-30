<template>
	<div
		:style="{ width: '100%', height }"
		class="flex flex-col items-center justify-center gap-4 text-[#909399]"
	>
		<el-icon :size="48"><Document /></el-icon>
		<p class="text-base font-medium">Presentation editor coming soon</p>
		<p class="text-sm">Slide editing is not yet supported in this version.</p>
	</div>
</template>

<script setup lang="ts">
import { ElIcon } from 'element-plus';
import { Document } from '@element-plus/icons-vue';

interface Props {
	slideData?: object;
	docId?: string;
	height?: string;
}

withDefaults(defineProps<Props>(), {
	height: '100%',
});

defineExpose({
	getInstance: () => null,
});
</script>
