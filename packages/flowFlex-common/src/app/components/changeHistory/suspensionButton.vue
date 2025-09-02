<template>
	<div>
		<div class="flex items-center" @click="toggleTable">
			<el-icon class="mr-2"><historySvg /></el-icon>
			Change History
		</div>
	</div>
</template>

<script setup lang="ts">
import { inject } from 'vue';
import historySvg from '@assets/svg/global/history.svg';
import { WFEMoudels } from '@/enums/appEnum';

interface Props {
	id: string;
	type: WFEMoudels;
}

const props = defineProps<Props>();
const History = inject('History') as (id: string, type: WFEMoudels) => void | undefined;

const toggleTable = () => {
	History && History(props.id, props.type);
};
</script>

<style lang="scss" scoped></style>
