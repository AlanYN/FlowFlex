import { propertyTypeEnum } from '@/enums/appEnum';

export type FieldType = 0 | 1 | 2 | 3 | 4;

// 日期精度类型
export type DatePrecision = 'year' | 'month' | 'day' | 'hour' | 'minute' | 'second';

// 布尔显示样式
export type BoolDisplayStyle = 'switch' | 'checkbox' | 'radio';

// 格式化配置
export interface DynamicFieldFormat {
	formatType?: number;
	pattern?: string;
	decimalPlaces?: number; // Number 类型
	dateFormat?: string; // DateTime 类型
}

// 验证配置
export interface DynamicFieldValidate {
	minLength?: number;
	maxLength?: number;
	minValue?: number;
	maxValue?: number;
	pattern?: string;
	message?: string;
}

// 下拉选项
export interface DynamicDropdownItem {
	id: number;
	value: string;
	sort: number;
	isDefault: boolean;
}

// 扩展配置 (additionalInfo)
export interface DynamicFieldAdditionalInfo {
	// Number
	isFloat?: boolean;
	allowNegative?: boolean;
	isFinancial?: boolean;
	// DateTime
	precision?: DatePrecision;
	showTime?: boolean;
	// Bool
	trueLabel?: string;
	falseLabel?: string;
	displayStyle?: BoolDisplayStyle;
	// MultilineText
	rows?: number;
	placeholder?: string;
	// DropDown
	allowMultiple?: boolean;
	allowSearch?: boolean;
	// File / FileList / Image
	maxSize?: number;
	allowedExtensions?: string[];
	maxCount?: number;
	// Image
	aspectRatio?: string;
	maxWidth?: number;
	maxHeight?: number;
	// People
	sourceType?: string;
	// Connection
	targetModule?: string;
	displayField?: string;
}

// 创建动态字段时使用的类型
export interface CreateDynamicFieldParams {
	fieldName: string;
	description?: string;
	dataType: propertyTypeEnum;
	format?: DynamicFieldFormat;
	fieldValidate?: DynamicFieldValidate;
	additionalInfo?: DynamicFieldAdditionalInfo;
	dropdownItems?: DynamicDropdownItem[];
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
		items: T;
		totalCount: number;
		pageIndex: number;
		pageSize: number;
		totalPages: number;
		hasPreviousPage: boolean;
		hasNextPage: boolean;
	};
};

export interface DynamicList {
	id: string;
	moduleId: number;
	fieldName: string;
	description: string;
	dataType: number;
	isSystemDefine: boolean;
	isHidden: boolean;
	allowEdit: boolean;
	sort: number;
	allowEditItem: boolean;
	createBy: string;
	createDate: string;
	groupId: string;
	isComputed: boolean;
	isDisplayField: boolean;
	modifyBy: string;
	modifyDate: string;
	// 是否必填（由 stage 设置）
	isRequired?: boolean;
	// 格式化配置
	format?: DynamicFieldFormat;
	// 验证配置
	fieldValidate?: DynamicFieldValidate;
	// 扩展配置
	additionalInfo?: DynamicFieldAdditionalInfo;
	// 下拉选项 (DropDown 类型)
	dropdownItems?: DynamicDropdownItem[];
	// 是否被选择
	inStages: string[];
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
