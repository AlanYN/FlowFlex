<template>
	<div class="flex justify-between w-full">
		<el-input class="rounded-none" v-model="unitDiscountModel.unitDiscount" clearable>
			<template #prepend>
				<el-select
					v-model="unitDiscountModel.unitDiscountType"
					:placeholder="t('sys.placeholder.selectPlaceholder')"
					class="rounded-lg w-[90px]"
					clearable
					filterable
				>
					<el-option
						v-for="item in typeOptions"
						:key="item.key"
						:label="item.value"
						:value="item.key"
					/>
				</el-select>
			</template>
		</el-input>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { appEnum } from '@/stores/modules/appEnum';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const appEnumStore = appEnum();
const typeOptions = computed(() => appEnumStore.getProjectOptions?.unitDiscount || []);

interface Props {
	modelValue: {
		unitDiscountType: string;
		unitDiscount: string;
		[key: string]: any;
	};
}

const props = defineProps<Props>();
const emit = defineEmits(['update:modelValue']);

const unitDiscountModel = computed({
	get() {
		return props.modelValue;
	},
	set(val) {
		emit('update:modelValue', val);
	},
});
</script>

<style lang="scss" scoped>
:deep(.el-input .el-input__wrapper) {
	border-radius: 0px 0.5rem 0.5rem 0px !important;
}
</style>

<style lang="scss" scoped>
:deep(.el-select__wrapper) {
	@apply m-0;
}
</style>
