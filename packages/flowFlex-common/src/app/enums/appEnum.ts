export const SIDE_BAR_MINI_WIDTH = 48;
export const SIDE_BAR_SHOW_TIT_MINI_WIDTH = 80;

export enum ContentEnum {
	// auto width
	FULL = 'full',
	// fixed width
	FIXED = 'fixed',
}

// menu theme enum
export enum ThemeEnum {
	DARK = 'dark',
	LIGHT = 'light',
}

export enum SettingButtonPositionEnum {
	AUTO = 'auto',
	HEADER = 'header',
	FIXED = 'fixed',
}

export enum SessionTimeoutProcessingEnum {
	ROUTE_JUMP,
	PAGE_COVERAGE,
}

export const SexEnum = {
	0: 'Male',
	1: 'Female',
};

/**
 * 权限模式
 */
export enum PermissionModeEnum {
	// role
	// 角色权限
	ROLE = 'ROLE',
	// black
	// 后端
	BACK = 'BACK',
	// route mapping
	// 路由映射
	ROUTE_MAPPING = 'ROUTE_MAPPING',
}

// Route switching animation
// 路由切换动画
export enum RouterTransitionEnum {
	ZOOM_FADE = 'zoom-fade',
	ZOOM_OUT = 'zoom-out',
	FADE_SIDE = 'fade-slide',
	FADE = 'fade',
	FADE_BOTTOM = 'fade-bottom',
	FADE_SCALE = 'fade-scale',
}

export enum ProjectEnum {
	CRM = '1',
	VRM = '2',
}

export enum ReleationType {
	Companies = '3', //'1',
	Contact = '2',
	Deal = '4', //'3',
	Product = '5',
	LineItem = '6',
	Contract = '7',
}

export enum ActivityType {
	Activity = 1,
	Task = 8,
	Note = 9,
	OperationLog = 10,
}

export enum DescriptionType {
	Contact = 2,
	Company = 3,
	Deal = 4,
	Product = 5,
}

export enum GroupModule {
	about = 'groupAbort',
	name = 'groupName',
}

export enum CloseWonEnum {
	CloseWonAndAutoInvoice = '1891876314378539008',
}

export enum FilterType {
	NumberInput = 0,
	TextInput = 1,
	SelectInput = 2,
	TimeInput = 3,
	TimeRangeInput = 4,
	CheckBoxInput = 5,
	CascaderInput = 6,
}

export enum ViewType {
	Deal = 0,
	Company = 1,
	Contact = 2,
	Activity = 3,
}

export const dataTypeEnum = [
	{ key: 11, value: 'Single-line text' },
	{ key: 13, value: 'Number' },
	{ key: 10, value: 'Date picker' },
	{ key: 5, value: 'Dropdown select' },
	{ key: 19, value: 'People' },
	{ key: 20, value: 'Data connection' },
	{ key: 17, value: 'File' },
	{ key: 3, value: 'Phone' },
	{ key: 4, value: 'Email' },
	{ key: 23, value: 'Time line' },
];

export enum propertyTypeEnum {
	SingleLineText = 11,
	Number = 13,
	DatePicker = 10,
	DropdownSelect = 5,
	Pepole = 19,
	Connection = 20,
	FileList = 17,
	Phone = 3,
	Email = 4,
	Image = 22,
	TimeLine = 23,
}

export const booleanEnum = [
	{ key: false, value: 'No' },
	{ key: true, value: 'Yes' },
];

export const moduleType = [
	{
		key: 0,
		value: 'None',
	},
	{
		key: 1,
		value: 'Activity',
	},
	{
		key: 2,
		value: 'Contact',
	},
	{
		key: 3,
		value: 'Company',
	},
	{
		key: 4,
		value: 'Deal',
	},
	{
		key: 5,
		value: 'Product',
	},
	{
		key: 6,
		value: 'Line Item',
	},
	{
		key: 7,
		value: 'Contract',
	},
	{
		key: 8,
		value: 'Task',
	},
	{
		key: 9,
		value: 'Note',
	},
	{
		key: 10,
		value: 'Operationlog',
	},
];

export enum TriggerTypeEnum {
	Stage = 'Stage',
	Task = 'Task',
	Questionnaire = 'Question',
	Workflow = 'Workflow',
}

export enum WFEMoudels {
	Workflow = 1,
	Stage = 2,
	Checklist = 3,
	Questionnaire = 4,
	ChecklistTask = 5,
}
