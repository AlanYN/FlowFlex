import { MenuTypeEnum, MenuModeEnum, TriggerEnum, MixSidebarTriggerEnum } from '@/enums/menuEnum';
import { inputTextraAutosize } from '@/settings/projectSetting';
import {
	ContentEnum,
	PermissionModeEnum,
	ThemeEnum,
	RouterTransitionEnum,
	SettingButtonPositionEnum,
	SessionTimeoutProcessingEnum,
} from '@/enums/appEnum';

import { CacheTypeEnum } from '@/enums/cacheEnum';

export type LocaleType = 'zh_CN' | 'en' | 'ru' | 'ja' | 'ko' | string;

export interface MenuSetting {
	bgColor: string;
	fixed: boolean;
	collapsed: boolean;
	siderHidden: boolean;
	canDrag: boolean;
	show: boolean;
	hidden: boolean;
	split: boolean;
	menuWidth: number;
	mode: MenuModeEnum;
	type: MenuTypeEnum;
	theme: ThemeEnum;
	topMenuAlign: 'start' | 'center' | 'end';
	trigger: TriggerEnum;
	accordion: boolean;
	closeMixSidebarOnChange: boolean;
	collapsedShowTitle: boolean;
	mixSideTrigger: MixSidebarTriggerEnum;
	mixSideFixed: boolean;
}

export interface MultiTabsSetting {
	cache: boolean;
	show: boolean;
	showQuick: boolean;
	canDrag: boolean;
	showRedo: boolean;
	showFold: boolean;
	autoCollapse: boolean;
}

export interface HeaderSetting {
	bgColor: string;
	fixed: boolean;
	show: boolean;
	theme: ThemeEnum;
	// Turn on full screen
	showFullScreen: boolean;
	// Whether to show the lock screen
	useLockPage: boolean;
	// Show document button
	showDoc: boolean;
	// Show message center button
	showNotice: boolean;
	showSearch: boolean;
	showApi: boolean;
}

export interface LocaleSetting {
	showPicker: boolean;
	// Current language
	locale: LocaleType;
	// default language
	fallback: LocaleType;
	// available Locales
	availableLocales: LocaleType[];
}

export interface TransitionSetting {
	//  Whether to open the page switching animation
	enable: boolean;
	// Route basic switching animation
	basicTransition: RouterTransitionEnum;
	// Whether to open page switching loading
	openPageLoading: boolean;
	// Whether to open the top progress bar
	openNProgress: boolean;
}

export interface ProjectConfig {
	// Storage location of permission related information
	permissionCacheType: CacheTypeEnum;
	// Whether to show the configuration button
	showSettingButton: boolean;
	// Whether to show the theme switch button
	showDarkModeToggle: boolean;
	// Configure where the button is displayed
	settingButtonPosition: SettingButtonPositionEnum;
	// Permission mode
	permissionMode: PermissionModeEnum;
	// Session timeout processing
	sessionTimeoutProcessing: SessionTimeoutProcessingEnum;
	// Website gray mode, open for possible mourning dates
	grayMode: boolean;
	// Whether to turn on the color weak mode
	colorWeak: boolean;
	// Theme color
	themeColor: string;

	// The main interface is displayed in full screen, the menu is not displayed, and the top
	fullContent: boolean;
	// content width
	contentMode: ContentEnum;
	// Whether to display the logo
	showLogo: boolean;
	// Whether to show the global footer
	showFooter: boolean;
	// menuType: MenuTypeEnum;
	headerSetting: HeaderSetting;
	// menuSetting
	menuSetting: MenuSetting;
	// Multi-tab settings
	multiTabsSetting: MultiTabsSetting;
	// Animation configuration
	transitionSetting: TransitionSetting;
	// pageLayout whether to enable keep-alive
	openKeepAlive: boolean;
	// Lock screen time
	lockTime: number;
	// Show breadcrumbs
	showBreadCrumb: boolean;
	// Show breadcrumb icon
	showBreadCrumbIcon: boolean;
	// Use error-handler-plugin
	useErrorHandle: boolean;
	// Whether to open back to top
	useOpenBackTop: boolean;
	// Is it possible to embed iframe pages
	canEmbedIFramePage: boolean;
	// Whether to delete unclosed messages and notify when switching the interface
	closeMessageOnSwitch: boolean;
	// Whether to cancel the http request that has been sent but not responded when switching the interface.
	removeAllHttpPending: boolean;
	environment: string;
}

