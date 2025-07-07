import { propertyTypeEnum, ReleationType } from '../enums/appEnum';

export type FieldType =
	| 'email'
	| 'phone'
	| 'dropdownlist'
	| 'string'
	| 'file'
	| 'people'
	| 'timeline'
	| 'date'
	| 'number';

export type OperationType =
	| ''
	| 'is'
	| 'is_not'
	| 'contains'
	| 'not_contains'
	| 'is_empty'
	| 'is_not_empty'
	| 'within'
	| 'contain'
	| 'greater_than'
	| 'greater_equal'
	| 'less_than'
	| 'less_equal'
	| 'date_range';

export interface FilterCondition {
	id: string;
	field: string;
	operator: OperationType;
	value: ValueType;
	fieldType: FieldType;
}

export interface DateRange {
	startDate: string;
	endDate: string;
}

export type ValueType = string | number | DateRange | Date;

export interface FilterGroup {
	id: string;
	logic: 'AND' | 'OR';
	conditions: FilterCondition[];
	groups?: FilterGroup[];
}

export interface ListConfig {
	name: string;
	moduleId: ReleationType;
	type: 'static' | 'dynamic';
	description?: string;
	shared: boolean;
	conditions: FilterGroup;
}

export interface FieldOption {
	used?: boolean;
	value: string;
	label: string;
	type: FieldType;
	options: OptionItem[];
	remote?: boolean;
	remoteMethod?: (
		query: string | undefined,
		id: string | undefined
	) => Promise<Array<OptionItem>>;
}

export interface OptionItem {
	id: string;
	name: string;
}

export interface ListInfoModel {
	id: string;
	name: string;
	description: string;
	moduleId: number;
	isDynamic: boolean;
	expression: FilterExpression;
}

export interface FilterExpression {
	logic: LogicEnum;
	fieldName?: string;
	fieldType?: propertyTypeEnum;
	fieldValue?: any;
	condition: FilterExpression[];
}

export enum LogicEnum {
	/// <summary>
	/// 并且
	/// </summary>
	And = 1,

	/// <summary>
	/// 或者
	/// </summary>
	Or = 2,

	/// <summary>
	/// 等于
	/// </summary>
	Equal = 3,

	/// <summary>
	/// 不等于
	/// </summary>
	NotEqual = 4,

	/// <summary>
	/// 大于
	/// </summary>
	GreaterThan = 5,

	/// <summary>
	/// 大于等于
	/// </summary>
	GreaterThanOrEqual = 6,

	/// <summary>
	/// 小于
	/// </summary>
	LessThan = 7,

	/// <summary>
	/// 小于等于
	/// </summary>
	LessThanOrEqual = 8,

	/// <summary>
	/// 在 [......] 里面
	/// </summary>
	In = 9,

	/// <summary>
	/// 不在 [....] 里面
	/// </summary>
	NotIn = 10,

	/// <summary>
	/// 模糊匹配
	/// </summary>
	Like = 11,

	/// <summary>
	/// 左侧模糊匹配
	/// </summary>
	LikeLeft = 12,

	/// <summary>
	/// 右侧模糊匹配
	/// </summary>
	LikeRight = 13,

	/// <summary>
	///
	/// </summary>
	NoEqual = 14,

	/// <summary>
	/// 是NULL 或者是空
	/// </summary>
	IsNullOrEmpty = 15,

	/// <summary>
	/// 不是 NULL 只能用在 NULL 上
	/// </summary>
	IsNotNull = 16,

	/// <summary>
	/// 模糊匹配取反
	/// </summary>
	NoLike = 17,

	/// <summary>
	/// 是 NULL 只能用在 NULL 上
	/// </summary>
	IsNull = 18,

	/// <summary>
	/// 在 [....] 中模糊匹配
	/// </summary>
	InLike = 19,

	/// <summary>
	/// 在某个范围内
	/// </summary>
	Range = 20,

	/// <summary>
	/// 用在 TimeLine 上,
	/// start >= filter.start && end <= filter.end
	/// </summary>
	WithIn = 21,

	/// <summary>
	/// 用在 TimeLine 上,
	/// start <= filter.start && end >= filter.end
	/// </summary>
	WithOut = 22,
}

export function getFieldType(fieldType?: propertyTypeEnum): FieldType {
	switch (fieldType) {
		case propertyTypeEnum.SingleLineText:
			return 'string';
		case propertyTypeEnum.Number:
			return 'number';
		case propertyTypeEnum.DatePicker:
			return 'date';
		case propertyTypeEnum.Email:
			return 'email';
		case propertyTypeEnum.Phone:
			return 'phone';
		case propertyTypeEnum.DropdownSelect:
			return 'dropdownlist';
		case propertyTypeEnum.FileList:
			return 'file';
		case propertyTypeEnum.Pepole:
			return 'people';
		case propertyTypeEnum.TimeLine:
			return 'timeline';
		default:
			return 'string';
	}
}

