<template>
	<div class="flex justify-between w-full">
		<el-input
			class="rounded-none"
			v-model="phoneModel.phone"
			@input="validatePhoneNumber"
			clearable
		>
			<template #prepend>
				<el-select
					v-model="phoneModel.phoneNumberPrefixesId"
					:placeholder="t('sys.placeholder.selectPlaceholder')"
					class="rounded-xl w-[90px]"
					clearable
					filterable
					:filter-method="filterPhoneCode"
				>
					<el-option
						v-for="item in phoneCodeOptions"
						:key="item.key"
						:label="item.value.dialingCode"
						:value="item.key"
					>
						<div>{{ item.value.description }}</div>
					</el-option>
				</el-select>
			</template>
		</el-input>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { appEnum } from '@/stores/modules/appEnum';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const appEnumStore = appEnum();

const filiterrPhoneCode = ref(appEnumStore.getPhoneArea);

const phoneCodeOptions = computed(() => filiterrPhoneCode.value);

interface Props {
	modelValue: {
		phone: string | number | boolean | null | undefined;
		phoneNumberPrefixesId: string | number | boolean | null | undefined;
	};
}

const props = defineProps<Props>();

const emit = defineEmits(['update:modelValue']);

const phoneModel = computed({
	get() {
		return props.modelValue;
	},
	set(val) {
		emit('update:modelValue', val);
	},
});

const filterPhoneCode = (value) => {
	filiterrPhoneCode.value = appEnumStore.getPhoneArea.filter((item) => {
		return item.value.description.toLowerCase().includes(value.toLowerCase());
	});
};

// const clearPrefix = () => {
// 	isClearPrefix.value = true;
// };

const validatePhoneNumber = (val) => {
	phoneModel.value.phone = val.replace(/[^0-9\s\-()]/g, '');
};
</script>

<style lang="scss" scoped>
:deep(.el-input .el-input__wrapper) {
	@apply rounded-xl;
}
</style>

<style lang="scss" scoped>
:deep(.el-select__wrapper) {
	@apply m-0;
}
</style>
