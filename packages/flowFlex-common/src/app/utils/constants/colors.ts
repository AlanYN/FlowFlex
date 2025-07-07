// 字段类型对应的主题色
export const FIELD_TYPE_COLORS = {
	// 文本类
	SINGLE_LINE_TEXT: '#6366f1', // Indigo-500
	MULTI_LINE_TEXT: '#8b5cf6', // Violet-500
	RICH_TEXT: '#a855f7', // Purple-500

	// 数字类
	NUMBER: '#0ea5e9', // Sky-500
	CURRENCY: '#06b6d4', // Cyan-500
	PERCENTAGE: '#14b8a6', // Teal-500

	// 日期类
	DATE_PICKER: '#22c55e', // Green-500
	DATE_RANGE: '#10b981', // Emerald-500
	TIME_PICKER: '#84cc16', // Lime-500

	// 选择类
	DROPDOWN_SELECT: '#f59e0b', // Amber-500
	MULTI_SELECT: '#f97316', // Orange-500
	CHECKBOX: '#ef4444', // Red-500

	// 关联类
	PEOPLE: '#ec4899', // Pink-500
	CONNECTION: '#d946ef', // Fuchsia-500
	FILE_LIST: '#f43f5e', // Rose-500,

	// 默认
	DEFAULT: '#64748b', // Slate-500

	// 手机号类型
	PHONE: '#0ea5e9', // Sky-500

	// 邮箱类型
	EMAIL: '#f59e0b', // Amber-500
} as const;

// 导出类型
export type FieldTypeColor = keyof typeof FIELD_TYPE_COLORS;
