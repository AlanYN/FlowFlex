import type {
	ComponentRenderProxy,
	VNode,
	VNodeChild,
	ComponentPublicInstance,
	FunctionalComponent,
	PropType as VuePropType,
} from 'vue';

import { UserInfo } from '#/config';
import { Options } from '#/setting';

declare global {
	const __APP_INFO__: {
		pkg: {
			name: string;
			version: string;
			dependencies: Recordable<string>;
			devDependencies: Recordable<string>;
		};
		lastBuildTime: string;
	};
	// declare interface Window {
	//   // Global vue app instance
	//   __APP__: App<Element>;
	// }

	// fix FullScreen type error
	interface Document {
		mozFullScreenElement?: Element;
		msFullscreenElement?: Element;
		webkitFullscreenElement?: Element;
	}

	// vue
	declare type PropType<T> = VuePropType<T>;
	declare type VueNode = VNodeChild | JSX.Element;

	export type Writable<T> = {
		-readonly [P in keyof T]: T[P];
	};

	declare type Nullable<T> = T | null;
	declare type NonNullable<T> = T extends null | undefined ? never : T;
	declare type Recordable<T = any> = Record<string, T>;
	declare type ReadonlyRecordable<T = any> = {
		readonly [key: string]: T;
	};
	declare type Indexable<T = any> = {
		[key: string]: T;
	};
	declare type DeepPartial<T> = {
		[P in keyof T]?: DeepPartial<T[P]>;
	};
	declare type TimeoutHandle = ReturnType<typeof setTimeout>;
	declare type IntervalHandle = ReturnType<typeof setInterval>;

	declare interface ChangeEvent extends Event {
		target: HTMLInputElement;
	}

	declare interface WheelEvent {
		path?: EventTarget[];
	}
	interface ImportMetaEnv extends ViteEnv {
		__: unknown;
	}

	declare interface ViteEnv {
		VITE_USE_MOCK: boolean;
		VITE_PUBLIC_PATH: string;
		VITE_GLOB_APP_TITLE: string;
		VITE_BUILD_COMPRESS: 'gzip' | 'brotli' | 'none';
	}

	declare function parseInt(s: string | number, radix?: number): number;

	declare function parseFloat(string: string | number): number;

	namespace JSX {
		// tslint:disable no-empty-interface
		type Element = VNode;
		// tslint:disable no-empty-interface
		type ElementClass = ComponentRenderProxy;
		interface ElementAttributesProperty {
			$props: any;
		}
		interface IntrinsicElements {
			[elem: string]: any;
		}
		interface IntrinsicAttributes {
			[elem: string]: any;
		}
	}
}

declare module 'vue' {
	export type JSXComponent<Props = any> =
		| { new (): ComponentPublicInstance<Props> }
		| FunctionalComponent<Props>;
}

export interface FileList {
	id: string;
	name: string;
	fileUploadType: string;
	createDate: string;
	status: string;
	raw: FileList;
	percentage: number;
}

export interface AppEnum {
	country: Country[];
	state: Country[];
	phoneArea: PhoneArea[];
	propertyObj: DynamicFieldsList;
	productMenu: ProjectOptions | null;
	assignOptionsCache: Record<string, any[]>;
}

export interface Country {
	code: string;
	id: string;
	name: string;
	parentCode: string;
}

export interface PhoneArea {
	key: string;
	value: {
		countryCode: string;
		description: string;
		dialingCode: string;
	};
}

