import { InputProperty } from '#/config';

export interface AboutProps {
	label: string;
	value: string;
	type:
		| 'text'
		| 'tag'
		| 'date'
		| 'select'
		| 'link'
		| 'number'
		| 'mention'
		| 'fileList'
		| 'connectionSelect'
		| 'peopleSelect';
	key: string;
	map?: string;
	property?: InputProperty;
	searchApi?: (text: string) => Promise<any>;
	required?: boolean;
	errorTip?: string;
}

export interface BaseRangeInfo {
	type: 'text' | 'tag' | 'date' | 'select' | 'link' | 'number' | 'phone';
	property?: any;
	triggerChange?: boolean;
	errorTip?: string;
	isCopy?: boolean;
	displayName: string;
	value: string;
	fieldName: string;
	required?: boolean;
	tipType?: string;
}

export interface ICountryOrStateItem {
	id: string;
	code: string;
	label: string;
	value: string;
	isDefault?: boolean;
}

export interface DataEnums {
	countryList: Array<ICountryOrStateItem>;
	stateList: Array<ICountryOrStateItem>;
}

export interface DelasInfo {
	amount?: number | null;
	closeDate?: string;
	dealDate?: string;
	dealDescription?: string;
	dealName?: string;
	dealOwner?: string;
	dealStage?: number | null;
	dealType?: number | null;
	id?: string;
	priority?: number | null;
}

export interface ContactInfo {
	contactOwner: string;
	createDate: string;
	dataSource: string;
	email: string;
	firstName: string;
	id: string;
	jobTitle: string;
	lastName: string;
	lifecyclestage: string | null;
	phone: string;
	priority: string | null;
}

export interface CompanyInfo {
	city: string;
	companyName: string;
	companyOwner: string;
	country: string;
	createDate: string;
	dataSource: string;
	domainName: string;
	id: string;
	industry: number;
	lifecyclestage: number;
	name: string;
	priority: number;
	state: string;
	timeZone: string;
	zipCode: string | null;
}
