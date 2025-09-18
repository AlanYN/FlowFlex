<template>
	<el-drawer v-model="drawer" :title="drawerTitle" @before-close="closeDrawer">
		<div class="flex gap-4">
			<div
				v-for="item in colorArr"
				:key="item.name"
				:style="`background:${item.color}`"
				class="w-[50px] h-[50px] rounded-xl"
				@click="setPrimary(item.name)"
			></div>
		</div>
	</el-drawer>
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue';
import { setPrimary } from '@/utils/theme';

interface Props {
	value: boolean;
	drawerTitle?: string;
}

const props = withDefaults(defineProps<Props>(), {
	drawerTitle: '',
});

const emit = defineEmits(['closeDrawer']);

const drawer = ref(false);
watch(
	() => props.value,
	(newVal) => {
		drawer.value = newVal;
	}
);

const colorArr = [
	{
		color: '#7E22CE',
		name: 'pruple',
	},
	{
		color: '#409EFF',
		name: 'blue',
	},
];

// const cancelClick = () => {
// 	drawer.value = false;
// 	closeDrawer();
// };

// const confirmClick = () => {
// 	console.log('提交');
// 	closeDrawer();
// };

const closeDrawer = () => {
	emit('closeDrawer');
};
</script>
