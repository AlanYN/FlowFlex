export type * from 'element-plus';

export interface Retailer {
	id: string;
	retailerMaster: string;
	matchValue: string;
	matchingStrategy: number;
	createBy: string;
	createDate: string;
	children?: Retailer[];
}

export interface Column {
	prop: string;
	label: string;
	width?: string;
	sortable?: string;
	align?: string;
	fixed?: string;
	minWidth?: string;
	formatter?: (row: any) => string;
}

export interface Options {
	key: string | number | boolean;
	value: string;
	disabled?: boolean;
	code?: string;
	isDefault?: boolean;
	email?: string;
}

export interface OptionsTree {
	id: number;
	name: string;
	sub?: OptionsTree[];
}

export interface CountryOrState {
	id: string;
	code: string;
	label: string;
	value: string;
	isDefault: boolean;
}

export interface MappingForm {
	customerName: string;
	emailLastSendingTime: null;
	emailSendingCount: number;
	id: string;
	matchedId: string;
	matchingMasterName: string;
	matchingSourceTag: number;
	originalName: string;
	receivedTime: string;
	status: number;
	mappingMatchedId: string;
}

export interface ListCard {
	deals: any[];
	name: string;
	pipelineStageId: string;
}

export interface DealGroup {
	[key: string]: DealGroupDetail;
}

export interface DealGroupDetail {
	leads: {
		leadsId: string;
		headUrl: string;
		leadsType: 2;
		name: string;
		logoImageId: string;
	}[];

	tasksUpdateTime: string;

	notesUpdateTime: string;

	dealUpdateTime: string;
}

export interface DropdownItem {
	id: string;
	propertyId: string;
	refId: string | null;
	displayName: string;
	itemName: string;
	description: string;
	isDefault: boolean;
	sort: number;
	isAllowDelete: boolean;
}

export interface FormField {
	fieldName: string;
	displayName: string;
	dataType: number;
	required: boolean;
	dropdownItems: DropdownItem[];
	connectionItems: [string, string][];
	propertyId: string;
	isSystemDefine: boolean;
	createBy: string;
	createDate: string;
	modifyBy: string;
	modifyDate: string;
	sourceType: number;
	allowEdit: boolean;
	allowEditItem: boolean;
	isStatic: boolean;
	isDisplayField: boolean;
	metadata: any;
	groupId: string;
	description: string | null;
	sortType: number;
	formatId: string | null;
	validateId: string | null;
	refFieldId: string | null;
	sort: number;
	validate?: {
		validateId: string;
		numberValidate: any;
		dateValidate: any;
		textValidate: any;
		fileListValidate?: {
			maxCount: number;
			maxFileLength?: number;
			accept?: string;
		};
	};
	format: any;
}

export interface DynamicFormDetail {
	id: string;
	properties: {
		fieldName: string;
		displayName: string;
		value: string | null | any[];
		fieldId: string;
		fieldType: number;
		description: string | null;
		sort: number | null;
		metadata: Record<string, any>;
	}[];
}
