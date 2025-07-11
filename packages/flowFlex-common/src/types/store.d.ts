import { ErrorTypeEnum } from '@/enums/exceptionEnum';
import { MenuModeEnum, MenuTypeEnum } from '@/enums/menuEnum';

// Lock screen information
export interface LockInfo {
	// Password required
	pwd?: string | undefined;
	// Is it locked?
	isLock?: boolean;
}

export interface ApiAddress {
	key: string;
	val: string;
}

// Error-log information
export interface ErrorLogInfo {
	// Type of error
	type: ErrorTypeEnum;
	// Error file
	file: string;
	// Error name
	name?: string;
	// Error message
	message: string;
	// Error stack
	stack?: string;
	// Error detail
	detail: string;
	// Error url
	url: string;
	// Error time
	time?: string;
}

export interface UserInfo {
	userId: string | number;
	userName: string;
	realName: string;
	avatar: string;
	desc?: string;
	homePath?: string;
	roles: any[];
	companyIds: [string | number];
}

export interface BeforeMiniState {
	menuCollapsed?: boolean;
	menuSplit?: boolean;
	menuMode?: MenuModeEnum;
	menuType?: MenuTypeEnum;
}

export interface TableSetting {
	size: any;
	showIndexColumn: any;
	columns: any;
	showRowSelection: any;
}
