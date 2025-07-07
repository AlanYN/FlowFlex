/* eslint-disable no-debugger */
//自定义表单校验规则
import { isNil } from 'lodash-es';
import { checkEmail, checkTrueNum, checkNumber, checkRate, checkUrl } from '@/utils/check';

export const VALIDATOR_NUMBER = 'Please enter a valid number';
export const VALIDATOR_EMAIL = 'Please enter a valid email address';
/**
 * 校验正实数
 */
export const validateTrueNum = checkWrap(
	(value: number | string) => checkTrueNum(value),
	'Please enter a positive real number'
);

/**
 * 校验网址
 */
export const validateUrl = checkWrap(
	(value: string) => checkUrl(value),
	'Please enter a valid URL'
);

/**
 * 校验邮箱
 */
export const validateEmail = checkWrap((value: string) => checkEmail(value), VALIDATOR_EMAIL);

/**
 * 校验数字
 */
export const validateNumber = checkWrap(
	(value: number | string) => checkNumber(String(value)),
	'Please enter a valid number'
);

export const validatorNumber = (rule, value, callback) => {
	const reg = /^(([1-9]{1}\d*)|(0{1}))$/;
	if (isNil(value) || value === '') {
		return callback();
	}
	if (!reg.test(value)) {
		return callback(new Error(VALIDATOR_NUMBER));
	} else {
		return callback();
	}
};

export const decimalsNumber = (rule, value, callback) => {
	const reg = /^(0|[1-9]\d*)(\.\d+)?$/; // 修改正则表达式以允许非负小数
	if (isNil(value) || value === '') {
		return callback();
	}
	if (!reg.test(value)) {
		return callback(new Error(VALIDATOR_NUMBER)); // 如果值不符合正则表达式，则返回错误消息
	} else {
		return callback(); // 如果值符合要求，则返回通过验证
	}
};

/**
 * 校验率数
 */
export const validRate = checkWrap(
	(value: number | string) => checkRate(String(value)),
	'Please enter a valid rate'
);

/**
 * 校验文本长度
 */
export const validateTextLength = (length = 0) => {
	return checkWrap(
		(value: number | string) => value.toString().length <= length,
		`Field length cannot exceed ${length}`
	);
};

interface validateTimeType {
	getRef?: any;
	start?: any;
	end?: any;
	startLabel?: string;
	endLabel?: string;
	minStartTime?: Function;
	minStartText?: string;
	required?: boolean;
}

/**
 * 时间大小判断
 * @param {object} form 表单对象
 * @param {array} [startName,endName ] 起始字段名，结束字段名
 * @param {string} startLabelName 起始标签名
 * @param {string} endLabelName 结束标签名
 */
export const validateStartTime = ({
	getRef,
	start,
	end,
	startLabel = 'Satrt time',
	endLabel = 'End time',
	minStartTime = () => {},
	minStartText,
}: validateTimeType) => {
	return (_rule: any, value: any, callback: Function) => {
		const ref = getRef();
		const min = compareTimeGt(value, minStartTime());

		if (!min) {
			return callback(new Error(minStartText));
		}

		const result = compareTimeGt(ref.model[end], value) && compareTimeGt(value, minStartTime());
		if (result) {
			if (ref.model[end]) {
				setTimeout(() => ref.clearValidate(end), 100);
			}
			callback();
		} else {
			callback(new Error(`${startLabel} should not exceed ${endLabel}`));
		}
	};
};

export const validateEndTime = ({
	getRef,
	start,
	end,
	startLabel = 'Satrt time',
	endLabel = 'End time',
	minStartTime = () => {},
	minStartText,
}: validateTimeType) => {
	return (_rule: any, value: any, callback: Function) => {
		const ref = getRef();
		const result = compareTimeGt(value, ref.model[start]);
		if (result) {
			if (ref.model[start] && compareTimeGt(ref.model[start], minStartTime())) {
				setTimeout(() => ref.clearValidate(start), 100);
			}
			callback();
		} else {
			callback(new Error(`${endLabel} should not be less than ${startLabel}`));
		}
	};
};

export const validateTimeRange = ({
	getRef,
	start,
	end,
	startLabel = 'Satrt time',
	endLabel = 'End time',
	minStartTime,
	minStartText,
	required,
}: validateTimeType) => {
	return {
		[start]: [
			{
				required,
				message: `Please select ${startLabel}`,
			},
			{
				validator: validateStartTime({
					getRef,
					end,
					startLabel,
					endLabel,
					minStartTime,
					minStartText,
				}),
			},
		],
		[end]: [
			{
				required,
				message: `Please select ${endLabel}`,
			},
			{
				validator: validateEndTime({
					getRef,
					start,
					startLabel,
					endLabel,
					minStartTime,
					minStartText,
				}),
			},
		],
	};
};

/**
 * 时间大小校验方法 对比时间值1>对比时间值2 为校验通过
 * @param {date} date  对比时间值1
 * @param {date} value 对比时间值2
 * @param {string} errorMsg 检验不通过信息
 */
function compareTimeGt(date: any, value: any) {
	if (!date || !value) {
		// 空值返回true
		return true;
	}
	if (typeof date === 'string' && typeof value === 'string') {
		date = new Date(date);
		value = new Date(value);
	}
	if (Number(value) > Number(date)) {
		return false;
	} else {
		return true;
	}
}

export const customerNameRule = (rule, value, callback) => {
	// const regex = /^[A-Za-z0-9\s,._\\[\]/]*$/;
	const regex = /^\s+$/;
	if (!regex.test(value)) {
		callback();
	} else {
		callback(new Error('Please enter valid name'));
	}
};

/**
 *
 * @param {function} checkFunction 校验方法
 * @param {string} error 错误提示
 */
function checkWrap(checkFunction: Function, error: string) {
	return (_rule: any, value: any, callback: Function) => {
		if (value && !checkFunction(value)) {
			return callback(new Error(error));
		}
		callback();
	};
}
