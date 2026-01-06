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

export enum CompanyCode {
	UF = 'LT',
	UT = 'SBFH',
	CW = 'CW',
	OTHER = 'OTHER',
}

export const SexEnum = {
	0: 'Male',
	1: 'Female',
};

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
	WFE = '5',
}

export enum ViewType {
	Deal = 0,
	Company = 1,
	Contact = 2,
	Activity = 3,
}

export const fieldsTypeEnum = [
	{ key: 11, value: 'Single-line Text' },
	{ key: 3, value: 'Phone' },
	{ key: 4, value: 'Email' },
	{ key: 5, value: 'Dropdown Select' },
	{ key: 7, value: 'Switch' },
	{ key: 10, value: 'Date Picker' },
	{ key: 12, value: 'Multiline Text' },
	{ key: 13, value: 'Number' },
	{ key: 16, value: 'File' },
	{ key: 19, value: 'People' },
];

export enum propertyTypeEnum {
	Phone = 3,
	Email = 4,
	DropdownSelect = 5,
	Switch = 7,
	DatePicker = 10,
	SingleLineText = 11,
	MultilineText = 12,
	Number = 13,
	File = 16,
	Pepole = 19,
}

export const booleanEnum = [
	{ key: false, value: 'No' },
	{ key: true, value: 'Yes' },
];

export enum TriggerTypeEnum {
	Stage = 'Stage',
	Task = 'Task',
	Questionnaire = 'Question',
	Workflow = 'Workflow',
	Integration = 'Integration',
}

export enum WFEMoudels {
	Workflow = 1,
	Stage = 2,
	Checklist = 3,
	Questionnaire = 4,
	ChecklistTask = 5,
	Onboarding = 6,
}

export enum StageComponentPortal {
	Hidden = 0,
	Viewable = 1,
	Completable = 2,
}

export enum ToolsType {
	UseTool = 1,
	MyTool = 2,
	NewTool = 3,
	SystemTools = 4,
}