type ProductEnum =
	| 'errorCode'
	| 'operation'
	| 'scene'
	| 'accountOptionType'
	| 'approvalFlow'
	| 'attachmentType'
	| 'attachmentUploadType'
	| 'checkFile'
	| 'company'
	| 'country'
	| 'creditApplicationStatus'
	| 'creditApprovalType'
	| 'creditCardType'
	| 'crmStatusCode'
	| 'customerContactStatus'
	| 'customerContactType'
	| 'customerRisk'
	| 'dataPermissionOperationType'
	| 'dataPermissionType'
	| 'defaultInvoiceFormat'
	| 'importType'
	| 'invoiceByUF'
	| 'invoiceByUFList'
	| 'invoiceByUT'
	| 'invoice'
	| 'matchingStatus'
	| 'moduleType'
	| 'netTerm'
	| 'notificationEventType'
	| 'notificationModuleType'
	| 'operateType'
	| 'paymentType'
	| 'billingFrequency'
	| 'rateType'
	| 'unitDiscount'
	| 'recordSource'
	| 'role'
	| 'sendingFrequency'
	| 'sendingOption'
	| 'sendNotificationBusinessType'
	| 'supportingUF'
	| 'thirdParty'
	| 'userEventRecordLogType'
	| 'userQueryType'
	| 'addressAddFieldData'
	| 'addressStatus'
	| 'addressType'
	| 'conditionsType'
	| 'creditAppFormExempt'
	| 'currencyCode'
	| 'individualOptionType'
	| 'customerAccountsCategoryEum'
	| 'customerAccountsCustomerType'
	| 'customerAccountsGroup'
	| 'customerAccountsStatus'
	| 'customerAddressCategory'
	| 'customerBasicTags'
	| 'customerBillingInvoiceFormat'
	| 'customerBusinessLine'
	| 'customerBusinessType'
	| 'customerMainSubType'
	| 'customerUsageScenario'
	| 'licenseClass'
	| 'loginAccountSource'
	| 'mainSubType'
	| 'matchingRuleFieldsType'
	| 'matchingRuleOption'
	| 'matchingStrategy'
	| 'notificationBusinessType'
	| 'commonNotificationType'
	| 'averageStack'
	| 'fulfillmentType'
	| 'inventoryCount'
	| 'inventoryMethod'
	| 'inventoryReportBy'
	| 'notificationType'
	| 'outboundMethods'
	| 'outboundSelectOption'
	| 'operationStatusType'
	| 'position'
	| 'recruitStatus'
	| 'relationshipIdType'
	| 'relationshipStatus'
	| 'shift'
	| 'workFlowBussinessType'
	| 'yearsOfExperience'
	| 'workFlowBusinessType'
	| 'syncSource'
	| 'additionalService'
	| 'avgShipmentVolume'
	| 'freightPackaged'
	| 'selectOption'
	| 'shipCommodityType'
	| 'shipTo'
	| 'stockRotationSelectOption'
	| 'customerOrderBy'
	| 'uom'
	| 'buyingReason'
	| 'buyingTimeframe'
	| 'companiesFilter'
	| 'companiesOptionType'
	| 'companiesSort'
	| 'companiesType'
	| 'industry'
	| 'contactFilter'
	| 'contactOptionType'
	| 'contactSort'
	| 'dealContractSigningEntity'
	| 'dealFilter'
	| 'dealOptionType'
	| 'dealSort'
	| 'dealStage'
	| 'dealType'
	| 'lifeCycleStage'
	| 'optionNames'
	| 'preferencesCommunication'
	| 'preferencesCurrency'
	| 'preferencesLanguage'
	| 'priority'
	| 'relationalBusinessType'
	| 'relationalType'
	| 'sentDataStatus'
	| 'source'
	| 'taskFilter'
	| 'taskOptionType'
	| 'taskSort'
	| 'taskType'
	| 'toDo'
	| 'sendReminderRule'
	| 'dataType'
	| 'tabType'
	| 'dataType'
	| 'numberTypeFormat'
	| 'cache'
	| 'ruleEngineOperator'
	| 'dealStatus'
	| 'ruleEngineConditionField'
	| 'customerStatus'
	| 'contractType'
	| 'contractStatus'
	| 'creditLimitShareType'
	| 'viewShareType';

export type ProjectOptions = {
	[Key in ProductEnum]: Options[];
};

export interface Filter {
	labelName: string;
	filterKey: string;
	filterValue: string | string[];
	filterType: number;
	filterOptions?: Options[];
	cascaderApi?: (module: number, pipelineId: string) => Promise<any>;
	cascaderModule?: DescriptionType;
	remote?: boolean;
	loading?: boolean;
	remoteMethod?: (query: string) => void;
}

export interface FilterGroup {
	id: string;
	name: string;
	shareType: number;
	moduleType: number;
	filters: Filter[];
	createBy: string;
	createDate: string;
	modifyBy: string;
	modifyDate: string;
	createUserId: number;
	modifyUserId: number;
	isEdit: boolean;
}

export interface TableView {
	id: string;
	name: string;
	shareType: number;
	moduleType: number;
	isDefault: boolean;
	createBy: string;
	createDate: string;
	modifyBy: string;
	modifyDate: string;
	createUserId: number;
	modifyUserId: number;
	isEdit: boolean;
}

export interface ErrorInfo {
	errorId: string;
	errorInfo: ErrorInfoItem[];
}

export interface ErrorInfoItem {
	timestamp: number;
	statusCode: number;
	requestUrl: string;
	requestMethod: string;
	requestParams: any;
	errorMessage: string;
	token: string;
	feedbackContent?: string;
	contactInfo?: string;
	userInfo: UserInfo;
}

declare global {
	interface Window {
		__POWERED_BY_WUJIE__: boolean;
		__WUJIE_MOUNT: () => void;
		__WUJIE_UNMOUNT: () => void;
		__WUJIE__: {
			mount: () => void;
			unmount: () => void;
			bus: {
				$on: (event: string, handler: Function) => void;
				$off: (event: string, handler?: Function) => void;
				$emit: (event: string, ...args: any[]) => void;
			};
		};
		$wujie: {
			bus: {
				$on: (event: string, handler: Function) => void;
				$off: (event: string, handler?: Function) => void;
				$emit: (event: string, ...args: any[]) => void;
			};
			props: Record<string, any>;
		};
		__WUJIE: { mount: () => void };
	}
}

export interface FlowflexUser {
	id: string;
	name: string;
	type: 'user' | 'team';
	children: FlowflexUser[];
	email: string;
	username: string;
	memberCount?: number;
}

export type NotificationType = { name: 'tenant-change'; msg: any };
export type NotificationAction = (p: NotificationType) => Promise | void;
