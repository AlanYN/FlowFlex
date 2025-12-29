export type FieldType = 0 | 1 | 2 | 3 | 4;
//0=Text, 1=Number, 2=Date, 3=Boolean, 4=Lookup

export interface DynamciFile {
	id: string;
	fieldId: string;
	fieldLabel: string;
	formProp: string;
	category: string;
	fieldType: FieldType;
	sortOrder: number;
	isRequired: boolean;
	isSystem: boolean;
	createDate: string;
	modifyDate: string;
}

export type DynamicApiResponse<T> = {
	code: string;
	msg: string;
	data: T;
};

export interface DynamicList {
	id: string;
	moduleId: number;
	displayName: string;
	fieldName: string;
	description: string;
	dataType: number;
	isSystemDefine: boolean;
	isRequired: boolean;
	isHidden: boolean;
	allowEdit: boolean;
	sort: number;
}

export interface DynamicSearch {
	fieldName?: string;
}
