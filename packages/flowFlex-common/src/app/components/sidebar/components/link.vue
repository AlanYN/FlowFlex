<template>
	<div>
		<component :is="type" v-bind="linkProps()">
			<slot></slot>
		</component>
	</div>
</template>

<script setup lang="ts">
import { isExternal } from '@/utils/utils';
import { computed } from 'vue';

const props = defineProps({
	to: {
		type: [String, Object],
		required: true,
	},
});

const isExt = computed(() => {
	return isExternal(props.to);
});

const type = computed(() => {
	if (isExt.value) {
		return 'a';
	}
	return 'router-link';
});

function linkProps() {
	if (isExt.value) {
		return {
			href: props.to,
			target: '_blank',
			rel: 'noopener',
		};
	}
	return {
		to: props.to,
	};
}
</script>
