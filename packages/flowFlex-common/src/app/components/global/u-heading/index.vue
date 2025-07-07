<template>
	<div class="m-0">
		<div
			:class="[
				'font-medium',
				'leading-7',
				'tracking-tight',
				'dark:text-white',
				'font-helvetica',
				'text-[28px]',
				props.size,
			]"
		>
			{{ props.titlename }}
			<slot></slot>
		</div>
	</div>
</template>
<script lang="ts" setup>
const props = defineProps<{
	titlename: string;
	size?: string;
}>();
</script>
