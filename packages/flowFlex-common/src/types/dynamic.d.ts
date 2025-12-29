import { propertyTypeEnum } from '@/enums/appEnum';

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

// 创建动态字段时使用的类型（只需要必要字段）
export interface CreateDynamicFieldParams {
	displayName: string;
	fieldName: string;
	description?: string;
	dataType: propertyTypeEnum;
}

export type DynamicApiResponse<T> = {
	code: string;
	msg: string;
	data: T;
};

export type DynamicApiListResponse<T> = {
	code: string;
	msg: string;
	data: {
		items: T,
    totalCount: number,
    pageIndex:number,
    pageSize: number,
    totalPages: number,
    hasPreviousPage: boolean,
    hasNextPage: boolean
	};
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
	allowEditItem: boolean;
	createBy: string;
	createDate: string;
	groupId: string;
	isComputed: boolean;
	isDisplayField: boolean;
	isRequired: boolean;
	modifyBy: string;
	modifyDate: string;
}

export interface DynamicSearch {
	pageIndex?: number;
	pageSize?: number;
	fieldName?: string;
	displayName?: string | null;
	dataType?: number;
	createBy?: string;
	modifyBy?: string | null;
	createDateStart?: string;
	createDateEnd?: string;
	modifyDateStart?: string | null;
	modifyDateEnd?: string | null;
	sortField?: string;
	isAsc?: boolean;
}