export interface GlobConfig {
	// Site title
	title: string;
	// Service interface url
	apiUrl: string;
	// Upload url
	uploadUrl?: string;
	//  Service interface url prefix
	urlPrefix?: string;
	// Project abbreviation
	shortName: string;
	// itme app code
	ssoCode?: string;
	// 单点登录地址
	ssoURL?: string;
	// 当前环境
	environment?: string;
	// 使用的api版本号
	apiVersion?: string;
	// 使用的api项目名称
	apiProName?: string;
	// 调用iam的接口域名
	idmUrl?: string;
	// 门户访问地址
	domainUrl?: string;
}

export interface GlobEnvConfig {
	// Site title
	VITE_GLOB_APP_TITLE: string;
	// Service interface url
	VITE_GLOB_API_URL: string;
	// Service interface url prefix
	VITE_GLOB_API_URL_PREFIX?: string;
	// Upload url
	VITE_GLOB_UPLOAD_URL?: string;
	// itme app code
	VITE_GLOB_CODE?: string;
	// 单点登录地址
	VITE_GLOB_SSOURL?: string;
	// 当前环境
	VITE_GLOB_ENVIRONMENT?: string;
	// 门户访问地址
	VITE_GLOB_DOMAIN_URL?: string;
	// 调用idm的接口域名
	VITE_GLOB_IDM_URL?: string;
}

export interface RoleMenu {
	code?: string;
	hidden?: boolean;
	icon?: string;
	isMenu?: boolean;
	menuId?: string;
	name?: string;
	ordinal?: number;
	parentId?: string;
	status?: boolean;
	url?: string;
}

export interface CompanyEnum {
	[key: number]: string;
}

export interface UserInfo {
	userType?: number;
	tenantId?: number;
	userId?: string | number;
	userName?: string;
	realName?: string;
	avatar?: string;
	desc?: string;
	homePath?: string;
	roles?: string[];
	email?: string | null;
	companyIds?: Array<string | number>;
	tenants?: CompanyEnum;
	clientShortName: string;
	defaultTimeZone?: string;
	attachmentId?: string;
	avatarUrl?: string;
	tenantDisplayType?: string;
}

export interface ILayout {
	hideMenu?: boolean;
	hideEditMenu?: boolean;
}

export interface UserState {
	userInfo: UserInfo | null;
	token?: string;
	roleList: RoleEnum[];
	sessionTimeout?: boolean;
	lastUpdateTime: number;
	terminalCode: any[];
	tokenObj?: TokenObj;
	layout: {};
	isLogin: boolean;
	tenantSwitching: {
		isActive: boolean;
		fromTenantId: string | null;
		toTenantId: string | null;
		progress: number;
		currentStep: string;
		error: string | null;
	};
}

export interface ParametersToken {
	BNPToken?: string;
	BNPVendorID?: string;
	hideEditMenu?: boolean;
	token?: string;
	userId?: string;
	oauth?: boolean;
	userName?: string;
	ticket?: string;
	hideMenu?: boolean;
	loginType?: string;
	code?: string;
	state?: string;
	appCode?: string;
	tenantId?: string;
}

export interface FileList {
	id: string;
	name: string;
	fileUploadType: string;
	createDate: string;
	status: string;
	raw: FileList;
}

export interface InputProperty {
	// InputProperty的属性
	type?: string;
	clearable?: boolean;
	readonly?: boolean;
	placeholder?: string;
	maxlength?: number;
	disabled?: boolean;
	multiple?: boolean;
	showWordLimit?: boolean;
	autoSize?: typeof inputTextraAutosize;
	dateType?:
		| 'date'
		| 'year'
		| 'years'
		| 'month'
		| 'dates'
		| 'week'
		| 'datetime'
		| 'datetimerange'
		| 'daterange'
		| 'monthrange';
	valueFormat?: string;
	remote?: boolean;
	isFoloat?: boolean;
	minusNumber?: boolean;
	isFinancial?: boolean;
	connectionItems?: any[] | null;

	// InputProperty的validate属性
	validate?: {
		fileListValidate?: {
			maxCount: number;
			maxFileLength?: number;
		};
	};

	// DynamicFieldsList的属性
	createBy?: string | null;
	dataType?: number;
	description?: string | null;
	displayName?: string | null;
	dropdownItems?: DynamicOptions[];
	fieldName?: string;
	format?: string | null;
	formatId?: string | null;
	groupId?: string | null;
	isSystemDefine?: boolean | null;
	propertyId?: string | null;
	refFieldId?: string | null;
	sortType?: number | null;
	validateId?: string | null;
	allowEdit?: boolean;
	allowEditItem?: boolean;
	createDate?: string;
	hidden?: boolean;
	isDisplayField?: boolean;
	isStatic?: boolean;
	metadata?: null;
	modifyBy?: string;
	modifyDate?: string;
	required?: boolean;
	sourceType?: number;
	value?: any;
}
