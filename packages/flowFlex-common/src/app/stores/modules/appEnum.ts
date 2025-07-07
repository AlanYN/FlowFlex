import { defineStore } from 'pinia';
import { store } from '@/stores';
import { DynamicFieldsList } from '#/leadAndDeal';
import { AppEnum, Country, PhoneArea, ProjectOptions } from '#/golbal';

const defaultPropertyObj = {
	dataType: null,
	createBy: '',
	description: '',
	displayName: '',
	dropdownItems: [],
	fieldName: '',
	format: '',
	formatId: '',
	groupId: '',
	isSystemDefine: null,
	propertyId: '',
	refFieldId: '',
	sortType: null,
	fieldType: null,
	connectionItems: null,
	validate: {
		fileListValidate: {
			maxCount: 1,
			maxFileLength: 15,
		},
	},
};

export const useCrmEnumStore = defineStore({
	id: 'app',
	state: (): AppEnum => ({
		country: [],
		state: [],
		phoneArea: [],
		propertyObj: JSON.parse(JSON.stringify(defaultPropertyObj)) as DynamicFieldsList,
		productMenu: null,
		assignOptionsCache: {},
	}),
	getters: {
		getCountry: (state: AppEnum) => {
			return state.country;
		},
		getPhoneArea: (state: AppEnum) => {
			return state.phoneArea;
		},
		getPropertyObj(state: AppEnum) {
			return state.propertyObj;
		},
		getProjectOptions(state: AppEnum) {
			return state.productMenu;
		},
		getAssignOptions: (state: AppEnum) => (searchText: string) => {
			return state.assignOptionsCache[searchText] || [];
		},
	},
	actions: {
		setCountry(data: Country[]) {
			this.country = data;
		},
		setPhone(data: PhoneArea[]) {
			this.phoneArea = data;
		},
		setProductOptions(data: ProjectOptions) {
			this.productMenu = data;
		},
		setPropertyObj(data: Partial<DynamicFieldsList>) {
			const property = {
				...JSON.parse(JSON.stringify(defaultPropertyObj)),
				...data,
			};
			this.propertyObj = property as DynamicFieldsList;
		},
		setAssignOptions(searchText: string, options: any[]) {
			this.assignOptionsCache[searchText] = options;
		},
	},
});

export function appEnum() {
	return useCrmEnumStore(store);
}