export function getPropertyType(fieldType: FieldType): propertyTypeEnum {
	switch (fieldType) {
		case 'string':
			return propertyTypeEnum.SingleLineText;
		case 'number':
			return propertyTypeEnum.Number;
		case 'date':
			return propertyTypeEnum.DatePicker;
		case 'email':
			return propertyTypeEnum.Email;
		case 'phone':
			return propertyTypeEnum.Phone;
		case 'dropdownlist':
			return propertyTypeEnum.DropdownSelect;
		case 'file':
			return propertyTypeEnum.FileList;
		case 'people':
			return propertyTypeEnum.Pepole;
		case 'timeline':
			return propertyTypeEnum.TimeLine;
		default:
			return propertyTypeEnum.SingleLineText;
	}
}

export function getOperator(logic: LogicEnum): OperationType {
	switch (logic) {
		case LogicEnum.Equal:
			return 'is';
		case LogicEnum.NotEqual:
			return 'is_not';
		case LogicEnum.GreaterThan:
			return 'greater_than';
		case LogicEnum.GreaterThanOrEqual:
			return 'greater_equal';
		case LogicEnum.LessThan:
			return 'less_than';
		case LogicEnum.LessThanOrEqual:
			return 'less_equal';
		case LogicEnum.Like:
			return 'contains';
		case LogicEnum.NoLike:
			return 'not_contains';
		case LogicEnum.IsNull:
			return 'is_empty';
		case LogicEnum.IsNotNull:
			return 'is_not_empty';
		case LogicEnum.Range:
			return 'date_range';
		case LogicEnum.WithIn:
			return 'within';
		case LogicEnum.WithOut:
			return 'contain';
		default:
			return 'is';
	}
}

export function getLogicEnum(operator: OperationType): LogicEnum {
	switch (operator) {
		case 'is':
			return LogicEnum.Equal;
		case 'is_not':
			return LogicEnum.NotEqual;
		case 'greater_than':
			return LogicEnum.GreaterThan;
		case 'greater_equal':
			return LogicEnum.GreaterThanOrEqual;
		case 'less_than':
			return LogicEnum.LessThan;
		case 'less_equal':
			return LogicEnum.LessThanOrEqual;
		case 'contains':
			return LogicEnum.Like;
		case 'not_contains':
			return LogicEnum.NoLike;
		case 'is_empty':
			return LogicEnum.IsNull;
		case 'is_not_empty':
			return LogicEnum.IsNotNull;
		case 'date_range':
			return LogicEnum.Range;
		case 'within':
			return LogicEnum.WithIn;
		case 'contain':
			return LogicEnum.WithOut;
		default:
			return LogicEnum.Equal;
	}
}

export interface PropertyType {
	propertyId: string;
	dataType: propertyTypeEnum;
	displayName: string;
	fieldName: string;
	isSystemDefine: boolean;
	dropdownItems: {
		displayName: string;
		itemName: string;
		refId: string;
		id: string;
	}[];
}

export function expressionToConfig(expression: FilterExpression): FilterGroup {
	return {
		logic: expression.logic === LogicEnum.Or ? 'OR' : 'AND',
		id: generateId(),
		conditions: expression.condition.map((x) => {
			return {
				id: generateId(),
				field: x.fieldName ?? '',
				operator: getOperator(x.logic),
				value: x.fieldValue ?? '',
				fieldType: getFieldType(x.fieldType),
			};
		}),
		groups: [],
	};
}

export function configToExpression(config: FilterGroup): FilterExpression {
	return {
		logic: config.logic === 'OR' ? LogicEnum.Or : LogicEnum.And,
		condition: config.conditions.map((x) => {
			return {
				logic: getLogicEnum(x.operator),
				fieldName: x.field,
				fieldType: getPropertyType(x.fieldType),
				fieldValue: x.value,
				condition: [],
			};
		}),
	};
}

export function generateId(): string {
	return Math.random().toString(36).substr(2, 9);
}

export function isValid(conditions: FilterGroup | undefined) {
	if (!conditions || !conditions.conditions) return false;
	if (conditions.conditions.length < 0) return false;

	for (let i = 0; i < conditions.conditions.length; i++) {
		const c = conditions.conditions[i];
		if (!c.field) return false;
		if (!c.operator) return false;
		if (c.operator != 'is_empty' && c.operator != 'is_not_empty') {
			if (!c.value) return false;
		}
	}

	return true;
}
