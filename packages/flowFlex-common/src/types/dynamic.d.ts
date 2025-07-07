export interface TableColumn {
	groupId: string;
	groupName: string;
	fields: AllTableColumn[];
}

export interface AllTableColumn {
	selected?: boolean;
	displayName: string;
	dynamicFieldId: string;
	isTableMustShow: boolean;
	displayOrder: number;
}

export interface TableAndFilter {
	tableCode: string;
	tableName: string;
	description: string;
	columns: Column[] & DynamicFieldsList[];
	filters: Filters[] & DynamicFieldsList[];
	createUserId: string;
}

export interface FilterItem {
	fieldCount: number;
	fieldId: string;
	fieldName: string;
}

export interface Filters {
	dataType: number;
	dataTypeStr: string;
	displayName: string;
	displayOrder: number;
	dynamicFieldId: string;
	dynamicFieldIsStatic: boolean;
	exampleItems: FilterItem[];
	fieldName: string;
	filterValue: string;
	isStatic: boolean;
	options: Option[];
}

interface Operator {
	code: string;
	name: string;
}

interface Option {
	code: string;
	name: string;
}

export interface Column {
	dynamicFieldId: string;
	displayName: string;
	displayOrder: number;
	fixedPosition: string;
	isFixed: boolean;
	columnType: number;
	isSortable: boolean;
	minWidth: number;
	width: number;
	fieldName: string;
	dataTypeStr: string;
	dataType: number;
	isSetting?: boolean;
	formatDateTime?: boolean;
	isTableMustShow?: boolean;
	isDate?: boolean;
	checkbox?: boolean;
	linkType?: string;
	fieldType?: string;
	isStatic?: boolean;
	formatter?: (row: any, column: any) => string;
}

export interface SortEvent {
	dirction: number;
	name: string;
}

// 新的动态表格数据结构
export interface DynamicTableProperty {
	fieldName: string;
	displayName: string;
	value: any;
	fieldId: number;
	fieldType: number;
	description: string;
	sort: number;
	isUse?: boolean;
	metadata: {
		[key: string]: any;
	};
}

export interface DynamicTableRow {
	id: string;
	properties: DynamicTableProperty[];
	userPermissions?: {
		currentUserCanEdit?: boolean;
		currentUserCanView?: boolean;
		currentUserCanDelete?: boolean;
	};
}

export interface DynamicTableResponse {
	data: DynamicTableRow[];
	total: number;
}
