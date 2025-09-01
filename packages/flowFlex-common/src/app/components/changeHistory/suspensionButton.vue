<template>
	<div v-if="hasCustomerId">
		<div class="fixed right-8 bottom-8 history-button">
			<el-tooltip content="Current page change history">
				<el-button :icon="historySvg" size="large" circle @click="toggleTable" />
			</el-tooltip>
		</div>
		<historyTable :showDialog="showTable" @visible:hide-dialog="closeVisible" />
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import historySvg from '@assets/svg/global/history.svg';
import historyTable from './historyTable.vue';
import { useRoute } from 'vue-router';

const route = useRoute();

defineProps({
	msg: String,
});

const hasCustomerId = computed(() => {
	// console.log('route.query.customerId:', route.query.customerId);
	if (route.meta.hiddenChangeLog === true) return false;
	return !!route.query.customerId || !!route.query.id;
});

const showTable = ref(false);
const toggleTable = () => {
	showTable.value = !showTable.value;
};

const closeVisible = () => {
	showTable.value = false;
};

onMounted(() => {
	showTable.value = false;
});
</script>

<style lang="scss" scoped>
.history-button {
	z-index: 999;

	:deep(.el-button .el-icon) {
		width: 20px !important;
		height: 20px !important;

		svg {
			width: 20px !important;
			height: 20px !important;
		}
	}
}
</style>
