<template>
	<div class="flex justify-between w-full">
		<el-input class="rounded-none" v-model="phone" @input="validatePhoneNumber" clearable>
			<template #prepend>
				<el-select
					v-model="phoneNumberPrefixesId"
					:placeholder="t('sys.placeholder.selectPlaceholder')"
					class="rounded-lg w-[120px]"
					clearable
					filterable
					:filter-method="filterPhoneCode"
				
				:teleported="false">
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
	phone: string | null | undefined;
	phoneNumberPrefixesId: string | null | undefined;
	property?: Record<string, any>;
}

const props = defineProps<Props>();

const emit = defineEmits(['update:phoneNumberPrefixesId', 'update:phone']);

const phoneNumberPrefixesId = computed({
	get() {
		return props.phoneNumberPrefixesId;
	},
	set(val) {
		emit('update:phoneNumberPrefixesId', val);
	},
});

const phone = computed({
	get() {
		return props.phone;
	},
	set(val) {
		emit('update:phone', val);
	},
});

// const initPrefixesId = () => {
// 	const obj = phoneCodeOptions.value.find((item) => item.value.dialingCode == '+1');
// 	if (!obj) {
// 		isClearPrefix.value = true;
// 		return '';
// 	}
// 	return obj.key;
// };
// const isClearPrefix = ref(false);

// watch(
// 	() => props.phoneNumberPrefixesId,
// 	(newVal) => {
// 		if (!newVal && !isClearPrefix.value) {
// 			emit('update:phoneNumberPrefixesId', initPrefixesId());
// 		} else {
// 			isClearPrefix.value = false;
// 		}
// 	},
// 	{ immediate: true }
// );

const filterPhoneCode = (value) => {
	filiterrPhoneCode.value = appEnumStore.getPhoneArea.filter((item) => {
		return (
			item.value.description.toLowerCase().includes(value.toLowerCase()) ||
			item.value.countryCode.toLowerCase().includes(value.toLowerCase())
		);
	});
};

// const clearPrefix = () => {
// 	isClearPrefix.value = true;
// };

const validatePhoneNumber = (val) => {
	phone.value = val.replace(/[^0-9\s\-()]/g, '');
};
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
