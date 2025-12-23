/**
 * 穿梭框数据项接口
 */
export interface ITransferItem {
	/** 唯一标识 */
	key: string;
	/** 显示标签 */
	label: string;
	/** 描述信息（可选） */
	description?: string;
	/** 分组标签（可选） */
	group?: string;
	/** 是否禁用（可选） */
	disabled?: boolean;
}
