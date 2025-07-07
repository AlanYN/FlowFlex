export enum StageColorEnum {
	INDIGO = '#4F46E5',
	SKY = '#0EA5E9',
	EMERALD = '#10B981',
	AMBER = '#F59E0B',
	PINK = '#EC4899',
	VIOLET = '#8B5CF6',
	CYAN = '#06B6D4',
	TEAL = '#14B8A6',
	ROSE = '#F43F5E',
	GREEN = '#22C55E',
	BLUE = '#3B82F6',
	PURPLE = '#A855F7',
}

export const stageColorOptions = Object.values(StageColorEnum);

// 辅助类型定义，允许使用字符串作为颜色值
export type StageColorType = StageColorEnum | string;
