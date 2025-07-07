<template>
	<div class="flex w-full items-center justify-center my-[200px]">
		<div class="leading-6 text-left">
			<h1 class="page_title_blod text-xl font-bold mb-4">
				{{ title }}
			</h1>
			<div v-for="(item, index) in description" :key="index" class="flex items-start mb-2">
				<span class="text-primary-500 mr-2">➔</span>
				<p class="m-0">{{ item }}</p>
			</div>
		</div>
		<div class="justify-self-end justify-end h-full">
			<component :is="icon" class="h-[100px]" />
		</div>
	</div>
</template>

<script setup lang="ts">
defineProps<{
	title: string;
	description: string[];
	icon: any;
}>();
</script>
